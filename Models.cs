using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grant_Searcher
{
    // Model for organization information
    public class OrganizationInfo
    {
        public string Name { get; set; }
        public string Mission { get; set; }
        public string Geography { get; set; }
        public string GrantType { get; set; }
        public string Services { get; set; }
        public string TargetAudience { get; set; }
    }

    // Model for representing a grant
    public class Grant
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Agency { get; set; }
        public string Eligibility { get; set; }
        public string Deadline { get; set; }
        public string Link { get; set; }
        public string Geography { get; set; }
        public string GrantType { get; set; }
    }
}

