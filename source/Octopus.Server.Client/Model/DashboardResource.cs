using System;
using System.Collections.Generic;

namespace Octopus.Client.Model
{
    public class DashboardResource : Resource
    {
        public List<DashboardProjectResource> Projects { get; set; }
        public List<DashboardProjectGroupResource> ProjectGroups { get; set; }
        public List<DashboardEnvironmentResource> Environments { get; set; }
        public List<DashboardTenantResource> Tenants { get; set; }
        public List<DashboardItemResource> Items { get; set; }
        public int? ProjectLimit { get; set; }
        public bool IsFiltered { get; set; }
    }
}
