using Microsoft.AspNetCore.Http.HttpResults;
using projectWork.Services;
using projectWork.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;

namespace projectWork.Endpoints;

public static class ImagesEndpoints
{
    public record ImgUpload(
        [FromForm] IFormFile photo,
        [FromForm] string title,
        [FromForm] string originalTitle,
        [FromForm] short year,
        [FromForm] string place,
        [FromForm] string description,
        [FromForm] string state,
        [FromForm] decimal price,
        [FromForm] int bookedBy
    );

    public static void AddImagesEndpoints(this IEndpointRouteBuilder route)
    {
        var group = route.MapGroup("/api/photos").WithTags("api photos");

        group.MapGet("/", GetImagesAsync);
        group.MapGet("/{id:int}", GetImageByIdAsync);

        group.MapPost("/", CreateImageAsync);

        group.MapPut("/", UpdateImageAsync);

        group.MapDelete("/{id:int}", DeleteImageAsync);

        // upload immagini
        route.MapPost("/api/upload", Upload).Accepts<ImgUpload>("multipart/form-data").WithTags("uploadImmagini").DisableAntiforgery();
    }

    public static async Task<Ok<IEnumerable<Models.Image>>> GetImagesAsync(ImagesServices imagesServices)
    {
        var list = await imagesServices.GetListAsync();
        return TypedResults.Ok(list);
    }

    public static async Task<Results<Ok<Models.Image>, NotFound>> GetImageByIdAsync(ImagesServices imagesServices, int id)
    {
        var image = await imagesServices.GetByIdAsync(id);
        if (image is null)
            return TypedResults.NotFound();

        return TypedResults.Ok(image);
    }

    public static async Task<Created<Models.Image>> CreateImageAsync(ImagesServices imagesServices, Models.Image image)
    {
        var id = await imagesServices.InsertAsync(image);

        image.Id = id;
        return TypedResults.Created("/images/"+id, image);
    }

    public static async Task<Results<NoContent, NotFound>> UpdateImageAsync(ImagesServices imagesServices, Models.Image image)
    {
        if (await imagesServices.GetByIdAsync(image.Id) is null)
            return TypedResults.NotFound();

        await imagesServices.UpdateAsync(image);
        return TypedResults.NoContent();
    }

    public static async Task<Results<NoContent, NotFound>> DeleteImageAsync(ImagesServices imagesServices, int id)
    {
        if (await imagesServices.GetByIdAsync(id) is null)
            return TypedResults.NotFound();

        await imagesServices.DeleteAsync(id);
        return TypedResults.NoContent();
    }

    // =============================================================================== Upload immagini

    // necessario [FromForm] nel parametro, altrimenti legge come json
    public static async Task<Results<Ok, BadRequest<string>>> Upload(ImagesServices imagesServices, [FromForm] ImgUpload request)
    {
        var file = request.photo;

        if (file == null || file.Length == 0)
            return TypedResults.BadRequest("Nessun file caricato");

        Image img = new();

        img.Title = request.title;
        img.OriginalTitle = request.originalTitle;
        img.Year = request.year;
        img.Place = request.place;
        img.Path = $"/photos/{Guid.NewGuid()}.{Path.GetExtension(file.FileName)}";
        img.Description = request.description;
        img.State = request.state;
        img.Price = request.price;
        img.BookedBy = request.bookedBy;

        await imagesServices.UploadAsync(file, img.Path);
        await imagesServices.InsertAsync(img);

        return TypedResults.Ok();
    }
}
