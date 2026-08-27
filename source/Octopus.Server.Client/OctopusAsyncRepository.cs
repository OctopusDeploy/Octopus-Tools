using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Exceptions;
using Octopus.Client.Model;
using Octopus.Client.Repositories.Async;
using Octopus.Client.Tasks;

namespace Octopus.Client
{
    public static partial class OctopusRepositoryExtensions
    {
        public static IOctopusAsyncRepository CreateRepository(this IOctopusAsyncClient client, RepositoryScope scope = null)
        {
            return new OctopusAsyncRepository(client, scope);
        }

        public static IOctopusSpaceAsyncRepository ForSpace(this IOctopusAsyncRepository repo, SpaceResource space)
        {
            return repo.Client.ForSpace(space);
        }

        public static IOctopusSystemAsyncRepository ForSystem(this IOctopusAsyncRepository repo)
        {
            return repo.Client.ForSystem();
        }
    }

    /// <summary>
    /// A simplified interface to commonly-used parts of the API.
    /// Functionality not exposed by this interface can be accessed
    /// using <see cref="IOctopusCommonAsyncRepository.Client" />.
    /// </summary>
    /// <remarks>
    /// Create using:
    /// <code>
    /// using(var client = new OctopusAsyncClient(new OctopusServerEndpoint("http://myoctopus/"))
    /// {
    ///     var repository = client.CreateRepository();
    /// }
    /// </code>
    /// </remarks>
    public class OctopusAsyncRepository : IOctopusAsyncRepository
    {
        internal static int SecondsToWaitForServerToStart = 60;
        private readonly AsyncLazy<RootResource> loadRootResource;
        private readonly Lazy<Task<SpaceRootResource>> loadSpaceRootResource;
        private static readonly string rootDocumentUri = "~/api";

