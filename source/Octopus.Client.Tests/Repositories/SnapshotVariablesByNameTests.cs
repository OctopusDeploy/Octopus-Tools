using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Octopus.Client.Model;

namespace Octopus.Client.Tests.Repositories
{
    [TestFixture]
    public class SnapshotVariablesByNameTests
    {
        const string releaseRoute = "~/api/{spaceId}/releases/{releaseId}/snapshot-variables-by-name";
        const string runbookRoute = "~/api/{spaceId}/runbookSnapshots/{runbookSnapshotId}/snapshot-variables-by-name";

        string urlUsed;
        object bodyUsed;
        object parametersUsed;

        [Test]
        public void Release_PostsExpectedRouteParametersAndBody()
        {
            var client = Substitute.For<IOctopusClient>();
            var repository = new OctopusRepository(client);

            client.Post<object, ReleaseResource>(
                    Arg.Do<string>(x => urlUsed = x),
                    Arg.Do<object>(x => bodyUsed = x),
                    Arg.Do<object>(x => parametersUsed = x))
                .Returns(new ReleaseResource());

            var release = new ReleaseResource { Id = "Releases-1", SpaceId = "Spaces-1" };
            var variables = new[] { new VariableIdentifier("Database.ConnectionString", "Projects-101") };

            repository.Releases.SnapshotVariablesByName(release, variables);

            urlUsed.Should().Be(releaseRoute);
            parametersUsed.Should().BeEquivalentTo(new { spaceId = "Spaces-1", releaseId = "Releases-1" });
            bodyUsed.Should().BeEquivalentTo(new { Variables = variables });
        }

        [Test]
        public async Task AsyncRelease_PostsExpectedRouteParametersAndBody()
        {
            var client = Substitute.For<IOctopusAsyncClient>();
            var repository = new OctopusAsyncRepository(client);

            client.Post<object, ReleaseResource>(
                    Arg.Do<string>(x => urlUsed = x),
                    Arg.Do<object>(x => bodyUsed = x),
                    Arg.Do<object>(x => parametersUsed = x),
                    Arg.Any<CancellationToken>())
                .Returns(new ReleaseResource());

            var release = new ReleaseResource { Id = "Releases-1", SpaceId = "Spaces-1" };
            var variables = new[] { new VariableIdentifier("Api.Key", "LibraryVariableSets-45") };

            await repository.Releases.SnapshotVariablesByName(release, variables, CancellationToken.None);

            urlUsed.Should().Be(releaseRoute);
            parametersUsed.Should().BeEquivalentTo(new { spaceId = "Spaces-1", releaseId = "Releases-1" });
            bodyUsed.Should().BeEquivalentTo(new { Variables = variables });
        }

        [Test]
        public void RunbookSnapshot_PostsExpectedRouteParametersAndBody()
        {
            var client = Substitute.For<IOctopusClient>();
            var repository = new OctopusRepository(client);

            client.Post<object, RunbookSnapshotResource>(
                    Arg.Do<string>(x => urlUsed = x),
                    Arg.Do<object>(x => bodyUsed = x),
                    Arg.Do<object>(x => parametersUsed = x))
                .Returns(new RunbookSnapshotResource());

            var runbookSnapshot = new RunbookSnapshotResource { Id = "RunbookSnapshots-1", SpaceId = "Spaces-1" };
            var variables = new[] { new VariableIdentifier("Database.ConnectionString", "Projects-101") };

            repository.RunbookSnapshots.SnapshotVariablesByName(runbookSnapshot, variables);

            urlUsed.Should().Be(runbookRoute);
            parametersUsed.Should().BeEquivalentTo(new { spaceId = "Spaces-1", runbookSnapshotId = "RunbookSnapshots-1" });
            bodyUsed.Should().BeEquivalentTo(new { Variables = variables });
        }

        [Test]
        public async Task AsyncRunbookSnapshot_PostsExpectedRouteParametersAndBody()
        {
            var client = Substitute.For<IOctopusAsyncClient>();
            var repository = new OctopusAsyncRepository(client);

            client.Post<object, RunbookSnapshotResource>(
                    Arg.Do<string>(x => urlUsed = x),
                    Arg.Do<object>(x => bodyUsed = x),
                    Arg.Do<object>(x => parametersUsed = x),
                    Arg.Any<CancellationToken>())
                .Returns(new RunbookSnapshotResource());

            var runbookSnapshot = new RunbookSnapshotResource { Id = "RunbookSnapshots-1", SpaceId = "Spaces-1" };
            var variables = new[] { new VariableIdentifier("Api.Key", "LibraryVariableSets-45") };

            await repository.RunbookSnapshots.SnapshotVariablesByName(runbookSnapshot, variables, CancellationToken.None);

            urlUsed.Should().Be(runbookRoute);
            parametersUsed.Should().BeEquivalentTo(new { spaceId = "Spaces-1", runbookSnapshotId = "RunbookSnapshots-1" });
            bodyUsed.Should().BeEquivalentTo(new { Variables = variables });
        }

