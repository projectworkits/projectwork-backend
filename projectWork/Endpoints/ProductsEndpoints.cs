using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using projectWork.Models;
using projectWork.Services;
using System.Xml.Linq;

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

        group.MapGet("/{id:int}", async Task<Results<Ok<Product>, NotFound>> (ProductsServices productsServices, int id) =>
        {
            var product = await productsServices.GetByIdAsync(id);

            if (product is null)
                return TypedResults.NotFound();

            return TypedResults.Ok(product);
        });

        group.MapPost("/", async Task<Results<Created, BadRequest>> (ProductsServices productsServices, InsertProduct productRecord) =>
        {
            //potrebbe richiedere di essere collaboratori o admin

            // fare controllo input dentro il record in caso
            var product = productRecord.ToEntity();

            await productsServices.InsertAsync(product);

            return TypedResults.Created();
                
        }).RequireAuthorization();

        group.MapPut("/", async Task<Results<NoContent, NotFound>> (ProductsServices productsServices, Product product) =>
        {
            //potrebbe richiedere di essere collaboratori o admin

            if (await productsServices.GetByIdAsync(product.Id) is null)
                return TypedResults.NotFound();

            await productsServices.UpdateAsync(product);

            return TypedResults.NoContent();
        }).RequireAuthorization();

        group.MapDelete("/{id:int}", async Task<Results<NoContent, NotFound>> (ProductsServices productsServices, int id) =>
        {
            //potrebbe richiedere di essere collaboratori o admin

            if (await productsServices.GetByIdAsync(id) is null)
                return TypedResults.NotFound();

            await productsServices.DeleteAsync(id);

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
