using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Web.Hosting.Identity;

namespace MSIDemo;

static partial class Program
{
    // https://msazure.visualstudio.com/One/_git/AAPT-Antares-Websites?path=/src/Hosting/Azure/tools/src/AntaresDeployment/vmss/VmssTool.cs&version=GBdev&line=6021&lineEnd=6088&lineStartColumn=1&lineEndColumn=10&lineStyle=plain&_a=contents
    static async Task UseTokenToAccessSqlServer()
    {
        Console.WriteLine($"\r\n=======  {nameof(UseTokenToAccessSqlServer)}  =======");
        var credential = DefaultAzureCredentialHelper.GetDefaultAzureCredential(authorityHost: AzureAuthorityHosts.AzurePublicCloud.AbsoluteUri, managedIdentityResourceId: mi_res_id);
        var token = await credential.GetTokenAsync(new TokenRequestContext(scopes: [DatabaseResourceUrl], tenantId: TenantId));

        using var connection = new SqlConnection($"Data Source={SqlServer};Initial Catalog=hosting;MultipleActiveResultSets=True");
        connection.AccessToken = token.Token;
        Console.WriteLine($"SqlConnection.Opening with token 'Data Source={SqlServer};Initial Catalog=hosting;MultipleActiveResultSets=True'");
        await connection.OpenAsync(default);
        Console.WriteLine($"connection state {connection.State}");
    }

    static async Task UseConnectionStringToAccessSqlServer()
    {
        Console.WriteLine($"\r\n=======  {nameof(UseConnectionStringToAccessSqlServer)}  =======");

        Console.WriteLine($"SqlConnection.Opening with native connection string 'Data Source={SqlServer};User Id={client_id};Authentication=Active Directory Default;Initial Catalog=hosting;MultipleActiveResultSets=True'");
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