        public OctopusAsyncRepository(IOctopusAsyncClient client, RepositoryScope repositoryScope = null)
        {
#if FULL_FRAMEWORK
            LocationChecker.CheckAssemblyLocation();
#endif
            Client = client;
            Scope = repositoryScope ?? RepositoryScope.Unspecified();
            Accounts = new AccountRepository(this);
            ActionTemplates = new ActionTemplateRepository(this);
            Artifacts = new ArtifactRepository(this);
            Backups = new BackupRepository(this);
            BuiltInPackageRepository = new BuiltInPackageRepositoryRepository(this);
            BuildInformationRepository = new BuildInformationRepository(this);
            CertificateConfiguration = new CertificateConfigurationRepository(this);
            Certificates = new CertificateRepository(this);
            Channels = new ChannelRepository(this);
            CommunityActionTemplates = new CommunityActionTemplateRepository(this);
            Configuration = new ConfigurationRepository(this);
            DashboardConfigurations = new DashboardConfigurationRepository(this);
            Dashboards = new DashboardRepository(this);
            Defects = new DefectsRepository(this);
            DeploymentProcesses = new DeploymentProcessRepository(this);
            DeploymentSettings = new DeploymentSettingsRepository(this);
            Deployments = new DeploymentRepository(this);
            Environments = new EnvironmentRepository(this);
            Events = new EventRepository(this);
            ArchivedEventFiles = new ArchivedEventFileRepository(this);
            FeaturesConfiguration = new FeaturesConfigurationRepository(this);
            Feeds = new FeedRepository(this);
            GitCredentials = new GitCredentialRepository(this);
            Interruptions = new InterruptionRepository(this);
            KubernetesMonitors = new KubernetesMonitorRepository(this);
            LibraryVariableSets = new LibraryVariableSetRepository(this);
            Licenses = new LicensesRepository(this);
            Lifecycles = new LifecyclesRepository(this);
            MachinePolicies = new MachinePolicyRepository(this);
            MachineRoles = new MachineRoleRepository(this);
            Machines = new MachineRepository(this);
            Migrations = new MigrationRepository(this);
            Observability = new ObservabilityRepository(this);
            OctopusServerNodes = new OctopusServerNodeRepository(this);
            PerformanceConfiguration = new PerformanceConfigurationRepository(this);
            PlatformHubPolicies = new PlatformHubPolicyRepository(client);
            PlatformHubVersionControlSettings = new PlatformHubVersionControlSettingsRepository(client);
            RateLimitingPolicies = new RateLimitingPolicyRepository(client);
            ProjectGroups = new ProjectGroupRepository(this);
            Projects = new ProjectRepository(this);
            Runbooks = new RunbookRepository(this);
            RunbookProcesses = new RunbookProcessRepository(this);
            RunbookSnapshots = new RunbookSnapshotRepository(this);
            RunbookRuns = new RunbookRunRepository(this);
            ProjectTriggers = new ProjectTriggerRepository(this);
            Proxies = new ProxyRepository(this);
            Releases = new ReleaseRepository(this);
            RetentionPolicies = new RetentionPolicyRepository(this);
            Schedulers = new SchedulerRepository(this);
            ServerStatus = new ServerStatusRepository(this);
            Spaces = new SpaceRepository(this);
            Subscriptions = new SubscriptionRepository(this);
            TagSets = new TagSetRepository(this);
            Tasks = new TaskRepository(this);
            Teams = new TeamsRepository(this);
            TelemetryConfigurationRepository = new TelemetryConfigurationRepository(this);
            Tenants = new TenantRepository(this);
            TenantVariables = new TenantVariablesRepository(this);
            UserInvites = new UserInvitesRepository(this);
            UserRoles = new UserRolesRepository(this);
            Users = new UserRepository(this);
            VariableSets = new VariableSetRepository(this);
            Workers = new WorkerRepository(this);
            WorkerPools = new WorkerPoolRepository(this);
            ScopedUserRoles = new ScopedUserRoleRepository(this);
            UserPermissions = new UserPermissionsRepository(this);
            UserTeams = new UserTeamsRepository(this);
            UpgradeConfiguration = new UpgradeConfigurationRepository(this);
            loadRootResource = new AsyncLazy<RootResource>(LoadRootDocumentInner);
            loadSpaceRootResource = new Lazy<Task<SpaceRootResource>>(LoadSpaceRootDocumentInner, true);
            DeploymentFreezes = new DeploymentFreezeRepository(client);
            SpaceDefaultRetentionPolicies = new SpaceDefaultRetentionPolicyRepository(client);
            SshKnownHosts = new SshKnownHostRepository(this);
        }


