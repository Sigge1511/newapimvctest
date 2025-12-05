using System.IdentityModel.Tokens.Jwt;

namespace api_carrental.Dtos
{
    public class TokenCollection
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }

    }
}
