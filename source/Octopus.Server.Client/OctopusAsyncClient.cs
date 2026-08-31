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
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Logging;
using Octopus.Client.Util;

namespace Octopus.Client
{
    /// <summary>
    /// The Octopus Deploy RESTful HTTP API client.
    /// </summary>
    public class OctopusAsyncClient : IOctopusAsyncClient
    {
        private static readonly ILog Logger = LogProvider.For<OctopusAsyncClient>();

        private readonly OctopusServerEndpoint serverEndpoint;
        private readonly JsonSerializerSettings defaultJsonSerializerSettings = JsonSerialization.GetDefaultSerializerSettings();
        private readonly ResourceSelfLinkExtractor resourceSelfLinkExtractor = new ResourceSelfLinkExtractor();
        private readonly HttpClient client;
        private readonly CookieContainer cookieContainer = new CookieContainer();
        private readonly Uri cookieOriginUri;
        // ReSharper disable once RedundantDefaultMemberInitializer. Mandatory assignment to prevent compiler error CS0649.
        private readonly bool ignoreSslErrors = false;
        private bool ignoreSslErrorMessageLogged;
        private string antiforgeryCookieName;
        private readonly SemaphoreSlim requestSemaphore;
        private readonly OidcAccessTokenCache oidcCache;

        // Use the Create method to instantiate
        protected OctopusAsyncClient(OctopusServerEndpoint serverEndpoint, OctopusClientOptions options, bool addCertificateCallback, string requestingTool)
        {
            var clientOptions = options ?? new OctopusClientOptions();
            this.serverEndpoint = serverEndpoint;
            cookieOriginUri = BuildCookieUri(serverEndpoint);
            requestSemaphore = new SemaphoreSlim(clientOptions.MaxSimultaneousRequests, clientOptions.MaxSimultaneousRequests);
            var rateLimitPacer = clientOptions.UseRateLimitHeaders ? new RateLimitPacer() : null;

            var handler = new HttpClientHandler
            {
                CookieContainer = cookieContainer,
                Credentials = serverEndpoint.Credentials ?? CredentialCache.DefaultNetworkCredentials,
                UseProxy = clientOptions.AllowDefaultProxy
            };

            if (clientOptions.Proxy != null)
            {
                handler.UseProxy = true;
                handler.Proxy = new ClientProxy(clientOptions.Proxy, clientOptions.ProxyUsername, clientOptions.ProxyPassword);
            }

#if HTTP_CLIENT_SUPPORTS_SSL_OPTIONS
            handler.SslProtocols = clientOptions.SslProtocols;
            if (addCertificateCallback)
            {
                ignoreSslErrors = clientOptions.IgnoreSslErrors;
                handler.ServerCertificateCustomValidationCallback = IgnoreServerCertificateCallback;
            }
#endif

            if (serverEndpoint.Proxy != null)
            {
                handler.UseProxy = true;
                handler.Proxy = serverEndpoint.Proxy;
            }

            HttpMessageHandler handlerChain = handler;
            if (serverEndpoint.OidcCredentials != null)
            {
                var openIdConfigUri = serverEndpoint.OctopusServer.Resolve(OidcAccessTokenCache.WellKnownOpenIdConfigurationUrl);
                // disposeHandler: false — the main HttpClient owns and disposes the handler
                var oidcHttpClient = OctopusClientFactory.BuildHttpClient(handler, clientOptions, requestingTool, disposeHandler: false, rateLimitPacer: rateLimitPacer);
                oidcCache = new OidcAccessTokenCache(serverEndpoint.OidcCredentials, openIdConfigUri, oidcHttpClient);
                handlerChain = new OidcAuthenticationHandler(oidcCache, handler);
            }

            client = OctopusClientFactory.BuildHttpClient(handlerChain, clientOptions, requestingTool, rateLimitPacer: rateLimitPacer);
            if (serverEndpoint.OidcCredentials == null)
            {
                if (serverEndpoint.ApiKey != null)
                {
                    client.DefaultRequestHeaders.Add(ApiConstants.ApiKeyHttpHeaderName, serverEndpoint.ApiKey);
                }
                if (serverEndpoint.BearerToken != null)
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", serverEndpoint.BearerToken);
                }
            }
            Repository = new OctopusAsyncRepository(this);
        }


