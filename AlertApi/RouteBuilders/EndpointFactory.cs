using Common.Models.Requests;
using Common.Models.Requests.Publish;
using Common.Models.Responses;
using Dapr.Client;
using Newtonsoft.Json;
using System.Net;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using SendGrid;
using SendGrid.Helpers.Mail;

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

                var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
                var twilioAccountSid = configuration["Twilio:AccountSid"];
                var twilioAuthToken = configuration["Twilio:AuthToken"];
                var fromNumber = configuration["Twilio:From"];

                if (string.IsNullOrEmpty(twilioAccountSid) || string.IsNullOrEmpty(twilioAuthToken) || string.IsNullOrEmpty(fromNumber))
                {
                    throw new Exception("Twilio configuration is incomplete.");
                }

                TwilioClient.Init(twilioAccountSid, twilioAuthToken);

                var message = await MessageResource.CreateAsync(
                    body: "Dapr is awesome",
                    from: new Twilio.Types.PhoneNumber(fromNumber),
                    to: new Twilio.Types.PhoneNumber(phoneNumber)
                );

                app.Logger.LogInformation($"SMS sent successfully. Message SID: {message.Sid}");

                var metadata = new Dictionary<string, string>
                {
                    { "toNumber", phoneNumber },
                    { "fromNumber", fromNumber }
                };

                await client.InvokeBindingAsync<object>("twiliobinding", "create", null, metadata);
                var subApiResponse = await client.InvokeBindingAsync<object, object>("subapi", "post", metadata);
                app.Logger.LogInformation($"Posted to SubApi: {(int)HttpStatusCode.OK}");
                
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                await context.Response.WriteAsync("SMS sent successfully");
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
                 {   using var client = new DaprClientBuilder().Build();
                     var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
                     var requestData = JsonConvert.DeserializeObject<Dictionary<string, string>>(requestBody);
                     if (requestData == null || !requestData.ContainsKey("emailAddress"))
                     {
                         throw new ArgumentException("Invalid request body: 'emailAddress' is required.");
                     }

                     var emailAddress = requestData["emailAddress"];
                     var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
                     var fromEmail = configuration["EmailSettings:FromEmail"];
                     var emailApiKey = configuration["EmailSettings:ApiKey"];

                     if (string.IsNullOrEmpty(fromEmail))
                     {
                         throw new Exception("From email address is not configured.");
                     }

                     if (string.IsNullOrEmpty(emailApiKey))
                     {
                         throw new Exception("SendGrid API key is not configured.");
                     }

                    var sendGridClient = new SendGridClient(emailApiKey);

                    // Prepare email content
                    var from = new EmailAddress(fromEmail);
                    var to = new EmailAddress(emailAddress);
                    var subject = "This is a test email";
                    var plainTextContent = "This is the body of the test email.";
                    var htmlContent = "<strong>This is the body of the test email.</strong>";
                    var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

                    // Send email asynchronously
                    var response = await sendGridClient.SendEmailAsync(msg);


                     var metadata = new Dictionary<string, string>
                     {
                        { "emailFrom", fromEmail },
                        { "emailTo", emailAddress },
                        { "subject", "This is a test email" },
                        { "body", "This is the body of the test email." }
                     };

                     metadata.Add("api-key", emailApiKey);

                     app.Logger.LogInformation($"Sending email with metadata: {JsonConvert.SerializeObject(metadata)}");

                     try
                     {
                         await client.InvokeBindingAsync<object>("emailbinding", "create", null, metadata);
                         context.Response.StatusCode = (int)HttpStatusCode.OK;
                         await context.Response.WriteAsync("Email sent successfully");
                     }
                     catch (Dapr.Client.InvocationException ex)
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
                        var subApiResponse = await client.InvokeBindingAsync<object, object>("subapi", "post", pubRequest);
                        app.Logger.LogInformation($"Posted to SubApi: {(int)HttpStatusCode.OK}");

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

        private static async Task SendSmsAsync(DaprClient client, Dictionary<string, string> metadata, HttpContext context, ILogger logger)
        {
            await client.InvokeBindingAsync<object>("emailbinding", "create", null, (IReadOnlyDictionary<string, string>)metadata);
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
    }
}