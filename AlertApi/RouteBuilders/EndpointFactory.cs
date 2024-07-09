using Common.Interfaces.Requests;
using Common.Models.Requests;
using Common.Models.Requests.Publish;
using Common.Models.Responses;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq.Expressions;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

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

                    await SendSmsAsync(client, jsonContent, context, app.Logger);
                }
                catch (Exception ex)
                {
                    app.Logger.LogError($"Failed to send SMS: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        app.Logger.LogError($"InnerException: {ex.InnerException.Message}");
                    }
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

            var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
            var fromEmail = configuration["EmailSettings:FromEmail"];

            using var client = new DaprClientBuilder().Build();

            var email = new Dictionary<string, string>
            {
                { "from", fromEmail },
                { "to", emailAddress },
                { "subject", "This is a test email" },
                { "body", "This is the body of the test email." }
            };
            var jsonContent = JsonConvert.SerializeObject(email);

            app.Logger.LogInformation($"Sending email with JSON content: {jsonContent}");

            await client.InvokeBindingAsync("emailbinding", "create", jsonContent);

            await context.Response.WriteAsync("Email sent successfully");
        }
        catch (Exception ex)
        {
            app.Logger.LogError($"Failed to send Email: {ex.Message}");
            if (ex.InnerException != null)
            {
                app.Logger.LogError($"InnerException: {ex.InnerException.Message}");
                app.Logger.LogError($"InnerException StackTrace: {ex.InnerException.StackTrace}");
            }
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

        private static async Task SendSmsAsync(DaprClient client, string jsonContent, HttpContext context, ILogger logger)
        {
            await client.InvokeBindingAsync("twiliobinding", "create", jsonContent);
            logger.LogInformation("SMS sent successfully");

            await client.PublishEventAsync("rabbitmq", "smsSend", new PublishAlertRequest
            {
                AlertType = "SMS",
                PublishRequestTime = DateTime.Now
            });

            var response = new AlertResponse
            {
                Message = "SMS sent successfully",
                ResponseTime = DateTime.Now
            };

            await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
            context.Response.StatusCode = (int)HttpStatusCode.OK;
        }

        private static async Task SendEmailAsync(DaprClient client, string jsonContent, HttpContext context, ILogger logger)
        {
            try
            {
                await client.InvokeBindingAsync("emailbinding", "create", jsonContent);
                logger.LogInformation("Email sent successfully");

                await client.PublishEventAsync("rabbitmq", "sendEmail", new PublishAlertRequest
                {
                    AlertType = "Email",
                    PublishRequestTime = DateTime.Now
                });

                var response = new AlertResponse
                {
                    Message = "Email sent successfully",
                    ResponseTime = DateTime.Now
                };

                await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
                context.Response.StatusCode = (int)HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to send Email: {ex.Message}");
                if (ex.InnerException != null)
                {
                    logger.LogError($"InnerException: {ex.InnerException.Message}");
                }
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await context.Response.WriteAsync($"Failed to send Email: {ex.Message}");
            }
        }

    }
}