using Octopus.Client.Extensibility.Attributes;

namespace Octopus.Client.Model;

/// <summary>
/// The version control settings for the Platform Hub.
/// </summary>
public class PlatformHubVersionControlSettingsResource
{
    /// <summary>
    /// The URL of the Git repository used by the Platform Hub.
    /// An unconfigured Platform Hub has an empty URL.
    /// </summary>
    [Writeable]
    public string Url { get; set; } = string.Empty;

    [Writeable]
    public ProjectGitCredentialResource Credentials { get; set; } = new AnonymousProjectGitCredentialResource();

    [Writeable]
    public string DefaultBranch { get; set; }

    [Writeable]
    public string BasePath { get; set; }
}
