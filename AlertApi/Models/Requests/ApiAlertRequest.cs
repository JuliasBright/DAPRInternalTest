using System.ComponentModel.DataAnnotations;
using Common.Models.Requests;

/// <summary>
/// ApiAlertRequest class, inherits from AlertRequest.
/// This class is used to send API alerts and does not have any additional properties.
/// </summary>
public class ApiAlertRequest : AlertRequest {}

/// <summary>
/// SmsRequest class, used to send SMS requests.
/// </summary>
public class SmsRequest {
    /// <summary>
    /// The phone number to send the SMS to.
    /// This property is required and must not be null or empty.
    /// </summary>
    [Required]
    [RegularExpression(@"^\+?\d{1,4}?[-.\s]?\(?(?:\d{2,3}|\d{4})\)?[-.\s]?\d\d\d?[-.\s]?\d\d\d\d$", ErrorMessage = "Invalid phone number format.")]
    [StringLength(20, MinimumLength = 10, ErrorMessage = "Phone number must be between 10 and 20 characters.")]
    public required string PhoneNumber { get; set; }
}
