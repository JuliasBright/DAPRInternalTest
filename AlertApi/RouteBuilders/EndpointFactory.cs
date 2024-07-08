using Common.Interfaces.Requests;
using Common.Models.Requests;
using Common.Models.Requests.Publish;
using Common.Models.Responses;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq.Expressions;
using System.Net;

namespace AlertApi.RouteBuilders
{
    public static class EndpointFactory
    {
        public static void RegisterEndpoints(this WebApplication app)
        {
            app.MapPost("/sendSms", async (HttpContext context) =>
            {
                try
                {
                    using var reader = new StreamReader(context.Request.Body);
                    var requestBody = await reader.ReadToEndAsync();
                    var request = JsonConvert.DeserializeObject<SmsRequest>(requestBody);

                    if (request == null || string.IsNullOrEmpty(request.PhoneNumber))
                    {
                        throw new ArgumentNullException("phoneNumber", "Phone number cannot be null or empty.");
                    }

                    string phoneNumber = request.PhoneNumber;

                    using var client = new DaprClientBuilder().Build();

                    var sms = new Dictionary<string, string>
                    {
                        { "toNumber", phoneNumber }
                    };

                    var jsonContent = JsonConvert.SerializeObject(sms);
                    await client.InvokeBindingAsync("twiliobinding", "create", jsonContent);

                    app.Logger.LogInformation("SMS sent successfully");

                    var pubRequest = new PublishAlertRequest
                    {
                        AlertType = "SMS",
                        PublishRequestTime = DateTime.Now
                    };
                    await client.PublishEventAsync("rabbitmq", "smsSend", pubRequest);

                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(new AlertResponse
                    {
                        Message = "SMS sent successfully",
                        ResponseTime = DateTime.Now
                    }));
                }
                catch (Exception ex)
                {
                    app.Logger.LogError($"Failed to send SMS: {ex.Message}");
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    await context.Response.WriteAsync($"Failed to send SMS: {ex.Message}");
                }
            });

            app.MapPost("/sendEmail", async (HttpContext context) =>
            {
                try
                {
                    using var reader = new StreamReader(context.Request.Body);
                    var requestBody = await reader.ReadToEndAsync();
                    var requestData = JsonConvert.DeserializeObject<Dictionary<string, string>>(requestBody);
                    string emailAddress = requestData["emailAddress"];

                    using var client = new DaprClientBuilder().Build();

                    var email = new Dictionary<string, string>
                    {
                        { "emailTo", emailAddress },
                        { "subject", "This is a test email" },
                        { "body", "This is the body of the test email." }
                    };

                    await client.InvokeBindingAsync("emailbinding", "create", email);

                    app.Logger.LogInformation("Email sent successfully");

                    var pubRequest = new PublishAlertRequest
                    {
                        AlertType = "Email",
                        PublishRequestTime = DateTime.Now
                    };
                    await client.PublishEventAsync("rabbitmq", "sendEmail", pubRequest);

                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(new AlertResponse
                    {
                        Message = "Email sent successfully",
                        ResponseTime = DateTime.Now
                    }));
                }
                catch (Exception ex)
                {
                    app.Logger.LogError($"Failed to send Email: {ex.Message}");
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    await context.Response.WriteAsync($"Failed to send Email: {ex.Message}");
                }
            });

            app.MapPost("/sendAlert", async (HttpContext context) =>
            {
                try
                {
                    using var reader = new StreamReader(context.Request.Body);
                    var requestBody = await reader.ReadToEndAsync();
                    var alertRequest = JsonConvert.DeserializeObject<AlertRequest>(requestBody);

                    using var client = new DaprClientBuilder().Build();
                    foreach (var alertType in alertRequest.AlertTypes)
                    {
                        app.Logger.LogInformation($"Publishing alert: {alertType} for client: {alertRequest.ClientName}");
                        var pubRequest = new PublishAlertRequest
                        {
                            AlertType = alertType,
                            PublishRequestTime = DateTime.Now
                        };

                        await client.PublishEventAsync("rabbitmq", "Alert Api", pubRequest);
                        app.Logger.LogInformation($"Published data at: {pubRequest.PublishRequestTime}");

                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }

                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(new AlertResponse
                    {
                        Message = "Alerts published successfully",
                        ResponseTime = DateTime.Now
                    }));
                }
                catch (Exception ex)
                {
                    app.Logger.LogError($"Failed to publish alerts: {ex.Message}");
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    await context.Response.WriteAsync($"Failed to publish alerts: {ex.Message}");
                }
            });
        }
    }
}
