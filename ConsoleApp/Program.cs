using Dapr.Client;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Common.Models.Requests;

using var client = new DaprClientBuilder().Build();

List<string> alerts = new() { "Alert1", "Alert1", "Alert3" };

var alertrequest = new AlertRequest
{
    AlertTypes = alerts,
    ClientName = "Generic Client"
};

// Invoke each method of the AlertApi here

Console.Read();

