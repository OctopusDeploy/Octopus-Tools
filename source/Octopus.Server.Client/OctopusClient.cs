using System;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Octopus.Client.Exceptions;
using Octopus.Client.Model;
using Octopus.Client.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using Octopus.Client.Logging;

namespace Octopus.Client
{
    /// <summary>
    /// The Octopus Deploy RESTful HTTP API client.
    /// </summary>
    public class OctopusClient : IHttpOctopusClient
    {
        private static readonly ILog Logger = LogProvider.For<OctopusClient>();

        readonly OctopusServerEndpoint serverEndpoint;
        readonly CookieContainer cookieContainer = new CookieContainer();
        readonly Uri cookieOriginUri;
        readonly JsonSerializerSettings defaultJsonSerializerSettings = JsonSerialization.GetDefaultSerializerSettings();
        private readonly ResourceSelfLinkExtractor resourceSelfLinkExtractor = new ResourceSelfLinkExtractor();
        readonly OctopusCustomHeaders octopusCustomHeaders;
        private string antiforgeryCookieName = null;
        private readonly SemaphoreSlim requestSemaphore;
        private readonly OidcAccessTokenCache oidcTokenCache;
        private readonly OctopusClientOptions options;
        private readonly RateLimitPacer rateLimitPacer;

        /// <summary>
        /// Initializes a new instance of the <see cref="OctopusClient" /> class.
        /// </summary>
        /// <param name="serverEndpoint">The server endpoint.</param>
        /// <param name="options">The configuration options for this client instance.</param>
        public OctopusClient(OctopusServerEndpoint serverEndpoint, OctopusClientOptions options = default) : this(serverEndpoint, null, options)
        {
        }

        internal OctopusClient(OctopusServerEndpoint serverEndpoint, string requestingTool, OctopusClientOptions options = default)
        {
            this.serverEndpoint = serverEndpoint;
            options ??= new OctopusClientOptions();
            this.options = options;
            rateLimitPacer = options.UseRateLimitHeaders ? new RateLimitPacer() : null;

            cookieOriginUri = BuildCookieUri(serverEndpoint);
            octopusCustomHeaders = new OctopusCustomHeaders(requestingTool);
            requestSemaphore = new SemaphoreSlim(options.MaxSimultaneousRequests, options.MaxSimultaneousRequests);

            if (serverEndpoint.OidcCredentials != null)
            {
                var openIdConfigUri = serverEndpoint.OctopusServer.Resolve(OidcAccessTokenCache.WellKnownOpenIdConfigurationUrl);
                var oidcHandler = new HttpClientHandler
                {
                    CookieContainer = cookieContainer,
                    Credentials = serverEndpoint.Credentials ?? CredentialCache.DefaultNetworkCredentials,
                    UseProxy = options.AllowDefaultProxy
                };
                if (serverEndpoint.Proxy != null)
                {
                    oidcHandler.UseProxy = true;
                    oidcHandler.Proxy = serverEndpoint.Proxy;
                }

                var oidcHttpClient = OctopusClientFactory.BuildHttpClient(oidcHandler, options, octopusCustomHeaders, rateLimitPacer: rateLimitPacer);
                oidcTokenCache = new OidcAccessTokenCache(serverEndpoint.OidcCredentials, openIdConfigUri, oidcHttpClient);
            }

            Repository = new OctopusRepository(this);
        }

        public RootResource RootDocument => Repository.LoadRootDocument();
        public IOctopusRepository Repository { get; private set; }

        /// <summary>
        /// Indicates whether a secure (SSL) connection is being used to communicate with the server.
        /// </summary>
        public bool IsUsingSecureConnection => serverEndpoint.IsUsingSecureConnection;

        public IOctopusSpaceRepository ForSpace(SpaceResource space)
        {
            ValidateSpaceId(space);
            return new OctopusRepository(this, RepositoryScope.ForSpace(space));
        }

