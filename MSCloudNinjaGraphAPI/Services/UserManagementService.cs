using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.AssignLicense;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions;
using System.Web;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Headers;
using Azure.Identity;
using Microsoft.Identity.Client;
using Azure.Core;

namespace MSCloudNinjaGraphAPI.Services
{
    public class CreateUserRequest
    {
        public string UserPrincipalName { get; set; }
        public string FirstName { get; set; }
        public string Surname { get; set; }
        public string DisplayName { get; set; }
        public string AdditionalEmail { get; set; }
        public bool SetAdditionalEmailAsPrimary { get; set; }
        public string ManagerId { get; set; }
        public List<string> GroupIds { get; set; }
        public List<string> LicenseIds { get; set; }
    }

    public interface IUserManagementService
    {
        Task<List<User>> GetAllUsersAsync();
        Task<List<Group>> GetAllGroupsAsync();
        Task<List<License>> GetAvailableLicensesAsync();
        Task CreateUserAsync(CreateUserRequest request);
        Task DisableUserAsync(string userId);
        Task RemoveFromGlobalAddressListAsync(string userId);
        Task RemoveFromAllGroupsAsync(string userId);
        Task UpdateManagerForEmployeesAsync(string userId);
        Task RemoveUserLicensesAsync(string userId);
        Task RevokeUserSignInSessionsAsync(string userId);
        Task<List<string>> GetDomainNamesAsync();
        Task<bool> IsUserSyncedFromOnPremAsync(string userId);
        Task<List<Group>> GetUserSyncedGroupsAsync(string userId);
        Task<List<User>> GetUserSyncedDirectReportsAsync(string userId);
    }

    public class UserManagementService : IUserManagementService
    {
        private readonly GraphServiceClient _graphClient;
        private readonly LogService _logService;

        public UserManagementService(GraphServiceClient graphClient, LogService logService)
        {
            _graphClient = graphClient;
            _logService = logService;
        }

        private async Task LogOperationAsync(string message, bool isError = false)
        {
            await _logService.LogAsync(message, isError);
        }

        private async Task LogExceptionAsync(Exception ex, bool isError = false)
        {
            await _logService.LogAsync(ex.Message, isError);
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            var users = new List<User>();
            var pageCount = 0;

            try
            {
                var queryOptions = new string[]
                {
                    "id",
                    "displayName",
                    "userPrincipalName",
                    "accountEnabled",
                    "department",
                    "jobTitle"
                };

                var response = await _graphClient.Users.GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Select = queryOptions;
                    requestConfiguration.QueryParameters.Top = 999;
                    requestConfiguration.Headers.Add("ConsistencyLevel", "eventual");
                    requestConfiguration.QueryParameters.Orderby = new[] { "userPrincipalName" };
                });

                while (response?.Value != null)
                {
                    pageCount++;
                    var newUsers = response.Value.Where(u => !string.IsNullOrEmpty(u.UserPrincipalName)).ToList();
                    users.AddRange(newUsers);
                    await LogOperationAsync($"Page {pageCount}: Loaded {newUsers.Count} users (Total: {users.Count})");

                    if (string.IsNullOrEmpty(response.OdataNextLink))
                        break;

                    try
                    {
                        string skipToken = response.OdataNextLink[(response.OdataNextLink.IndexOf("$skiptoken=") + "$skiptoken=".Length)..];
                        var requestInformation = _graphClient.Users.ToGetRequestInformation(requestConfiguration =>
                        {
                            requestConfiguration.QueryParameters.Select = queryOptions;
                            requestConfiguration.QueryParameters.Top = 999;
                            requestConfiguration.Headers.Add("ConsistencyLevel", "eventual");
                            requestConfiguration.QueryParameters.Orderby = new[] { "userPrincipalName" };
                        });

                        requestInformation.UrlTemplate = requestInformation.UrlTemplate[..^1] + ",%24skiptoken" + requestInformation.UrlTemplate[^1];
                        requestInformation.QueryParameters.Add("%24skiptoken", skipToken);

                        response = await _graphClient.RequestAdapter.SendAsync(requestInformation,
                            UserCollectionResponse.CreateFromDiscriminatorValue);
                    }
                    catch (Exception ex)
                    {
                        await LogExceptionAsync(ex, true);
                        break;
                    }
                }

