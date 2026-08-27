using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async;

public interface IPlatformHubVersionControlSettingsRepository
{
    Task<PlatformHubVersionControlSettingsResource> Get(CancellationToken cancellationToken);
    Task<PlatformHubVersionControlSettingsResource> Modify(PlatformHubVersionControlSettingsResource resource, CancellationToken cancellationToken);
}

internal class PlatformHubVersionControlSettingsRepository : IPlatformHubVersionControlSettingsRepository
{
    private readonly IOctopusAsyncClient client;

    public PlatformHubVersionControlSettingsRepository(IOctopusAsyncClient client)
    {
        this.client = client;
    }

    public async Task<PlatformHubVersionControlSettingsResource> Get(CancellationToken cancellationToken)
    {
        return await client
            .Get<PlatformHubVersionControlSettingsResource>(
                path: PlatformHubVersionControlSettingsRepositoryPathResolver.Path,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<PlatformHubVersionControlSettingsResource> Modify(PlatformHubVersionControlSettingsResource resource, CancellationToken cancellationToken)
    {
        var command = new ModifyPlatformHubVersionControlSettingsCommand(resource);
        await client
            .Put(
                path: PlatformHubVersionControlSettingsRepositoryPathResolver.Path,
                resource: command,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        return await Get(cancellationToken);
    }
}