        public IOctopusAsyncClient Client { get; }
        public RepositoryScope Scope { get; private set; }
        public IAccountRepository Accounts { get; }
        public IActionTemplateRepository ActionTemplates { get; }
        public IArtifactRepository Artifacts { get; }
        public IBackupRepository Backups { get; }
        public IBuiltInPackageRepositoryRepository BuiltInPackageRepository { get; }
        public IBuildInformationRepository BuildInformationRepository { get; }
        public ICertificateConfigurationRepository CertificateConfiguration { get; }
        public ICertificateRepository Certificates { get; }
        public IChannelRepository Channels { get; }
        public ICommunityActionTemplateRepository CommunityActionTemplates { get; }
        public IConfigurationRepository Configuration { get; }
        public IDashboardConfigurationRepository DashboardConfigurations { get; }
        public IDashboardRepository Dashboards { get; }
        public IDefectsRepository Defects { get; }
        public IDeploymentProcessRepository DeploymentProcesses { get; }
        public IDeploymentSettingsRepository DeploymentSettings { get; }
        public IDeploymentRepository Deployments { get; }
        public IEnvironmentRepository Environments { get; }
        public IEventRepository Events { get; }
        public IArchivedEventFileRepository ArchivedEventFiles { get; }
        public IFeaturesConfigurationRepository FeaturesConfiguration { get; }
        public IFeedRepository Feeds { get; }
        public IGitCredentialRepository GitCredentials { get; }
        public IInterruptionRepository Interruptions { get; }
        public IKubernetesMonitorRepository KubernetesMonitors { get; }
        public ILibraryVariableSetRepository LibraryVariableSets { get; }
        public ILicensesRepository Licenses { get; }
        public ILifecyclesRepository Lifecycles { get; }
        public IMachinePolicyRepository MachinePolicies { get; }
        public IMachineRepository Machines { get; }
        public IMachineRoleRepository MachineRoles { get; }
        public IMigrationRepository Migrations { get; }
        public IObservabilityRepository Observability { get; }
        public IOctopusServerNodeRepository OctopusServerNodes { get; }
        public IPerformanceConfigurationRepository PerformanceConfiguration { get; }
        public IPlatformHubPolicyRepository PlatformHubPolicies { get; }
        public IPlatformHubVersionControlSettingsRepository PlatformHubVersionControlSettings { get; }
        public IRateLimitingPolicyRepository RateLimitingPolicies { get; }
        public IProjectGroupRepository ProjectGroups { get; }
        public IProjectRepository Projects { get; }
        public IRunbookRepository Runbooks { get; }
        public IRunbookProcessRepository RunbookProcesses { get; }
        public IRunbookSnapshotRepository RunbookSnapshots { get; }
        public IRunbookRunRepository RunbookRuns { get; }
        public IProjectTriggerRepository ProjectTriggers { get; }
        public IProxyRepository Proxies { get; }
        public IReleaseRepository Releases { get; }
        public IRetentionPolicyRepository RetentionPolicies { get; }
        public ISpaceDefaultRetentionPolicyRepository SpaceDefaultRetentionPolicies { get; }
        public ISchedulerRepository Schedulers { get; }
        public IServerStatusRepository ServerStatus { get; }
        public ISpaceRepository Spaces { get; }
        public ISubscriptionRepository Subscriptions { get; }
        public ITagSetRepository TagSets { get; }
        public ITaskRepository Tasks { get; }
        public ITeamsRepository Teams { get; }
        public ITenantRepository Tenants { get; }
        public ITenantVariablesRepository TenantVariables { get; }
        public IUserInvitesRepository UserInvites { get; }
        public IUserRepository Users { get; }
        public IUserRolesRepository UserRoles { get; }
        public IVariableSetRepository VariableSets { get; }
        public IWorkerPoolRepository WorkerPools { get; }
        public IWorkerRepository Workers { get; }
        public IScopedUserRoleRepository ScopedUserRoles { get; }
        public IUserPermissionsRepository UserPermissions { get; }
        public IUserTeamsRepository UserTeams { get; }
        public IUpgradeConfigurationRepository UpgradeConfiguration { get; }
        public ITelemetryConfigurationRepository TelemetryConfigurationRepository { get; }
        public IDeploymentFreezeRepository DeploymentFreezes { get; }
        public ISshKnownHostRepository SshKnownHosts { get; }

        public async Task<bool> HasLink(string name)
        {
            var rootDocument = await loadRootResource.Value(CancellationToken.None).ConfigureAwait(false);
            var spaceRootDocument = await loadSpaceRootResource.Value.ConfigureAwait(false);
            return spaceRootDocument != null && spaceRootDocument.HasLink(name) || rootDocument.HasLink(name);
        }

        public async Task<bool> HasLinkParameter(string linkName, string parameterName)
        {
            string link;
            var spaceRootDocument = await loadSpaceRootResource.Value.ConfigureAwait(false);

            if (spaceRootDocument != null && spaceRootDocument.HasLink(linkName))
                link = spaceRootDocument.Link(linkName);
            else
            {
                var rootDocument = await loadRootResource.Value(CancellationToken.None).ConfigureAwait(false);
                if (rootDocument.HasLink(linkName))
                    link = rootDocument.Link(linkName);
                else
                    return false;
            }

            var template = new UrlTemplate(link);
            return template.GetParameterNames().Contains(parameterName);
        }

