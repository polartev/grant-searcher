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
                        AwardCeiling = grantElement.Element(ns + "AwardCeiling")?.Value,
                        AwardFloor = grantElement.Element(ns + "AwardFloor")?.Value,
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

            string currentTime = DateTime.Now.ToString("MMddyyyy");

            if (!string.IsNullOrEmpty(grant.Deadline))
            {
                if (DateTime.TryParseExact(grant.Deadline, "MMddyyyy", null, System.Globalization.DateTimeStyles.None, out DateTime deadlineDate) &&
                    DateTime.TryParseExact(currentTime, "MMddyyyy", null, System.Globalization.DateTimeStyles.None, out DateTime currentDate))
                {
                    if (deadlineDate <= currentDate)
                        matches = false;
                }
                else
                {
                    matches = false;
                }
            }

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
                if (grant.Description != null)
                {
                    matches &= orgInfo.Geography.IndexOf(grant.Description, StringComparison.OrdinalIgnoreCase) >= 0;
                }
                else
                {
                    matches = false;
                }
            }

            // Check AwardCeiling
            if (!string.IsNullOrEmpty(orgInfo.AwardCeiling) && decimal.TryParse(orgInfo.AwardCeiling, out var orgAwardCeiling))
            {
                if (decimal.TryParse(grant.AwardCeiling, out var grantAwardCeiling))
                {
                    matches &= grantAwardCeiling <= orgAwardCeiling;
                }
                else
                {
                    matches = false;
                }
            }

            // Check AwardFloor
            if (!string.IsNullOrEmpty(orgInfo.AwardFloor) && decimal.TryParse(orgInfo.AwardFloor, out var orgAwardFloor))
            {
                if (decimal.TryParse(grant.AwardFloor, out var grantAwardFloor))
                {
                    matches &= grantAwardFloor >= orgAwardFloor;
                }
                else
                {
                    matches = false;
                }
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