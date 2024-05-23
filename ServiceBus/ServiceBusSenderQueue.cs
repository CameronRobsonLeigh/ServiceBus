using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

class ServiceBusSenderQueue
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

        await SendMessagesAsync(connectionString, queueName);
    }

    static async Task SendMessagesAsync(string connectionString, string queueName)
    {
        await using (ServiceBusClient client = new ServiceBusClient(connectionString))
        {
            ServiceBusSender sender = client.CreateSender(queueName);

            for (int i = 1; i <= 10; i++)
            {
                string messageBody = $"Message {i}";
                ServiceBusMessage message = new ServiceBusMessage(messageBody);
                Console.WriteLine($"Sending message: {messageBody}");
                await sender.SendMessageAsync(message);
            }
        }
    }
}
