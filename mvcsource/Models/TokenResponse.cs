using System.Text.Json.Serialization;

namespace assignment_mvc_carrental.Models
{
    public class TokenResponse
    {
        //Försöker vara övertydligen så allt
        //funkar när mvc ska läsa min token       
        
        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; }
        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; }
    }
}
