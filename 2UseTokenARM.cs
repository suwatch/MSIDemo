using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager.AppService;
using Microsoft.Web.Hosting.Identity;
using Newtonsoft.Json.Linq;
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

        var uri = $"https://management.azure.com/subscriptions/d2a4e313-c345-472f-b747-68f3b959349b/resourceGroups/asev3-euap-rg/providers/Microsoft.Web/sites/asev3-euap-app-01?api-version=2024-04-01";
        Console.WriteLine($"HttpClient GET {uri}");
        Console.WriteLine($"Authorization: Bearer {token.Token.Substring(0, 10)}...");
        using var response = await client.GetAsync(uri);
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine(JObject.Parse(content));
    }

    static async Task UseAzureSDKToAccessARM()
    {
        Console.WriteLine($"\r\n=======  {nameof(UseAzureSDKToAccessARM)}  =======");
        var credential = DefaultAzureCredentialHelper.GetDefaultAzureCredential(authorityHost: AzureAuthorityHosts.AzurePublicCloud.AbsoluteUri, managedIdentityResourceId: mi_res_id);

        var client = new Azure.ResourceManager.ArmClient(credential, SubscriptionId);
        var resource = client.GetWebSiteResource(new ResourceIdentifier("/subscriptions/d2a4e313-c345-472f-b747-68f3b959349b/resourceGroups/asev3-euap-rg/providers/Microsoft.Web/sites/asev3-euap-app-01"));
        
        Console.WriteLine($"{resource.GetType().Name} GetAsync");
        var website = await resource.GetAsync();
        Console.WriteLine(JObject.Parse(website.GetRawResponse().Content.ToString()));
    }
}
