using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Nancy;
using NUnit.Framework;
using Octopus.Client.Exceptions;

#nullable enable

namespace Octopus.Client.Tests.Integration.OctopusClient
{
    /// <summary>
    /// Covers the client's handling of HTTP 429 responses from the server's rate limiter.
    /// </summary>
    public class RateLimitRetryTests : HttpIntegrationTestBase
    {
        // The Nancy module (which is this class) is constructed per request, so the request counters have to be static.
        static int retryAfterRequests;
        static int noRetryAfterHeaderRequests;
        static int alwaysLimitedRequests;

        public RateLimitRetryTests()
            : base(UrlPathPrefixBehaviour.UseClassNameAsUrlPathPrefix)
        {
            // Rejects the first two requests with a Retry-After of 1 second, then succeeds.
            Get($"{TestRootPath}/retryafter", p =>
                Interlocked.Increment(ref retryAfterRequests) <= 2
                    ? TooManyRequestsWithRetryAfter("1")
                    : Response.AsText("Success"));

            // Rejects the first request without telling us when to come back, then succeeds.
            Get($"{TestRootPath}/noretryafterheader", p =>
                Interlocked.Increment(ref noRetryAfterHeaderRequests) <= 1
                    ? TooManyRequestsWithRetryAfter(null)
                    : Response.AsText("Success"));

            // Never lets us through, so the client eventually gives up.
            Get($"{TestRootPath}/alwayslimited", p =>
            {
                Interlocked.Increment(ref alwaysLimitedRequests);
                return TooManyRequestsWithRetryAfter("0");
            });
        }

        Response TooManyRequestsWithRetryAfter(string? retryAfter)
        {
            var response = Response.AsJson(new { ErrorMessage = "Too many requests" })
                .WithStatusCode((HttpStatusCode)429);

            return retryAfter == null ? response : response.WithHeader("Retry-After", retryAfter);
        }

        public override async Task Setup()
        {
            retryAfterRequests = 0;
            noRetryAfterHeaderRequests = 0;
            alwaysLimitedRequests = 0;
            await base.Setup();
        }

        [Test]
        public async Task AsyncClientWaitsForRetryAfterAndRetries()
        {
            var sw = Stopwatch.StartNew();

            var result = await AsyncClient.Get<string>("~/retryafter");

            result.Should().Be("Success");
            retryAfterRequests.Should().Be(3);
            sw.Elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.FromSeconds(2), "we should have waited 1 second for each of the two rejections");
        }

        [Test]
        public void SyncClientWaitsForRetryAfterAndRetries()
        {
            var result = SyncClient.Get<string>($"~/retryafter");

            result.Should().Be("Success");
            retryAfterRequests.Should().Be(3);
        }

        [Test]
        public async Task AsyncClientDoesNotRetryIfRateLimitRetryCountIsZero()
        {
            var configuredClient = await OctopusAsyncClient.Create(
                new OctopusServerEndpoint(HostBaseUri + TestRootPath),
                new OctopusClientOptions { RateLimitRetryCount = 0 });

            Func<Task> act = () => configuredClient.Get<string>("~/retryafter");

            var ex = (await act.Should().ThrowAsync<OctopusException>()).Subject.Single();
            ex.HttpStatusCode.Should().Be(429);

            retryAfterRequests.Should().Be(1);
        }

        [Test]
        public void SyncClientDoesNotRetryIfRateLimitRetryCountIsZero()
        {
            var configuredClient = new Octopus.Client.OctopusClient(
                new OctopusServerEndpoint(HostBaseUri + TestRootPath),
                new OctopusClientOptions { RateLimitRetryCount = 0 });

            Action act = () => configuredClient.Get<string>("~/retryafter");

            var ex = act.Should().Throw<OctopusException>().Subject.Single();
            ex.HttpStatusCode.Should().Be(429);

            retryAfterRequests.Should().Be(1);
        }

        [Test]
        public async Task AsyncClientWaitsTheDefaultDelayWhenThereIsNoRetryAfterHeader()
        {
            var sw = Stopwatch.StartNew();

            var result = await AsyncClient.Get<string>($"~/noretryafterheader");

            result.Should().Be("Success");
            noRetryAfterHeaderRequests.Should().Be(2);
            sw.Elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.FromSeconds(2), "the default delay is 2 seconds");
        }

        [Test]
        public async Task AsyncClientGivesUpAfterTheConfiguredNumberOfRetries()
        {
            Func<Task> get = () => AsyncClient.Get<string>($"~/alwayslimited");

            await get.Should().ThrowAsync<OctopusException>();

            // The initial request, plus RateLimitRetryCount (3) retries
            alwaysLimitedRequests.Should().Be(4);
        }

        [Test]
        public void SyncClientGivesUpAfterTheConfiguredNumberOfRetries()
        {
            Action get = () => SyncClient.Get<string>($"~/alwayslimited");

            get.Should().Throw<OctopusException>();

            alwaysLimitedRequests.Should().Be(4);
        }

        [Test]
        public void SyncClientWaitsTheDefaultDelayWhenThereIsNoRetryAfterHeader()
        {
            var sw = Stopwatch.StartNew();

            var result = SyncClient.Get<string>("~/noretryafterheader");

            result.Should().Be("Success");
            noRetryAfterHeaderRequests.Should().Be(2);
            sw.Elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.FromSeconds(2), "the default delay is 2 seconds");
        }
    }
}
