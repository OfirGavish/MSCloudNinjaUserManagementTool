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
using MSCloudNinjaGraphAPI.Models;

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
        public string UsageLocation { get; set; } = "US";  // Default to US if not specified
    }

    public interface IUserManagementService
    {
        Task<List<User>> GetAllUsersAsync();
        Task<List<Group>> GetAllGroupsAsync();
        Task<List<License>> GetAvailableLicensesAsync();
        Task<(User User, string Password, List<string> Errors)> CreateUserAsync(CreateUserRequest request);
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

                await LogOperationAsync("Starting to fetch users...");
                var response = await _graphClient.Users.GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Select = queryOptions;
                    requestConfiguration.QueryParameters.Top = 999;
                    requestConfiguration.Headers.Add("ConsistencyLevel", "eventual");
                    requestConfiguration.QueryParameters.Count = true;
                    requestConfiguration.QueryParameters.Orderby = new[] { "displayName" };
                });

                while (response?.Value != null)
                {
                    pageCount++;
                    users.AddRange(response.Value);
                    await LogOperationAsync($"Page {pageCount}: Loaded {response.Value.Count} users (Total: {users.Count})");

                    // Get next page if it exists
                    if (response.OdataNextLink == null)
                        break;

                    response = await _graphClient.Users
                        .WithUrl(response.OdataNextLink)
                        .GetAsync();
                }

                await LogOperationAsync($"Finished loading {users.Count} users from {pageCount} pages.");
                return users;
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
                    "securityEnabled",
                    "membershipRule"
                };

                await LogOperationAsync("Starting to fetch assignable groups...");
                var response = await _graphClient.Groups.GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Select = queryOptions;
                    requestConfiguration.QueryParameters.Filter = "onPremisesSyncEnabled eq null or onPremisesSyncEnabled eq false";
                    requestConfiguration.QueryParameters.Top = 999;
                    requestConfiguration.Headers.Add("ConsistencyLevel", "eventual");
                    requestConfiguration.QueryParameters.Count = true;
                    requestConfiguration.QueryParameters.Orderby = new[] { "displayName" };
                });

                if (response?.Value != null)
                {
                    // Filter out dynamic groups (those with membershipRule)
                    var validGroups = response.Value
                        .Where(g => string.IsNullOrEmpty(g.MembershipRule))
                        .ToList();

                    await LogOperationAsync($"Found {validGroups.Count} groups on first page");
                    groups.AddRange(validGroups);
                    pageCount++;

                    // Get additional pages if they exist
                    var nextPageRequest = response.OdataNextLink;
                    while (!string.IsNullOrEmpty(nextPageRequest) && pageCount < 10)
                    {
                        var nextPageResponse = await _graphClient.Groups.WithUrl(nextPageRequest).GetAsync();
                        if (nextPageResponse?.Value != null)
                        {
                            var nextPageGroups = nextPageResponse.Value
                                .Where(g => string.IsNullOrEmpty(g.MembershipRule))
                                .ToList();

                            await LogOperationAsync($"Found {nextPageGroups.Count} groups on page {pageCount + 1}");
                            groups.AddRange(nextPageGroups);
                            pageCount++;
                            nextPageRequest = nextPageResponse.OdataNextLink;
                        }
                    }
                }

                // Log group types breakdown
                var stats = groups
                    .GroupBy(g => g.SecurityEnabled == true ? 
                        (g.MailEnabled == true ? "Mail-Enabled Security" : "Security") :
                        (g.MailEnabled == true ? "Distribution" : 
                         (g.GroupTypes != null && g.GroupTypes.Contains("Unified") ? "Microsoft 365" : "Other")))
                    .Select(g => $"{g.Key}: {g.Count()}")
                    .ToList();

                await LogOperationAsync($"Found total of {groups.Count} assignable groups:");
                foreach (var stat in stats)
                {
                    await LogOperationAsync($"  {stat}");
                }

                return groups;
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex);
                throw;
            }
        }

        public async Task<List<License>> GetAvailableLicensesAsync()
        {
            var licenses = new List<License>();

            try
            {
                await LogOperationAsync("Starting to fetch licenses...");
                var response = await _graphClient.SubscribedSkus.GetAsync();

                if (response?.Value != null)
                {
                    foreach (var sku in response.Value)
                    {
                        var license = new License
                        {
                            Id = sku.Id?.ToString(),
                            SkuId = sku.SkuId?.ToString(),
                            SkuPartNumber = sku.SkuPartNumber,
                            DisplayName = sku.CapabilityStatus,
                            TotalLicenses = sku.PrepaidUnits?.Enabled ?? 0,
                            UsedLicenses = sku.ConsumedUnits ?? 0,
                            FriendlyName = License.GetFriendlyName(sku.SkuPartNumber, sku.CapabilityStatus)
                        };

                        if (license.TotalLicenses > 0)  // Only add if there are total licenses
                        {
                            licenses.Add(license);
                        }
                    }
                }

                await LogOperationAsync($"Found {licenses.Count} license types");
                return licenses.OrderBy(l => l.FriendlyName).ToList();
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex);
                throw;
            }
        }

        private string GenerateRandomPassword()
        {
            const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
            const string numeric = "0123456789";
            const string special = "@#$%^&*";
            
            var random = new Random();
            var password = new StringBuilder();

            // Ensure at least one of each required character type
            password.Append(upperCase[random.Next(upperCase.Length)]);
            password.Append(lowerCase[random.Next(lowerCase.Length)]);
            password.Append(numeric[random.Next(numeric.Length)]);
            password.Append(special[random.Next(special.Length)]);

            // Fill the rest with random characters
            var allChars = upperCase + lowerCase + numeric + special;
            while (password.Length < 12)
            {
                password.Append(allChars[random.Next(allChars.Length)]);
            }

            // Shuffle the password
            return new string(password.ToString().ToCharArray().OrderBy(x => random.Next()).ToArray());
        }

        public async Task<(User User, string Password, List<string> Errors)> CreateUserAsync(CreateUserRequest request)
        {
            var errors = new List<string>();
            User createdUser = null;
            string password = GenerateRandomPassword();

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
                    UsageLocation = request.UsageLocation,  // Set usage location
                    PasswordProfile = new PasswordProfile
                    {
                        ForceChangePasswordNextSignIn = true,
                        Password = password
                    }
                };

                if (!string.IsNullOrEmpty(request.AdditionalEmail))
                {
                    user.OtherMails = new List<string> { request.AdditionalEmail };
                    if (request.SetAdditionalEmailAsPrimary)
                    {
                        user.Mail = request.AdditionalEmail;
                    }
                }

                createdUser = await _graphClient.Users.PostAsync(user);
                await LogOperationAsync($"User {request.UserPrincipalName} created successfully");

                // Set manager if specified
                if (!string.IsNullOrEmpty(request.ManagerId))
                {
                    try
                    {
                        await _graphClient.Users[createdUser.Id].Manager.Ref.PutAsync(new ReferenceUpdate
                        {
                            OdataId = $"https://graph.microsoft.com/v1.0/users/{request.ManagerId}"
                        });
                        await LogOperationAsync($"Manager set successfully for {request.UserPrincipalName}");
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Failed to set manager: {ex.Message}");
                        await LogExceptionAsync(ex);
                    }
                }

                // Add to groups
                foreach (var groupId in request.GroupIds ?? new List<string>())
                {
                    try
                    {
                        // Check if it's a mail-enabled group
                        var group = await _graphClient.Groups[groupId].GetAsync();
                        if (group.MailEnabled == true)
                        {
                            // Use Exchange endpoint for mail-enabled groups
                            await _graphClient.Groups[groupId].Members.Ref.PostAsync(new ReferenceCreate
                            {
                                OdataId = $"https://graph.microsoft.com/v1.0/users/{createdUser.Id}"
                            });
                        }
                        else
                        {
                            // Use standard endpoint for security groups
                            await _graphClient.Groups[groupId].Members.Ref.PostAsync(new ReferenceCreate
                            {
                                OdataId = $"https://graph.microsoft.com/v1.0/users/{createdUser.Id}"
                            });
                        }
                        await LogOperationAsync($"Added user to group {groupId}");
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Failed to add to group {groupId}: {ex.Message}");
                        await LogExceptionAsync(ex);
                    }
                }

                // Assign licenses
                if (request.LicenseIds?.Any() == true)
                {
                    try
                    {
                        var addLicenses = new List<Microsoft.Graph.Models.AssignedLicense>();
                        var removeLicenses = new List<Guid?>();

                        foreach (var licenseId in request.LicenseIds)
                        {
                            if (Guid.TryParse(licenseId, out Guid skuId))
                            {
                                addLicenses.Add(new Microsoft.Graph.Models.AssignedLicense
                                {
                                    SkuId = skuId
                                });
                            }
                            else
                            {
                                errors.Add($"Invalid license ID format: {licenseId}");
                            }
                        }

                        if (addLicenses.Any())
                        {
                            var requestBody = new Microsoft.Graph.Users.Item.AssignLicense.AssignLicensePostRequestBody
                            {
                                AddLicenses = addLicenses,
                                RemoveLicenses = removeLicenses
                            };

                            await _graphClient.Users[createdUser.Id].AssignLicense.PostAsync(requestBody);
                            await LogOperationAsync($"Licenses assigned successfully to {request.UserPrincipalName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Failed to assign licenses: {ex.Message}");
                        await LogExceptionAsync(ex);
                    }
                }

                return (createdUser, password, errors);
            }
            catch (Exception ex)
            {
                if (createdUser != null)
                {
                    errors.Add(ex.Message);
                    return (createdUser, password, errors);
                }
                
                await LogExceptionAsync(ex);
                throw;
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

        // Custom User class to handle showInAddressList property correctly
        public class CustomUser : User
        {
            [JsonPropertyName("showInAddressList")]
            public new bool? ShowInAddressList { get; set; }
        }
    }
}