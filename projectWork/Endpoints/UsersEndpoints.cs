using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using projectWork.Authentication;
using projectWork.Models;
using projectWork.Services;
using System.Security.Claims;

namespace projectWork.Endpoints;

public static class UsersEndpoints
{
    public static void AddUsersEndpoints(this IEndpointRouteBuilder route)
    {
        var group = route.MapGroup("/api/users").WithTags("api users");

        group.MapGet("/user", async Task<Results<Ok<User>, UnauthorizedHttpResult>>
            (HttpContext context, UsersServices usersServices) =>
        {
            var stringUserId = context.User.FindFirstValue("userId");

            if (!int.TryParse(stringUserId, out int userId))
                return TypedResults.Unauthorized();
            
            User user = await usersServices.GetByIdAsync(userId);

            return TypedResults.Ok(user);
        }).RequireAuthorization().WithName("get self");

        group.MapGet("/", async Task<Results<Ok<IEnumerable<User>>, ForbidHttpResult, UnauthorizedHttpResult>>
            (UsersServices usersServices, HttpContext context) =>
        {
            //------------------------- check se admin o collaboratore
            var stringUserId = context.User.FindFirstValue("userId");

            if (!int.TryParse(stringUserId, out int userId))
                return TypedResults.Unauthorized();

            if (!(await usersServices.IsAdmin(userId) || await usersServices.IsCollaborator(userId)))
                return TypedResults.Forbid();
            //--------------------------------------------------------

            return TypedResults.Ok(await usersServices.GetListAsync());
        }).RequireAuthorization();

        group.MapGet("/{id:int}", async Task<Results<Ok<User>, NotFound, ForbidHttpResult, UnauthorizedHttpResult>>
            (UsersServices usersServices, HttpContext context, int id) =>
        {
            //------------------------- check se admin o collaboratore
            var stringUserId = context.User.FindFirstValue("userId");

            if (!int.TryParse(stringUserId, out int userId))
                return TypedResults.Unauthorized();

            if (!(await usersServices.IsAdmin(userId) || await usersServices.IsCollaborator(userId)))
                return TypedResults.Forbid();
            //--------------------------------------------------------

            User user = await usersServices.GetByIdAsync(id);
            if(user is null)
                return TypedResults.NotFound();

            return TypedResults.Ok(user);
        }).RequireAuthorization();

        group.MapPost("/register", async Task<Results<Created, BadRequest>>
            (RegisterRequest request, PasswordServices passwordServices, UsersServices usersServices) =>
        {
            var username = request.Username;
            var password = request.Password;
            var email = request.Email;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(email))
                return TypedResults.BadRequest();

            var (hash, salt) = passwordServices.CreateHash(password.Trim());

            await usersServices.InsertAsync(username.Trim(), hash, salt, email.Trim());

            return TypedResults.Created();
        });

        group.MapPut("/", async Task<Results<NoContent, NotFound, UnauthorizedHttpResult, ForbidHttpResult>>
            (UsersServices usersServices, HttpContext context, User user) =>
        {
            //------------------------- check se admin o collaboratore
            var stringUserId = context.User.FindFirstValue("userId");

            if (!int.TryParse(stringUserId, out int userId))
                return TypedResults.Unauthorized();

            if (!(await usersServices.IsAdmin(userId) || await usersServices.IsCollaborator(userId)))
                return TypedResults.Forbid();
            //--------------------------------------------------------

            if (await usersServices.GetByIdAsync(user.Id) is null)
                return TypedResults.NotFound();

            await usersServices.UpdateAsync(user);
            return TypedResults.NoContent();
        }).RequireAuthorization();

        group.MapDelete("/{id:int}", async Task<Results<NoContent, NotFound, UnauthorizedHttpResult, ForbidHttpResult>>
            (UsersServices usersServices, HttpContext context, int id) =>
        {
            //------------------------- check se admin o collaboratore
            var stringUserId = context.User.FindFirstValue("userId");

            if (!int.TryParse(stringUserId, out int userId))
                return TypedResults.Unauthorized();

            if (!(await usersServices.IsAdmin(userId) || await usersServices.IsCollaborator(userId)))
                return TypedResults.Forbid();
            //--------------------------------------------------------

            if (await usersServices.GetByIdAsync(id) is null)
                return TypedResults.NotFound();

            if(await usersServices.IsAdmin(id))
                return TypedResults.Unauthorized();

            await usersServices.DeleteAsync(id);

            context.Response.Cookies.Delete("AccessToken");
            context.Response.Cookies.Delete("RefreshToken");

            await usersServices.ExpireRefreshToken(id);

            return TypedResults.NoContent();

        }).RequireAuthorization();
    }
}

// ======================================================================== record necessari alle richieste

public record RegisterRequest(string Username, string Password, string Email)
{

};
