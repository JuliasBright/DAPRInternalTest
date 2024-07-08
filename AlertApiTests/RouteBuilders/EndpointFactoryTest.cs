using AlertApi.RouteBuilders;
using Common.Models.Requests;
using Common.Models.Requests.Publish;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace AlertApi.Tests
{
    public class EndpointFactoryTests
    {
        [Test]
        public async Task SendSms_Endpoint_ValidRequest_ReturnsOk()
        {
            // Arrange
            var app = new Mock<WebApplication>();

            var requestBody = JsonConvert.SerializeObject(new ApiAlertRequest
            {
                PhoneNumber = "+1234567890"
            });
            var request = new DefaultHttpContext().Request;
            request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(requestBody));
            request.ContentType = "application/json";

            var response = new DefaultHttpContext().Response;

            app.Setup(a => a.MapPost("/sendSms", It.IsAny<Func<HttpContext, Task>>()))
               .Callback<string, Func<HttpContext, Task>>((path, handler) =>
               {
                   handler(new DefaultHttpContext { Request = request, Response = response }).Wait();
               });

            // Act
            await EndpointFactory.RegisterEndpoints(app.Object);

            // Assert
            Assert.AreEqual((int)HttpStatusCode.OK, response.StatusCode);
            var responseBody = await new StreamReader(response.Body).ReadToEndAsync();
            var alertResponse = JsonConvert.DeserializeObject<AlertResponse>(responseBody);
            Assert.IsNotNull(alertResponse);
            Assert.AreEqual("SMS sent successfully", alertResponse.Message);
        }

        [Test]
        public async Task SendEmail_Endpoint_ValidRequest_ReturnsOk()
        {
            // Arrange
            var app = new Mock<WebApplication>();

            var requestBody = JsonConvert.SerializeObject(new Dictionary<string, string>
            {
                { "emailAddress", "test@example.com" }
            });
            var request = new DefaultHttpContext().Request;
            request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(requestBody));
            request.ContentType = "application/json";

            var response = new DefaultHttpContext().Response;

            app.Setup(a => a.MapPost("/sendEmail", It.IsAny<Func<HttpContext, Task>>()))
               .Callback<string, Func<HttpContext, Task>>((path, handler) =>
               {
                   handler(new DefaultHttpContext { Request = request, Response = response }).Wait();
               });

            // Act
            await EndpointFactory.RegisterEndpoints(app.Object);

            // Assert
            Assert.AreEqual((int)HttpStatusCode.OK, response.StatusCode);
            var responseBody = await new StreamReader(response.Body).ReadToEndAsync();
            var alertResponse = JsonConvert.DeserializeObject<AlertResponse>(responseBody);
            Assert.IsNotNull(alertResponse);
            Assert.AreEqual("Email sent successfully", alertResponse.Message);
        }

        [Test]
        public async Task SendAlert_Endpoint_ValidRequest_ReturnsOk()
        {
            // Arrange
            var app = new Mock<WebApplication>();

            var requestBody = JsonConvert.SerializeObject(new AlertRequest
            {
                ClientName = "TestClient",
                AlertTypes = new List<string> { "AlertType1", "AlertType2" }
            });
            var request = new DefaultHttpContext().Request;
            request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(requestBody));
            request.ContentType = "application/json";

            var response = new DefaultHttpContext().Response;

            app.Setup(a => a.MapPost("/sendAlert", It.IsAny<Func<HttpContext, Task>>()))
               .Callback<string, Func<HttpContext, Task>>((path, handler) =>
               {
                   handler(new DefaultHttpContext { Request = request, Response = response }).Wait();
               });

            // Act
            await EndpointFactory.RegisterEndpoints(app.Object);

            // Assert
            Assert.AreEqual((int)HttpStatusCode.OK, response.StatusCode);
            var responseBody = await new StreamReader(response.Body).ReadToEndAsync();
            var alertResponse = JsonConvert.DeserializeObject<AlertResponse>(responseBody);
            Assert.IsNotNull(alertResponse);
            Assert.AreEqual("Alerts published successfully", alertResponse.Message);
        }
    }
}
