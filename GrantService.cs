using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Grant_Searcher
{
    public class GrantService
    {
        private readonly string _xmlFilePath;

        public GrantService()
        {
            var dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            _xmlFilePath = Path.Combine(dataFolder, "gov_grants.xml");
        }

        public async Task<List<Grant>> FindGrantsAsync(OrganizationInfo orgInfo)
        {
            var grants = new List<Grant>();

            if (!File.Exists(_xmlFilePath))
                throw new FileNotFoundException("Grant data file not found. Please update data before searching.", _xmlFilePath);

            await Task.Run(() =>
            {
                XDocument doc = XDocument.Load(_xmlFilePath);

                XNamespace ns = "http://apply.grants.gov/system/OpportunityDetail-V1.0";

                foreach (var grantElement in doc.Descendants(ns + "OpportunitySynopsisDetail_1_0"))
                {
                    var grant = new Grant
                    {
                        Title = grantElement.Element(ns + "OpportunityTitle")?.Value,
                        Description = grantElement.Element(ns + "Description")?.Value,
                        Agency = grantElement.Element(ns + "AgencyName")?.Value,
                        Eligibility = grantElement.Element(ns + "EligibleApplicants")?.Value,
                        Deadline = grantElement.Element(ns + "CloseDate")?.Value,
                        Link = grantElement.Element(ns + "OpportunityNumber") != null ?
                               $"https://www.grants.gov/search-results-detail/{grantElement.Element(ns + "OpportunityID")?.Value}" :
                               null,
                        Geography = grantElement.Element(ns + "Locations")?.Value,
                        GrantType = grantElement.Element(ns + "FundingInstrumentType")?.Value
                    };

                    if (GrantMatchesCriteria(grant, orgInfo))
                    {
                        grants.Add(grant);
                    }
                }
            });

            return grants;
        }

        private bool GrantMatchesCriteria(Grant grant, OrganizationInfo orgInfo)
        {
            bool matches = true;

            if (!string.IsNullOrEmpty(orgInfo.Mission))
            {
                var missionKeywords = orgInfo.Mission.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                bool missionMatch = missionKeywords.All(keyword =>
                    (grant.Title != null && grant.Title.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (grant.Description != null && grant.Description.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                );
                matches &= missionMatch;
            }

            if (!string.IsNullOrEmpty(orgInfo.Geography))
            {
                if (grant.Geography != null)
                {
                    matches &= grant.Geography.IndexOf(orgInfo.Geography, StringComparison.OrdinalIgnoreCase) >= 0;
                }
                else
                {
                    matches = false;
                }
            }

            if (!string.IsNullOrEmpty(orgInfo.AwardCeiling))
            {
                if (grant.GrantType != null)
                {
                    matches &= grant.GrantType.IndexOf(orgInfo.AwardCeiling, StringComparison.OrdinalIgnoreCase) >= 0;
                }
                else
                {
                    matches = false;
                }
            }

            if (!string.IsNullOrEmpty(orgInfo.AwardFloor))
            {
                var serviceKeywords = orgInfo.AwardFloor.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                bool servicesMatch = serviceKeywords.All(keyword =>
                    (grant.Title != null && grant.Title.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (grant.Description != null && grant.Description.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                );
                matches &= servicesMatch;
            }

            if (!string.IsNullOrEmpty(orgInfo.Agency))
            {
                if (grant.Eligibility != null)
                {
                    matches &= grant.Eligibility.IndexOf(orgInfo.Agency, StringComparison.OrdinalIgnoreCase) >= 0;
                }
                else
                {
                    matches = false;
                }
            }

            return matches;
        }

    }
}