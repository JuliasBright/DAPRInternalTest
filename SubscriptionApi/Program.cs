using System.Text.Json.Serialization;
using Common.Models.Requests;
using Common.Models.Requests.Publish;
using Dapr;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

if (app.Environment.IsDevelopment()) { app.UseDeveloperExceptionPage(); }


app.UseCloudEvents();

app.MapPost("/alert", (PublishAlertRequest pubRequest) =>
{
    Console.WriteLine($"Subscriber received: {pubRequest.AlertType} at {pubRequest.PublishRequestTime}");
    return Results.Ok();
}).WithTopic("rabbitmq", "Subscriber");

app.MapSubscribeHandler();


await app.RunAsync();
