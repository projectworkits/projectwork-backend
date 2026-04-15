using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace projectWork.Authentication;

public static class AuthEndpoints
{
    record LoginRequest(string Username, string Password);
    public static void AddAuthenticationEndpoints(this IEndpointRouteBuilder route)
    {
        var group = route.MapGroup("/api/auth");

        group.MapGet("/refresh", async Task<Results<Ok, UnauthorizedHttpResult>> (HttpContext context) =>
        {
            var authService = context.RequestServices.GetRequiredService<Authentication>();
            var result = await authService.VerifyRefreshToken(context);

            if (result.Result is not Ok)
                return TypedResults.Unauthorized();

            return TypedResults.Ok();
        });

        group.MapGet("/logout", async Task<Ok> (HttpContext context) =>
        {
            context.Response.Cookies.Delete("AccessToken");
            context.Response.Cookies.Delete("RefreshToken");

            return TypedResults.Ok();
        }); 

        group.MapPost("/login", async Task<Results<Ok, BadRequest>> ([FromBody] LoginRequest request, HttpContext context) =>
        {
            var authService = context.RequestServices.GetRequiredService<Authentication>();

            if (request.Username == "" || request.Password == "")
                return TypedResults.BadRequest();

            var userId = await authService.VerifyLogin(request.Username, request.Password);
            if (userId is null)
                return TypedResults.BadRequest();

            var accessToken = authService.GenerateAccessToken(userId.ToString()!);
            var refreshToken = Guid.NewGuid().ToString();

            //token viene hashato con sha256, tradotto in base64 e salvato nel db
            await authService.SaveRefreshToken(refreshToken, (int)userId);

            context.Response.Cookies.Append("AccessToken", accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddMinutes(15)
            });

            context.Response.Cookies.Append("RefreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

            return TypedResults.Ok();
        }).Accepts<LoginRequest>("application/json").DisableAntiforgery();
    }
}
