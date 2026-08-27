using Octopus.Client.Model;

namespace Octopus.Client.Repositories;

public interface IPlatformHubVersionControlSettingsRepository
{
    PlatformHubVersionControlSettingsResource Get();
    PlatformHubVersionControlSettingsResource Modify(PlatformHubVersionControlSettingsResource resource);
}

internal class PlatformHubVersionControlSettingsRepository : IPlatformHubVersionControlSettingsRepository
{
    private readonly IOctopusClient client;

    public PlatformHubVersionControlSettingsRepository(IOctopusClient client)
    {
        this.client = client;
    }

    public PlatformHubVersionControlSettingsResource Get()
    {
        return client.Get<PlatformHubVersionControlSettingsResource>(
            path: PlatformHubVersionControlSettingsRepositoryPathResolver.Path
        );
    }

    public PlatformHubVersionControlSettingsResource Modify(PlatformHubVersionControlSettingsResource resource)
    {
        var command = new ModifyPlatformHubVersionControlSettingsCommand(resource);
        client.Put(
            path: PlatformHubVersionControlSettingsRepositoryPathResolver.Path,
            resource: command
        );

        return Get();
    }
}

internal class ModifyPlatformHubVersionControlSettingsCommand
{
    public string Url { get; }
    public ProjectGitCredentialResource Credentials { get; }
    public string DefaultBranch { get; }
    public string BasePath { get; }

    public ModifyPlatformHubVersionControlSettingsCommand(PlatformHubVersionControlSettingsResource resource)
    {
        Url = resource.Url;
        Credentials = resource.Credentials;
        DefaultBranch = resource.DefaultBranch;
        BasePath = resource.BasePath;
    }
}

internal static class PlatformHubVersionControlSettingsRepositoryPathResolver
{
    public const string Path = "~/api/platformhub/versioncontrol";
}