        public IOctopusSystemRepository ForSystem()
        {
            return new OctopusRepository(this, RepositoryScope.ForSystem());
        }

        public void SignIn(LoginCommand loginCommand)
        {
            if (loginCommand.State == null)
            {
                loginCommand.State = new LoginState { UsingSecureConnection = IsUsingSecureConnection };
            }
            Post(Repository.LoadRootDocument().Links["SignIn"], loginCommand);

            antiforgeryCookieName = cookieContainer.GetCookies(cookieOriginUri)
                .Cast<Cookie>()
                .Single(c => c.Name.StartsWith(ApiConstants.AntiforgeryTokenCookiePrefix)).Name;

            Repository = new OctopusRepository(this);
        }

        public void SignOut()
        {
            Post(Repository.LoadRootDocument().Links["SignOut"]);
            antiforgeryCookieName = null;
        }

        /// <summary>
        /// Occurs when a request is about to be sent.
        /// </summary>
        public event Action<WebRequest> BeforeSendingHttpRequest;

        /// <summary>
        /// Occurs when a response has been received.
        /// </summary>
        public event Action<WebResponse> AfterReceivingHttpResponse;

        /// <summary>
        /// Occurs when a request is about to be sent.
        /// </summary>
        public event Action<OctopusRequest> SendingOctopusRequest;

        /// <summary>
        /// Occurs when a response is received from the Octopus Server.
        /// </summary>
        public event Action<OctopusResponse> ReceivedOctopusResponse;

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public void Initialize()
        {
            Repository.Link("Self");
        }

        private Uri BuildCookieUri(OctopusServerEndpoint octopusServerEndpoint)
        {
            // The CookieContainer is a bit funny - it sets the cookie without the port, but doesn't ignore the port when retreiving cookies
            // From what I can see it uses the Uri.Authority value - which contains the port number
            // We need to clear the port in order to successfully get cookies for the same origin
            var uriBuilder = new UriBuilder(octopusServerEndpoint.OctopusServer.Resolve("/")) { Port = 0 };

            return uriBuilder.Uri;
        }

        /// <summary>
        /// Fetches a single resource from the server using the HTTP GET verb.
        /// </summary>
        /// <typeparam name="TResource"></typeparam>
        /// <param name="path">The path from which to fetch the resource.</param>
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        /// <returns>
        /// The resource from the server.
        /// </returns>
        public TResource Get<TResource>(string path, object pathParameters = null)
        {
            var uri = QualifyUri(path, pathParameters);

            return DispatchRequest<TResource>(new OctopusRequest("GET", uri), true).ResponseResource;
        }

        /// <summary>
        /// Fetches a collection of resources from the server using the HTTP GET verb. The collection itself will usually be
        /// limited in size (pagination) and links to the next page of data is available in the <see cref="Resource.Links" />
        /// property.
        /// </summary>
        /// <typeparam name="TResource"></typeparam>
        /// <param name="path">The path from which to fetch the resources.</param>
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        /// <returns>
        /// The collection of resources from the server.
        /// </returns>
        public ResourceCollection<TResource> List<TResource>(string path, object pathParameters = null)
        {
            return Get<ResourceCollection<TResource>>(path, pathParameters);
        }

        /// <summary>
        /// Fetches a collection of resources from the server using the HTTP GET verb. All pages will be retrieved.
        /// property.
        /// </summary>
        /// <typeparam name="TResource"></typeparam>
        /// <param name="path">The path from which to fetch the resources.</param>
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        /// <returns>
        /// The collection of resources from the server.
        /// </returns>
        public IReadOnlyList<TResource> ListAll<TResource>(string path, object pathParameters = null)
        {
            var resources = new List<TResource>();
            Paginate<TResource>(path, pathParameters, r =>
            {
                resources.AddRange(r.Items);
                return true;
            });
            return resources;
        }

