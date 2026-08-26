#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics;
using Octopus.Client.Serialization;

namespace Octopus.Client.Model
{
    /// <summary>
    /// Permissions are restricted via roles; a role may be restricted in a way that some of the
    /// included permissions are not. So, when permission sets are loaded we only set up restrictions
    /// that are supported by the permission type.
    /// </summary>
    [DebuggerDisplay("Id")]
    [TypeConverter(typeof(PermissionTypeConverter))]
    public class Permission : IEquatable<Permission>
    {
        [Description("Perform system-level functions like configuring HTTP web hosting, the public URL, server nodes, maintenance mode, and server diagnostics")] public static readonly Permission AdministerSystem = new Permission("AdministerSystem");

        [Description("Edit project details")][SupportsRestriction(PermissionScope.Projects, PermissionScope.Tenants)] public static readonly Permission ProjectEdit = new Permission("ProjectEdit");

        [Description("View the details of projects")][SupportsRestriction(PermissionScope.Projects, PermissionScope.Tenants)] public static readonly Permission ProjectView = new Permission("ProjectView");

        [Description("Create projects")][SupportsRestriction(PermissionScope.ProjectGroups)] public static readonly Permission ProjectCreate = new Permission("ProjectCreate");

        [Description("Delete projects")][SupportsRestriction(PermissionScope.Projects, PermissionScope.Tenants)] public static readonly Permission ProjectDelete = new Permission("ProjectDelete");

        [Description("View the deployment process and channels associated with a project")][SupportsRestriction(PermissionScope.Projects)] public static readonly Permission ProcessView = new Permission("ProcessView");

        [Description("Edit the deployment process and channels associated with a project")][SupportsRestriction(PermissionScope.Projects)] public static readonly Permission ProcessEdit = new Permission("ProcessEdit");

        [Description("Edit variables belonging to a project")][SupportsRestriction(PermissionScope.Projects, PermissionScope.Environments)] public static readonly Permission VariableEdit = new Permission("VariableEdit");

        [Description("Edit non-environment scoped variables belonging to a project or library variable set")][SupportsRestriction(PermissionScope.Projects)] public static readonly Permission VariableEditUnscoped = new Permission("VariableEditUnscoped");

        [Description("View variables belonging to a project or library variable set")][SupportsRestriction(PermissionScope.Projects, PermissionScope.Environments)] public static readonly Permission VariableView = new Permission("VariableView");

        [Description("View non-environment scoped variables belonging to a project or library variable set")][SupportsRestriction(PermissionScope.Projects)] public static readonly Permission VariableViewUnscoped = new Permission("VariableViewUnscoped");

        [Description("Create a release for a project")][SupportsRestriction(PermissionScope.Projects, PermissionScope.Tenants)] public static readonly Permission ReleaseCreate = new Permission("ReleaseCreate");

        [Description("View a release of a project")][SupportsRestriction(PermissionScope.Projects, PermissionScope.Tenants)] public static readonly Permission ReleaseView = new Permission("ReleaseView");

        [Description("Edit a release of a project")][SupportsRestriction(PermissionScope.Projects, PermissionScope.Tenants)] public static readonly Permission ReleaseEdit = new Permission("ReleaseEdit");

        [Description("Delete a release of a project")][SupportsRestriction(PermissionScope.Projects, PermissionScope.Tenants)] public static readonly Permission ReleaseDelete = new Permission("ReleaseDelete");

        [Description("Block a release from progressing to the next lifecycle phase")][SupportsRestriction(PermissionScope.Projects)] public static readonly Permission DefectReport = new Permission("DefectReport");

        [Description("Unblock a release so it can progress to the next phase")][SupportsRestriction(PermissionScope.Projects)] public static readonly Permission DefectResolve = new Permission("DefectResolve");

        [Description("Deploy releases to target environments")][SupportsRestriction(PermissionScope.Projects, PermissionScope.Environments, PermissionScope.Tenants, ExplicitTenantScopeRequired = true)] public static readonly Permission DeploymentCreate = new Permission("DeploymentCreate");

