using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Octopus.Client.Extensibility;
using Octopus.Client.Extensibility.Attributes;

namespace Octopus.Client.Model
{
    public class RunbookSnapshotResource : Resource, ISnapshotResource, IHaveSpaceResource
    {
        [JsonConstructor]
        public RunbookSnapshotResource()
        {
            SelectedPackages = new List<SelectedPackage>();
            SelectedGitResources = new List<SelectedGitResource>();
        }

        public RunbookSnapshotResource(string projectId) : this()
        {
            ProjectId = projectId;
        }

        [Writeable]
        public string Name { get; set; }

        [WriteableOnCreate]
        public string RunbookId { get; set; }

        [Writeable]
        public string Notes { get; set; }

        public string FrozenRunbookProcessId { get; set; }

        public string FrozenProjectVariableSetId { get; set; }

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

        public SnapshotGitReferenceResource GitReference { get; set; }

        public string ProjectVariableSetSnapshotId { get; set; }

        /// <summary>
        /// Identifies the runbook snapshot's current variable snapshots.
        /// </summary>
        public string VariableSnapshotConcurrencyToken { get; set; }

        public string SpaceId { get; set; }
    }
}
