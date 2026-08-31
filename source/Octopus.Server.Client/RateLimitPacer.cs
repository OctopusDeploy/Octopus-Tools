using System;
using System.Diagnostics;
using System.Globalization;

namespace Octopus.Client
{
    /// <summary>
    /// Paces requests so we stay inside the rate limit the server advertises, rather than running at it and
    /// relying on being told off with an HTTP 429.
    /// </summary>
    /// <remarks>
    /// The server describes its limit with two advisory headers:
    /// <code>
    /// Octopus-RateLimit-Policy: l=200;rpm=600
    /// Octopus-RateLimit: r=7;t=18
    /// </code>
    /// <c>l</c> is the burst limit and <c>rpm</c> how fast it refills; <c>r</c> is what we have left and <c>t</c> how
    /// long until the whole allowance is back. The policy is sent on its own so that HTTP/2 header compression can
    /// leave it off the wire while it stays the same.
    ///
    /// We model the server's token bucket with GCRA (the Generic Cell Rate Algorithm), which tracks a single value -
    /// the theoretical arrival time (TAT), the point at which the requests we have already sent will have been paid
    /// for. A request may go as soon as the TAT is within <see cref="burstTolerance" /> of now, and each request
    /// pushes the TAT out by one <see cref="emissionInterval" />. That gives us the same "burst then settle to the
    /// steady rate" shape as the server's bucket, without needing a timer to refill anything.
    ///
    /// Every response resyncs the TAT to what the server says is left, so we self-correct: if something else is
    /// spending the same user's allowance, or the server's replenishment doesn't line up exactly with our arithmetic,
    /// the next response pulls us back into line. The resync only ever moves the TAT later, never earlier, so a
    /// response that overtook a newer one can't talk us into speeding up.
    ///
    /// We stop one request short of the advertised allowance (<see cref="ReservedRequests" />), so the rounding either
    /// side of the wire has somewhere to go and we never spend the server's last token.
    /// </remarks>
    internal class RateLimitPacer
    {
        public const string PolicyHeaderName = "Octopus-RateLimit-Policy";
        public const string RateLimitHeaderName = "Octopus-RateLimit";

        /// <summary>
        /// How much of the allowance we leave unspent. The server reports whole requests remaining and rounds down,
        /// and its bucket refills on a timer rather than continuously, so both sides' idea of "one request left" can
        /// disagree by a fraction. Holding one back means that disagreement costs us a small delay instead of a 429.
        /// </summary>
        const int ReservedRequests = 1;

        readonly object stateLock = new();

        // Elapsed time rather than wall clock: we only ever compare our own timestamps, and this can't jump.
        readonly Stopwatch clock = Stopwatch.StartNew();

        bool havePolicy;
        int burstLimit;
        double requestsPerMinute;
        TimeSpan emissionInterval;
        TimeSpan burstTolerance;
        TimeSpan theoreticalArrivalTime;

