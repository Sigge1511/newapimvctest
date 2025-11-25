using System.IdentityModel.Tokens.Jwt;

namespace api_carrental.Dtos
{
    public class TokenCollection
    {
        public JwtSecurityToken AccessToken { get; set; }
        public JwtSecurityToken RefreshToken { get; set; }

    }
}
