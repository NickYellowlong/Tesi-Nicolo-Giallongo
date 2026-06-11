using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ThesisBackendAPI;

public class ActionRequest
{
    [JsonInclude]
    [Required]
    [JsonPropertyName("content")]
    public string content { get; set; }
}