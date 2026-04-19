using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using projectWork.Models;
using projectWork.Services;
using Image = projectWork.Models.Image;

namespace projectWork.Endpoints;

public static class ImagesEndpoints
{
    public static void AddImagesEndpoints(this IEndpointRouteBuilder route)
    {
        var group = route.MapGroup("/api/photos").WithTags("api photos");

        group.MapGet("/", async Task<Ok<IEnumerable<Image>>> (ImagesServices imagesServices) =>
        {
            var list = await imagesServices.GetListAsync();
            return TypedResults.Ok(list);
        });

        group.MapGet("/{id:int}", async Task<Results<Ok<Image>, NotFound>> (ImagesServices imagesServices, int id) =>
        {
            var image = await imagesServices.GetByIdAsync(id);

            if (image is null)
                return TypedResults.NotFound();

            return TypedResults.Ok(image);
        });

        // upload immagini
        // necessario [FromForm] nel parametro
        group.MapPost("/upload", async Task <Results<Created, BadRequest<string>>> (ImagesServices imagesServices, [FromForm] InsertImage imageRecord) =>
        {
            //potrebbe richiedere di essere collaboratori o admin

            var file = imageRecord.photo;

            if (file == null || file.Length == 0)
                return TypedResults.BadRequest("Nessun file caricato");

            // fare controllo input dentro il record in caso
            Image img = imageRecord.ToEntity();

            await imagesServices.InsertAsync(img, file);

            return TypedResults.Created();
        }).Accepts<InsertImage>("multipart/form-data").RequireAuthorization().WithName("upload Immagini").DisableAntiforgery();

        group.MapPut("/", async Task<Results<NoContent, NotFound>> (ImagesServices imagesServices, Image image) =>
        {
            //potrebbe richiedere di essere collaboratori o admin

            if (await imagesServices.GetByIdAsync(image.Id) is null)
                return TypedResults.NotFound();

            await imagesServices.UpdateAsync(image);

            return TypedResults.NoContent();
        }).RequireAuthorization();

        group.MapDelete("/{id:int}", async Task<Results<NoContent, NotFound>> (ImagesServices imagesServices, int id) =>
        {
            //potrebbe richiedere di essere collaboratori o admin

            if (await imagesServices.GetByIdAsync(id) is null)
                return TypedResults.NotFound();

            await imagesServices.DeleteAsync(id);

            return TypedResults.NoContent();
        }).RequireAuthorization();
    }
}

// ======================================================================== record necessari alle richieste

public record InsertImage(
    IFormFile photo,
    string title,
    string originalTitle,
    short year,
    string place,
    string? description,
    PhotoState state,
    decimal price,
    int? bookedBy
)
{
    public Image ToEntity() => new()
    {
        Title = title,
        OriginalTitle = originalTitle,
        Year = year,
        Place = place,
        Path = $"/frontend/photos/{Guid.NewGuid()}{Path.GetExtension(photo.FileName)}",
        Description = description,
        State = state,
        Price = price,
        BookedBy = bookedBy
    };
};
