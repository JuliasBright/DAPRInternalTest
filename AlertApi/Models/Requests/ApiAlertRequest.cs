using Common.Models.Requests;

public class ApiAlertRequest : AlertRequest {}

public class SmsRequest {
     public required string PhoneNumber { get; set; }
}