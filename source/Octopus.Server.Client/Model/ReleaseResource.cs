using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Octopus.Client.Extensibility.Attributes;
using Newtonsoft.Json;
using Octopus.Client.Extensibility;
using Octopus.Client.Model.BuildInformation;

namespace Octopus.Client.Model
{
    public class ReleaseResource : Resource, ISnapshotResource, IHaveSpaceResource
    {
        [JsonConstructor]
        public ReleaseResource()
        {
            SelectedPackages = new List<SelectedPackage>();
            BuildInformation = new List<ReleasePackageVersionBuildInformationResource>();
        }

        public ReleaseResource(string version, string projectId, string channelId)
            : this()
        {
            Version = version;
            ProjectId = projectId;
            ChannelId = channelId;
        }

        public ReleaseResource(string version, string projectId)
            : this(version, projectId, null)
        {
        }

        public ReleaseResource(string projectId)
            : this(null, projectId, null)
        {
        }

        [Required(ErrorMessage = "Please provide a version number for this release.")]
        [StringLength(349, ErrorMessage = "The version number is too long. Please enter a shorter version number.")]
        [Trim]
        [Writeable]
        public string Version { get; set; }

        [Writeable]
        public string ChannelId { get; set; }

        [Writeable]
        public string ReleaseNotes { get; set; }

        public string ProjectDeploymentProcessSnapshotId { get; set; }

        public string ProjectVariableSetSnapshotId { get; set; }

        /// <summary>
        /// Identifies the release's current variable snapshots.
        /// </summary>
        public string VariableSnapshotConcurrencyToken { get; set; }

        [Writeable]
        [NotReadable]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool IgnoreChannelRules { get; set; }

        [WriteableOnCreate]
        public List<ReleasePackageVersionBuildInformationResource> BuildInformation { get; set; }

        public DateTimeOffset Assembled { get; set; }

        [WriteableOnCreate]
        public string ProjectId { get; set; }

        /// <summary>
        /// Snapshots of the project's included library variable sets. The
        /// snapshots are <see cref="VariableSetResource" />s, not <see cref="LibraryVariableSetResource" />s.
        /// </summary>
        public List<string> LibraryVariableSetSnapshotIds { get; set; }

        public List<SelectedPackage> SelectedPackages { get; set; }

        public List<SelectedGitResource> SelectedGitResources { get; set; }

        [JsonProperty("VersionControlReference")]
        public SnapshotGitReferenceResource GitReference { get; set; }

        public string SpaceId { get; set; }
    }
}
