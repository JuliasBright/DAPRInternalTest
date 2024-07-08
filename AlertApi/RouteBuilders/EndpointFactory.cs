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
using System.Text.Json;

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
                    var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
                    var request = JsonConvert.DeserializeObject<SmsRequest>(requestBody)
                                    ?? throw new ArgumentNullException("request", "Request cannot be null.");
                    var phoneNumber = request.PhoneNumber ?? throw new ArgumentNullException("phoneNumber", "Phone number cannot be null or empty.");

                    using var client = new DaprClientBuilder().Build();

                    var sms = new Dictionary<string, string> { { "toNumber", phoneNumber } };
                    var jsonContent = JsonConvert.SerializeObject(sms);

                    await (await client.InvokeBindingAsync("twiliobinding", "create", jsonContent))
                        .ContinueWith(t => app.Logger.LogInformation("SMS sent successfully"))
                        .Unwrap()
                        .ContinueWith(t => client.PublishEventAsync("rabbitmq", "smsSend", new PublishAlertRequest
                        {
                            AlertType = "SMS",
                            PublishRequestTime = DateTime.Now
                        }))
                        .Unwrap()
                        .ContinueWith(t => context.Response.WriteAsync(JsonConvert.SerializeObject(new AlertResponse
                        {
                            Message = "SMS sent successfully",
                            ResponseTime = DateTime.Now
                        })))
                        .Unwrap()
                        .ContinueWith(t => context.Response.StatusCode = (int)HttpStatusCode.OK);
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
                    var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
                    var requestData = JsonConvert.DeserializeObject<Dictionary<string, string>>(requestBody);
                    var emailAddress = requestData["emailAddress"];

                    using var client = new DaprClientBuilder().Build();

                    var email = new Dictionary<string, string>
                    {
                        { "emailTo", emailAddress },
                        { "subject", "This is a test email" },
                        { "body", "This is the body of the test email." }
                    };
                    var jsonContent = JsonConvert.SerializeObject(email);

                    await (await client.InvokeBindingAsync("emailbinding", "create", jsonContent))
                        .ContinueWith(t => app?.Logger.LogInformation("Email sent successfully"))
                        .Unwrap()
                        .ContinueWith(t => client.PublishEventAsync("rabbitmq", "sendEmail", new PublishAlertRequest
                        {
                            AlertType = "Email",
                            PublishRequestTime = DateTime.Now
                        }))
                        .Unwrap()
                        .ContinueWith(t => context.Response.WriteAsync(JsonConvert.SerializeObject(new AlertResponse
                        {
                            Message = "Email sent successfully",
                            ResponseTime = DateTime.Now
                        })))
                        .Unwrap()
                        .ContinueWith(t => { context.Response.StatusCode = (int)HttpStatusCode.OK; return Task.CompletedTask; });
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
                    var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
                    var alertRequest = JsonConvert.DeserializeObject<AlertRequest>(requestBody)
                                       ?? throw new ArgumentNullException("request", "Request cannot be null.");

                    using var client = new DaprClientBuilder().Build();

                    foreach (var alertType in alertRequest.AlertTypes)
                    {
                        app.Logger.LogInformation($"Publishing alert: {alertType} for client: {alertRequest.ClientName}");
                        var pubRequest = new PublishAlertRequest
                        {
                            AlertType = alertType,
                            PublishRequestTime = DateTime.Now
                        };

                        await (await client.PublishEventAsync("rabbitmq", "Alert Api", pubRequest))
                         .ContinueWith(t => app?.Logger.LogInformation($"Published data at: {pubRequest.PublishRequestTime}"))
                         .Unwrap()
                         .ContinueWith(t => Task.Delay(TimeSpan.FromSeconds(1)))
                         .Unwrap();
                    }

                    await context.Response.WriteAsync(JsonConvert.SerializeObject(new AlertResponse
                    {
                        Message = "Alerts published successfully",
                        ResponseTime = DateTime.Now
                    }));
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
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