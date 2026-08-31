using System;
using FluentAssertions;
using NUnit.Framework;

namespace Octopus.Client.Tests
{
    /// <summary>
    /// Covers the GCRA scheduling the client uses to keep itself inside the rate limit the server advertises.
    /// </summary>
    /// <remarks>
    /// The pacer hands back the delay it wants rather than sleeping for it, so these run at full speed. The delays are
    /// measured against a real clock, though, so they're compared with a tolerance rather than exactly.
    /// </remarks>
    public class RateLimitPacerTests
    {
        // A burst of 10 refilling at one request a second. The pacer holds one request back, so it will let 9 of those
        // 10 go at once and then settle to a request a second.
        const string Policy = "l=10;rpm=60";

        static readonly TimeSpan EmissionInterval = TimeSpan.FromSeconds(1);
        static readonly TimeSpan TwoEmissionIntervals = TimeSpan.FromSeconds(2);
        static readonly TimeSpan Tolerance = TimeSpan.FromMilliseconds(250);

        RateLimitPacer pacer;

        [SetUp]
        public void SetUp() => pacer = new RateLimitPacer();

        [Test]
        public void ReserveSlot_BeforeTheServerHasToldUsAnything_DoesNotWait()
        {
            pacer.ReserveSlot().Should().Be(TimeSpan.Zero);
        }

        [Test]
        public void ReserveSlot_WhenTheServerSendsNoRateLimitingHeaders_DoesNotWait()
        {
            // A server with no policy for this caller says nothing at all, and we shouldn't invent a limit for it.
            pacer.ObserveResponse(null, null);

            pacer.ReserveSlot().Should().Be(TimeSpan.Zero);
        }

        [Test]
        public void ReserveSlot_WithQuotaToSpare_DoesNotWait()
        {
            pacer.ObserveResponse(Policy, "r=5;t=5");

            pacer.ReserveSlot().Should().Be(TimeSpan.Zero);
        }

        [Test]
        public void ReserveSlot_WhenOnlyTheReservedRequestIsLeft_WaitsForOneRequestToComeBack()
        {
            // One request left means the server would let us through, but the request after that would be rejected,
            // and the two sides' arithmetic doesn't agree to the last fraction of a request. So we stop here.
            pacer.ObserveResponse(Policy, "r=1;t=9");

            pacer.ReserveSlot().Should().BeCloseTo(EmissionInterval, Tolerance);
        }

        [Test]
        public void ReserveSlot_WhenTheQuotaIsSpent_WaitsForTheReservedRequestToComeBackToo()
        {
            pacer.ObserveResponse(Policy, "r=0;t=10");

            pacer.ReserveSlot().Should().BeCloseTo(TwoEmissionIntervals, Tolerance);
        }

        [Test]
        public void ReserveSlot_WithNoFurtherResponses_SpendsTheBurstAndThenPacesItself()
        {
            pacer.ObserveResponse(Policy, "r=10;t=0");

            // A burst of 10, less the one we hold in reserve. Each slot is taken as it's handed out, so consecutive
            // callers work their way through the burst rather than all being waved through.
            for (var i = 0; i < 9; i++)
                pacer.ReserveSlot().Should().Be(TimeSpan.Zero, "request {0} is still within the burst", i + 1);

            pacer.ReserveSlot().Should().BeCloseTo(EmissionInterval, Tolerance);
        }

        [Test]
        public void ObserveResponse_WhenAnOlderResponseArrivesLast_DoesNotTalkUsIntoSpeedingUp()
        {
            pacer.ObserveResponse(Policy, "r=1;t=9");

            // Responses can overtake each other, and this one is reporting a bucket that was fuller than the one we
            // already know about. Believing it would have us spend quota that has already gone.
            pacer.ObserveResponse(Policy, "r=9;t=1");

            pacer.ReserveSlot().Should().BeCloseTo(EmissionInterval, Tolerance);
        }

        [Test]
        public void ObserveResponse_WhenTheServerReportsLessQuotaThanWeExpected_SlowsUsDown()
        {
            pacer.ObserveResponse(Policy, "r=9;t=1");
            pacer.ReserveSlot().Should().Be(TimeSpan.Zero);

            // Something else has been spending the same allowance, so what's left isn't what our own arithmetic said.
            pacer.ObserveResponse(Policy, "r=0;t=10");

            pacer.ReserveSlot().Should().BeCloseTo(TwoEmissionIntervals, Tolerance);
        }

        [Test]
        public void ObserveResponse_WithABurstOfOne_LeavesNoRoomToBurstRatherThanGoingBackwards()
        {
            // There is nothing to hold in reserve here, so the burst tolerance would come out negative if we let it.
            pacer.ObserveResponse("l=1;rpm=60", "r=1;t=0");
            pacer.ReserveSlot().Should().Be(TimeSpan.Zero);

            pacer.ObserveResponse("l=1;rpm=60", "r=0;t=1");
            pacer.ReserveSlot().Should().BeCloseTo(EmissionInterval, Tolerance);
        }

