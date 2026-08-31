using System;
using System.Globalization;

namespace Octopus.Client
{
    /// <summary>
    /// Shared logic for working out how long to wait before retrying a request that the server
    /// rejected with HTTP 429 (Too Many Requests).
    /// </summary>
    internal static class RateLimitRetry
    {
        /// <summary>
        /// Works out how long we should wait before retrying, based on the Retry-After value the server sent us.
        /// </summary>
        public static bool TryGetDelay(TimeSpan? retryAfterDelta, OctopusClientOptions options, out TimeSpan delay)
        {
            delay = retryAfterDelta ?? options.RateLimitRetryDefaultDelay;

            // A Retry-After of zero means we can go again straight away
            if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;

            // If the server wants us to wait longer than we're prepared to, don't retry; let the caller see the 429
            // rather than blocking them for an unbounded amount of time.
            return delay <= options.RateLimitRetryMaxDelay;
        }

        /// <summary>
        /// Parses a raw Retry-After header value, which is either a number of seconds or an HTTP date.
        /// Used by the synchronous client, which deals in raw header strings rather than typed header values.
        /// </summary>
        public static TimeSpan? ParseRetryAfterHeader(string headerValue)
        {
            if (!string.IsNullOrWhiteSpace(headerValue) && int.TryParse(headerValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
            {
                return TimeSpan.FromSeconds(seconds);
            }

            return null;
        }
    }
}