        private Uri BuildCookieUri(OctopusServerEndpoint octopusServerEndpoint)
        {
            // The CookieContainer is a bit funny - it sets the cookie without the port, but doesn't ignore the port when retrieving cookies
            // From what I can see it uses the Uri.Authority value - which contains the port number
            // We need to clear the port in order to successfully get cookies for the same origin
            var uriBuilder = new UriBuilder(octopusServerEndpoint.OctopusServer.Resolve("/")) { Port = 0 };
            return uriBuilder.Uri;
        }

        private bool IgnoreServerCertificateCallback(HttpRequestMessage message, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors errors)
        {
            if (errors == SslPolicyErrors.None)
            {
                return true;
            }

            var warning = $@"The following certificate errors were encountered when establishing the HTTPS connection to the server: {errors}
Certificate subject name: {certificate.SubjectName.Name}
Certificate thumbprint:   {certificate.Thumbprint}";

            if (ignoreSslErrors)
            {
                if (!ignoreSslErrorMessageLogged)
                {
                    Logger.Warn(warning);
                    Logger.Warn("Because IgnoreSslErrors was set, this will be ignored.");
                    ignoreSslErrorMessageLogged = true;
                }
                return true;
            }

            Logger.Error(warning);
            return false;
        }

        public IOctopusSpaceAsyncRepository ForSpace(SpaceResource space)
        {
            ValidateSpaceId(space);
            return new OctopusAsyncRepository(this, RepositoryScope.ForSpace(space));
        }

        public IOctopusSystemAsyncRepository ForSystem()
        {
            return new OctopusAsyncRepository(this, RepositoryScope.ForSystem());
        }

        public static async Task<IOctopusAsyncClient> Create(OctopusServerEndpoint serverEndpoint, OctopusClientOptions options = null)
        {
#if HTTP_CLIENT_SUPPORTS_SSL_OPTIONS
            try
            {
                return await Create(serverEndpoint, options, true).ConfigureAwait(false);
            }
            catch (PlatformNotSupportedException ex)
            {
                if (options?.IgnoreSslErrors ?? false)
                    throw new Exception("This platform does not support ignoring SSL certificate errors", ex);
                return await Create(serverEndpoint, options, false).ConfigureAwait(false);
            }
#else
            return await Create(serverEndpoint, options, false).ConfigureAwait(false);
#endif
        }

        internal static async Task<IOctopusAsyncClient> Create(OctopusServerEndpoint serverEndpoint, OctopusClientOptions options, string requestingTool)
        {
#if HTTP_CLIENT_SUPPORTS_SSL_OPTIONS
            try
            {
                return await Create(serverEndpoint, options, true, requestingTool);
            }
            catch (PlatformNotSupportedException ex)
            {
                if (options?.IgnoreSslErrors ?? false)
                    throw new Exception("This platform does not support ignoring SSL certificate errors", ex);
                return await Create(serverEndpoint, options, false, requestingTool);
            }
#else
            return await Create(serverEndpoint, options, false, requestingTool);
#endif
        }

        private static async Task<IOctopusAsyncClient> Create(OctopusServerEndpoint serverEndpoint, OctopusClientOptions options, bool addHandler, string requestingTool = null)
        {
            options ??= new OctopusClientOptions();
            var client = new OctopusAsyncClient(serverEndpoint, options, addHandler, requestingTool);
            // User used to see this exception 
            // System.PlatformNotSupportedException: The handler does not support custom handling of certificates with this combination of libcurl (7.29.0) and its SSL backend
            await client.Repository.LoadRootDocument().ConfigureAwait(false);
            return client;
        }