        /// <summary>
        /// Fetches a collection of resources from the server one page at a time using the HTTP GET verb.
        /// </summary>
        /// <typeparam name="TResource"></typeparam>
        /// <param name="path">The path from which to fetch the resources.</param>
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        /// <param name="getNextPage">
        /// A callback invoked for each page of data found. If the callback returns <c>true</c>, the next
        /// page will also be requested.
        /// </param>
        public void Paginate<TResource>(string path, object pathParameters, Func<ResourceCollection<TResource>, bool> getNextPage)
        {
            var page = List<TResource>(path, pathParameters);

            while (getNextPage(page) && page.Items.Count > 0 && page.HasLink("Page.Next"))
            {
                page = List<TResource>(page.Link("Page.Next"));
            }
        }

        /// <summary>
        /// Fetches a collection of resources from the server one page at a time using the HTTP GET verb.
        /// </summary>
        /// <typeparam name="TResource"></typeparam>
        /// <param name="path">The path from which to fetch the resources.</param>
        /// <param name="getNextPage">
        /// A callback invoked for each page of data found. If the callback returns <c>true</c>, the next
        /// page will also be requested.
        /// </param>
        public void Paginate<TResource>(string path, Func<ResourceCollection<TResource>, bool> getNextPage)
        {
            Paginate(path, null, getNextPage);
        }

        /// <summary>
        /// Creates a resource at the given URI on the server using the POST verb, then performs a fresh GET request to fetch
        /// the created item.
        /// </summary>
        /// <typeparam name="TResource"></typeparam>
        /// <param name="path">The path to the container resource.</param>
        /// <param name="resource">The resource to create.</param>
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        /// <returns>
        /// The latest copy of the resource from the server.
        /// </returns>
        public TResource Create<TResource>(string path, TResource resource, object pathParameters = null)
        {
            var uri = QualifyUri(path, pathParameters);

            var response = DispatchRequest<TResource>(new OctopusRequest("POST", uri, requestResource: resource), true);

            var getUrl = resourceSelfLinkExtractor.GetSelfUrlOrNull(response.ResponseResource) ?? path;
            return Get<TResource>(getUrl);
        }

        /// <inheritdoc/>
        public TResponse Create<TCommand, TResponse>(string path, TCommand command, object pathParameters = null)
        {
            var uri = QualifyUri(path, pathParameters);
            var response = DispatchRequest<TResponse>(new OctopusRequest("POST", uri, requestResource: command), true);
            return response.ResponseResource;
        }

        /// <summary>
        /// Sends a command to a resource at the given URI on the server using the POST verb.
        /// </summary>
        /// <typeparam name="TResource"></typeparam>
        /// <param name="path">The path to the container resource.</param>
        /// <param name="resource">The resource to create.</param>
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        public void Post<TResource>(string path, TResource resource, object pathParameters = null)
        {
            var uri = QualifyUri(path, pathParameters);

            DispatchRequest<TResource>(new OctopusRequest("POST", uri, requestResource: resource), false);
        }

        /// <summary>
        /// Sends a command to a resource at the given URI on the server using the POST verb.
        /// </summary>
        /// <typeparam name="TResource"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="path">The path to the container resource.</param>
        /// <param name="resource">The resource to post.</param>
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        public TResponse Post<TResource, TResponse>(string path, TResource resource, object pathParameters = null)
        {
            var uri = QualifyUri(path, pathParameters);

            return DispatchRequest<TResponse>(new OctopusRequest("POST", uri, requestResource: resource), true).ResponseResource;
        }

        /// <summary>
        /// Sends a command to a resource at the given URI on the server using the POST verb.
        /// </summary>
        /// <param name="path">The path to the container resource.</param>
        public void Post(string path)
        {
            var uri = QualifyUri(path);

            DispatchRequest<string>(new OctopusRequest("POST", uri), false);
        }

