using Microsoft.AspNetCore.Http.HttpResults;

namespace projectWork.Authentication;

public static class AuthEndpoints
{
    public static void AddAuthenticationEndpoints(this IEndpointRouteBuilder route)
    {
        route.MapGet("/api/auth/refresh", async Task<Results<Ok, UnauthorizedHttpResult>> (HttpContext context) =>
        {
            var authService = context.RequestServices.GetRequiredService<Authentication>();
            var result = await authService.VerifyRefreshToken(context);

            if (result.Result is not Ok)
                return TypedResults.Unauthorized();

            return TypedResults.Ok();
        });

        route.MapGet("/api/auth/logout", async Task<Ok> (HttpContext context) =>
        {
            context.Response.Cookies.Delete("AccessToken");
            context.Response.Cookies.Delete("RefreshToken");

            return TypedResults.Ok();
        });

        route.MapPost("/api/auth/login", async Task<Results<Ok, BadRequest>> (HttpContext context) =>
        {
            var authService = context.RequestServices.GetRequiredService<Authentication>();

            var email = context.Request.Form["email"].ToString();
            var password = context.Request.Form["password"].ToString();

            if (email == "" || password == "")
                return TypedResults.BadRequest();

            //verificare mail e password dal db e prendere user id

            var userId = "0";
            var accessToken = authService.GenerateAccessToken(userId);
            var refreshToken = Guid.NewGuid().ToString();

            // memorizza nel db il refreshToken (hash forte)

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
        });
    }
}
