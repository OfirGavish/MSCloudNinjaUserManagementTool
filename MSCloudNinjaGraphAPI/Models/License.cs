using System;
using System.Collections.Generic;

namespace MSCloudNinjaGraphAPI.Models
{
    public class License
    {
        public string? Id { get; set; }
        public string? SkuId { get; set; }
        public string? SkuPartNumber { get; set; }
        public string? DisplayName { get; set; }
        public string? FriendlyName { get; set; }
        public int TotalLicenses { get; set; }
        public int UsedLicenses { get; set; }
        public int AvailableLicenses => TotalLicenses - UsedLicenses;
        public bool HasAvailableLicenses => AvailableLicenses > 0;

        public string GetDisplayText()
        {
            return $"{FriendlyName} ({SkuPartNumber}) - {AvailableLicenses} available of {TotalLicenses} total";
        }

        public static string GetFriendlyName(string? skuPartNumber, string? defaultDisplayName)
        {
            if (string.IsNullOrEmpty(skuPartNumber))
            {
                return defaultDisplayName ?? "Unknown License";
            }

            if (SkuToFriendlyName.TryGetValue(skuPartNumber, out var friendlyName))
            {
                return friendlyName;
            }

            // If we don't have a mapping, clean up the default display name
            if (!string.IsNullOrEmpty(defaultDisplayName))
            {
                // Remove common status words
                var cleanName = defaultDisplayName
                    .Replace("Enabled", "")
                    .Replace("Disabled", "")
                    .Replace("Pending", "")
                    .Replace("Warning", "")
                    .Replace("Suspended", "")
                    .Trim();

                return !string.IsNullOrEmpty(cleanName) ? cleanName : skuPartNumber;
            }

            return skuPartNumber;
        }

        private static readonly Dictionary<string, string> SkuToFriendlyName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Enterprise Suites
            { "SPE_E3", "Microsoft 365 E3" },
            { "SPE_E5", "Microsoft 365 E5" },
            { "SPE_F1", "Microsoft 365 F3" },
            { "ENTERPRISEPACK", "Office 365 E3" },
            { "ENTERPRISEPREMIUM", "Office 365 E5" },
            
            // Business Suites
            { "SPB", "Microsoft 365 Business" },
            { "O365_BUSINESS_PREMIUM", "Microsoft 365 Business Standard" },
            { "O365_BUSINESS_ESSENTIALS", "Microsoft 365 Business Basic" },
            { "O365_BUSINESS", "Microsoft 365 Apps for Business" },
            
            // Exchange Online
            { "EXCHANGESTANDARD", "Exchange Online Plan 1" },
            { "EXCHANGEENTERPRISE", "Exchange Online Plan 2" },
            { "EXCHANGEESSENTIALS", "Exchange Online Essentials" },
            { "EXCHANGE_S_STANDARD", "Exchange Online Plan 1" },
            { "EXCHANGE_S_ENTERPRISE", "Exchange Online Plan 2" },
            
            // SharePoint Online
            { "SHAREPOINTSTANDARD", "SharePoint Online Plan 1" },
            { "SHAREPOINTENTERPRISE", "SharePoint Online Plan 2" },
            { "SHAREPOINT_S_STANDARD", "SharePoint Online Plan 1" },
            { "SHAREPOINT_S_ENTERPRISE", "SharePoint Online Plan 2" },
            
            // Teams
            { "TEAMS_COMMERCIAL_TRIAL", "Microsoft Teams Commercial Trial" },
            { "TEAMS_EXPLORATORY", "Microsoft Teams Exploratory" },
            { "TEAMS_FREE", "Microsoft Teams Free" },
            { "TEAMS_FREE_TIER1", "Microsoft Teams (Free)" },
            { "TEAMS_FREE_TIER2", "Microsoft Teams (Free)" },
            
            // Power Platform
            { "POWER_BI_PRO", "Power BI Pro" },
            { "POWER_BI_STANDARD", "Power BI Free" },
            { "FLOW_FREE", "Power Automate Free" },
            { "POWERAPPS_VIRAL", "Power Apps Trial" },
            
            // Azure Active Directory
            { "AAD_PREMIUM", "Azure AD Premium P1" },
            { "AAD_PREMIUM_P2", "Azure AD Premium P2" },
            { "AAD_BASIC", "Azure AD Basic" },
            
            // Enterprise Mobility + Security
            { "EMS", "Enterprise Mobility + Security E3" },
            { "EMSPREMIUM", "Enterprise Mobility + Security E5" },
            
            // Dynamics 365
            { "DYN365_ENTERPRISE_PLAN1", "Dynamics 365 Customer Engagement Plan" },
            { "DYN365_ENTERPRISE_SALES", "Dynamics 365 Sales Enterprise" },
            { "DYN365_FINANCIALS_BUSINESS_SKU", "Dynamics 365 Business Central" },
            
            // Visual Studio
            { "VSULTSTD", "Visual Studio Enterprise" },
            { "VSSPREMIUM", "Visual Studio Premium" },
            { "VS_PREMIUM", "Visual Studio Premium" },
            { "VS_PROFESSIONAL", "Visual Studio Professional" },

            // Intune
            { "INTUNE_A", "Microsoft Intune" },
            { "INTUNE_A_D", "Microsoft Intune Device" },
            { "INTUNE_A_VL", "Microsoft Intune Volume License" },
            { "INTUNE_O365", "Microsoft Intune for Office 365" },
            { "INTUNE_SMBIZ", "Microsoft Intune Small Business" },

            // Project
            { "PROJECTPREMIUM", "Project Plan 5" },
            { "PROJECTPROFESSIONAL", "Project Plan 3" },
            { "PROJECT_P1", "Project Plan 1" },
            { "PROJECTESSENTIALS", "Project Online Essentials" },

            // Visio
            { "VISIO_PLAN1", "Visio Plan 1" },
            { "VISIO_PLAN2", "Visio Plan 2" },
            { "VISIOCLIENT", "Visio Online Plan 2" },

            // Windows
            { "WIN10_PRO_ENT_SUB", "Windows 10 Enterprise E3" },
            { "WIN10_VDA_E3", "Windows 10 Enterprise E3" },
            { "WIN10_VDA_E5", "Windows 10 Enterprise E5" },

            // Common Add-ons
            { "ATP_ENTERPRISE", "Office 365 Advanced Threat Protection" },
            { "MCOEV", "Phone System" },
            { "MCOMEETADV", "Audio Conferencing" },
            { "DEFENDER_ENDPOINT_P1", "Microsoft Defender for Endpoint P1" },
            { "DEFENDER_ENDPOINT_P2", "Microsoft Defender for Endpoint P2" }
        };
    }
}
