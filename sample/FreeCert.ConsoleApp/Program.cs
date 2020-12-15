using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DynamicDns.TencentCloud;
using FreeCert.Core;
using Microsoft.Extensions.Logging;

namespace FreeCert.ConsoleApp
{
    class Program
    {
        //Tencent Cloud API Key 
        static string tcloudSecretId = "<id>";
        static string tcloudSecretKey = "<key>";

        static string workDir = Directory.GetCurrentDirectory();
        static string accountKeyFile = Path.Combine(Directory.GetCurrentDirectory(), "account.key");
        static string orderUriFile = Path.Combine(Directory.GetCurrentDirectory(), "order.uri");
        private static string email = "644928779@qq.com";
        private static string domain = "cyanstream.com";

        static async Task Main()
        {
            var loggerFactory = new LoggerFactory();
            // Create 
            FreeCertContext context;
            if (File.Exists(accountKeyFile) && File.Exists(orderUriFile))
            {
                context = await new FreeCertBuilder(true, workDir, loggerFactory, true)
                    .LoadAccount(File.ReadAllText(accountKeyFile))
                    .LoadOrder(new Uri(File.ReadAllText(orderUriFile)))
                    .BuildAsync();
            }
            else
            {
                context = await new FreeCertBuilder(true, workDir, loggerFactory, true)
                    .AddNewAccount(email)
                    .SetDomain(domain)
                    .BuildAsync();
            }

            var account = await context.GetAccountAsync();
            Console.WriteLine("AccountInformation:");
            Console.WriteLine($" Status   : {account.Status}");
            Console.WriteLine($" Contacts : {string.Join(",", account.Contacts)}");
            Console.WriteLine($" AcceptTos: {account.AcceptTos}");
            await File.WriteAllTextAsync(accountKeyFile, context.AcmeContext.AccountKey.ToPem());
            Console.WriteLine($" Account Key saved at {accountKeyFile}.");

            Console.WriteLine();
            await GetOrderInfoAsync(context);

            Console.WriteLine();
            var authorizations = await context.GetAuthorizationsAsync();
            Console.WriteLine("Order Authorization Information:");
            Console.WriteLine(" DNS Authorization");
            foreach (var item in authorizations)
            {
                Console.WriteLine($" Record: {item.Record}");
                Console.WriteLine($" Type  : {item.RecordType}");
                Console.WriteLine($" Value : {item.Value}");
                Console.WriteLine($" Status: {item.Status}");
                Console.WriteLine("----------------------------");
            }

            string input;
            do
            {
                Console.WriteLine();
                Console.WriteLine("Menu: 1.AutoCreateDnsRecord 2.GetDnsRecord 3.CheckDnsRecord 4.Authorization 5.GetOrderInfo 6.Finish 7.Export ");
                Console.WriteLine("Enter 'e' to exit ");
                input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        await AutoCreateDnsAsync(context);
                        break;
                    case "2":
                        await GetDnsRecordAsync(context);
                        break;
                    case "3":
                        await CheckDnsRecordAsync(context);
                        break;
                    case "4":
                        await AuthorizationAsync(context);
                        break;
                    case "5":
                        await GetOrderInfoAsync(context);
                        break;
                    case "6":
                        await FinishAsync(context);
                        break;
                    case "7":
                        await ExportAsync(context);
                        break;
                }

            } while (input != "e");


        }

        static async Task FinishAsync(FreeCertContext context)
        {
            await context.OrderFinalizeAsync();
            Console.WriteLine("Finish Success!");
        }

        static async Task ExportAsync(FreeCertContext context)
        {
            await context.ExportCertAsync("123456");
            Console.WriteLine("Export Success!");
        }

        static async Task AutoCreateDnsAsync(FreeCertContext context)
        {
            var authResult = await context.AutoCreateDnsRecord(new TencentCloudDynamicDns(new TencentCloudOptions()
            {
                SecretId = tcloudSecretId,
                SecretKey = tcloudSecretKey
            }));
            Console.WriteLine();
            Console.WriteLine("Auto CreateDns Result:");
            Console.WriteLine($" Success : {authResult.Success}");
            Console.WriteLine($" Message : {authResult.ErrorMessage}");
        }

        static async Task GetOrderInfoAsync(FreeCertContext context)
        {
            var order = await context.GetOrderInfoAsync();

            Console.WriteLine("Current OrderInformation:");
            Console.WriteLine($" Status : {order.Status}");
            Console.WriteLine($" Domains: {string.Join(",", order.Domains)}");
            Console.WriteLine($" Expires: {order.Expires:yyyy-MM-dd HH:mm:ss}");

            await File.WriteAllTextAsync(orderUriFile, context.OrderContext.Location.ToString());
            Console.WriteLine($" Order Uri saved at {orderUriFile}.");
        }

        static async Task GetDnsRecordAsync(FreeCertContext context)
        {
            var records = await context.GetDnsTxtRecordAsync();
            Console.WriteLine("Dns Record Information:");

            if (records.Any())
            {
                foreach (var record in records)
                {
                    Console.WriteLine(" " + record);
                }
            }
            else
            {
                Console.WriteLine("No record.");
            }
        }

        static async Task CheckDnsRecordAsync(FreeCertContext context)
        {
            var res = await context.CheckDnsTxtRecordAsync();
            Console.WriteLine("Dns Check Information:");
            Console.WriteLine(" Status:" + res);
        }

        static async Task AuthorizationAsync(FreeCertContext context)
        {
            await context.AuthorizationAsync();
            Console.WriteLine("AuthorizationComplete!");
        }
    }
}
