using Dapr.Client;
using Common.Models.Requests;
using Common.Models.Requests.Publish;

class Program
{
    static async Task Main(string[] args)
    {
        using var client = new DaprClientBuilder().Build();

        List<string> alerts = new() { "SendSms", "SendEmail", "SendAlert" };

        var alertRequest = new AlertRequest
        {
            AlertTypes = alerts,
            ClientName = "Generic Client"
        };

        // Invoke SendSms endpoint
        string phoneNumber = "+27847576329";
        var requestData = new SmsRequest
        {
            PhoneNumber = phoneNumber
        };
        var smsPayload = new Dictionary<string, string>
        {
            { "PhoneNumber", phoneNumber }
        };

        await InvokeMethodAsync(client, HttpMethod.Post, "AlertApi", "sendSms", smsPayload);

        // Invoke sendEmail endpoint
        string emailAddress = "juliasbright@gmail.com";
        var emailPayload = new Dictionary<string, string>
        {
            { "emailAddress", emailAddress }
        };

        await InvokeMethodAsync(client, HttpMethod.Post, "AlertApi", "sendEmail", emailPayload);

        // Invoke sendAlert endpoint
        await InvokeMethodAsync(client, HttpMethod.Post, "AlertApi", "sendAlert", alertRequest);

        foreach (var alertType in alertRequest.AlertTypes)
        {
            var pubRequest = new PublishAlertRequest
            {
                AlertType = alertType,
                PublishRequestTime = DateTime.Now
            };

            await client.PublishEventAsync("rabbitmq", "Send Alert from Console App", pubRequest);

            Console.WriteLine($"Published alert: {pubRequest.AlertType}");

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        Console.ReadLine();
    }

    static async Task InvokeMethodAsync(DaprClient client, HttpMethod method, string app, string methodname, object payload)
    {
        try
        {
            var response = await client.InvokeMethodAsync<object, object>(method, app, methodname, payload);
            Console.WriteLine($"{methodname} Response: {response}");
        }
        catch (InvocationException ex)
        {
            await HandleExceptionAsync(ex);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred: {ex.Message}");
        }
    }

    static async Task HandleExceptionAsync(InvocationException ex)
    {
        Console.WriteLine($"An error occurred while invoking method: {ex.Message}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
        }
        if (ex.Response != null)
        {
            Console.WriteLine("Error response:");
            Console.WriteLine(await ex.Response.Content.ReadAsStringAsync());
        }
    }
}