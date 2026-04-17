using Dapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using projectWork.Authentication;
using projectWork.Models;
using projectWork.Services;

namespace projectWork.Endpoints;

public static class UsersEndpoints
{
    record RegisterRequest(string Username, string Password, string Email);
    public static void AddUsersEndpoints(this IEndpointRouteBuilder route)
    {
        var group = route.MapGroup("/api/users").WithTags("api users");

        group.MapPost("/register", async Task<Results<Created, BadRequest>> ([FromBody] RegisterRequest request, HttpContext context,
                                                                        PasswordServices passwordServices, UsersServices usersServices) =>
        {
            if(string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.Email))
                return TypedResults.BadRequest();

            var (hash, salt) = passwordServices.CreateHash(request.Password.Trim());
            
            await usersServices.RegisterUser(request.Username.Trim(), hash, salt, request.Email.Trim());

            return TypedResults.Created();
        });

        group.MapGet("/user", async Task<Results<Ok<User>, UnauthorizedHttpResult>> (HttpContext context, UsersServices usersServices) =>
        {
            var stringUserId = context.User.FindFirst("userId")?.Value;

            if (!int.TryParse(stringUserId, out int userId))
                return TypedResults.Unauthorized();
            
            User user = await usersServices.GetById(userId);

            return TypedResults.Ok(user);
        }).RequireAuthorization().WithName("get self");

        group.MapGet("/users", async Task<Ok<IEnumerable<User>>> (UsersServices usersServices) =>
        {
            //potrebbe richiedere di essere collaboratori o admin

            return TypedResults.Ok(await usersServices.GetList());
        }).RequireAuthorization();

        group.MapGet("/user/{id:int}", async Task<Results<Ok<User>, NotFound>> (UsersServices usersServices, int id) =>
        {
            //potrebbe richiedere di essere collaboratori o admin

            User user = await usersServices.GetById(id);
            if(user is null)
                return TypedResults.NotFound();

            return TypedResults.Ok(user);
        }).RequireAuthorization();

        group.MapPut("/user", async Task<Results<NoContent, NotFound>> (UsersServices usersServices, User user) =>
        {
            //potrebbe richiedere di essere collaboratori o admin
            
            if (await usersServices.GetById(user.Id) is null)
                return TypedResults.NotFound();

            await usersServices.UpdateUser(user);
            return TypedResults.NoContent();
        }).RequireAuthorization();

        group.MapDelete("/user/{id:int}", async Task<Results<NoContent, UnauthorizedHttpResult, NotFound>> (UsersServices usersServices, HttpContext context, int id) =>
        {
            //potrebbe richiedere di essere collaboratori o admin

            if (await usersServices.GetById(id) is null)
                return TypedResults.NotFound();

            if(await usersServices.IsAdmin(id))
                return TypedResults.Unauthorized();

            await usersServices.DeleteUser(id);

            context.Response.Cookies.Delete("AccessToken");
            context.Response.Cookies.Delete("RefreshToken");

            return TypedResults.NoContent();

        }).RequireAuthorization();
    }
}
