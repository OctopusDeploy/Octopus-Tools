using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Octopus.Client
{
    /// <summary>
    /// Slows requests down to stay inside the rate limit the server advertises in its
    /// <c>Octopus-RateLimit-Policy</c> and <c>Octopus-RateLimit</c> headers. See <see cref="RateLimitPacer" />.
    /// </summary>
    /// <remarks>
    /// This sits underneath <see cref="RateLimitRetryHandler" />, so a retried request is paced like any other
    /// rather than going straight back out at the server that just rejected it.
    /// </remarks>
    class RateLimitPacingHandler(HttpMessageHandler innerHandler, RateLimitPacer pacer) : DelegatingHandler(innerHandler)
    {
        readonly RateLimitPacer pacer = pacer;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var delay = pacer.ReserveSlot();
            if (delay > TimeSpan.Zero) await Task.Delay(delay, cancellationToken).ConfigureAwait(false);

            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            pacer.ObserveResponse(
                GetHeader(response, RateLimitPacer.PolicyHeaderName),
                GetHeader(response, RateLimitPacer.RateLimitHeaderName));

            return response;
        }

        static string GetHeader(HttpResponseMessage response, string name)
        {
            return response.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;
        }
    }
}
