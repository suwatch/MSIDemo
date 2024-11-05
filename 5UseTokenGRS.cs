using Azure.Core;
using Azure.Identity;
using Microsoft.Web.Hosting.Identity;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

namespace MSIDemo;

static partial class Program
{

    static async Task UseAccessTokenToCallGRS()
    {
        Console.WriteLine($"\r\n=======  {nameof(UseAccessTokenToCallGRS)}  =======");
        var credential = DefaultAzureCredentialHelper.GetDefaultAzureCredential(authorityHost: AzureAuthorityHosts.AzurePublicCloud.AbsoluteUri, managedIdentityResourceId: mi_res_id);
        var token = await credential.GetTokenAsync(new TokenRequestContext(scopes: [AppServiceResourceUrl], tenantId: TenantId));

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("x-ms-correlation-request-id", $"{Guid.NewGuid()}");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

        var uri = $"https://kudu1gr.eastus.cloudapp.azure.com/sitestamps?stampName=kudu1";
        Console.WriteLine($"HttpClient GET {uri}");
        Console.WriteLine($"Authorization: Bearer {token.Token.Substring(0, 10)}...");
        using var response = await client.GetAsync(uri);
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine(content);
    }
}
