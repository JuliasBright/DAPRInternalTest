using Common.Interfaces.Requests;
using Common.Models.Requests;
using Common.Models.Requests.Publish;
using Common.Models.Responses;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace AlertApi.RouteBuilders
{
    public static class EndpointFactory
    {
        public static void RegisterEndpoints(this WebApplication app)
        {
            app.MapPost("/sendSms", async ([FromBody] string phoneNumber) =>
            {
                // Invoke a binding to an sms service provider (You will need to create your own twilion account) 
                // No body as free tier of twilio offers no message capability
                using var client = new DaprClientBuilder().Build();

                var sms = new Dictionary<string, string>();
                sms.Add("toNumber", phoneNumber);

                app.Logger.LogInformation("SMS sent succesfully");
            });

            app.MapPost("/sendEmail", async ([FromBody] string emailAddress) =>
            {
                // Invoke a binding to an email service provider (you will need to create your own sendgrid account)
                using var client = new DaprClientBuilder().Build();

                var email = new Dictionary<string, string>();
                email.Add("emailTo", emailAddress);
                email.Add("subject", "This is a test email");

                app.Logger.LogInformation("Email sent succesfully");
            });

            app.MapPost("/sendAlert", async (AlertRequest alertRequest) =>
            {
                // Publish multiple events via rabbitmq (could also use redis but only in self-hosted kubernetes mode) 
                using var client = new DaprClientBuilder().Build();

                for (int i = 0; i < alertRequest.AlertTypes.Count(); i++)
                {
                    app.Logger.LogInformation("Publishing alert: " + alertRequest.AlertTypes[i] + "\n      For client: " + alertRequest.ClientName);
                    DateTime now = DateTime.Now;

                    PublishAlertRequest pubRequest = new PublishAlertRequest()
                    {
                        AlertType = alertRequest.AlertTypes[i],
                        PublishRequestTime = now
                    };

                    app.Logger.LogInformation("Published data at: " + pubRequest.PublishRequestTime);    

                    await Task.Delay(TimeSpan.FromSeconds(1));
                }

            });
        }
    }
}
