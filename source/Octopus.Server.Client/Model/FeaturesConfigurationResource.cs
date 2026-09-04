using System;
using Octopus.Client.Extensibility.Attributes;

namespace Octopus.Client.Model
{
    public class FeaturesConfigurationResource : Resource
    {
        [Writeable]
        public bool IsBuiltInWorkerEnabled { get; set; }

        [Writeable]
        public bool IsCommunityActionTemplatesEnabled { get; set; }

        [Writeable]
        public bool IsHelpSidebarEnabled { get; set; }

        [Writeable]
        public string HelpSidebarSupportLink { get; set; }

        [Writeable]
        public string HelpSidebarSupportLinkLabel { get; set; }

        [Writeable]
        public bool IsKubernetesCloudTargetDiscoveryEnabled { get; set; }

        [Writeable]
        public bool IsCompositeDockerHubRegistryFeedEnabled { get; set; }

        [Writeable]
        public bool IsConfigureFeedsWithLocalOrSmbPathsEnabled { get; set; }

        [Writeable]
        public bool IsNavigationVisualUpliftEnabled { get; set; }

        [Writeable]
        public bool IsProjectsPageOptimizationEnabled { get; set; }

        [Writeable]
        public bool IsProjectsPageOnboardingEnabled { get; set; }

        [Writeable]
        public bool IsGitHubAppEnabled { get; set; }

        [Writeable]
        public string DefaultPowerShellEdition { get; set; }

        [Writeable]
        public bool IsWebhookTriggerEnabled { get; set; }
    }
}