        /// <summary>
        /// Claims the next slot in the schedule, and returns how long the caller should wait before sending.
        /// The slot is taken whether or not the caller waits, so concurrent callers get consecutive slots rather
        /// than all being told the same thing.
        /// </summary>
        public TimeSpan ReserveSlot()
        {
            lock (stateLock)
            {
                // Until the server has told us what the limit is, there's nothing to pace against.
                if (!havePolicy) return TimeSpan.Zero;

                var now = clock.Elapsed;
                var earliestSendTime = theoreticalArrivalTime - burstTolerance;

                theoreticalArrivalTime = Later(theoreticalArrivalTime, now) + emissionInterval;

                return earliestSendTime > now ? earliestSendTime - now : TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Takes in the advisory headers from a response. Either may be absent: a server that isn't rate limiting us
        /// sends neither, and HTTP/2 aside, the policy only tells us something we may already know. A response with
        /// nothing left to tell us gives back the slot its request claimed - see <see cref="RefundSlot" />.
        /// </summary>
        public void ObserveResponse(string policyHeaderValue, string rateLimitHeaderValue)
        {
            lock (stateLock)
            {
                double limit, rate;
                if (TryGetParameter(policyHeaderValue, "l", out limit)
                    && TryGetParameter(policyHeaderValue, "rpm", out rate)
                    && limit >= 1
                    && rate > 0
                    && (!havePolicy || limit != burstLimit || rate != requestsPerMinute))
                {
                    burstLimit = (int)limit;
                    requestsPerMinute = rate;
                    emissionInterval = TimeSpan.FromMinutes(1 / rate);

                    // How far ahead of the steady rate we're allowed to run: the whole allowance, less the request
                    // being sent and the one we hold in reserve.
                    var allowedBurst = burstLimit - 1 - ReservedRequests;
                    burstTolerance = allowedBurst > 0 ? TimeSpan.FromTicks(emissionInterval.Ticks * allowedBurst) : TimeSpan.Zero;

                    // The schedule we'd built is denominated in the old policy's units and means nothing now. The
                    // resync below puts us back on the new one, using what this same response says is left.
                    theoreticalArrivalTime = TimeSpan.Zero;

                    havePolicy = true;
                }

                if (!havePolicy)
                    return;

                double remaining;
                if (!TryGetParameter(rateLimitHeaderValue, "r", out remaining))
                {
                    RefundSlot();
                    return;
                }

                if (remaining < 0) remaining = 0;
                if (remaining > burstLimit) remaining = burstLimit;

                // The allowance the server says we've spent is worth this much time on our schedule.
                var spent = TimeSpan.FromTicks((long)(emissionInterval.Ticks * (burstLimit - remaining)));
                theoreticalArrivalTime = Later(theoreticalArrivalTime, clock.Elapsed + spent);
            }
        }

        /// <summary>
        /// Gives back the slot a request claimed, because the response says the request never cost the limiter
        /// anything: no remaining count means either the endpoint isn't rate limited (the server opts a handful of
        /// them out, and puts others on limiters that send no advisory headers) or the request never reached the
        /// limiter at all.
        /// </summary>
        /// <remarks>
        /// Without this, <see cref="ReserveSlot" /> spends schedule on requests the server didn't charge us for, and
        /// a caller that mixes limited and unlimited endpoints paces its whole request volume against a limit that
        /// only governs part of it.
        ///
        /// Refunding too much is safe in a way that spending too much isn't: the resync in
        /// <see cref="ObserveResponse" /> only ever moves the schedule towards what the server says is left, so the
        /// next response carrying a count puts the floor straight back under us. Nothing lowers the schedule after we
        /// have overspent it, except time passing.
        /// </remarks>
        void RefundSlot()
        {
            // Only give back time we are actually holding. A schedule that has already fallen behind the clock isn't
            // pacing anything, and pushing it further into the past would bank credit for a burst we haven't earned.
            var now = clock.Elapsed;
            if (theoreticalArrivalTime > now)
                theoreticalArrivalTime = Later(theoreticalArrivalTime - emissionInterval, now);
        }

        static TimeSpan Later(TimeSpan first, TimeSpan second) => first > second ? first : second;

        /// <summary>
        /// Pulls one <c>name=value</c> parameter out of a header like <c>l=200;rpm=600</c>. Unrecognised parameters
        /// are ignored, so the server can add more without breaking older clients.
        /// </summary>
        internal static bool TryGetParameter(string headerValue, string name, out double value)
        {
            value = 0;

            if (string.IsNullOrEmpty(headerValue))
                return false;

            foreach (var parameter in headerValue.Split(';'))
            {
                var separator = parameter.IndexOf('=');
                if (separator < 0)
                    continue;

                if (!string.Equals(parameter.Substring(0, separator).Trim(), name, StringComparison.OrdinalIgnoreCase))
                    continue;

                return double.TryParse(
                    parameter.Substring(separator + 1).Trim(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out value);
            }

            return false;
        }
    }
}
