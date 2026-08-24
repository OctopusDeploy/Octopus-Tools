using System;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories
{
    public interface IRunbookSnapshotRepository : IGet<RunbookSnapshotResource>, ICreate<RunbookSnapshotResource>, IPaginate<RunbookSnapshotResource>, IModify<RunbookSnapshotResource>, IDelete<RunbookSnapshotResource>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="runbookSnapshot"></param>
        /// <param name="skip">Number of records to skip</param>
        /// <param name="take">Number of records to take (First supported in Server 3.14.159)</param>
        /// <returns></returns>
        ResourceCollection<RunbookRunResource> GetRunbookRuns(RunbookSnapshotResource runbookSnapshot, int skip = 0, int? take = null);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="runbookSnapshot"></param>
        /// <param name="skip">Number of records to skip</param>
        /// <param name="take">Number of records to take (First supported in Server 3.14.159)</param>
        /// <returns></returns>
        ResourceCollection<ArtifactResource> GetArtifacts(RunbookSnapshotResource runbookSnapshot, int skip = 0, int? take = null);
        RunbookRunTemplateResource GetTemplate(RunbookSnapshotResource runbookSnapshot);
        RunbookRunPreviewResource GetPreview(DeploymentPromotionTarget promotionTarget);
        RunbookSnapshotResource SnapshotVariables(RunbookSnapshotResource runbookSnapshot);
        /// <summary>
        /// Refreshes the runbook snapshot's variable snapshot to the latest project and library variable values.
        /// </summary>
        /// <param name="runbookSnapshot"></param>
        /// <param name="variableSnapshotConcurrencyToken">
        /// The VariableSnapshotConcurrencyToken read from the runbook snapshot. When supplied, the update fails with a
        /// conflict if the snapshot's variable snapshots have changed since. Omit to skip the check.
        /// </param>
        RunbookSnapshotResource SnapshotVariables(RunbookSnapshotResource runbookSnapshot, string variableSnapshotConcurrencyToken);
        /// <summary>
        /// Updates only the named variables within the runbook snapshot's variable snapshot,
        /// leaving all other snapshotted variables untouched.
        /// </summary>
        /// <remarks>
        /// This API is experimental and controlled by toggle `partial-updates-on-variables`
        /// </remarks>
        /// <param name="runbookSnapshot"></param>
        /// <param name="variables"></param>
        /// <returns></returns>
        RunbookSnapshotResource SnapshotVariablesByName(RunbookSnapshotResource runbookSnapshot, VariableIdentifier[] variables);
        /// <summary>
        /// Updates only the named variables within the runbook snapshot's variable snapshot,
        /// leaving all other snapshotted variables untouched.
        /// </summary>
        /// <remarks>
        /// This API is experimental and controlled by toggle `partial-updates-on-variables`
        /// </remarks>
        /// <param name="runbookSnapshot"></param>
        /// <param name="variables"></param>
        /// <param name="variableSnapshotConcurrencyToken">
        /// The VariableSnapshotConcurrencyToken read from the runbook snapshot. When supplied, the update fails with a
        /// conflict if the snapshot's variable snapshots have changed since. Omit to skip the check.
        /// </param>
        RunbookSnapshotResource SnapshotVariablesByName(RunbookSnapshotResource runbookSnapshot, VariableIdentifier[] variables, string variableSnapshotConcurrencyToken);
        RunbookSnapshotResource Create(RunbookSnapshotResource runbookSnapshot);
    }

    class RunbookSnapshotRepository : BasicRepository<RunbookSnapshotResource>, IRunbookSnapshotRepository
    {
        public RunbookSnapshotRepository(IOctopusRepository repository)
            : base(repository, "RunbookSnapshots")
        {
        }

        public ResourceCollection<RunbookRunResource> GetRunbookRuns(RunbookSnapshotResource runbookSnapshot, int skip = 0, int? take = null)
        {
            return Client.List<RunbookRunResource>(runbookSnapshot.Link("RunbookRuns"), new { skip, take });
        }

        public ResourceCollection<ArtifactResource> GetArtifacts(RunbookSnapshotResource runbookSnapshot, int skip = 0, int? take = null)
        {
            return Client.List<ArtifactResource>(runbookSnapshot.Link("Artifacts"), new { skip, take });
        }

        public RunbookRunTemplateResource GetTemplate(RunbookSnapshotResource runbookSnapshot)
        {
            return Client.Get<RunbookRunTemplateResource>(runbookSnapshot.Link("RunbookRunTemplate"));
        }

        public RunbookRunPreviewResource GetPreview(DeploymentPromotionTarget promotionTarget)
        {
            return Client.Get<RunbookRunPreviewResource>(promotionTarget.Link("RunbookRunPreview"));
        }

        public RunbookSnapshotResource SnapshotVariables(RunbookSnapshotResource runbookSnapshot)
        {
            Client.Post(runbookSnapshot.Link("SnapshotVariables"));
            return Get(runbookSnapshot.Id);
        }

        public RunbookSnapshotResource SnapshotVariables(RunbookSnapshotResource runbookSnapshot, string variableSnapshotConcurrencyToken)
        {
            Client.Post(runbookSnapshot.Link("SnapshotVariables"), new { VariableSnapshotConcurrencyToken = variableSnapshotConcurrencyToken });
            return Get(runbookSnapshot.Id);
        }

        public RunbookSnapshotResource SnapshotVariablesByName(RunbookSnapshotResource runbookSnapshot, VariableIdentifier[] variables)
        {
            const string route = "~/api/{spaceId}/runbookSnapshots/{runbookSnapshotId}/snapshot-variables-by-name";
            return Client.Post<object, RunbookSnapshotResource>(
                route,
                new { Variables = variables },
                new { spaceId = runbookSnapshot.SpaceId, runbookSnapshotId = runbookSnapshot.Id }
                );
        }

        public RunbookSnapshotResource SnapshotVariablesByName(RunbookSnapshotResource runbookSnapshot, VariableIdentifier[] variables, string variableSnapshotConcurrencyToken)
        {
            const string route = "~/api/{spaceId}/runbookSnapshots/{runbookSnapshotId}/snapshot-variables-by-name";
            return Client.Post<object, RunbookSnapshotResource>(
                route,
                new { Variables = variables, VariableSnapshotConcurrencyToken = variableSnapshotConcurrencyToken },
                new { spaceId = runbookSnapshot.SpaceId, runbookSnapshotId = runbookSnapshot.Id }
                );
        }

        public RunbookSnapshotResource Create(RunbookSnapshotResource runbookSnapshot)
        {
            return Client.Create(Repository.Link(CollectionLinkName), runbookSnapshot);
        }
    }
}
