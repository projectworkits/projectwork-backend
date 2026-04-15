using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using projectWork.Authentication;
using projectWork.Services;

namespace projectWork.Endpoints;

public static class UsersEndpoints
{
    record RegisterRequest(string Username, string Password, string Email);
    public static void AddUsersEndpoints(this IEndpointRouteBuilder route)
    {
        var group = route.MapGroup("/api/users");

        group.MapPost("/register", async Task<Results<Ok, BadRequest>> ([FromBody] RegisterRequest request, HttpContext context) =>
        {
            if(request.Username.Trim() == "" || request.Password.Trim() == "" || request.Email.Trim() == "")
                return TypedResults.BadRequest();

            var passwordServices = context.RequestServices.GetRequiredService<PasswordServices>();
            var (hash, salt) = passwordServices.CreateHash(request.Password.Trim());

            var usersServices = context.RequestServices.GetRequiredService<UsersServices>();
            await usersServices.RegisterUser(request.Username.Trim(), hash, salt, request.Email.Trim());

            return TypedResults.Ok();
        });
    }
}
