using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace projectWork.Authentication;

public class Authentication
{
    // ======================================================================= Verify access token
    public async Task<Results<Ok, UnauthorizedHttpResult>> VerifyAccessToken(HttpContext context)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = context.User.FindFirst(ClaimTypes.Email)?.Value;

        if(true)// Qui andresti sul DB con userId per prendere i dati dell'utente
            return TypedResults.Ok();
        else
            return TypedResults.Unauthorized();
    }

    // ======================================================================= Verify Refresh token
    public async Task<Results<Ok, UnauthorizedHttpResult>> VerifyRefreshToken(HttpContext context)
    {
        var refreshToken = context.Request.Cookies["RefreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
            return TypedResults.Unauthorized();

        // verifica che il refresh token esista nel DB e non sia scaduto

        var newAccessToken = GenerateAccessToken("id");

        context.Response.Cookies.Append("AccessToken", newAccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddMinutes(15)
        });

        return TypedResults.Ok();
    }

    // ======================================================================= Generate access token
    public string GenerateAccessToken(string userId)
    {
        var jwtKey = "la-tua-chiave-segreta-lunga-almeno-32-caratteri";
        var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(keyBytes),
                SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ======================================================================= Generate refresh token
    public string GenerateRefreshToken()
    {
        return Guid.NewGuid().ToString();
    }
}