        /// <summary>
        /// Indicates whether a secure (SSL) connection is being used to communicate with the server.
        /// </summary>
        public bool IsUsingSecureConnection => serverEndpoint.IsUsingSecureConnection;

        /// <inheritdoc />
        public async Task SignIn(LoginCommand loginCommand)
        {
            await SignIn(loginCommand, CancellationToken.None);
        }

        /// <inheritdoc />
        public async Task SignIn(LoginCommand loginCommand, CancellationToken cancellationToken)
        {
            if (loginCommand.State == null)
            {
                loginCommand.State = new LoginState { UsingSecureConnection = IsUsingSecureConnection };
            }
            await Post(await Repository.Link("SignIn").ConfigureAwait(false), loginCommand, null, cancellationToken).ConfigureAwait(false);

            // Capture the cookie name here so that the Dispatch method does not rely on the rootDocument to get the InstallationId
            antiforgeryCookieName = cookieContainer.GetCookies(cookieOriginUri)
                .Cast<Cookie>()
                .Single(c => c.Name.StartsWith(ApiConstants.AntiforgeryTokenCookiePrefix)).Name;

            Repository = new OctopusAsyncRepository(this);
        }

        /// <inheritdoc />
        public async Task SignOut()
        {
            await SignOut(CancellationToken.None);
        }

