using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MSIDemo;

static partial class Program
{
    public const string ARMResourceUrl = "https://management.azure.com/";
    public const string StorageResourceUrl = "https://storage.azure.com/";
    public const string DatabaseResourceUrl = "https://database.windows.net/";
    public const string SubscriptionId = "d2a4e313-c345-472f-b747-68f3b959349b";
    public const string TenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
    public const string StampName = "kudu1";
    public const string StorageName = "kudu1";
    public const string SqlServer = "antareseastusa50e142.database.windows.net";
    public const string Role = "controllerrole";

    static async Task Main(string[] args)
    {
        try
        {
            var methods = typeof(Program).GetMethods(BindingFlags.NonPublic | BindingFlags.Static);
            var method = args.Length == 0 ? null : methods.FirstOrDefault(m => m.Name.IndexOf(args[0]) >= 0);
            if (method == null)
            {
                Console.WriteLine($"Usage:");
                foreach (var m in methods.Where(m => m.Name.IndexOf("Main") < 0))
                {
                    Console.WriteLine($"MSIDemo.exe {m.Name}");
                }
                return;
            }

            await (Task)method.Invoke(null, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}