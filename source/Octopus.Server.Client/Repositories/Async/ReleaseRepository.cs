using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface IReleaseRepository : IGet<ReleaseResource>, ICreate<ReleaseResource>, IPaginate<ReleaseResource>, IModify<ReleaseResource>, IDelete<ReleaseResource>
    {
        /// <param name="release"></param>
        /// <param name="skip">Number of records to skip</param>
        /// <param name="take">Number of records to take (First supported in Server 3.14.15)</param>
        [Obsolete("Please use the overload with cancellation token instead.", false)]
        Task<ResourceCollection<DeploymentResource>> GetDeployments(ReleaseResource release, int skip = 0, int? take = null);

        /// <param name="release"></param>
        /// <param name="cancellationToken">Request cancellation token</param>
        Task<ResourceCollection<DeploymentResource>> GetDeployments(ReleaseResource release, CancellationToken cancellationToken);

        /// <param name="release"></param>
        /// <param name="skip">Number of records to skip</param>
        /// <param name="take">Number of records to take (First supported in Server 3.14.15)</param>
        /// <param name="cancellationToken">Request cancellation token</param>
        Task<ResourceCollection<DeploymentResource>> GetDeployments(ReleaseResource release, int skip, int? take, CancellationToken cancellationToken);


        /// <param name="release"></param>
        /// <param name="skip">Number of records to skip</param>
        /// <param name="take">Number of records to take (First supported in Server 3.14.15)</param>
        [Obsolete("Please use the overload with cancellation token instead.", false)]
        Task<ResourceCollection<ArtifactResource>> GetArtifacts(ReleaseResource release, int skip = 0, int? take = null);

        /// <param name="release"></param>
        /// <param name="cancellationToken">Request cancellation token</param>
        Task<ResourceCollection<ArtifactResource>> GetArtifacts(ReleaseResource release, CancellationToken cancellationToken);

        /// <param name="release"></param>
        /// <param name="skip">Number of records to skip</param>
        /// <param name="take">Number of records to take (First supported in Server 3.14.15)</param>
        /// <param name="cancellationToken">Request cancellation token</param>
        Task<ResourceCollection<ArtifactResource>> GetArtifacts(ReleaseResource release, int skip, int? take, CancellationToken cancellationToken);

        [Obsolete("Please use the overload with cancellation token instead.", false)]
        Task<DeploymentTemplateResource> GetTemplate(ReleaseResource release);
        Task<DeploymentTemplateResource> GetTemplate(ReleaseResource release, CancellationToken cancellationToken);

        [Obsolete("Please use the overload with cancellation token instead.", false)]
        Task<DeploymentPreviewResource> GetPreview(DeploymentPromotionTarget promotionTarget);
        Task<DeploymentPreviewResource> GetPreview(DeploymentPromotionTarget promotionTarget, CancellationToken cancellationToken);

        [Obsolete("Please use the overload with cancellation token instead.", false)]
        Task<ReleaseResource> SnapshotVariables(ReleaseResource release);
        Task<ReleaseResource> SnapshotVariables(ReleaseResource release, CancellationToken cancellationToken);
        /// <param name="release">release to have its snapshot updated</param>
        /// <param name="variableSnapshotConcurrencyToken">
        /// The VariableSnapshotConcurrencyToken read from the release. When supplied, the update fails with a
        /// conflict if the release's variable snapshots have changed since. Omit to skip the check.
        /// </param>
        /// <param name="cancellationToken">Request cancellation token</param>
        Task<ReleaseResource> SnapshotVariables(ReleaseResource release, string variableSnapshotConcurrencyToken, CancellationToken cancellationToken);
        Task<ReleaseResource> SnapshotVariablesByName(ReleaseResource release, VariableIdentifier[] variables, CancellationToken cancellationToken);
        /// <param name="release">release to have its snapshot updated</param>
        /// <param name="variables">identifiers for Variables to be updated</param>
        /// <param name="variableSnapshotConcurrencyToken">
        /// The VariableSnapshotConcurrencyToken read from the release. When supplied, the update fails with a
        /// conflict if the release's variable snapshots have changed since. Omit to skip the check.
        /// </param>
        /// <param name="cancellationToken">Request cancellation token</param>
        Task<ReleaseResource> SnapshotVariablesByName(ReleaseResource release, VariableIdentifier[] variables, string variableSnapshotConcurrencyToken, CancellationToken cancellationToken);

        [Obsolete("Please use the overload with cancellation token instead.", false)]
        Task<ReleaseResource> Create(ReleaseResource release, bool ignoreChannelRules = false);
        Task<ReleaseResource> Create(ReleaseResource release, bool ignoreChannelRules, CancellationToken cancellationToken);

        [Obsolete("Please use the overload with cancellation token instead.", false)]
        Task<LifecycleProgressionResource> GetProgression(ReleaseResource release);
        Task<LifecycleProgressionResource> GetProgression(ReleaseResource release, CancellationToken cancellationToken);

        Task<GetMissingPackagesForReleaseResponse> GetMissingPackagesForRelease(GetMissingPackagesForReleaseRequest request, CancellationToken cancellationToken);
    }

    class ReleaseRepository : BasicRepository<ReleaseResource>, IReleaseRepository
    {
        public ReleaseRepository(IOctopusAsyncRepository repository)
            : base(repository, "Releases")
        {
        }

        public Task<ResourceCollection<DeploymentResource>> GetDeployments(ReleaseResource release, int skip = 0, int? take = null)
        {
            return GetDeployments(release, skip: skip, take: take, CancellationToken.None);
        }

        public Task<ResourceCollection<DeploymentResource>> GetDeployments(ReleaseResource release, CancellationToken cancellationToken)
        {
            return GetDeployments(release, 0, null, cancellationToken);
        }

        public Task<ResourceCollection<DeploymentResource>> GetDeployments(ReleaseResource release, int skip, int? take, CancellationToken cancellationToken)
        {
            return Client.List<DeploymentResource>(release.Link("Deployments"), new { skip, take }, cancellationToken);
        }

        public Task<ResourceCollection<ArtifactResource>> GetArtifacts(ReleaseResource release, int skip = 0, int? take = null)
        {
            return GetArtifacts(release, skip: skip, take: take, cancellationToken: CancellationToken.None);
        }

        public Task<ResourceCollection<ArtifactResource>> GetArtifacts(ReleaseResource release, CancellationToken cancellationToken)
        {
            return GetArtifacts(release, 0, null, cancellationToken);
        }

        public Task<ResourceCollection<ArtifactResource>> GetArtifacts(ReleaseResource release, int skip, int? take, CancellationToken cancellationToken)
        {
            return Client.List<ArtifactResource>(release.Link("Artifacts"), new { skip, take }, cancellationToken);
        }

        public Task<DeploymentTemplateResource> GetTemplate(ReleaseResource release)
        {
            return GetTemplate(release, CancellationToken.None);
        }

        public Task<DeploymentTemplateResource> GetTemplate(ReleaseResource release, CancellationToken cancellationToken)
        {
            return Client.Get<DeploymentTemplateResource>(release.Link("DeploymentTemplate"), null, cancellationToken);
        }

        public Task<DeploymentPreviewResource> GetPreview(DeploymentPromotionTarget promotionTarget, CancellationToken cancellationToken)
        {
            return Client.Get<DeploymentPreviewResource>(promotionTarget.Link("Preview"), null, cancellationToken);
        }

        public Task<DeploymentPreviewResource> GetPreview(DeploymentPromotionTarget promotionTarget)
        {
            return GetPreview(promotionTarget, CancellationToken.None);
        }

        public async Task<ReleaseResource> SnapshotVariables(ReleaseResource release)
        {
            return await SnapshotVariables(release, CancellationToken.None);
        }

        public async Task<ReleaseResource> SnapshotVariables(ReleaseResource release, CancellationToken cancellationToken)
        {
            await Client.Post(release.Link("SnapshotVariables"), cancellationToken).ConfigureAwait(false);
            return await Get(release.Id).ConfigureAwait(false);
        }

        public async Task<ReleaseResource> SnapshotVariables(ReleaseResource release, string variableSnapshotConcurrencyToken, CancellationToken cancellationToken)
        {
            await Client.Post(release.Link("SnapshotVariables"), new { VariableSnapshotConcurrencyToken = variableSnapshotConcurrencyToken }, cancellationToken).ConfigureAwait(false);
            return await Get(release.Id).ConfigureAwait(false);
        }

        public async Task<ReleaseResource> SnapshotVariablesByName(ReleaseResource release, VariableIdentifier[] variables, CancellationToken cancellationToken)
        {
            const string route = "~/api/{spaceId}/releases/{releaseId}/snapshot-variables-by-name";
            return await Client.Post<object, ReleaseResource>(
            route,
            new { Variables = variables },
            new { spaceId = release.SpaceId, releaseId = release.Id },
            cancellationToken).ConfigureAwait(false);
        }

        public async Task<ReleaseResource> SnapshotVariablesByName(ReleaseResource release, VariableIdentifier[] variables, string variableSnapshotConcurrencyToken, CancellationToken cancellationToken)
        {
            const string route = "~/api/{spaceId}/releases/{releaseId}/snapshot-variables-by-name";
            return await Client.Post<object, ReleaseResource>(
            route,
            new { Variables = variables, VariableSnapshotConcurrencyToken = variableSnapshotConcurrencyToken },
            new { spaceId = release.SpaceId, releaseId = release.Id },
            cancellationToken).ConfigureAwait(false);
        }

        public async Task<ReleaseResource> Create(ReleaseResource release, bool ignoreChannelRules = false)
        {
            return await Create(release, ignoreChannelRules, CancellationToken.None);
        }

        public async Task<ReleaseResource> Create(ReleaseResource release, bool ignoreChannelRules, CancellationToken cancellationToken)
        {
            return await Client.Create(await Repository.Link(CollectionLinkName).ConfigureAwait(false), release, new { ignoreChannelRules }, cancellationToken).ConfigureAwait(false);
        }

        public Task<LifecycleProgressionResource> GetProgression(ReleaseResource release)
        {
            return GetProgression(release, CancellationToken.None);
        }

        public Task<LifecycleProgressionResource> GetProgression(ReleaseResource release, CancellationToken cancellationToken)
        {
            return Client.Get<LifecycleProgressionResource>(release.Links["Progression"], null, cancellationToken);
        }

        public Task<GetMissingPackagesForReleaseResponse> GetMissingPackagesForRelease(
            GetMissingPackagesForReleaseRequest request,
            CancellationToken cancellationToken)
        {
            const string link = "/api/{spaceId}/releases/{releaseId}/missingpackages";
            return Client.Get<GetMissingPackagesForReleaseResponse>(link, new { request.SpaceId, request.ReleaseId }, cancellationToken);
        }
    }
}