                await LogOperationAsync($"Finished loading {users.Count} users from {pageCount} pages.");
                return users.OrderBy(u => u.DisplayName).ToList();
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex, true);
                throw;
            }
        }

        public async Task<List<Group>> GetAllGroupsAsync()
        {
            var groups = new List<Group>();
            var pageCount = 0;

            try
            {
                var queryOptions = new string[]
                {
                    "id",
                    "displayName",
                    "description",
                    "onPremisesSyncEnabled",
                    "groupTypes",
                    "mailEnabled",
                    "securityEnabled"
                };

                await LogOperationAsync("Starting to fetch cloud-only groups...");
                var response = await _graphClient.Groups.GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Select = queryOptions;
                    requestConfiguration.QueryParameters.Filter = "onPremisesSyncEnabled eq null or onPremisesSyncEnabled eq false";
                    requestConfiguration.QueryParameters.Top = 999;
                    requestConfiguration.Headers.Add("ConsistencyLevel", "eventual");
                    requestConfiguration.QueryParameters.Count = true;
                });

                while (response?.Value != null)
                {
                    pageCount++;
                    var newGroups = response.Value
                        .Where(g => g.OnPremisesSyncEnabled == null || g.OnPremisesSyncEnabled == false)
                        .ToList();
                    
                    groups.AddRange(newGroups);
                    await LogOperationAsync($"Page {pageCount}: Loaded {newGroups.Count} cloud-only groups (Total: {groups.Count})");

                    if (response.OdataNextLink == null)
                        break;

                    response = await _graphClient.Groups
                        .WithUrl(response.OdataNextLink)
                        .GetAsync();
                }

                await LogOperationAsync($"Finished loading {groups.Count} cloud-only groups from {pageCount} pages.");
                
                // Order groups by type and name for better organization
                return groups
                    .OrderBy(g => g.DisplayName)
                    .ToList();
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex, true);
                throw;
            }
        }

        public async Task<List<License>> GetAvailableLicensesAsync()
        {
            try
            {
                await LogOperationAsync("Fetching available licenses...");
                var subscribedSkus = await _graphClient.SubscribedSkus.GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Select = new[] 
                    { 
                        "skuId",
                        "skuPartNumber",
                        "prepaidUnits",
                        "consumedUnits",
                        "servicePlans",
                        "capabilityStatus"
                    };
                });

                var licenses = subscribedSkus?.Value?.Select(s => new License
                {
                    Id = s.SkuId.ToString(),
                    SkuId = s.SkuId.ToString(),
                    SkuPartNumber = s.SkuPartNumber,
                    DisplayName = s.CapabilityStatus,
                    FriendlyName = License.GetFriendlyName(s.SkuPartNumber, s.CapabilityStatus),
                    TotalLicenses = s.PrepaidUnits?.Enabled ?? 0,
                    UsedLicenses = s.ConsumedUnits ?? 0
                }).ToList() ?? new List<License>();

                // Log license availability
                foreach (var license in licenses.Where(l => l.TotalLicenses > 0))
                {
                    await LogOperationAsync(
                        $"License {license.FriendlyName} ({license.SkuPartNumber}): " +
                        $"{license.AvailableLicenses} available of {license.TotalLicenses} total");
                }

                return licenses;
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex);
                throw;
            }
        }

        public async Task CreateUserAsync(CreateUserRequest request)
        {
            var errors = new List<string>();
            User createdUser = null;

            try
            {
                await LogOperationAsync($"Starting user creation for {request.UserPrincipalName}");
                
                // Create user
                var user = new User
                {
                    UserPrincipalName = request.UserPrincipalName,
                    DisplayName = request.DisplayName,
                    GivenName = request.FirstName,
                    Surname = request.Surname,
                    MailNickname = request.UserPrincipalName.Split('@')[0],
                    AccountEnabled = true,
                    PasswordProfile = new PasswordProfile
                    {
                        ForceChangePasswordNextSignIn = true,
                        Password = GenerateRandomPassword()
                    }
                };

                await LogOperationAsync("Creating base user account...");
                createdUser = await _graphClient.Users.PostAsync(user);
                await LogOperationAsync($"Base user account created successfully with ID: {createdUser.Id}");

                // Set manager if specified
                if (!string.IsNullOrEmpty(request.ManagerId))
                {
                    try
                    {
                        await LogOperationAsync($"Setting manager (ID: {request.ManagerId}) for user...");
                        await _graphClient.Users[createdUser.Id].Manager.Ref.PutAsync(new ReferenceUpdate
                        {
                            OdataId = $"https://graph.microsoft.com/v1.0/users/{request.ManagerId}"
                        });
                        await LogOperationAsync("Manager set successfully");
                    }
                    catch (Exception ex)
                    {
                        await LogExceptionAsync(ex, true);
                        errors.Add($"Failed to set manager: {ex.Message}");
                    }
                }

                // Add additional email if specified
                if (!string.IsNullOrEmpty(request.AdditionalEmail))
                {
                    try
                    {
                        await LogOperationAsync($"Adding additional email: {request.AdditionalEmail}");
                        var proxyAddress = $"smtp:{request.AdditionalEmail}";
                        
                        if (request.SetAdditionalEmailAsPrimary)
                        {
                            await LogOperationAsync("Setting additional email as primary...");
                            var updateUser = new User
                            {
                                Mail = request.AdditionalEmail,
                                ProxyAddresses = new List<string> { proxyAddress }
                            };
                            await _graphClient.Users[createdUser.Id].PatchAsync(updateUser);
                        }
                        else
                        {
                            await LogOperationAsync("Adding additional email as secondary...");
                            var updateUser = new User
                            {
                                ProxyAddresses = new List<string> { proxyAddress }
                            };
                            await _graphClient.Users[createdUser.Id].PatchAsync(updateUser);
                        }
                        await LogOperationAsync("Email configuration completed");
                    }
                    catch (Exception ex)
                    {
                        await LogExceptionAsync(ex, true);
                        errors.Add($"Failed to set email addresses: {ex.Message}");
                    }
                }

                // Add to groups
                if (request.GroupIds?.Any() == true)
                {
                    await LogOperationAsync($"Adding user to {request.GroupIds.Count} groups...");
                    foreach (var groupId in request.GroupIds)
                    {
                        try
                        {
                            await _graphClient.Groups[groupId].Members.Ref.PostAsync(new ReferenceCreate
                            {
                                OdataId = $"https://graph.microsoft.com/v1.0/directoryObjects/{createdUser.Id}"
                            });
                            await LogOperationAsync($"Added to group {groupId}");
                        }
                        catch (Exception ex)
                        {
                            await LogExceptionAsync(ex, true);
                            errors.Add($"Failed to add to group {groupId}: {ex.Message}");
                        }
                    }
                }

                // Assign licenses
                if (request.LicenseIds?.Any() == true)
                {
                    try
                    {
                        await LogOperationAsync($"Assigning {request.LicenseIds.Count} licenses...");
                        var addLicenses = request.LicenseIds
                            .Select(id => new AssignedLicense { SkuId = Guid.Parse(id) })
                            .ToList();

                        await _graphClient.Users[createdUser.Id].AssignLicense.PostAsync(new AssignLicensePostRequestBody
                        {
                            AddLicenses = addLicenses,
                            RemoveLicenses = new List<Guid?>()
                        });
                        await LogOperationAsync("Licenses assigned successfully");
                    }
                    catch (Exception ex)
                    {
                        await LogExceptionAsync(ex, true);
                        errors.Add($"Failed to assign licenses: {ex.Message}");
                    }
                }

                var message = errors.Any()
                    ? $"User created successfully, but some operations failed:\n\n{string.Join("\n", errors)}"
                    : "User created successfully with all requested configurations.";

                await LogOperationAsync(message);
                throw new AggregateException(message, errors.Select(e => new Exception(e)));
            }
            catch (Exception ex)
            {
                var finalMessage = createdUser != null
                    ? $"User was created but with errors: {ex.Message}"
                    : $"Failed to create user: {ex.Message}";
                
                await LogExceptionAsync(ex, true);
                throw new Exception(finalMessage, ex);
            }
        }

        public async Task DisableUserAsync(string userId)
        {
            try
            {
                await LogOperationAsync($"Starting to disable user with ID: {userId}");
                var user = new User { AccountEnabled = false };
                await _graphClient.Users[userId].PatchAsync(user);
                await LogOperationAsync($"Successfully disabled user with ID: {userId}");
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex, true);
                throw;
            }
        }

        public async Task RemoveFromGlobalAddressListAsync(string userId)
        {
            try
            {
                await LogOperationAsync($"Removing {userId} from Global Address List");

                try
                {
                    // First, get the current user to verify we can modify them
                    var user = await _graphClient.Users[userId].GetAsync(requestConfig =>
                    {
                        requestConfig.QueryParameters.Select = new[] { "id", "showInAddressList" };
                    });

                    if (user == null)
                    {
                        throw new Exception($"User {userId} not found");
                    }

                    await LogOperationAsync($"Current ShowInAddressList value: {user.ShowInAddressList}");

                    // Create custom user object for update
                    var updateUser = new CustomUser
                    {
                        ShowInAddressList = false
                    };

                    // Update the user
                    await _graphClient.Users[userId].PatchAsync(updateUser);

                    // Wait for potential replication
                    await Task.Delay(2000);

                    // Verify the change
                    var updatedUser = await _graphClient.Users[userId].GetAsync(requestConfig =>
                    {
                        requestConfig.QueryParameters.Select = new[] { "id", "showInAddressList" };
                    });

                    await LogOperationAsync($"Updated user ShowInAddressList value: {updatedUser?.ShowInAddressList}");

                    // Note: The UI might show a different value since it's computed from multiple sources
                    await LogOperationAsync($"Successfully removed {userId} from Global Address List. Note: Changes may take time to reflect in the admin portal.");
                }
                catch (Microsoft.Graph.Models.ODataErrors.ODataError ex)
                {
                    var errorMessage = ex.Error?.Message ?? "Unknown error";
                    var errorCode = ex.Error?.Code ?? "Unknown code";
                    await LogOperationAsync($"Graph API Error - Code: {errorCode}, Message: {errorMessage}", true);

                    if (errorMessage.Contains("external service"))
                    {
                        await LogOperationAsync($"User {userId} is an external user and cannot be hidden from GAL", true);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex, true);
                throw;
            }
        }

        public async Task RemoveFromAllGroupsAsync(string userId)
        {
            try
            {
                var memberOfGroups = await _graphClient.Users[userId].MemberOf.GetAsync();
                if (memberOfGroups?.Value != null)
                {
                    foreach (var directoryObject in memberOfGroups.Value)
                    {
                        if (directoryObject is Group group)
                        {
                            try
                            {
                                await _graphClient.Groups[group.Id].Members[userId].Ref.DeleteAsync();
                                await LogOperationAsync($"Removed user {userId} from group {group.DisplayName}");
                            }
                            catch (Exception ex)
                            {
                                await LogExceptionAsync(ex, true);
                            }
                        }
                    }
                }

                var transitiveGroups = await _graphClient.Users[userId].TransitiveMemberOf.GetAsync();
                if (transitiveGroups?.Value != null)
                {
                    foreach (var directoryObject in transitiveGroups.Value)
                    {
                        if (directoryObject is Group group && !memberOfGroups.Value.Any(g => g.Id == group.Id))
                        {
                            try
                            {
                                await _graphClient.Groups[group.Id].Members[userId].Ref.DeleteAsync();
                                await LogOperationAsync($"Removed user {userId} from transitive group {group.DisplayName}");
                            }
                            catch (Exception ex)
                            {
                                await LogExceptionAsync(ex, true);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex, true);
                throw;
            }
        }

        public async Task UpdateManagerForEmployeesAsync(string userId)
        {
            try
            {
                await LogOperationAsync($"Starting to update manager for direct reports of user {userId}");

                string? managerId = null;
                try
                {
                    var managerResponse = await _graphClient.Users[userId].Manager.GetAsync();
                    if (managerResponse != null)
                    {
                        managerId = managerResponse.Id;
                        await LogOperationAsync($"Found manager {managerId} for user {userId}");
                    }
                    else
                    {
                        await LogOperationAsync($"User {userId} has no manager assigned");
                    }
                }
                catch (Microsoft.Graph.Models.ODataErrors.ODataError ex) when (ex.Message.Contains("Resource 'manager' does not exist"))
                {
                    await LogOperationAsync($"User {userId} has no manager assigned");
                }

                var directReports = await _graphClient.Users[userId].DirectReports.GetAsync();
                if (directReports?.Value == null || !directReports.Value.Any())
                {
                    await LogOperationAsync($"No direct reports found for user {userId}");
                    return;
                }

                foreach (var report in directReports.Value)
                {
                    try
                    {
                        if (managerId != null)
                        {
                            var managerRef = new ReferenceUpdate { OdataId = $"https://graph.microsoft.com/v1.0/users/{managerId}" };
                            await _graphClient.Users[report.Id].Manager.Ref.PutAsync(managerRef);
                            await LogOperationAsync($"Updated manager to {managerId} for direct report {report.Id}");
                        }
                        else
                        {
                            await _graphClient.Users[report.Id].Manager.Ref.DeleteAsync();
                            await LogOperationAsync($"Removed manager reference for direct report {report.Id}");
                        }
                    }
                    catch (Exception ex)
                    {
                        await LogExceptionAsync(ex, true);
                    }
                }
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex, true);
                throw;
            }
        }

        private async Task<string> GetAuthTokenAsync()
        {
            try
            {
                // For managed identity, we can only use one scope
                var scopes = new[] { "https://graph.microsoft.com/.default" };
                
                // Get the token using the default Azure credentials
                var credential = new DefaultAzureCredential();
                var token = await credential.GetTokenAsync(new Azure.Core.TokenRequestContext(scopes));
                
                return token.Token;
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex, true);
                throw;
            }
        }

        public async Task RemoveUserLicensesAsync(string userId)
        {
            try
            {
                await LogOperationAsync($"Starting to remove licenses for user with ID: {userId}");

                // Get user's assigned licenses
                var user = await _graphClient.Users[userId].GetAsync(requestConfig =>
                {
                    requestConfig.QueryParameters.Select = new[] { "id", "displayName", "assignedLicenses" };
                });

                if (user?.AssignedLicenses == null || !user.AssignedLicenses.Any())
                {
                    await LogOperationAsync($"No licenses found for user {userId}");
                    return;
                }

                await LogOperationAsync($"Found {user.AssignedLicenses.Count} licenses to remove");

                // Create the request to remove all licenses
                var requestBody = new AssignLicensePostRequestBody
                {
                    AddLicenses = new List<AssignedLicense>(),
                    RemoveLicenses = user.AssignedLicenses
                        .Where(l => l.SkuId.HasValue)
                        .Select(l => l.SkuId)
                        .ToList()
                };

                // Remove all licenses in one call
                await _graphClient.Users[userId].AssignLicense.PostAsync(requestBody);
                await LogOperationAsync($"Successfully removed all licenses from user {userId}");
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex, true);
                throw;
            }
        }

        public async Task RevokeUserSignInSessionsAsync(string userId)
        {
            try
            {
                await LogOperationAsync($"Starting to revoke sign-in sessions for user with ID: {userId}");
                await _graphClient.Users[userId].RevokeSignInSessions.PostAsync();
                await LogOperationAsync($"Successfully revoked sign-in sessions for user with ID: {userId}");
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex, true);
                throw;
            }
        }

        public async Task<List<string>> GetDomainNamesAsync()
        {
            try
            {
                var domains = await _graphClient.Domains.GetAsync();
                return domains?.Value?
                    .Where(d => d.IsVerified == true)
                    .Select(d => d.Id)
                    .OrderBy(d => d)
                    .ToList() ?? new List<string>();
            }
            catch (Exception ex)
            {
                await _logService.LogAsync($"Error getting domain names: {ex.Message}", true);
                throw;
            }
        }

        public async Task<bool> IsUserSyncedFromOnPremAsync(string userId)
        {
            try
            {
                var user = await _graphClient.Users[userId].GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Select = new[] { "onPremisesSyncEnabled" };
                });

                return user?.OnPremisesSyncEnabled == true;
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex);
                throw;
            }
        }

        public async Task<List<Group>> GetUserSyncedGroupsAsync(string userId)
        {
            try
            {
                var memberOf = await _graphClient.Users[userId].MemberOf.GetAsync();
                var syncedGroups = memberOf.Value
                    .OfType<Group>()
                    .Where(g => g.OnPremisesSyncEnabled == true)
                    .ToList();

                return syncedGroups;
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex);
                throw;
            }
        }

        public async Task<List<User>> GetUserSyncedDirectReportsAsync(string userId)
        {
            try
            {
                var directReports = await _graphClient.Users[userId].DirectReports.GetAsync();
                var syncedReports = directReports.Value
                    .OfType<User>()
                    .Where(u => u.OnPremisesSyncEnabled == true)
                    .ToList();

                return syncedReports;
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex);
                throw;
            }
        }

        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 16)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }

    public class License
    {
        public string Id { get; set; }
        public string SkuId { get; set; }
        public string SkuPartNumber { get; set; }
        public string DisplayName { get; set; }
        public string FriendlyName { get; set; }
        public int TotalLicenses { get; set; }
        public int UsedLicenses { get; set; }
        public int AvailableLicenses => TotalLicenses - UsedLicenses;
        public bool HasAvailableLicenses => AvailableLicenses > 0;

        public string GetDisplayText()
        {
            return $"{FriendlyName} ({SkuPartNumber}) - {AvailableLicenses} available of {TotalLicenses} total";
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

        public static string GetFriendlyName(string skuPartNumber, string defaultDisplayName)
        {
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
    }

    // Custom User class to handle showInAddressList property correctly
    public class CustomUser : User
    {
        [JsonPropertyName("showInAddressList")]
        public new bool? ShowInAddressList { get; set; }
    }
}