        [Test]
        public void Release_WithConcurrencyToken_PostsExpectedBody()
        {
            var client = Substitute.For<IOctopusClient>();
            var repository = new OctopusRepository(client);

            client.Post<object, ReleaseResource>(
                    Arg.Do<string>(x => urlUsed = x),
                    Arg.Do<object>(x => bodyUsed = x),
                    Arg.Do<object>(x => parametersUsed = x))
                .Returns(new ReleaseResource());

            var release = new ReleaseResource { Id = "Releases-1", SpaceId = "Spaces-1" };
            var variables = new[] { new VariableIdentifier("Database.ConnectionString", "Projects-101") };

            repository.Releases.SnapshotVariablesByName(release, variables, "concurrency-token-1");

            urlUsed.Should().Be(releaseRoute);
            parametersUsed.Should().BeEquivalentTo(new { spaceId = "Spaces-1", releaseId = "Releases-1" });
            bodyUsed.Should().BeEquivalentTo(new { Variables = variables, VariableSnapshotConcurrencyToken = "concurrency-token-1" });
        }

        [Test]
        public async Task AsyncRelease_WithConcurrencyToken_PostsExpectedBody()
        {
            var client = Substitute.For<IOctopusAsyncClient>();
            var repository = new OctopusAsyncRepository(client);

            client.Post<object, ReleaseResource>(
                    Arg.Do<string>(x => urlUsed = x),
                    Arg.Do<object>(x => bodyUsed = x),
                    Arg.Do<object>(x => parametersUsed = x),
                    Arg.Any<CancellationToken>())
                .Returns(new ReleaseResource());

            var release = new ReleaseResource { Id = "Releases-1", SpaceId = "Spaces-1" };
            var variables = new[] { new VariableIdentifier("Api.Key", "LibraryVariableSets-45") };

            await repository.Releases.SnapshotVariablesByName(release, variables, "concurrency-token-1", CancellationToken.None);

            urlUsed.Should().Be(releaseRoute);
            parametersUsed.Should().BeEquivalentTo(new { spaceId = "Spaces-1", releaseId = "Releases-1" });
            bodyUsed.Should().BeEquivalentTo(new { Variables = variables, VariableSnapshotConcurrencyToken = "concurrency-token-1" });
        }

        [Test]
        public void RunbookSnapshot_WithConcurrencyToken_PostsExpectedBody()
        {
            var client = Substitute.For<IOctopusClient>();
            var repository = new OctopusRepository(client);

            client.Post<object, RunbookSnapshotResource>(
                    Arg.Do<string>(x => urlUsed = x),
                    Arg.Do<object>(x => bodyUsed = x),
                    Arg.Do<object>(x => parametersUsed = x))
                .Returns(new RunbookSnapshotResource());

            var runbookSnapshot = new RunbookSnapshotResource { Id = "RunbookSnapshots-1", SpaceId = "Spaces-1" };
            var variables = new[] { new VariableIdentifier("Database.ConnectionString", "Projects-101") };

            repository.RunbookSnapshots.SnapshotVariablesByName(runbookSnapshot, variables, "concurrency-token-1");

            urlUsed.Should().Be(runbookRoute);
            parametersUsed.Should().BeEquivalentTo(new { spaceId = "Spaces-1", runbookSnapshotId = "RunbookSnapshots-1" });
            bodyUsed.Should().BeEquivalentTo(new { Variables = variables, VariableSnapshotConcurrencyToken = "concurrency-token-1" });
        }

        [Test]
        public async Task AsyncRunbookSnapshot_WithConcurrencyToken_PostsExpectedBody()
        {
            var client = Substitute.For<IOctopusAsyncClient>();
            var repository = new OctopusAsyncRepository(client);

            client.Post<object, RunbookSnapshotResource>(
                    Arg.Do<string>(x => urlUsed = x),
                    Arg.Do<object>(x => bodyUsed = x),
                    Arg.Do<object>(x => parametersUsed = x),
                    Arg.Any<CancellationToken>())
                .Returns(new RunbookSnapshotResource());

            var runbookSnapshot = new RunbookSnapshotResource { Id = "RunbookSnapshots-1", SpaceId = "Spaces-1" };
            var variables = new[] { new VariableIdentifier("Api.Key", "LibraryVariableSets-45") };

            await repository.RunbookSnapshots.SnapshotVariablesByName(runbookSnapshot, variables, "concurrency-token-1", CancellationToken.None);

            urlUsed.Should().Be(runbookRoute);
            parametersUsed.Should().BeEquivalentTo(new { spaceId = "Spaces-1", runbookSnapshotId = "RunbookSnapshots-1" });
            bodyUsed.Should().BeEquivalentTo(new { Variables = variables, VariableSnapshotConcurrencyToken = "concurrency-token-1" });
        }
    }
}