        public async Task<string> Link(string name)
        {
            var rootDocument = await loadRootResource.Value(CancellationToken.None).ConfigureAwait(false);
            var spaceRootDocument = await loadSpaceRootResource.Value.ConfigureAwait(false);
            return spaceRootDocument != null && spaceRootDocument.Links.TryGetValue(name, out var value)
                ? value.AsString()
                : rootDocument.Link(name);
        }

        public Task<RootResource> LoadRootDocument() => LoadRootDocument(CancellationToken.None);
        public Task<RootResource> LoadRootDocument(CancellationToken cancellationToken) => loadRootResource.Value(cancellationToken);
        public Task<SpaceRootResource> LoadSpaceRootDocument() => loadSpaceRootResource.Value;

        private async Task<RootResource> LoadRootDocumentInner(CancellationToken cancellationToken)
        {
            var watch = Stopwatch.StartNew();
            Exception lastError = null;

            RootResource rootDocument;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (watch.Elapsed > TimeSpan.FromSeconds(SecondsToWaitForServerToStart))
                {
                    if (lastError == null)
                    {
                        throw new Exception("Unable to connect to the Octopus Deploy server.");
                    }

                    throw new Exception("Unable to connect to the Octopus Deploy server. See the inner exception for details.", lastError);
                }

                try
                {
                    rootDocument = await Client.Get<RootResource>(rootDocumentUri, cancellationToken).ConfigureAwait(false);
                    break;
                }
                catch (HttpRequestException ex)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
                    lastError = ex;
                }
                catch (OctopusServerException ex)
                {
                    if (ex.HttpStatusCode != 503)
                    {
                        // 503 means the service is starting, so give it some time to start
                        throw;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(0.5), cancellationToken).ConfigureAwait(false);
                    lastError = ex;
                }
            }

            if (string.IsNullOrWhiteSpace(rootDocument.ApiVersion))
                throw new UnsupportedApiVersionException("This Octopus Deploy server uses an older API specification than this tool can handle. Please check for updates to the Octo tool.");

            var min = SemanticVersion.Parse(ApiConstants.SupportedApiSchemaVersionMin);
            var max = SemanticVersion.Parse(ApiConstants.SupportedApiSchemaVersionMax);
            var current = SemanticVersion.Parse(rootDocument.ApiVersion);

            if (current < min || current > max)
                throw new UnsupportedApiVersionException($"This Octopus Deploy server uses a newer API specification ({rootDocument.ApiVersion}) than this tool can handle ({ApiConstants.SupportedApiSchemaVersionMin} to {ApiConstants.SupportedApiSchemaVersionMax}). Please check for updates to this tool.");

            return rootDocument;
        }

        private Task<SpaceRootResource> LoadSpaceRootDocumentInner()
        {
            return Scope.Apply(LoadSpaceRootResourceFor,
                () => Task.FromResult<SpaceRootResource>(null),
                async () =>
                {
                    var defaultSpace = await TryGetDefaultSpace().ConfigureAwait(false);
                    return defaultSpace != null
                        ? await LoadSpaceRootResourceFor(defaultSpace).ConfigureAwait(false)
                        : null;
                });

            async Task<SpaceRootResource> LoadSpaceRootResourceFor(SpaceResource space)
            {
                return await Client.Get<SpaceRootResource>(space.Link("SpaceHome"), new { space.Id }).ConfigureAwait(false);
            }

            async Task<SpaceResource> TryGetDefaultSpace()
            {
                var rootDocument = await loadRootResource.Value(CancellationToken.None).ConfigureAwait(false);
                var spacesIsSupported = rootDocument.HasLink("Spaces");
                if (!spacesIsSupported)
                {
                    return null;
                }
                try
                {
                    var currentUser = await Client.Get<UserResource>(rootDocument.Links["CurrentUser"]).ConfigureAwait(false);
                    var userSpaces = await Client.Get<SpaceResource[]>(currentUser.Links["Spaces"]).ConfigureAwait(false);
                    return userSpaces.SingleOrDefault(s => s.IsDefault);
                }
                catch (OctopusSecurityException)
                {
                    return null;
                }
            }
        }
    }
}
