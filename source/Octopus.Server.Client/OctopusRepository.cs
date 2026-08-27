using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Octopus.Client.Exceptions;
using Octopus.Client.Model;
using Octopus.Client.Repositories;

namespace Octopus.Client
{
    public static partial class OctopusRepositoryExtensions
    {
        public static IOctopusSpaceRepository ForSpace(this IOctopusRepository repo, SpaceResource space)
        {
            return repo.Client.ForSpace(space);
        }

        public static IOctopusSystemRepository ForSystem(this IOctopusRepository repo)
        {
            return repo.Client.ForSystem();
        }
    }

    /// <summary>
    /// A simplified interface to commonly-used parts of the API.
    /// Functionality not exposed by this interface can be accessed
    /// using <see cref="IOctopusCommonRepository.Client" />.
    /// </summary>
    /// <remarks>
    /// Create using:
    /// <code>
    /// var repository = new OctopusRepository(new OctopusServerEndpoint("http://myoctopus/"));
    /// </code>
    /// </remarks>
    public class OctopusRepository : IOctopusRepository
    {
        private readonly Lazy<RootResource> loadRootResource;
        private readonly Lazy<SpaceRootResource> loadSpaceRootResource;

        public OctopusRepository(OctopusServerEndpoint endpoint) : this(new OctopusClient(endpoint))
        {
        }

        public OctopusRepository(OctopusServerEndpoint endpoint, RepositoryScope repositoryScope) : this(new OctopusClient(endpoint), repositoryScope)
        {
        }

        public OctopusRepository(IOctopusClient client, RepositoryScope repositoryScope = null)
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
            Interruptions = new InterruptionRepository(this);
            KubernetesMonitors = new KubernetesMonitorRepository(this);
            LibraryVariableSets = new LibraryVariableSetRepository(this);
            Lifecycles = new LifecyclesRepository(this);
            Licenses = new LicensesRepository(this);
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
            UserRoles = new UserRolesRepository(this);
            Users = new UserRepository(this);
            VariableSets = new VariableSetRepository(this);
            Workers = new WorkerRepository(this);
            WorkerPools = new WorkerPoolRepository(this);
            ScopedUserRoles = new ScopedUserRoleRepository(this);
            UserPermissions = new UserPermissionsRepository(this);
            UserTeams = new UserTeamsRepository(this);
            UserInvites = new UserInvitesRepository(this);
            UpgradeConfiguration = new UpgradeConfigurationRepository(this);
            loadRootResource = new Lazy<RootResource>(LoadRootDocumentInner, true);
            loadSpaceRootResource = new Lazy<SpaceRootResource>(LoadSpaceRootDocumentInner, true);
            DeploymentFreezes = new DeploymentFreezeRepository(client);
            SpaceDefaultRetentionPolicies = new SpaceDefaultRetentionPolicyRepository(client);
            SshKnownHosts = new SshKnownHostRepository(this);
        }

        public IOctopusClient Client { get; }
        public RepositoryScope Scope { get; }
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
        public IInterruptionRepository Interruptions { get; }
        public IKubernetesMonitorRepository KubernetesMonitors { get; }
        public ILibraryVariableSetRepository LibraryVariableSets { get; }
        public ILifecyclesRepository Lifecycles { get; }
        public ILicensesRepository Licenses { get; }
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
        public ISpaceDefaultRetentionPolicyRepository SpaceDefaultRetentionPolicies { get; }
        public ISshKnownHostRepository SshKnownHosts { get; }


        public bool HasLink(string name)
        {
            return loadSpaceRootResource.Value != null && loadSpaceRootResource.Value.HasLink(name) || loadRootResource.Value.HasLink(name);
        }

        public bool HasLinkParameter(string linkName, string parameterName)
        {
            string link;
            if (loadSpaceRootResource.Value != null && loadSpaceRootResource.Value.HasLink(linkName))
                link = loadSpaceRootResource.Value.Link(linkName);
            else if (loadRootResource.Value.HasLink(linkName))
                link = loadRootResource.Value.Link(linkName);
            else
                return false;

            var template = new UrlTemplate(link);
            return template.GetParameterNames().Contains(parameterName);
        }

        public string Link(string name)
        {
            return loadSpaceRootResource.Value != null && loadSpaceRootResource.Value.Links.TryGetValue(name, out var value)
                ? value.AsString()
                : loadRootResource.Value.Link(name);
        }

        public RootResource LoadRootDocument() => loadRootResource.Value;
        public SpaceRootResource LoadSpaceRootDocument() => loadSpaceRootResource.Value;

        RootResource LoadRootDocumentInner()
        {
            var watch = Stopwatch.StartNew();
            Exception lastError = null;

            // 60 second limit using Stopwatch alone makes debugging impossible.
            var retries = 3;

            RootResource rootDocument;
            while (true)
            {
                if (retries <= 0 && TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds) > TimeSpan.FromSeconds(60))
                {
                    if (lastError == null)
                    {
                        throw new Exception("Unable to connect to the Octopus Deploy server.");
                    }

                    throw new Exception("Unable to connect to the Octopus Deploy server. See the inner exception for details.", lastError);
                }

                try
                {
                    rootDocument = Client.Get<RootResource>("~/api");
                    break;
                }
                catch (WebException ex)
                {
                    Thread.Sleep(1000);
                    lastError = ex;
                }
                catch (OctopusServerException ex)
                {
                    if (ex.HttpStatusCode != 503)
                    {
                        // 503 means the service is starting, so give it some time to start
                        throw;
                    }

                    Thread.Sleep(500);
                    lastError = ex;
                }
                retries--;
            }

            if (string.IsNullOrWhiteSpace(rootDocument.ApiVersion))
                throw new UnsupportedApiVersionException("This Octopus Deploy server uses an older API specification than this tool can handle. Please check for updates to the Octo tool.");

            var min = SemanticVersion.Parse(ApiConstants.SupportedApiSchemaVersionMin);
            var max = SemanticVersion.Parse(ApiConstants.SupportedApiSchemaVersionMax);
            var current = SemanticVersion.Parse(rootDocument.ApiVersion);

            if (current < min || current > max)
                throw new UnsupportedApiVersionException(string.Format("This Octopus Deploy server uses a newer API specification ({0}) than this tool can handle ({1} to {2}). Please check for updates to this tool.", rootDocument.ApiVersion, ApiConstants.SupportedApiSchemaVersionMin, ApiConstants.SupportedApiSchemaVersionMax));

            return rootDocument;
        }

        SpaceRootResource LoadSpaceRootDocumentInner()
        {
            return Scope.Apply(LoadSpaceRootResourceFor,
                () => null,
                () =>
                {
                    var defaultSpace = TryGetDefaultSpace();
                    return defaultSpace != null ? LoadSpaceRootResourceFor(defaultSpace) : null;
                });

            SpaceRootResource LoadSpaceRootResourceFor(SpaceResource space)
            {
                return Client.Get<SpaceRootResource>(space.Link("SpaceHome"), new { space.Id });
            }

            SpaceResource TryGetDefaultSpace()
            {
                var rootResource = loadRootResource.Value;
                var spacesIsSupported = rootResource.HasLink("Spaces");
                if (!spacesIsSupported)
                    return null;
                try
                {
                    var currentUser = Client.Get<UserResource>(rootResource.Links["CurrentUser"]);
                    var userSpaces = Client.Get<SpaceResource[]>(currentUser.Links["Spaces"]);
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