        /// <summary>
        /// Sends a command to a resource at the given URI on the server using the PUT verb.
        /// </summary>
        /// <exception cref="OctopusSecurityException">
        /// HTTP 401 or 403: Thrown when the current user's API key was not valid, their
        /// account is disabled, or they don't have permission to perform the specified action.
        /// </exception>
        /// <exception cref="OctopusServerException">
        /// If any other error is successfully returned from the server (e.g., a 500
        /// server error).
        /// </exception>
        /// <exception cref="OctopusValidationException">HTTP 400: If there was a problem with the request provided by the user.</exception>
        /// <exception cref="OctopusResourceNotFoundException">HTTP 404: If the specified resource does not exist on the server.</exception>
        /// <param name="path">The path to the container resource.</param>
        /// <param name="resource">The resource to create.</param>
        public void Put<TResource>(string path, TResource resource)
        {
            var uri = QualifyUri(path);

            DispatchRequest<TResource>(new OctopusRequest("PUT", uri, requestResource: resource), false);
        }

        /// <summary>
        /// Sends a command to a resource at the given URI on the server using the PUT verb.
        /// </summary>
        /// <exception cref="OctopusSecurityException">
        /// HTTP 401 or 403: Thrown when the current user's API key was not valid, their
        /// account is disabled, or they don't have permission to perform the specified action.
        /// </exception>
        /// <exception cref="OctopusServerException">
        /// If any other error is successfully returned from the server (e.g., a 500
        /// server error).
        /// </exception>
        /// <exception cref="OctopusValidationException">HTTP 400: If there was a problem with the request provided by the user.</exception>
        /// <exception cref="OctopusResourceNotFoundException">HTTP 404: If the specified resource does not exist on the server.</exception>
        /// <param name="path">The path to the container resource.</param>
        public void Put(string path)
        {
            var uri = QualifyUri(path);

            DispatchRequest<string>(new OctopusRequest("PUT", uri), false);
        }

        /// <summary>
        /// Sends a command to a resource at the given URI on the server using the PUT verb.
        /// </summary>
        /// <typeparam name="TResource"></typeparam>
        /// <param name="path">The path to the container resource.</param>
        /// <param name="resource">The resource to create.</param>
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        public void Put<TResource>(string path, TResource resource, object pathParameters = null)
        {
            var uri = QualifyUri(path, pathParameters);

            DispatchRequest<TResource>(new OctopusRequest("PUT", uri, requestResource: resource), false);
        }

        /// <summary>
        /// Deletes the resource at the given URI from the server using a the DELETE verb.
        /// </summary>
        /// <param name="path">The path to the resource to delete.</param>
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        /// <param name="resource">The resource to pass as the request body, if supplied.</param>
        public void Delete(string path, object pathParameters = null, object resource = null)
        {
            var uri = QualifyUri(path, pathParameters);

            DispatchRequest<string>(new OctopusRequest("DELETE", uri, resource), true);
        }

        /// <inheritdoc/>
        public TResponse Delete<TCommand, TResponse>(string path, TCommand command, object pathParameters = null)
        {
            var uri = QualifyUri(path, pathParameters);

            var response = DispatchRequest<TResponse>(new OctopusRequest("DELETE", uri, command), true);
            return response.ResponseResource;
        }

        /// <summary>
        /// Updates the resource at the given URI on the server using the PUT verb, then performs a fresh GET request to reload
        /// the data.
        /// </summary>
        /// <typeparam name="TResource"></typeparam>
        /// <param name="path">The path to the resource to update.</param>
        /// <param name="resource">The resource to update.</param>
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        /// <returns>
        /// The latest copy of the resource from the server.
        /// </returns>
        public TResource Update<TResource>(string path, TResource resource, object pathParameters = null)
        {
            var uri = QualifyUri(path, pathParameters);

            var response = DispatchRequest<TResource>(new OctopusRequest("PUT", uri, requestResource: resource), readResponse: true);

            var getUrl = resourceSelfLinkExtractor.GetSelfUrlOrNull(response.ResponseResource) ?? path;
            return Get<TResource>(getUrl);
        }

