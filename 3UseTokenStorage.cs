using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Web.Hosting.Identity;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json.Linq;

namespace MSIDemo;

static partial class Program
{
    static async Task UseHttpClientToAccessStorage()
    {
        Console.WriteLine($"\r\n=======  {nameof(UseWindowsStorageSDKToAccessStorage)}  =======");
        var credential = DefaultAzureCredentialHelper.GetDefaultAzureCredential(authorityHost: AzureAuthorityHosts.AzurePublicCloud.AbsoluteUri, managedIdentityResourceId: mi_res_id);
        var token = await credential.GetTokenAsync(new TokenRequestContext(scopes: [StorageResourceUrl], tenantId: TenantId));
        var storageUrl = $"https://{StorageName}.blob.core.windows.net/config/data-service-configuration.json";
        
        using var client = new HttpClient();
        // curl https://{StorageName}.blob.core.windows.net/config/data-service-configuration.json -H "x-ms-version: 2020-04-08" -H "Authorization: Bearer token"
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);
        client.DefaultRequestHeaders.Add("x-ms-version", "2020-04-08");

        Console.WriteLine($"HttpClient GET {storageUrl}");
        Console.WriteLine($"Authorization: Bearer {token.Token.Substring(0, 10)}...");
        using var response = await client.GetAsync(storageUrl);
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine(JObject.Parse(content));
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
        Console.WriteLine($"{container.GetType().Name} ListBlobsSegmentedAsync");
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

        Console.WriteLine($"{container.GetType().Name} GetBlobsAsync");
        await foreach (var blob in container.GetBlobsAsync())
        {
            Console.WriteLine(blob.Name);
        }
    }
}
