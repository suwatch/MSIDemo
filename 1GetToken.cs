﻿using Azure.Core;
using Azure.Identity;
using Microsoft.Web.Hosting.Identity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MSIDemo;

static partial class Program
{
    // IMDS = Azure Instance Metadata Service
    public const string IMDSEndpoint = "http://169.254.169.254/";

    static string mi_res_id = $"/subscriptions/{SubscriptionId}/resourceGroups/{StampName}-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{StampName}-{Role}-Identity";
    static string tokenUri = $"{IMDSEndpoint}metadata/identity/oauth2/token?api-version=2018-02-01&resource={StorageResourceUrl}&mi_res_id={mi_res_id}";

    static string client_id = "0ca5c5b9-0fb7-421d-a831-1596ac8d5ce3";
    static string tokenUriByClient = $"{IMDSEndpoint}metadata/identity/oauth2/token?api-version=2018-02-01&resource={StorageResourceUrl}&client_id={client_id}";

    // Token valid for 24 hours
    // VMSS will refresh token every 1 hour (if failed, old token is used during retry)
    // See: https://eng.ms/docs/cloud-ai-platform/devdiv/serverless-paas-balam/serverless-paas-vikr/app-service-web-apps/app-service-team-documents/generalteamdocs/security/msiadoption/MSI-TokenCache-Resiliency
    static Task GetTokenUsingCurl()
    {
        Console.WriteLine($"\r\n=======  {nameof(GetTokenUsingCurl)}  =======");

        Console.WriteLine($"GetToken for MI '{mi_res_id}' to access {StorageResourceUrl}");
        var curlCmd1 = $"curl -v \"{tokenUri}\" -H \"Metadata: true\"";
        Console.WriteLine(curlCmd1);
        Console.WriteLine();

        Console.WriteLine($"GetToken for MI '{client_id}' to access {StorageResourceUrl}");
        var curlCmd2 = $"curl -v \"{tokenUriByClient}\" -H \"Metadata: true\"";
        Console.WriteLine(curlCmd2);

        return Task.CompletedTask;
    }

    static async Task GetTokenUsingHttpClient()
    {
        Console.WriteLine($"\r\n=======  {nameof(GetTokenUsingHttpClient)}  =======");
        using var client = new HttpClient();
        // XSS protection
        client.DefaultRequestHeaders.Add("Metadata", "true");

        Console.WriteLine($"HttpClient GET {tokenUri}");
        using var response = await client.GetAsync(tokenUri);
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine(JObject.Parse(content));
    }

    static async Task GetTokenUsingAzureIdentity()
    {
        Console.WriteLine($"\r\n=======  {nameof(GetTokenUsingAzureIdentity)}  =======");
        var credential = DefaultAzureCredentialHelper.GetDefaultAzureCredential(authorityHost: AzureAuthorityHosts.AzurePublicCloud.AbsoluteUri, managedIdentityResourceId: mi_res_id);

        Console.WriteLine($"{credential.GetType().Name} GetTokenAsync(scopes: [{StorageResourceUrl}], tenantId: {TenantId})");
        var token = await credential.GetTokenAsync(new TokenRequestContext(scopes: [StorageResourceUrl], tenantId: TenantId));
        Console.WriteLine(JObject.Parse(JsonConvert.SerializeObject(token)));
    }
}
