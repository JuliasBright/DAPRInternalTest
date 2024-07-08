using Dapr.Client;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Common.Models.Requests;
using Common.Models.Requests.Publish;
using Common.Models.Responses;
using Newtonsoft.Json;
using System.Data;
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
           //TODO:: uncomment upon demo 
        // string phoneNumber = "+1234567890";
        // var requestData = new Dictionary<string, string>
        // {
        //     { "PhoneNumber", phoneNumber }
        // };
        // var jsonContent = JsonConvert.SerializeObject(requestData);


        // var smsResponse = await client.InvokeMethodAsync<string, string>(
        //     HttpMethod.Post,
        //     "AlertApi",
        //     "sendSms",
        //     jsonContent);

        // Console.WriteLine($"sendSms Response: {smsResponse}");


         // Invoke sendEmail endpoint
         //TODO:: uncomment upon demo
        // string emailAddress = "juliasbright@gmail.com";
        // var emailPayload = new Dictionary<string, string>
        // {
        //     { "emailAddress", emailAddress }
        // };
        // var jsonContents = JsonConvert.SerializeObject(emailPayload);
        // var emailResponse = await client.InvokeMethodAsync<string, string>(
        //     HttpMethod.Post,
        //     "AlertApi",
        //     "sendEmail",
        //     jsonContents);

        // Console.WriteLine($"sendEmail Response: {emailResponse}");

        // Invoke sendAlert endpoint
        var alertResponse = await client.InvokeMethodAsync<AlertRequest, AlertResponse>(
            HttpMethod.Post,
            "AlertApi",
            "sendAlert",
            alertRequest);

        Console.WriteLine($"sendAlert Response: {alertResponse}");

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
}