        [Description("Delete deployments")][SupportsRestriction(PermissionScope.Projects, PermissionScope.Environments, PermissionScope.Tenants, ExplicitTenantScopeRequired = true)] public static readonly Permission DeploymentDelete = new Permission("DeploymentDelete");

        [Description("View deployments")][SupportsRestriction(PermissionScope.Projects, PermissionScope.Environments, PermissionScope.Tenants, ExplicitTenantScopeRequired = true)] public static readonly Permission DeploymentView = new Permission("DeploymentView");

        [Description("View environments")][SupportsRestriction(PermissionScope.Environments)] public static readonly Permission EnvironmentView = new Permission("EnvironmentView");

        [Description("Create environments")][SupportsRestriction(PermissionScope.Environments)] public static readonly Permission EnvironmentCreate = new Permission("EnvironmentCreate");

        [Description("Edit environments")][SupportsRestriction(PermissionScope.Environments)] public static readonly Permission EnvironmentEdit = new Permission("EnvironmentEdit");

        [Description("Delete environments")][SupportsRestriction(PermissionScope.Environments)] public static readonly Permission EnvironmentDelete = new Permission("EnvironmentDelete");

        [Description("Create machines")][SupportsRestriction(PermissionScope.Environments, PermissionScope.Tenants)] public static readonly Permission MachineCreate = new Permission("MachineCreate");

        [Description("Edit machines")][SupportsRestriction(PermissionScope.Environments, PermissionScope.Tenants)] public static readonly Permission MachineEdit = new Permission("MachineEdit");

        [Description("View machines")][SupportsRestriction(PermissionScope.Environments, PermissionScope.Tenants)] public static readonly Permission MachineView = new Permission("MachineView");

        [Description("Delete machines")][SupportsRestriction(PermissionScope.Environments, PermissionScope.Tenants)] public static readonly Permission MachineDelete = new Permission("MachineDelete");

        [Description("View the artifacts created manually and during deployment")][SupportsRestriction(PermissionScope.Projects, PermissionScope.Environments, PermissionScope.Tenants, ExplicitTenantScopeRequired = true)] public static readonly Permission ArtifactView = new Permission("ArtifactView");

        [Description("Manually create artifacts")][SupportsRestriction(PermissionScope.Projects, PermissionScope.Environments, PermissionScope.Tenants, ExplicitTenantScopeRequired = true)] public static readonly Permission ArtifactCreate = new Permission("ArtifactCreate");

        [Description("Edit the details describing artifacts")][SupportsRestriction(PermissionScope.Projects, PermissionScope.Environments, PermissionScope.Tenants, ExplicitTenantScopeRequired = true)] public static readonly Permission ArtifactEdit = new Permission("ArtifactEdit");

        [Description("Delete artifacts")][SupportsRestriction(PermissionScope.Projects, PermissionScope.Environments, PermissionScope.Tenants, ExplicitTenantScopeRequired = true)] public static readonly Permission ArtifactDelete = new Permission("ArtifactDelete");

        [Description("View package feeds and the packages in them")] public static readonly Permission FeedView = new Permission("FeedView");

        [Description("View release and deployment events")][SupportsRestriction(PermissionScope.Projects, PermissionScope.Environments, PermissionScope.Tenants, ExplicitTenantScopeRequired = true)] public static readonly Permission EventView = new Permission("EventView");

        [Description("View library variable sets")] public static readonly Permission LibraryVariableSetView = new Permission("LibraryVariableSetView");

        [Description("Create library variable sets")] public static readonly Permission LibraryVariableSetCreate = new Permission("LibraryVariableSetCreate");

        [Description("Edit library variable sets")] public static readonly Permission LibraryVariableSetEdit = new Permission("LibraryVariableSetEdit");

        [Description("Delete library variable sets")] public static readonly Permission LibraryVariableSetDelete = new Permission("LibraryVariableSetDelete");