        /// <inheritdoc/>
        public TResponse Update<TCommand, TResponse>(string path, TCommand command, object pathParameters = null)
        {
            var uri = QualifyUri(path, pathParameters);

            var response = DispatchRequest<TResponse>(new OctopusRequest("PUT", uri, requestResource: command), readResponse: true);
            return response.ResponseResource;
        }

        /// <summary>
        /// Fetches raw content from the resource at the specified path, using the GET verb.
        /// </summary>
        /// <exception cref="OctopusSecurityException">
        /// HTTP 401 or 403: Thrown when the current user's API key was not valid, their
        /// account is disabled, or they don't have permission to perform the specified action.
        /// </exception>
        /// <exception cref="OctopusServerException">
        /// If any other error is successfully returned from the server (e.g., a 500
        /// server error).
        /// </exception>
        /// <exception cref="OctopusValidationException">HTTP 400: If there was a problem with the request provided by the user.</exception>
        /// <exception cref="OctopusResourceNotFoundException">HTTP 404: If the specified resource does not exist on the server.</exception>
        /// <param name="path">The path to the resource to fetch.</param>
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        /// <returns>A stream containing the content of the resource.</returns>
        public Stream GetContent(string path, object pathParameters = null)
        {
            var uri = QualifyUri(path, pathParameters);
            return DispatchRequest<Stream>(new OctopusRequest("GET", uri), true).ResponseResource;
        }

        /// <summary>
        /// Creates or updates the raw content of the resource at the specified path, using the PUT verb.
        /// </summary>
        /// <param name="path">The path to the resource to create or update.</param>
        /// <param name="contentStream">A stream containing content of the resource.</param>
        public void PutContent(string path, Stream contentStream)
        {
            if (contentStream == null) throw new ArgumentNullException("contentStream");
            var uri = QualifyUri(path);
            DispatchRequest<Stream>(new OctopusRequest("PUT", uri, requestResource: contentStream), false);
        }

        public Uri QualifyUri(string path, object parameters = null)
        {
            var dictionary = parameters as IDictionary<string, object>;

            path = (dictionary == null) ? UrlTemplate.Resolve(path, parameters) : UrlTemplate.Resolve(path, dictionary);

            return serverEndpoint.OctopusServer.Resolve(path);
        }

