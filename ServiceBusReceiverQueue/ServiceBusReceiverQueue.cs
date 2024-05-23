using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;


namespace ServiceBus
{
    class ServiceBusReceiverQueue
    {
        static async Task Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
             .SetBasePath(AppContext.BaseDirectory)
             .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            IConfiguration config = builder.Build();
            string kv = config["KeyVault:KeyVaultUrl"];

            var keyVaultUrl = new Uri(kv);
            var client = new SecretClient(vaultUri: keyVaultUrl, credential: new DefaultAzureCredential());

            KeyVaultSecret connectionStringKv = client.GetSecret("SERVICEBUSCONNECTIONSTRING");
            KeyVaultSecret queueNameKv = client.GetSecret("QueueName");
            string connectionString = connectionStringKv.Value;
            string queueName = queueNameKv.Value;

            if (string.IsNullOrEmpty(connectionString))
            {
                Console.WriteLine("Service Bus connection string is not set.");
                return;
            }

            await ReceiveMessagesAsync(connectionString, queueName);
        }

        static async Task ReceiveMessagesAsync(string connectionString, string queueName)
        {
            await using (ServiceBusClient client = new ServiceBusClient(connectionString))
            {
                ServiceBusProcessor processor = client.CreateProcessor(queueName, new ServiceBusProcessorOptions());

                processor.ProcessMessageAsync += MessageHandler;
                processor.ProcessErrorAsync += ErrorHandler;

                await processor.StartProcessingAsync();

                Console.WriteLine("Press any key to end the processing...");
                Console.ReadKey();

                await processor.StopProcessingAsync();
            }
        }

        static async Task MessageHandler(ProcessMessageEventArgs args)
        {
            string body = args.Message.Body.ToString();
            Console.WriteLine($"Received message: {body}");

            await args.CompleteMessageAsync(args.Message);
        }

        static Task ErrorHandler(ProcessErrorEventArgs args)
        {
            Console.WriteLine($"Error: {args.Exception.Message}");
            return Task.CompletedTask;
        }
    }
}
