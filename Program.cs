using System;
using System.Data.SqlClient;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json.Linq;

namespace MSIDemo
{
    internal class Program
    {
        const string StorageResourceUrl = "https://storage.azure.com/";
        const string DatabaseResourceUrl = "https://database.windows.net/";
        const string SubscriptionId = "65dbb2c4-1f8d-436f-a431-6b4b27e6a13c";
        const string StampName = "suwatch-prod-blu-001";
        const string StorageName = "suwatchprodblu001infra";

        static readonly string UserAssignedId = $"/subscriptions/{SubscriptionId}/resourcegroups/{StampName}-rg/providers/microsoft.managedidentity/userassignedidentities/{StampName}-controllerrole-identity";

        static async Task Main(string[] args)
        {
            try
            {
                // get token for user assigned and talk to SQL
                var dbToken = await GetToken(resource: DatabaseResourceUrl, userAssignedId: UserAssignedId);
                //await TestSqlConnection(dbToken);

                // get token for user assigned and talk to Storage
                //var storageToken = await GetToken(resource: StorageResourceUrl, userAssignedId: UserAssignedId);
                //await TestStorageUrl(storageToken);
                //await TestCloudStorageAccount(storageToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        static async Task<string> GetToken(string resource, string userAssignedId = null)
        {
            Console.WriteLine($"========= {nameof(GetToken)}({resource}) ========= ");
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Metadata", "true");

                var address = $"http://169.254.169.254/metadata/identity/oauth2/token?api-version=2018-02-01&resource={resource}";
                if (!string.IsNullOrEmpty(userAssignedId))
                {
                    address = $"{address}&mi_res_id={userAssignedId}";
                }

                // curl "http://169.254.169.254/metadata/identity/oauth2/token?api-version=2018-02-01&resource=%_RESOURCE%&mi_res_id=%_MSI%" -H "Metadata: true"
                Console.WriteLine($"{nameof(GetToken)}: HttpGet {address}");
                using (var response = await client.GetAsync(address))
                {
                    Console.WriteLine($"{nameof(GetToken)}: Status={response.StatusCode}");
                    response.EnsureSuccessStatusCode();

                    var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                    Console.WriteLine($"{nameof(GetToken)}: Response body");
                    Console.WriteLine(json);
                    return json.Value<string>("access_token");
                }
            }
        }

        static async Task TestSqlConnection(string token)
        {
            Console.WriteLine($"========= {nameof(TestSqlConnection)} ========= ");
            var connectionString = $"Server=tcp:{StampName}.database.windows.net;Database=hosting;Trusted_Connection=False;Encrypt=True;trustservercertificate=false;Connection Timeout=30";
            using (var connection = new SqlConnection(connectionString))
            {
                connection.AccessToken = token;
                Console.WriteLine("Sql connection opening ...");
                await connection.OpenAsync();
                Console.WriteLine("Sql connection opened successfully!");
            }
        }

        static async Task TestStorageUrl(string token)
        {
            Console.WriteLine($"========= {nameof(TestStorageUrl)} ========= ");
            var storageUrl = $"https://{StorageName}.blob.core.windows.net/config/current/runtimedata.json";
            using (var client = new HttpClient())
            {
                // curl https://{InfraStorageName}.blob.core.windows.net/config/current/runtimedata.json -H "x-ms-version: 2020-04-08" -H "Authorization: Bearer token"
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Add("x-ms-version", "2020-04-08");
                Console.WriteLine($"{nameof(TestStorageUrl)}: HttpGet {storageUrl}");
                using (var response = await client.GetAsync(storageUrl))
                {
                    Console.WriteLine($"{nameof(TestStorageUrl)}: Status={response.StatusCode}");

                    var body = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"{nameof(TestStorageUrl)}: Response body");
                    Console.WriteLine(body);
                }
            }
        }

        static async Task TestCloudStorageAccount(string token)
        {
            Console.WriteLine($"========= {nameof(TestCloudStorageAccount)} ========= ");
            var credential = new TokenCredential(token);
            var account = new CloudStorageAccount(new StorageCredentials(credential), accountName: StorageName, endpointSuffix: "core.windows.net", useHttps: true);
            var client = account.CreateCloudBlobClient();
            var container = client.GetContainerReference("config");
            var blockBlob = container.GetBlockBlobReference("current/runtimedata.json");
            Console.WriteLine($"{nameof(TestCloudStorageAccount)}: DownloadTextAsync {blockBlob.Uri}");
            var body = await blockBlob.DownloadTextAsync();
            Console.WriteLine($"{nameof(TestCloudStorageAccount)}: Response body");
            Console.WriteLine(body);
        }
    }
}