namespace api_carrental.Constants
{
    public class JwtSettings
    {
        public string SecretKey { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int AccessTokenExpInMinutes { get; set; } 
        public int RefreshTokenExpInHours { get; set; }  
    }
}
