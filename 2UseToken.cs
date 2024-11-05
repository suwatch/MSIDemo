using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager.AppService;
using Azure.Storage.Blobs;
using Microsoft.Data.SqlClient;
using Microsoft.Web.Hosting.Identity;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net.Http.Headers;

namespace MSIDemo;

static partial class Program
{
    static async Task UseHttpClientToAccessARM()
    {
        Console.WriteLine($"\r\n=======  {nameof(UseHttpClientToAccessARM)}  =======");
        var credential = DefaultAzureCredentialHelper.GetDefaultAzureCredential(authorityHost: AzureAuthorityHosts.AzurePublicCloud.AbsoluteUri, managedIdentityResourceId: mi_res_id);
        var token = await credential.GetTokenAsync(new TokenRequestContext(scopes: [ARMResourceUrl], tenantId: TenantId));

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("x-ms-correlation-request-id", $"{Guid.NewGuid()}");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        using var response = await client.GetAsync($"https://management.azure.com/subscriptions/d2a4e313-c345-472f-b747-68f3b959349b/resourceGroups/asev3-euap-rg/providers/Microsoft.Web/sites/asev3-euap-app-01?api-version=2024-04-01");
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine(JObject.Parse(content));
    }

    static async Task UseAzureSDKToAccessARM()
    {
        Console.WriteLine($"\r\n=======  {nameof(UseAzureSDKToAccessARM)}  =======");
        var credential = DefaultAzureCredentialHelper.GetDefaultAzureCredential(authorityHost: AzureAuthorityHosts.AzurePublicCloud.AbsoluteUri, managedIdentityResourceId: mi_res_id);

        var client = new Azure.ResourceManager.ArmClient(credential, SubscriptionId);
        var resource = client.GetWebSiteResource(new ResourceIdentifier("/subscriptions/d2a4e313-c345-472f-b747-68f3b959349b/resourceGroups/asev3-euap-rg/providers/Microsoft.Web/sites/asev3-euap-app-01"));
        var website = await resource.GetAsync();
        Console.WriteLine(JObject.Parse(website.GetRawResponse().Content.ToString()));
    }

    static async Task UseWindowsStorageSDKToAccessStorage()
    {
        Console.WriteLine($"\r\n=======  {nameof(UseWindowsStorageSDKToAccessStorage)}  =======");
        var credential = DefaultAzureCredentialHelper.GetDefaultAzureCredential(authorityHost: AzureAuthorityHosts.AzurePublicCloud.AbsoluteUri, managedIdentityResourceId: mi_res_id);
        var token = await credential.GetTokenAsync(new TokenRequestContext(scopes: [StorageResourceUrl], tenantId: TenantId));

        var account = new CloudStorageAccount(new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(new Microsoft.WindowsAzure.Storage.Auth.TokenCredential(token.Token)),
            blobEndpoint: new Uri($"https://{StorageName}.blob.core.windows.net"),
            queueEndpoint: new Uri($"https://{StorageName}.queue.core.windows.net"),
            tableEndpoint: new Uri($"https://{StorageName}.table.core.windows.net"),
            fileEndpoint: new Uri($"https://{StorageName}.file.core.windows.net"));

        var client = account.CreateCloudBlobClient();
        var container = client.GetContainerReference("config");
        var result = await container.ListBlobsSegmentedAsync(null);
        foreach (var blob in result.Results)
        {
            Console.WriteLine(blob.Uri);
        }
    }

    static async Task UseAzureStorageSDKToAccessStorage()
    {
        Console.WriteLine($"\r\n=======  {nameof(UseAzureStorageSDKToAccessStorage)}  =======");
        var credential = DefaultAzureCredentialHelper.GetDefaultAzureCredential(authorityHost: AzureAuthorityHosts.AzurePublicCloud.AbsoluteUri, managedIdentityResourceId: mi_res_id);

        var blobService = new BlobServiceClient(new Uri("https://kudu1.blob.core.windows.net"), credential);
        var container = blobService.GetBlobContainerClient("config");
        await foreach(var blob in container.GetBlobsAsync())
        {
            Console.WriteLine(blob.Name);
        }
    }

    static async Task UseAccessTokenToAccessSqlServer()
    {
        Console.WriteLine($"\r\n=======  {nameof(UseAccessTokenToAccessSqlServer)}  =======");
        var credential = DefaultAzureCredentialHelper.GetDefaultAzureCredential(authorityHost: AzureAuthorityHosts.AzurePublicCloud.AbsoluteUri, managedIdentityResourceId: mi_res_id);
        var token = await credential.GetTokenAsync(new TokenRequestContext(scopes: [DatabaseResourceUrl], tenantId: TenantId));

        using var connection = new SqlConnection($"Data Source={SqlServer};Initial Catalog=hosting;MultipleActiveResultSets=True");
        connection.AccessToken = token.Token;
        await connection.OpenAsync(default);
        Console.WriteLine($"connection state {connection.State}");
    }

    static async Task UseConnectionStringToAccessSqlServer()
    {
        Console.WriteLine($"\r\n=======  {nameof(UseConnectionStringToAccessSqlServer)}  =======");

        using var connection = new SqlConnection($"Data Source={SqlServer};User Id={client_id};Authentication=Active Directory Default;Initial Catalog=hosting;MultipleActiveResultSets=True");
        await connection.OpenAsync(default);
        Console.WriteLine($"connection state {connection.State}");
    }
}