        [Description("View project groups")][SupportsRestriction(PermissionScope.ProjectGroups)] public static readonly Permission ProjectGroupView = new Permission("ProjectGroupView");

        [Description("Create project groups")][SupportsRestriction(PermissionScope.ProjectGroups)] public static readonly Permission ProjectGroupCreate = new Permission("ProjectGroupCreate");

        [Description("Edit project groups")][SupportsRestriction(PermissionScope.ProjectGroups)] public static readonly Permission ProjectGroupEdit = new Permission("ProjectGroupEdit");

        [Description("Delete project groups")][SupportsRestriction(PermissionScope.ProjectGroups)] public static readonly Permission ProjectGroupDelete = new Permission("ProjectGroupDelete");

        [Description("Create teams")] public static readonly Permission TeamCreate = new Permission("TeamCreate");

        [Description("View teams")] public static readonly Permission TeamView = new Permission("TeamView");

        [Description("Edit teams")] public static readonly Permission TeamEdit = new Permission("TeamEdit");

        [Description("Delete teams")] public static readonly Permission TeamDelete = new Permission("TeamDelete");

        [Description("View users")] public static readonly Permission UserView = new Permission("UserView");

        [Description("Invite users to register accounts")] public static readonly Permission UserInvite = new Permission("UserInvite");

        [Description("View other user's roles")] public static readonly Permission UserRoleView = new Permission("UserRoleView");

        [Description("Edit user role definitions")] public static readonly Permission UserRoleEdit = new Permission("UserRoleEdit");


        [Description("View summary-level information associated with a task")][SupportsRestriction(PermissionScope.Projects, PermissionScope.Environments, PermissionScope.Tenants)] public static readonly Permission TaskView = new Permission("TaskView");

        [Obsolete("TaskViewLog is no longer supported by Octopus Server. Instead use the TaskView permission, which also grants access to Task logs", false)]
        [Description("View detailed information about the execution of a task, including the task log output")][SupportsRestriction(PermissionScope.Projects, PermissionScope.Environments, PermissionScope.Tenants)] public static readonly Permission TaskViewLog = new Permission("TaskViewLog");

        [Description("Explicitly create (run) server tasks")][SupportsRestriction(PermissionScope.Projects, PermissionScope.Environments, PermissionScope.Tenants)] public static readonly Permission TaskCreate = new Permission("TaskCreate");

        [Description("Cancel server tasks")][SupportsRestriction(PermissionScope.Projects, PermissionScope.Environments, PermissionScope.Tenants)] public static readonly Permission TaskCancel = new Permission("TaskCancel");

        [Description("Edit server tasks")][SupportsRestriction(PermissionScope.Projects, PermissionScope.Environments, PermissionScope.Tenants)] public static readonly Permission TaskEdit = new Permission("TaskEdit");

        [Description("Create deployments that are prioritized")] public static readonly Permission TaskPrioritize = new("TaskPrioritize");

        [Description("View interruptions generated during deployments")][SupportsRestriction(PermissionScope.Projects, PermissionScope.Environments, PermissionScope.Tenants, ExplicitTenantScopeRequired = true)] public static readonly Permission InterruptionView = new Permission("InterruptionView");

        [Description("Take responsibility for and submit interruptions generated during deployments")][SupportsRestriction(PermissionScope.Projects, PermissionScope.Environments, PermissionScope.Tenants, ExplicitTenantScopeRequired = true)] public static readonly Permission InterruptionSubmit = new Permission("InterruptionSubmit");

        [Description("Take responsibility for and submit interruptions generated during deployments when the user is in a designated responsible team")][SupportsRestriction(PermissionScope.Projects, PermissionScope.Environments, PermissionScope.Tenants, ExplicitTenantScopeRequired = true)] public static readonly Permission InterruptionViewSubmitResponsible = new Permission("InterruptionViewSubmitResponsible");

        [Description("Push new packages to the built-in package repository")][SupportsRestriction(PermissionScope.Projects)] public static readonly Permission BuiltInFeedPush = new Permission("BuiltInFeedPush");

