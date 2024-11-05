using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Web.Hosting.Identity;

namespace MSIDemo;

static partial class Program
{
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

    static Task UseEntityFrameworkToAccessSqlServer()
    {
        // DbInterception.Add(new MsiAuthenticationInterceptor());
        // See:https://msazure.visualstudio.com/One/_git/AAPT-Antares-Websites?path=/src/Hosting/AdministrationService/Microsoft.Web.Hosting.Administration.Data/MsiAuthenticationInterceptor.cs&version=GBdev&line=46&lineEnd=47&lineStartColumn=1&lineEndColumn=1&lineStyle=plain&_a=contents
        return Task.CompletedTask;
    }
}
