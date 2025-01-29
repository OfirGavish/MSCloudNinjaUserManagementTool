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
    public interface IUserManagementService
    {
        Task<List<User>> GetAllUsersAsync();
        Task DisableUserAsync(string userId);
        Task RemoveFromGlobalAddressListAsync(string userId);
        Task RemoveFromAllGroupsAsync(string userId);
        Task UpdateManagerForEmployeesAsync(string userId);
        Task RemoveUserLicensesAsync(string userId);
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
    }

    // Custom User class to handle showInAddressList property correctly
    public class CustomUser : User
    {
        [JsonPropertyName("showInAddressList")]
        public new bool? ShowInAddressList { get; set; }
    }
}