        protected virtual OctopusResponse<TResponseResource> DispatchRequest<TResponseResource>(OctopusRequest request, bool readResponse)
        {
            // Unlike the async client, this one talks to the server via HttpWebRequest rather than HttpClient, so it
            // can't use RateLimitRetryHandler. We retry HTTP 429s here instead, so both clients behave the same way.
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    WaitForRateLimitPacing();
                    return SendRequest<TResponseResource>(request, readResponse);
                }
                catch (WebException wex) when (wex.Response is HttpWebResponse errorResponse)
                {
                    ObserveRateLimitHeaders(errorResponse);

                    if (attempt < options.RateLimitRetryCount
                        && (int)errorResponse.StatusCode == 429 // HttpStatusCode.TooManyRequests isn't in the netstandard2.0 profile
                        && CanRetryAfterRateLimit(request)
                        && TryGetRateLimitRetryDelay(errorResponse, out var delay))
                    {
                        errorResponse.Close();
                        if (delay > TimeSpan.Zero) Thread.Sleep(delay);
                        continue;
                    }

                    throw OctopusExceptionFactory.CreateException(wex, errorResponse);
                }
            }
        }

        /// <summary>
        /// A request whose body is a stream can't be retried, because we can't rewind the caller's stream.
        /// </summary>
        static bool CanRetryAfterRateLimit(OctopusRequest request)
            => !(request.RequestResource is Stream) && !(request.RequestResource is FileUpload);

        /// <summary>
        /// Waits out whatever the pacer thinks we owe the server, so we stay inside the advertised rate limit
        /// rather than being rejected by it. Does nothing unless <see cref="OctopusClientOptions.UseRateLimitHeaders" />
        /// is on.
        /// </summary>
        void WaitForRateLimitPacing()
        {
            if (rateLimitPacer == null) return;

            var delay = rateLimitPacer.ReserveSlot();
            if (delay > TimeSpan.Zero) Thread.Sleep(delay);
        }

        void ObserveRateLimitHeaders(HttpWebResponse response)
        {
            rateLimitPacer?.ObserveResponse(
                response.Headers.Get(RateLimitPacer.PolicyHeaderName),
                response.Headers.Get(RateLimitPacer.RateLimitHeaderName));
        }

        bool TryGetRateLimitRetryDelay(HttpWebResponse response, out TimeSpan delay)
        {
            var delta = RateLimitRetry.ParseRetryAfterHeader(response.Headers.Get("Retry-After"));
            return RateLimitRetry.TryGetDelay(delta, options, out delay);
        }

        OctopusResponse<TResponseResource> SendRequest<TResponseResource>(OctopusRequest request, bool readResponse)
        {
            var webRequest = (HttpWebRequest)WebRequest.Create(request.Uri);
            if (serverEndpoint.Proxy != null)
            {
                webRequest.Proxy = serverEndpoint.Proxy;
            }
            webRequest.CookieContainer = cookieContainer;
            webRequest.Accept = "application/json";
            webRequest.ContentType = "application/json";
            webRequest.ReadWriteTimeout = ApiConstants.DefaultClientRequestTimeout;
            webRequest.Timeout = ApiConstants.DefaultClientRequestTimeout;
            webRequest.Credentials = serverEndpoint.Credentials ?? CredentialCache.DefaultNetworkCredentials;
            webRequest.Method = request.Method;
            if (serverEndpoint.ApiKey != null)
            {
                webRequest.Headers[ApiConstants.ApiKeyHttpHeaderName] = serverEndpoint.ApiKey;
            }
            if (serverEndpoint.BearerToken != null)
            {
                webRequest.Headers["Authorization"] = $"Bearer {serverEndpoint.BearerToken}";
            }
            if (oidcTokenCache != null)
            {
                webRequest.Headers["Authorization"] = $"Bearer {oidcTokenCache.GetAccessToken()}";
            }
            webRequest.UserAgent = octopusCustomHeaders.UserAgent;

            if (webRequest.Method == "PUT")
            {
                webRequest.Headers["X-HTTP-Method-Override"] = "PUT";
                webRequest.Method = "POST";
            }

            if (webRequest.Method == "DELETE")
            {
                webRequest.Headers["X-HTTP-Method-Override"] = "DELETE";
                webRequest.Method = "POST";
            }

            if (!string.IsNullOrEmpty(antiforgeryCookieName))
            {
                var antiforgeryCookie = cookieContainer.GetCookies(cookieOriginUri)
                    .Cast<Cookie>()
                    .SingleOrDefault(c => string.Equals(c.Name, antiforgeryCookieName));
                if (antiforgeryCookie != null)
                {
                    webRequest.Headers[ApiConstants.AntiforgeryTokenHttpHeaderName] = antiforgeryCookie.Value;
                }
            }

            SendingOctopusRequest?.Invoke(request);

            BeforeSendingHttpRequest?.Invoke(webRequest);

            HttpWebResponse webResponse = null;

            requestSemaphore.Wait();
            try
            {
                if (request.RequestResource == null)
                {
                    webRequest.ContentLength = 0;
                }
                else
                {
                    var requestStreamContent = request.RequestResource as Stream;
                    if (requestStreamContent != null)
                    {
                        webRequest.Accept = null;
                        webRequest.ContentType = "application/octet-stream";
                        webRequest.ContentLength = requestStreamContent.Length;
                        requestStreamContent.CopyTo(webRequest.GetRequestStream());
                        // Caller owns stream.
                    }
                    else
                    {
                        var fileUploadContent = request.RequestResource as FileUpload;
                        if (fileUploadContent != null)
                        {
                            webRequest.AllowWriteStreamBuffering = false;
                            webRequest.SendChunked = true;

                            var boundary = "----------------------------" + DateTime.Now.Ticks.ToString("x");
                            var boundarybytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
                            webRequest.ContentType = "multipart/form-data; boundary=" + boundary;

                            var requestStream = webRequest.GetRequestStream();
                            requestStream.Write(boundarybytes, 0, boundarybytes.Length);

                            var headerTemplate = "Content-Disposition: form-data; name=file; filename={0}\r\nContent-Type: application/octet-stream\r\n\r\n";
                            var header = string.Format(headerTemplate, fileUploadContent.FileName);
                            var headerbytes = Encoding.UTF8.GetBytes(header);
                            requestStream.Write(headerbytes, 0, headerbytes.Length);
                            fileUploadContent.Contents.CopyTo(requestStream);

                            var boundarybytesTrailer = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
                            requestStream.Write(boundarybytesTrailer, 0, boundarybytesTrailer.Length);
                            requestStream.Flush();
                            requestStream.Close();
                        }
                        else
                        {
                            var text = JsonConvert.SerializeObject(request.RequestResource, defaultJsonSerializerSettings);
                            webRequest.ContentLength = Encoding.UTF8.GetByteCount(text);
                            var requestStream = new StreamWriter(webRequest.GetRequestStream());
                            requestStream.Write(text);
                            requestStream.Flush();
                        }
                    }
                }

                Logger.Trace($"DispatchRequest: {webRequest.Method} {webRequest.RequestUri}");

                webResponse = (HttpWebResponse)webRequest.GetResponse();
                AfterReceivingHttpResponse?.Invoke(webResponse);
                ObserveRateLimitHeaders(webResponse);

                var resource = default(TResponseResource);
                if (readResponse)
                {
                    var responseStream = webResponse.GetResponseStream();
                    if (responseStream != null)
                    {
                        if (typeof(TResponseResource) == typeof(Stream))
                        {
                            var stream = new MemoryStream();
                            responseStream.CopyTo(stream);
                            stream.Seek(0, SeekOrigin.Begin);
                            resource = (TResponseResource)(object)stream;
                        }
                        else if (typeof(TResponseResource) == typeof(byte[]))
                        {
                            var stream = new MemoryStream();
                            responseStream.CopyTo(stream);
                            resource = (TResponseResource)(object)stream.ToArray();
                        }
                        else if (typeof(TResponseResource) == typeof(string))
                        {
                            using (var reader = new StreamReader(responseStream))
                            {
                                resource = (TResponseResource)(object)reader.ReadToEnd();
                            }
                        }
                        else
                        {
                            using (var reader = new StreamReader(responseStream))
                            {
                                var content = reader.ReadToEnd();
                                try
                                {
                                    resource = JsonConvert.DeserializeObject<TResponseResource>(content, defaultJsonSerializerSettings);
                                }
                                catch (Exception ex)
                                {
                                    throw new OctopusDeserializationException((int)webResponse.StatusCode, "Unable to process response from server: " + ex.Message + ". Response content: " + (content.Length > 100 ? content.Substring(0, 100) : content), ex);
                                }
                            }
                        }
                    }
                }

                var locationHeader = webResponse.Headers.Get("Location");
                var octopusResponse = new OctopusResponse<TResponseResource>(request, webResponse.StatusCode, locationHeader, resource);
                ReceivedOctopusResponse?.Invoke(octopusResponse);

                return octopusResponse;
            }
            finally
            {
                requestSemaphore.Release();
                if (webResponse != null)
                {
                    try
                    {
                        webResponse.Close();
                    }
                    // ReSharper disable once EmptyGeneralCatchClause
                    catch
                    {
                    }
                }
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            requestSemaphore?.Dispose();
            oidcTokenCache?.Dispose();
        }

        private void ValidateSpaceId(SpaceResource space)
        {
            if (space == null)
            {
                throw new ArgumentNullException(nameof(space));
            }
        }
    }
}