        [Description("Replace or delete packages in the built-in package repository")][SupportsRestriction(PermissionScope.Projects)] public static readonly Permission BuiltInFeedAdminister = new Permission("BuiltInFeedAdminister");

        [Description("Retrieve the contents of packages in the built-in package repository")][SupportsRestriction(PermissionScope.Projects)] public static readonly Permission BuiltInFeedDownload = new Permission("BuiltInFeedDownload");

        [Description("View step templates")] public static readonly Permission ActionTemplateView = new Permission("ActionTemplateView");

        [Description("Create step templates")] public static readonly Permission ActionTemplateCreate = new Permission("ActionTemplateCreate");

        [Description("Edit step templates")] public static readonly Permission ActionTemplateEdit = new Permission("ActionTemplateEdit");

        [Description("Delete step templates")] public static readonly Permission ActionTemplateDelete = new Permission("ActionTemplateDelete");

        [Description("Create lifecycles")] public static readonly Permission LifecycleCreate = new Permission("LifecycleCreate");

        [Description("View lifecycles")] public static readonly Permission LifecycleView = new Permission("LifecycleView");

        [Description("Edit lifecycles")] public static readonly Permission LifecycleEdit = new Permission("LifecycleEdit");

        [Description("Delete lifecycles")] public static readonly Permission LifecycleDelete = new Permission("LifecycleDelete");

        [Description("View accounts")][SupportsRestriction(PermissionScope.Environments)] public static readonly Permission AccountView = new Permission("AccountView");

        [Description("Edit accounts")][SupportsRestriction(PermissionScope.Environments)] public static readonly Permission AccountEdit = new Permission("AccountEdit");

        [Description("Create accounts")][SupportsRestriction(PermissionScope.Environments)] public static readonly Permission AccountCreate = new Permission("AccountCreate");

        [Description("Delete accounts")][SupportsRestriction(PermissionScope.Environments)] public static readonly Permission AccountDelete = new Permission("AccountDelete");

        [Description("Create tenants")][SupportsRestriction(PermissionScope.Tenants)] public static readonly Permission TenantCreate = new Permission("TenantCreate");

        [Description("Edit tenants")][SupportsRestriction(PermissionScope.Tenants)] public static readonly Permission TenantEdit = new Permission("TenantEdit");

        [Description("View tenants")][SupportsRestriction(PermissionScope.Tenants)] public static readonly Permission TenantView = new Permission("TenantView");

        [Description("Delete tenants")][SupportsRestriction(PermissionScope.Tenants)] public static readonly Permission TenantDelete = new Permission("TenantDelete");

        [Description("Create tag sets")] public static readonly Permission TagSetCreate = new Permission("TagSetCreate");

        [Description("Edit tag sets")] public static readonly Permission TagSetEdit = new Permission("TagSetEdit");

        [Description("Delete tag sets")] public static readonly Permission TagSetDelete = new Permission("TagSetDelete");

        [Description("View telemetry data")] public static readonly Permission TelemetryView = new Permission("TelemetryView");

        [Description("Create health check policies")] public static readonly Permission MachinePolicyCreate = new Permission("MachinePolicyCreate");

        [Description("View health check policies")] public static readonly Permission MachinePolicyView = new Permission("MachinePolicyView");

        [Description("Edit health check policies")] public static readonly Permission MachinePolicyEdit = new Permission("MachinePolicyEdit");

        [Description("Delete health check policies")] public static readonly Permission MachinePolicyDelete = new Permission("MachinePolicyDelete");

        [Description("Create proxies")] public static readonly Permission ProxyCreate = new Permission("ProxyCreate");

        [Description("View proxies")] public static readonly Permission ProxyView = new Permission("ProxyView");

        [Description("Edit proxies")] public static readonly Permission ProxyEdit = new Permission("ProxyEdit");

        [Description("Delete proxies")] public static readonly Permission ProxyDelete = new Permission("ProxyDelete");

