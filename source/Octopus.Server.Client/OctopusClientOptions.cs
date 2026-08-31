using System;
using System.Net;
using System.Security.Authentication;
using Octopus.Client.Model;

namespace Octopus.Client
{
    /// <summary>
    /// Options used to change the behaviour of <see cref="OctopusAsyncClient" />
    /// </summary>
    public class OctopusClientOptions
    {
        public OctopusClientOptions()
        {
            Timeout = TimeSpan.FromMilliseconds(ApiConstants.DefaultClientRequestTimeout);
#if HTTP_CLIENT_SUPPORTS_SSL_OPTIONS
            SslProtocols = SslProtocols.Tls
                           | SslProtocols.Tls11
                           | SslProtocols.Tls12;
#endif
        }
#if HTTP_CLIENT_SUPPORTS_SSL_OPTIONS
        /// <summary>
        /// The allowed SSL Protocols
        /// </summary>
        public SslProtocols SslProtocols { get; set; }

        /// <summary>
        /// If true, SSL certificate errors are ignored
        /// </summary>
        public bool IgnoreSslErrors { get; set; }

#endif
        public TimeSpan Timeout { get; set; }
        public string Proxy { get; set; }
        public string ProxyUsername { get; set; }
        public string ProxyPassword { get; set; }

        /// <summary>
        /// Whether or not the default proxy can be used if the proxy is not set.
        /// </summary>
        public bool AllowDefaultProxy { get; set; } = true;

        /// <summary>
        /// Maximum number of simultaneous requests to make to the server
        /// </summary>
        public int MaxSimultaneousRequests = int.MaxValue;

        /// <summary>
        /// The maximum number of times a request will be retried after the server rejects it with
        /// HTTP 429 (Too Many Requests) because it hit a rate limit. Set this to 0 to turn off retrying,
        /// in which case the HTTP 429 surfaces to the caller as an error.
        /// </summary>
        public int RateLimitRetryCount { get; set; } = 3;

        /// <summary>
        /// How long to wait before retrying an HTTP 429 response that has no Retry-After header.
        /// </summary>
        public TimeSpan RateLimitRetryDefaultDelay { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// The longest we are willing to wait between retries. If the server's Retry-After asks us to wait
        /// longer than this, we stop retrying and the HTTP 429 surfaces to the caller as an error.
        /// </summary>
        public TimeSpan RateLimitRetryMaxDelay { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// If true, the client paces itself against the advisory Octopus-RateLimit-Policy and Octopus-RateLimit
        /// headers the server sends, spacing requests out so that it stays inside the limit instead of running at
        /// it and being rejected with HTTP 429. Off by default, because it makes the client wait on its own
        /// initiative, which a caller that would rather see the HTTP 429 doesn't want.
        /// </summary>
        public bool UseRateLimitHeaders { get; set; } = false;
    }
}