        /// <inheritdoc />
        public async Task SignOut(CancellationToken cancellationToken)
        {
            await Post(await Repository.Link("SignOut").ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
            antiforgeryCookieName = null;
        }

        /// <summary>
        /// Occurs when a request is about to be sent.
        /// </summary>
        public event Action<HttpRequestMessage> BeforeSendingHttpRequest;

        /// <summary>
        /// Occurs when a response has been received.
        /// </summary>
        public event Action<HttpResponseMessage> AfterReceivedHttpResponse;

        /// <summary>
        /// Gets a document that identifies the Octopus Server (from /api) and provides links to the resources available on the
        /// server. Instead of hardcoding paths,
        /// clients should use these link properties to traverse the resources on the server. This document is lazily loaded so
        /// that it is only requested once for
        /// the current <see cref="IOctopusAsyncClient" />.
        /// </summary>
        public RootResource RootDocument => Repository.LoadRootDocument().GetAwaiter().GetResult();

        /// <summary>
        /// Occurs when a request is about to be sent.
        /// </summary>
        public event Action<OctopusRequest> SendingOctopusRequest;

        /// <summary>
        /// Occurs when a response is received from the Octopus Server.
        /// </summary>
        public event Action<OctopusResponse> ReceivedOctopusResponse;

        /// <inheritdoc/>
        public async Task<TResource> Get<TResource>(string path, object pathParameters = null)
        {
            return await Get<TResource>(path, pathParameters, CancellationToken.None);
        }

        /// <inheritdoc/>
        public async Task<TResource> Get<TResource>(string path, CancellationToken cancellationToken)
        {
            return await Get<TResource>(path, null, cancellationToken);
        }

        /// <inheritdoc/>      
        public async Task<TResource> Get<TResource>(string path, object pathParameters, CancellationToken cancellationToken)
        {
            var uri = QualifyUri(path, pathParameters);
            var response = await DispatchRequest<TResource>(new OctopusRequest("GET", uri), true, cancellationToken).ConfigureAwait(false);

            return response.ResponseResource;
        }

        public IOctopusAsyncRepository Repository { get; protected set; }

        /// <inheritdoc/>
        public async Task<ResourceCollection<TResource>> List<TResource>(string path, object pathParameters = null)
        {
            return await List<TResource>(path, pathParameters, CancellationToken.None);
        }

        /// <inheritdoc/>
        public async Task<ResourceCollection<TResource>> List<TResource>(string path, CancellationToken cancellationToken)
        {
            return await List<TResource>(path, null, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<ResourceCollection<TResource>> List<TResource>(string path, object pathParameters, CancellationToken cancellationToken)
        {
            return await Get<ResourceCollection<TResource>>(path, pathParameters, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<TResource>> ListAll<TResource>(string path, object pathParameters = null)
        {
            return await ListAll<TResource>(path, pathParameters, CancellationToken.None);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<TResource>> ListAll<TResource>(string path, CancellationToken cancellationToken)
        {
            return await ListAll<TResource>(path, null, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<TResource>> ListAll<TResource>(string path, object pathParameters, CancellationToken cancellationToken)
        {
            var resources = new List<TResource>();
            await Paginate<TResource>(path, pathParameters, r =>
            {
                resources.AddRange(r.Items);
                return true;
            },
                cancellationToken
            ).ConfigureAwait(false);

            return resources;
        }

        /// <inheritdoc />
        public async Task Paginate<TResource>(string path, object pathParameters, Func<ResourceCollection<TResource>, bool> getNextPage)
        {
            await Paginate(path, pathParameters, getNextPage, CancellationToken.None);
        }

        /// <inheritdoc />
        public async Task Paginate<TResource>(string path, object pathParameters, Func<ResourceCollection<TResource>, bool> getNextPage, CancellationToken cancellationToken)
        {
            var page = await List<TResource>(path, pathParameters, cancellationToken).ConfigureAwait(false);

            while (getNextPage(page) && page.Items.Count > 0 && page.HasLink("Page.Next"))
            {
                page = await List<TResource>(page.Link("Page.Next"), null, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public Task Paginate<TResource>(string path, Func<ResourceCollection<TResource>, bool> getNextPage)
        {
            return Paginate(path, null, getNextPage);
        }

        /// <inheritdoc />
        public Task Paginate<TResource>(string path, Func<ResourceCollection<TResource>, bool> getNextPage, CancellationToken cancellationToken)
        {
            return Paginate(path, null, getNextPage, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<TResource> Create<TResource>(string path, TResource resource, object pathParameters = null)
        {
            return await Create(path, resource, pathParameters, CancellationToken.None);
        }

        /// <inheritdoc/>
        public async Task<TResource> Create<TResource>(string path, TResource resource, CancellationToken cancellationToken)
        {
            return await Create(path, resource, null, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<TResource> Create<TResource>(string path, TResource resource, object pathParameters, CancellationToken cancellationToken)
        {
            var uri = QualifyUri(path, pathParameters);
            var response = await DispatchRequest<TResource>(new OctopusRequest("POST", uri, requestResource: resource), true, cancellationToken).ConfigureAwait(false);
            var getUrl = resourceSelfLinkExtractor.GetSelfUrlOrNull(response.ResponseResource) ?? path;
            var result = await Get<TResource>(getUrl, null, cancellationToken).ConfigureAwait(false);

            return result;
        }

        /// <inheritdoc/>
        public async Task<TResponse> Create<TCommand, TResponse>(string path, TCommand command, object pathParameters, CancellationToken cancellationToken)
        {
            var uri = QualifyUri(path, pathParameters);
            var response = await DispatchRequest<TResponse>(new OctopusRequest("POST", uri, requestResource: command), true, cancellationToken).ConfigureAwait(false);
            return response.ResponseResource;
        }

        /// <inheritdoc />
        public Task Post<TResource>(string path, TResource resource, object pathParameters = null)
        {
            return Post(path, resource, pathParameters, CancellationToken.None);
        }

        /// <inheritdoc />
        public Task Post<TResource>(string path, TResource resource, CancellationToken cancellationToken)
        {
            return Post(path, resource, null, cancellationToken);
        }

        /// <inheritdoc />
        public Task Post<TResource>(string path, TResource resource, object pathParameters, CancellationToken cancellationToken)
        {
            var uri = QualifyUri(path, pathParameters);

            return DispatchRequest<TResource>(new OctopusRequest("POST", uri, requestResource: resource), false, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<TResponse> Post<TResource, TResponse>(string path, TResource resource, object pathParameters = null)
        {
            return await Post<TResource, TResponse>(path, resource, pathParameters, CancellationToken.None);
        }

        /// <inheritdoc />
        public async Task<TResponse> Post<TResource, TResponse>(string path, TResource resource, CancellationToken cancellationToken)
        {
            return await Post<TResource, TResponse>(path, resource, null, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<TResponse> Post<TResource, TResponse>(string path, TResource resource, object pathParameters, CancellationToken cancellationToken)
        {
            var uri = QualifyUri(path, pathParameters);
            var response = await DispatchRequest<TResponse>(new OctopusRequest("POST", uri, requestResource: resource), true, cancellationToken).ConfigureAwait(false);

            return response.ResponseResource;
        }

        /// <inheritdoc />
        public Task Post(string path)
        {
            return Post(path, CancellationToken.None);
        }

        /// <inheritdoc />
        public Task Post(string path, CancellationToken cancellationToken)
        {
            var uri = QualifyUri(path);

            return DispatchRequest<string>(new OctopusRequest("POST", uri), false, cancellationToken);
        }

        /// <inheritdoc />
        public Task Put<TResource>(string path, TResource resource)
        {
            return Put(path, resource, null, CancellationToken.None);
        }

        /// <inheritdoc />
        public Task Put<TResource>(string path, TResource resource, CancellationToken cancellationToken)
        {
            return Put(path, resource, null, cancellationToken);
        }

        /// <inheritdoc />
        public Task Put<TResource>(string path, TResource resource, object pathParameters = null)
        {
            return Put(path, resource, pathParameters, CancellationToken.None);
        }

        /// <inheritdoc />
        public Task Put<TResource>(string path, TResource resource, object pathParameters, CancellationToken cancellationToken)
        {
            var uri = QualifyUri(path, pathParameters);

            return DispatchRequest<TResource>(new OctopusRequest("PUT", uri, requestResource: resource), false, cancellationToken);
        }

        /// <inheritdoc />
        public Task Put(string path)
        {
            return Put(path, CancellationToken.None);
        }

        public Task Put(string path, CancellationToken cancellationToken)
        {
            var uri = QualifyUri(path);

            return DispatchRequest<string>(new OctopusRequest("PUT", uri), false, cancellationToken);
        }

        /// <inheritdoc />
        public Task Delete(string path, object pathParameters = null, object resource = null)
        {
            return Delete(path, pathParameters, resource, CancellationToken.None);
        }

        /// <inheritdoc />
        public Task Delete(string path, CancellationToken cancellationToken)
        {
            return Delete(path, null, null, cancellationToken);
        }

        /// <inheritdoc />
        public Task Delete(string path, object pathParameters, object resource, CancellationToken cancellationToken)
        {
            var uri = QualifyUri(path, pathParameters);

            return DispatchRequest<string>(new OctopusRequest("DELETE", uri, resource), true, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<TResponse> Delete<TCommand, TResponse>(string path, TCommand command, CancellationToken cancellationToken)
        {
            var uri = QualifyUri(path);

            var response = await DispatchRequest<TResponse>(new OctopusRequest("DELETE", uri, command), true, cancellationToken);
            return response.ResponseResource;
        }
        /// <inheritdoc/>
        public async Task<TResource> Update<TResource>(string path, TResource resource, object pathParameters = null)
        {
            return await Update(path, resource, pathParameters, CancellationToken.None);
        }

        /// <inheritdoc/>
        public async Task<TResource> Update<TResource>(string path, TResource resource, CancellationToken cancellationToken)
        {
            return await Update(path, resource, null, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<TResource> Update<TResource>(string path, TResource resource, object pathParameters, CancellationToken cancellationToken)
        {
            var uri = QualifyUri(path, pathParameters);
            var response = await DispatchRequest<TResource>(new OctopusRequest("PUT", uri, requestResource: resource), true, cancellationToken).ConfigureAwait(false);
            var getUrl = resourceSelfLinkExtractor.GetSelfUrlOrNull(response.ResponseResource) ?? path;
            var result = await Get<TResource>(getUrl, null, cancellationToken).ConfigureAwait(false);

            return result;
        }

        /// <inheritdoc/>
        public async Task<TResponse> Update<TCommand, TResponse>(string path, TCommand resource, object pathParameters, CancellationToken cancellationToken)
        {
            var uri = QualifyUri(path, pathParameters);
            var response = await DispatchRequest<TResponse>(new OctopusRequest("PUT", uri, requestResource: resource), true, cancellationToken).ConfigureAwait(false);
            return response.ResponseResource;
        }

        /// <inheritdoc />
        public async Task<Stream> GetContent(string path, object pathParameters = null)
        {
            return await GetContent(path, pathParameters, CancellationToken.None);
        }

        /// <inheritdoc />
        public async Task<Stream> GetContent(string path, CancellationToken cancellationToken)
        {
            return await GetContent(path, null, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<Stream> GetContent(string path, object pathParameters, CancellationToken cancellationToken)
        {
            var uri = QualifyUri(path, pathParameters);
            var response = await DispatchRequest<Stream>(new OctopusRequest("GET", uri), true, cancellationToken).ConfigureAwait(false);

            return response.ResponseResource;
        }

        /// <inheritdoc />
        public Task PutContent(string path, Stream contentStream)
        {
            return PutContent(path, contentStream, CancellationToken.None);
        }

        /// <inheritdoc />
        public Task PutContent(string path, Stream contentStream, CancellationToken cancellationToken)
        {
            if (contentStream == null) throw new ArgumentNullException("contentStream");
            var uri = QualifyUri(path);

            return DispatchRequest<Stream>(new OctopusRequest("PUT", uri, requestResource: contentStream), false, cancellationToken);
        }

        public Uri QualifyUri(string path, object parameters = null)
        {
            var dictionary = parameters as IDictionary<string, object>;
            path = (dictionary == null) ? UrlTemplate.Resolve(path, parameters) : UrlTemplate.Resolve(path, dictionary);

            return serverEndpoint.OctopusServer.Resolve(path);
        }

        [Obsolete("Please use the overload with cancellation token instead.", false)]
        protected async Task<OctopusResponse<TResponseResource>> DispatchRequest<TResponseResource>(
            OctopusRequest request, bool readResponse)
            => await DispatchRequest<TResponseResource>(request, readResponse, CancellationToken.None);

        protected virtual async Task<OctopusResponse<TResponseResource>> DispatchRequest<TResponseResource>(OctopusRequest request, bool readResponse, CancellationToken cancellationToken)
        {
            using var message = new HttpRequestMessage();

            message.RequestUri = request.Uri;
            message.Method = new HttpMethod(request.Method);

            if (request.Method == "PUT" || request.Method == "DELETE")
            {
                message.Method = HttpMethod.Post;
                message.Headers.Add("X-HTTP-Method-Override", request.Method);
            }

            if (!string.IsNullOrEmpty(antiforgeryCookieName))
            {
                var antiforgeryCookie = cookieContainer.GetCookies(cookieOriginUri)
                    .Cast<Cookie>()
                    .SingleOrDefault(c => string.Equals(c.Name, antiforgeryCookieName));
                if (antiforgeryCookie != null)
                {
                    message.Headers.Add(ApiConstants.AntiforgeryTokenHttpHeaderName, antiforgeryCookie.Value);
                }
            }

            SendingOctopusRequest?.Invoke(request);

            BeforeSendingHttpRequest?.Invoke(message);

            if (request.RequestResource != null)
                message.Content = GetContent(request);

            Logger.Trace($"DispatchRequest: {request.Method} {message.RequestUri}");

            var completionOption = readResponse ? HttpCompletionOption.ResponseContentRead : HttpCompletionOption.ResponseHeadersRead;

            await requestSemaphore.WaitAsync(cancellationToken);
            try
            {
                using var response = await client.SendAsync(message, completionOption, cancellationToken).ConfigureAwait(false);
                AfterReceivedHttpResponse?.Invoke(response);

                if (!response.IsSuccessStatusCode)
                    throw await OctopusExceptionFactory.CreateException(response).ConfigureAwait(false);

                var resource = readResponse
                    ? await ReadResponse<TResponseResource>(response).ConfigureAwait(false)
                    : default;

                var locationHeader = response.Headers.Location?.OriginalString;
                var octopusResponse = new OctopusResponse<TResponseResource>(request, response.StatusCode,
                    locationHeader, resource);
                ReceivedOctopusResponse?.Invoke(octopusResponse);

                return octopusResponse;
            }
            // Note: an earlier iteration of this code used (exception.CancellationToken != cancellationToken) to determine whether
            // we are observing the specific TimeoutException for this request, from some other ambient timeout exception (e.g. a test framework timeout).
            // This did not work on the NETFRAMEWORK target, because while .NET 6 flows the cancellationToken through to TaskCanceledException.CancellationToken,
            // the .NET desktop framework does not; it's not safe to compare tokens and so use the cancellation status as an approximation.
            catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested && exception.InnerException is TimeoutException)
            {
                throw new TimeoutException($"Timeout getting response from {request.Uri} (client timeout is set to {client.Timeout}).", exception);
            }
            finally
            {
                requestSemaphore.Release();
            }
        }

        private HttpContent GetContent(OctopusRequest request)
        {
            var requestStreamContent = request.RequestResource as Stream;
            if (requestStreamContent != null)
            {
                var streamContent = new StreamContent(requestStreamContent);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                return streamContent;
            }

            var fileUploadContent = request.RequestResource as FileUpload;
            if (fileUploadContent != null)
            {
                var formContent = new MultipartFormDataContent();
                var streamContent = new StreamContent(fileUploadContent.Contents);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                formContent.Add(streamContent, "file", fileUploadContent.FileName);
                return formContent;
            }

            var text = JsonConvert.SerializeObject(request.RequestResource, defaultJsonSerializerSettings);

            var content = new StringContent(text, Encoding.UTF8, "application/json");
            return content;
        }

        private async Task<T> ReadResponse<T>(HttpResponseMessage response)
        {
            var content = response.Content;

            if (typeof(T) == typeof(Stream))
            {
                var stream = new MemoryStream();
                await content.CopyToAsync(stream).ConfigureAwait(false);
                stream.Seek(0, SeekOrigin.Begin);
                return (T)(object)stream;
            }

            if (typeof(T) == typeof(byte[]))
                return (T)(object)await content.ReadAsByteArrayAsync().ConfigureAwait(false);

            var str = await content.ReadAsStringAsync().ConfigureAwait(false);
            if (typeof(T) == typeof(string))
                return (T)(object)str;

            try
            {
                var deserialized = JsonConvert.DeserializeObject<T>(str, defaultJsonSerializerSettings);
                return deserialized;
            }
            catch (Exception ex)
            {
                throw new OctopusDeserializationException((int)response.StatusCode,
                    $"Unable to process response from server: {ex.Message}. Response content: {(str.Length > 1000 ? str.Substring(0, 1000) : str)}", ex);
            }
        }

        private void ValidateSpaceId(SpaceResource space)
        {
            if (space == null)
            {
                throw new ArgumentNullException(nameof(space));
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            requestSemaphore?.Dispose();
            oidcCache?.Dispose();
            client?.Dispose();
        }
    }
}