        [Description("Create subscriptions")] public static readonly Permission SubscriptionCreate = new Permission("SubscriptionCreate");

        [Description("View subscriptions")] public static readonly Permission SubscriptionView = new Permission("SubscriptionView");

        [Description("Edit subscriptions")] public static readonly Permission SubscriptionEdit = new Permission("SubscriptionEdit");

        [Description("Delete subscriptions")] public static readonly Permission SubscriptionDelete = new Permission("SubscriptionDelete");

        [Description("Create triggers")][SupportsRestriction(PermissionScope.Projects)] public static readonly Permission TriggerCreate = new Permission("TriggerCreate");

        [Description("View triggers")][SupportsRestriction(PermissionScope.Projects)] public static readonly Permission TriggerView = new Permission("TriggerView");

        [Description("Edit triggers")][SupportsRestriction(PermissionScope.Projects)] public static readonly Permission TriggerEdit = new Permission("TriggerEdit");

        [Description("Delete triggers")][SupportsRestriction(PermissionScope.Projects)] public static readonly Permission TriggerDelete = new Permission("TriggerDelete");

        [Description("View certificates")][SupportsRestriction(PermissionScope.Environments, PermissionScope.Tenants)] public static readonly Permission CertificateView = new Permission("CertificateView");

        [Description("Create certificates")] public static readonly Permission CertificateCreate = new Permission("CertificateCreate");

        [Description("Edit certificates")][SupportsRestriction(PermissionScope.Environments, PermissionScope.Tenants)] public static readonly Permission CertificateEdit = new Permission("CertificateEdit");

        [Description("Delete certificates")][SupportsRestriction(PermissionScope.Environments, PermissionScope.Tenants)] public static readonly Permission CertificateDelete = new Permission("CertificateDelete");

        [Description("Export certificate private-keys")][SupportsRestriction(PermissionScope.Environments, PermissionScope.Tenants)] public static readonly Permission CertificateExportPrivateKey = new Permission("CertificateExportPrivateKey");

        [Description("Edit users")] public static readonly Permission UserEdit = new Permission("UserEdit");

        [Description("Configure server settings like Authentication, SMTP, and HTTP Security Headers")] public static readonly Permission ConfigureServer = new Permission("ConfigureServer");

        [Description("Edit feeds")] public static readonly Permission FeedEdit = new Permission("FeedEdit");

        [Description("View the workers in worker pools")] public static readonly Permission WorkerView = new Permission("WorkerView");

        [Description("Edit workers and worker pools")] public static readonly Permission WorkerEdit = new Permission("WorkerEdit");

        [Description("Edit spaces")] public static readonly Permission SpaceEdit = new Permission("SpaceEdit");

        [Description("View spaces")] public static readonly Permission SpaceView = new Permission("SpaceView");

        [Description("Delete spaces")] public static readonly Permission SpaceDelete = new Permission("SpaceDelete");

        [Description("Create spaces")] public static readonly Permission SpaceCreate = new Permission("SpaceCreate");

        [Description("Create/update build information")] public static readonly Permission BuildInformationPush = new Permission("BuildInformationPush");

        [Description("Replace or delete build information")] public static readonly Permission BuildInformationAdminister = new Permission("BuildInformationAdminister");

        [Description("View runbooks")][SupportsRestriction(PermissionScope.Projects)] public static readonly Permission RunbookView = new Permission("RunbookView");

        [Description("Edit runbooks")][SupportsRestriction(PermissionScope.Projects)] public static readonly Permission RunbookEdit = new Permission("RunbookEdit");

        [Description("Create runbook snapshots")][SupportsRestriction(PermissionScope.Projects)] public static readonly Permission RunbookSnapshotCreate = new Permission("RunbookSnapshotCreate");

        [Description("View runbook runs")][SupportsRestriction(PermissionScope.Projects, PermissionScope.Environments, PermissionScope.Tenants, ExplicitTenantScopeRequired = true)] public static readonly Permission RunbookRunView = new Permission("RunbookRunView");

