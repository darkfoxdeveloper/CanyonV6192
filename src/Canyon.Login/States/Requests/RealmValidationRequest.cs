using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Canyon.Login.States.Requests
{
    public class RealmValidationRequest
    {
        [Required]
        [JsonPropertyName("realmId")]
        public Guid RealmId { get; set; }
        [Required]
        [JsonPropertyName("userName")]
        public string UserName { get; set; }
        [Required]
        [JsonPropertyName("password")]
        public string Password { get; set; }
    }
}
