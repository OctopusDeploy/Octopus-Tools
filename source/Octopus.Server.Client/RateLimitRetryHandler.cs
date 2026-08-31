using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Octopus.Client
{
    /// <summary>
    /// Retries requests that the server rejected with HTTP 429 (Too Many Requests) because they hit a rate limit.
    /// We wait for the duration in the response's Retry-After header (or <see cref="OctopusClientOptions.RateLimitRetryDefaultDelay" />
    /// if the server didn't send one) and try again, up to <see cref="OctopusClientOptions.RateLimitRetryCount" /> times.
    /// </summary>
    /// <remarks>
    /// Requests carrying a streamed body (package uploads and other raw content) cannot be retried, because we
    /// can't rewind the caller's stream. Those requests are sent once and any 429 is passed back to the caller.
    /// </remarks>
    internal class RateLimitRetryHandler : DelegatingHandler
    {
        readonly OctopusClientOptions options;

        public RateLimitRetryHandler(HttpMessageHandler innerHandler, OctopusClientOptions options)
            : base(innerHandler)
        {
            this.options = options;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (options.RateLimitRetryCount <= 0)
            {
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }

            var body = await BufferedRequestBody.TryCreate(request).ConfigureAwait(false);
            if (body == null)
            {
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }

            // Snapshot the headers before we send anything: handlers further down the chain add headers of their
            // own (cookies, tracing) to the request, and replaying those on the next attempt would duplicate them.
            var headers = SnapshotHeaders(request.Headers);

            for (var attempt = 0; ; attempt++)
            {
                var attemptRequest = attempt == 0 ? request : CloneRequest(request, headers, body);
                var response = await base.SendAsync(attemptRequest, cancellationToken).ConfigureAwait(false);

                if ((int)response.StatusCode != 429 || attempt >= options.RateLimitRetryCount) return response;

                var retryAfter = response.Headers.RetryAfter;
                if (!RateLimitRetry.TryGetDelay(retryAfter?.Delta, options, out var delay)) return response;

                response.Dispose();
                if (attempt > 0) attemptRequest.Dispose(); // the caller owns the original request; we own our clones

                if (delay > TimeSpan.Zero) await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        static KeyValuePair<string, string[]>[] SnapshotHeaders(HttpHeaders headers)
            => headers.Select(h => new KeyValuePair<string, string[]>(h.Key, h.Value.ToArray())).ToArray();

        static HttpRequestMessage CloneRequest(HttpRequestMessage request, KeyValuePair<string, string[]>[] headers, BufferedRequestBody body)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Version = request.Version,
                Content = body.CreateContent()
            };

            foreach (var header in headers)
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

#pragma warning disable CS0618 // Properties is the only option available on the frameworks we target
            foreach (var property in request.Properties)
                clone.Properties[property.Key] = property.Value;
#pragma warning restore CS0618

            return clone;
        }

        /// <summary>
        /// A copy of a request's body that we can hand to as many attempts as we need.
        /// </summary>
        class BufferedRequestBody
        {
            static readonly BufferedRequestBody Empty = new(null, null);

            readonly byte[] bytes;
            readonly KeyValuePair<string, string[]>[] headers;

            BufferedRequestBody(byte[] bytes, KeyValuePair<string, string[]>[] headers)
            {
                this.bytes = bytes;
                this.headers = headers;
            }

            /// <summary>
            /// Returns a buffered copy of the request's body, or null if the body can't be replayed.
            /// </summary>
            public static async Task<BufferedRequestBody> TryCreate(HttpRequestMessage request)
            {
                var content = request.Content;
                if (content == null) return Empty;

                // ByteArrayContent (and its StringContent/FormUrlEncodedContent subclasses) is already fully in memory,
                // so copying it costs us nothing extra. Anything else is potentially a large or unrewindable stream.
                if (content is not ByteArrayContent) return null;

                var bytes = await content.ReadAsByteArrayAsync().ConfigureAwait(false);
                return new BufferedRequestBody(bytes, SnapshotHeaders(content.Headers));
            }

            public HttpContent CreateContent()
            {
                if (bytes == null) return null;

                var content = new ByteArrayContent(bytes);
                content.Headers.Clear();
                foreach (var header in headers)
                {
                    content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                return content;
            }
        }
    }
}