        [Description("Delete runbook runs")][SupportsRestriction(PermissionScope.Projects, PermissionScope.Environments, PermissionScope.Tenants, ExplicitTenantScopeRequired = true)] public static readonly Permission RunbookRunDelete = new Permission("RunbookRunDelete");

        [Description("Create runbook runs")][SupportsRestriction(PermissionScope.Projects, PermissionScope.Environments, PermissionScope.Tenants, ExplicitTenantScopeRequired = true)] public static readonly Permission RunbookRunCreate = new Permission("RunbookRunCreate");

        [Description("View Git credentials")] public static readonly Permission GitCredentialView = new Permission("GitCredentialView");

        [Description("Edit Git credentials")] public static readonly Permission GitCredentialEdit = new Permission("GitCredentialEdit");

        [Description("Delete archived event files")] public static readonly Permission EventRetentionDelete = new Permission("EventRetentionDelete");

        [Description("View/list archived event files")] public static readonly Permission EventRetentionView = new Permission("EventRetentionView");

        [Description("View Insights reports")] public static readonly Permission InsightsReportView = new("InsightsReportView");

        [Description("Create Insights reports")] public static readonly Permission InsightsReportCreate = new("InsightsReportCreate");

        [Description("Edit Insights reports")] public static readonly Permission InsightsReportEdit = new("InsightsReportEdit");

        [Description("Delete Insights reports")] public static readonly Permission InsightsReportDelete = new("InsightsReportDelete");

        [Description("Create, update, delete and override deployment freezes")] public static readonly Permission DeploymentFreezeAdminister = new("DeploymentFreezeAdminister");

        [Description("View deployment target tags")] public static readonly Permission TargetTagView = new("TargetTagView");

        [Description("Create, edit, delete deployment target tags")] public static readonly Permission TargetTagAdminister = new("TargetTagAdminister");

        [Description("View resources in the Platform Hub")] public static readonly Permission PlatformHubView = new("PlatformHubView");

        [Description("Create, edit, delete resources in the Platform Hub")] public static readonly Permission PlatformHubEdit = new("PlatformHubEdit");

        [Description("Administer retention policies")] public static readonly Permission RetentionAdminister = new("RetentionAdminister");

        [Description("Create, edit, and delete feature toggles")] public static readonly Permission FeatureToggleEdit = new("FeatureToggleEdit");

        [Description("Create, edit, delete Approval Rules")] public static readonly Permission ApprovalRuleAdminister = new("ApprovalRuleAdminister");

        [Description("Create, edit, delete SSH Known Hosts")] public static readonly Permission SshKnownHostsAdminister = new("SshKnownHostsAdminister");

        [Description("View SSH Known Hosts")] public static readonly Permission SshKnownHostsView = new("SshKnownHostsView");

        [Description("View AI agent transcripts")][SupportsRestriction(PermissionScope.Projects, PermissionScope.Environments, PermissionScope.Tenants, ExplicitTenantScopeRequired = true)] public static readonly Permission AiAgentTranscriptView = new("AiAgentTranscriptView");

        [Description("Administer deployed resources managed by Octopus")][SupportsRestriction(PermissionScope.Projects, PermissionScope.Environments, PermissionScope.Tenants, ExplicitTenantScopeRequired = true)] public static readonly Permission DeployedResourceAdminister = new("DeployedResourceAdminister");

        public Permission(string id)
        {
            Id = id;
        }

        private string Id { get; }

        public bool Equals(Permission? other)
        {
            if ((object?)other == null)
                return false;
            return (object)this == (object)other || string.Equals(this.Id, other.Id, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            if (obj == null)
                return false;
            if ((object)this == obj)
                return true;
            return !(obj.GetType() != this.GetType()) && this.Equals((Permission)obj);
        }

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(this.Id);

        public static bool operator ==(Permission left, Permission right) => object.Equals((object)left, (object)right);

        public static bool operator !=(Permission left, Permission right) => !object.Equals((object)left, (object)right);

        public override string ToString()
        {
            return Id;
        }
    }
}
