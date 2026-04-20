using Dapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.IdentityModel.Tokens;
using projectWork.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace projectWork.Authentication;

public class Authentication
{
    private readonly string _connectionString;
    private readonly string _jwtSecret;
    private readonly PasswordServices _passwordServices;
    public Authentication(IConfiguration configuration, PasswordServices passwordServices)
    {
        _connectionString = configuration.GetConnectionString("db");
        _jwtSecret = configuration["jwtSecret"];
        _passwordServices = passwordServices;
    }

    // ======================================================================= Verify Refresh token
    public async Task<Results<Ok, UnauthorizedHttpResult>> VerifyRefreshToken(HttpContext context)
    {
        var refreshToken = context.Request.Cookies["RefreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
            return TypedResults.Unauthorized();

        // verifica che il refresh token esista nel DB e non sia scaduto
        var userId = await FindRefreshToken(refreshToken);
        if(userId == null)
            return TypedResults.Unauthorized();

        var newAccessToken = GenerateAccessToken(userId.ToString()!);

        context.Response.Cookies.Append("AccessToken", newAccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddMinutes(15)
        });

        return TypedResults.Ok();
    }

    // ======================================================================= Find Refresh token
    public async Task<int?> FindRefreshToken(string refreshToken)
    {
        var hashed = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        var base64Token = Convert.ToBase64String(hashed);

        var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = """
            SELECT
                u.user_id
            FROM sessions s
            JOIN users u ON s.user_id = u.user_id
            WHERE 
                token = @refreshToken AND
                NOW() < s.expires AND
                s.expired = false;
            """;

        return await connection.QueryFirstOrDefaultAsync<int?>(query, new { refreshToken = base64Token });
    }

    // ======================================================================= Generate access token
    public string GenerateAccessToken(string userId)
    {
        var keyBytes = Encoding.UTF8.GetBytes(_jwtSecret);

        var claims = new[]
        {
            new Claim("userId", userId)
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

    // ======================================================================= Verify login
    public async Task<int?> VerifyLogin(string username, string password)
    {
        var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string passwordQuery = """
            SELECT
                password_salt,
                password_hash,
                user_id
            FROM users
            WHERE
                username = @username;
            """;

        var row = await connection.QueryFirstOrDefaultAsync(passwordQuery, new {username});

        if (row is null)
            return null;

        string salt = row.password_salt;
        string hash = row.password_hash;

        if(_passwordServices.VerifyPassword(password, hash, salt))
            return row.user_id;

        return null;
    }

    // ======================================================================= Generate refresh token
    public string GenerateRefreshToken()
    {
        return Guid.NewGuid().ToString();
    }

    // ======================================================================= Save refresh token in db
    public async Task SaveRefreshToken(string refreshToken, int userId)
    {
        var hashed = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        var base64Token = Convert.ToBase64String(hashed);

        var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = """
            INSERT INTO sessions
                (token, user_id)
            VALUES
                (@refreshToken, @userId)
            """;

        await connection.ExecuteAsync(query, new {refreshToken = base64Token, userId});
    }
}