        [Test]
        public void ObserveResponse_WhenThePolicyChanges_StartsAgainOnTheNewOne()
        {
            // Held back by a policy that only allows a request every 10 seconds...
            pacer.ObserveResponse("l=10;rpm=6", "r=1;t=90");
            pacer.ReserveSlot().Should().BeCloseTo(TimeSpan.FromSeconds(10), Tolerance);

            // ...which an administrator then loosens. The schedule we'd worked out is in the old policy's units, so
            // holding on to it would keep us slow long after the limit that justified it had gone.
            pacer.ObserveResponse(Policy, "r=10;t=0");

            pacer.ReserveSlot().Should().Be(TimeSpan.Zero);
        }

        [Test]
        public void ObserveResponse_WithAPolicyItCannotMakeSenseOf_KeepsOutOfTheWay()
        {
            pacer.ObserveResponse("l=nonsense;rpm=0", "r=0;t=10");

            pacer.ReserveSlot().Should().Be(TimeSpan.Zero);
        }

        [Test]
        public void ObserveResponse_WhenAResponseCarriesNoRemainingCount_GivesTheSlotBack()
        {
            pacer.ObserveResponse(Policy, "r=10;t=0");

            // Spend the burst, so the next request has to wait for the schedule we've built up.
            for (var i = 0; i < 10; i++)
                pacer.ReserveSlot();

            pacer.ReserveSlot().Should().BeCloseTo(TwoEmissionIntervals, Tolerance);

            // Two of those requests turn out to have gone somewhere the server doesn't rate limit, so they cost us
            // nothing and the schedule they claimed comes back. Otherwise we'd pace our whole request volume against
            // a limit that only governs part of it.
            pacer.ObserveResponse(null, null);
            pacer.ObserveResponse(Policy, null);

            pacer.ReserveSlot().Should().BeCloseTo(EmissionInterval, Tolerance);
        }

        [Test]
        public void ObserveResponse_WhenRefundsOutnumberTheSlotsTaken_DoesNotBankCreditForAFutureBurst()
        {
            pacer.ObserveResponse(Policy, "r=10;t=0");
            pacer.ReserveSlot();

            // Once the schedule is back level with the clock there's nothing left to give back, however many
            // unlimited responses arrive. Otherwise a quiet spell of telemetry would buy a burst we never earned.
            for (var i = 0; i < 20; i++)
                pacer.ObserveResponse(null, null);

            // Still the burst the policy allows, and no longer.
            for (var i = 0; i < 9; i++)
                pacer.ReserveSlot().Should().Be(TimeSpan.Zero, "request {0} is still within the burst", i + 1);

            pacer.ReserveSlot().Should().BeCloseTo(EmissionInterval, Tolerance);
        }

        [Test]
        public void ObserveResponse_AfterRefundingTooFar_PutsTheFloorBackUnderTheSchedule()
        {
            pacer.ObserveResponse(Policy, "r=0;t=10");

            // A request we refund may in fact have reached the limiter - an aborted one, say. Refunding is the safe
            // direction to be wrong in, because the next response that does report a count resyncs us. Had these
            // refunds stood, the request below would have gone straight out.
            pacer.ObserveResponse(null, null);
            pacer.ObserveResponse(null, null);
            pacer.ObserveResponse(null, null);
            pacer.ObserveResponse(Policy, "r=0;t=10");

            pacer.ReserveSlot().Should().BeCloseTo(TwoEmissionIntervals, Tolerance);
        }

        [Test]
        public void ObserveResponse_WhenTheServerStopsRateLimitingUs_StopsPacing()
        {
            pacer.ObserveResponse(Policy, "r=0;t=10");
            pacer.ReserveSlot().Should().BeCloseTo(TwoEmissionIntervals, Tolerance);

            // An administrator turned the policy off mid-session, so responses stop carrying the headers. The refunds
            // unwind the schedule we'd built, rather than leaving us pacing for ever against a limit that has gone.
            for (var i = 0; i < 11; i++)
                pacer.ObserveResponse(null, null);

            pacer.ReserveSlot().Should().Be(TimeSpan.Zero);
        }

        [TestCase("l=200;rpm=600", "l", 200)]
        [TestCase("l=200;rpm=600", "rpm", 600)]
        [TestCase("r=7;t=18", "t", 18)]
        [TestCase("l=200; rpm=600", "rpm", 600)]
        [TestCase("rpm=0.5", "rpm", 0.5)]
        public void TryGetParameter_ReadsTheValue(string header, string name, double expected)
        {
            double value;
            RateLimitPacer.TryGetParameter(header, name, out value).Should().BeTrue();
            value.Should().Be(expected);
        }

        [TestCase(null, "l")]
        [TestCase("", "l")]
        [TestCase("l=200", "rpm")]
        [TestCase("l=200", "")]
        [TestCase("nonsense", "l")]
        [TestCase("l=", "l")]
        [TestCase("l=abc", "l")]
        public void TryGetParameter_WhenItIsNotThere_SaysSo(string header, string name)
        {
            double value;
            RateLimitPacer.TryGetParameter(header, name, out value).Should().BeFalse();
        }

        [Test]
        public void TryGetParameter_IgnoresParametersItDoesNotKnowAbout()
        {
            // So the server can add to these headers without older clients falling over.
            double value;
            RateLimitPacer.TryGetParameter("l=200;something=new;rpm=600", "rpm", out value).Should().BeTrue();
            value.Should().Be(600);
        }
    }
}
