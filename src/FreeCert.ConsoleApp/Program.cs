using System;
using System.Threading.Tasks;
using DynamicDns.TencentCloud;
using FreeCert.Core;
using Microsoft.Extensions.Logging;

namespace FreeCert.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            ILoggerFactory loggerFactory = new LoggerFactory();
            var logger = loggerFactory.CreateLogger("xxx");
            //            var builder=await new FreeCertBuilder(true,logger,true)
            //                .AddNewAccount("stulzq@qq.com")
            //                .SetDomain("xcmaster.com")
            //                .SetDomain("*.xcmaster.com")
            //                .BuildAsync();
            var context = await new FreeCertBuilder(true, logger, true)
                .LoadAccount()
                .LoadOrder()
                .BuildAsync();

            var account = await context.GetAccountAsync();
            Console.WriteLine("AccountInformation:");
            Console.WriteLine($" Status   : {account.Status}");
            Console.WriteLine($" Contacts : {string.Join(",", account.Contacts)}");
            Console.WriteLine($" AcceptTos: {account.AcceptTos}");

            Console.WriteLine();
            await GetOrderInfo(context);

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
                Console.WriteLine("Menu: 1.GetDnsRecord 2.CheckDnsRecord 3.Authorization 4.GetOrderInfo 5.AutoCreateDnsRecord 6.Finish 7.Export ");
                Console.WriteLine("Enter 'e' to exit ");
                input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        await GetDnsRecord(context);
                        break;
                    case "2":
                        await CheckDnsRecord(context);
                        break;
                    case "3":
                        await Authorization(context);
                        break;
                    case "4":
                        await GetOrderInfo(context);
                        break;
                    case "5":
                        await AutoCreateDns(context);
                        break;
                    case "6":
                        await Finish(context);
                        break;
                    case "7":
                        await Export(context);
                        break;
                    default:
                        break;
                }

            } while (input != "e");


        }

        static async Task Finish(FreeCertContext context)
        {
            await context.OrderFinalizeAsync();
            Console.WriteLine("Finish Success!");
        }

        static async Task Export(FreeCertContext context)
        {
            await context.ExportCertAsync();
            Console.WriteLine("Export Success!");
        }

        static async Task AutoCreateDns(FreeCertContext context)
        {
            var authResult = await context.AutoCreateDnsRecord(new TencentCloudDynamicDns(new TencentCloudOptions()
            {
                SecretId = Environment.GetEnvironmentVariable("TENCENT_CLOUD_SECRETID", EnvironmentVariableTarget.User),
                SecretKey = Environment.GetEnvironmentVariable("TENCENT_CLOUD_SECRETKEY", EnvironmentVariableTarget.User)
            }));
            Console.WriteLine();
            Console.WriteLine("Auto CreateDns Result:");
            Console.WriteLine($" Success : {authResult.Success}");
            Console.WriteLine($" Message : {authResult.ErrorMessage}");
        }

        static async Task GetOrderInfo(FreeCertContext context)
        {
            var order = await context.GetOrderInfoAsync();
            Console.WriteLine("Current OrderInformation:");
            Console.WriteLine($" Status : {order.Status}");
            Console.WriteLine($" Domains: {string.Join(",", order.Domains)}");
            Console.WriteLine($" Expires: {order.Expires:yyyy-MM-dd HH:mm:ss}");
        }

        static async Task GetDnsRecord(FreeCertContext context)
        {
            var records = await context.GetDnsTxtRecordAsync();
            Console.WriteLine("Dns Record Information:");
            foreach (var record in records)
            {
                Console.WriteLine(" " + record);
            }
        }

        static async Task CheckDnsRecord(FreeCertContext context)
        {
            var res = await context.CheckDnsTxtRecordAsync();
            Console.WriteLine("Dns Check Information:");
            Console.WriteLine(" Status:"+res);
        }

        static async Task Authorization(FreeCertContext context)
        {
            await context.AuthorizationAsync();
            Console.WriteLine("AuthorizationComplete!");
        }
    }
}
