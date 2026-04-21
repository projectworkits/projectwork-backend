using Microsoft.AspNetCore.Http.HttpResults;
using projectWork.Models;
using projectWork.Services;
using System.Security.Claims;

namespace projectWork.Endpoints;

public static class ProductsEndpoints
{
    public static void AddProductsEndpoints(this IEndpointRouteBuilder route)
    {
        var group = route.MapGroup("/api/products").WithTags("api products");

        group.MapGet("/", async Task<Ok<IEnumerable<Product>>> (ProductsServices productsServices) =>
        {
            return TypedResults.Ok(await productsServices.GetListAsync());
        });

        group.MapGet("/{id:int}", async Task<Results<Ok<Product>, NotFound>> 
            (ProductsServices productsServices, int id) =>
        {
            var product = await productsServices.GetByIdAsync(id);

            if (product is null)
                return TypedResults.NotFound();

            return TypedResults.Ok(product);
        });

        group.MapPost("/", async Task<Results<Created, BadRequest, UnauthorizedHttpResult, ForbidHttpResult>>
            (ProductsServices productsServices, UsersServices usersServices, HttpContext context, InsertProduct productRecord) =>
        {
            //------------------------- check se admin o collaboratore
            var stringUserId = context.User.FindFirstValue("userId");

            if (!int.TryParse(stringUserId, out int userId))
                return TypedResults.Unauthorized();

            if (!(await usersServices.IsAdmin(userId) || await usersServices.IsCollaborator(userId)))
                return TypedResults.Forbid();
            //--------------------------------------------------------

            // fare controllo input dentro il record in caso
            var product = productRecord.ToEntity();

            await productsServices.InsertAsync(product);

            return TypedResults.Created();
                
        }).RequireAuthorization();

        group.MapPut("/", async Task<Results<NoContent, NotFound, UnauthorizedHttpResult, ForbidHttpResult>>
            (ProductsServices productsServices, UsersServices usersServices, HttpContext context, Product product) =>
        {
            //------------------------- check se admin o collaboratore
            var stringUserId = context.User.FindFirstValue("userId");

            if (!int.TryParse(stringUserId, out int userId))
                return TypedResults.Unauthorized();

            if (!(await usersServices.IsAdmin(userId) || await usersServices.IsCollaborator(userId)))
                return TypedResults.Forbid();
            //--------------------------------------------------------

            if (await productsServices.GetByIdAsync(product.ProductId) is null)
                return TypedResults.NotFound();

            await productsServices.UpdateAsync(product);

            return TypedResults.NoContent();
        }).RequireAuthorization();

        group.MapDelete("/{id:int}", async Task<Results<NoContent, NotFound, UnauthorizedHttpResult, ForbidHttpResult>>
            (ProductsServices productsServices, UsersServices usersServices, HttpContext context, int id) =>
        {
            //------------------------- check se admin o collaboratore
            var stringUserId = context.User.FindFirstValue("userId");

            if (!int.TryParse(stringUserId, out int userId))
                return TypedResults.Unauthorized();

            if (!(await usersServices.IsAdmin(userId) || await usersServices.IsCollaborator(userId)))
                return TypedResults.Forbid();
            //--------------------------------------------------------

            if (await productsServices.GetByIdAsync(id) is null)
                return TypedResults.NotFound();

            await productsServices.DeleteAsync(id);

            return TypedResults.NoContent();
        }).RequireAuthorization();

        // ======================================================================== api extra

        group.MapPut("sell/{productId:int}/{quantity:int}", async Task<Results<NoContent, NotFound, UnauthorizedHttpResult, ForbidHttpResult>>
            (UsersServices usersServices, ProductsServices productsServices, HttpContext context, int productId, int quantity) =>
        {
            //------------------------- check se admin o collaboratore
            var stringUserId = context.User.FindFirstValue("userId");

            if (!int.TryParse(stringUserId, out int userId))
                return TypedResults.Unauthorized();

            if (!(await usersServices.IsAdmin(userId) || await usersServices.IsCollaborator(userId)))
                return TypedResults.Forbid();
            //--------------------------------------------------------

            await productsServices.SetSoldProduct(productId, quantity);

            return TypedResults.NoContent();
        }).RequireAuthorization();

        group.MapPut("addAvailable/{productId:int}/{quantity:int}", async Task<Results<NoContent, NotFound, UnauthorizedHttpResult, ForbidHttpResult>>
            (UsersServices usersServices, ProductsServices productsServices, HttpContext context, int productId, int quantity) =>
        {
            //------------------------- check se admin o collaboratore
            var stringUserId = context.User.FindFirstValue("userId");

            if (!int.TryParse(stringUserId, out int userId))
                return TypedResults.Unauthorized();

            if (!(await usersServices.IsAdmin(userId) || await usersServices.IsCollaborator(userId)))
                return TypedResults.Forbid();
            //--------------------------------------------------------

            await productsServices.AddAvailableProduct(productId, quantity);

            return TypedResults.NoContent();
        }).RequireAuthorization();
    }
}

// ======================================================================== record necessari alle richieste

public record InsertProduct(string Name, string Description, decimal Price, int Available)
{
    public Product ToEntity() => new()
    {
        Name = Name,
        Description = Description,
        Price = Price,
        Available = Available
    };
}
