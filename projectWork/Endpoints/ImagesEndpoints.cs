using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using projectWork.Models;
using projectWork.Services;
using System.Security.Claims;
using System.Text.RegularExpressions;
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
        group.MapPost("/upload", async Task <Results<Created, BadRequest<string>, UnauthorizedHttpResult, ForbidHttpResult>>
            (ImagesServices imagesServices, UsersServices usersServices, HttpContext context, [FromForm] InsertImage imageRecord) =>
        {
            //------------------------- check se admin o collaboratore
            var stringUserId = context.User.FindFirstValue("userId");

            if (!int.TryParse(stringUserId, out int userId))
                return TypedResults.Unauthorized();

            if (!(await usersServices.IsAdmin(userId) || await usersServices.IsCollaborator(userId)))
                return TypedResults.Forbid();
            //--------------------------------------------------------

            var file = imageRecord.photo;

            if (file == null || file.Length == 0)
                return TypedResults.BadRequest("Nessun file caricato");

            // fare controllo input dentro il record in caso
            Image img = imageRecord.ToEntity();

            await imagesServices.InsertAsync(img, file);

            return TypedResults.Created();
        }).Accepts<InsertImage>("multipart/form-data").RequireAuthorization().WithName("upload Immagini").DisableAntiforgery();

        group.MapPut("/", async Task<Results<NoContent, NotFound, UnauthorizedHttpResult, ForbidHttpResult>>
            (ImagesServices imagesServices, UsersServices usersServices, HttpContext context, Image image) =>
        {
            //------------------------- check se admin o collaboratore
            var stringUserId = context.User.FindFirstValue("userId");

            if (!int.TryParse(stringUserId, out int userId))
                return TypedResults.Unauthorized();

            if (!(await usersServices.IsAdmin(userId) || await usersServices.IsCollaborator(userId)))
                return TypedResults.Forbid();
            //--------------------------------------------------------

            if (await imagesServices.GetByIdAsync(image.Id) is null)
                return TypedResults.NotFound();

            await imagesServices.UpdateAsync(image);

            return TypedResults.NoContent();
        }).RequireAuthorization();

        group.MapDelete("/{id:int}", async Task<Results<NoContent, NotFound, UnauthorizedHttpResult, ForbidHttpResult>>
            (ImagesServices imagesServices, UsersServices usersServices, HttpContext context, int id) =>
        {
            //------------------------- check se admin o collaboratore
            var stringUserId = context.User.FindFirstValue("userId");

            if (!int.TryParse(stringUserId, out int userId))
                return TypedResults.Unauthorized();

            if (!(await usersServices.IsAdmin(userId) || await usersServices.IsCollaborator(userId)))
                return TypedResults.Forbid();
            //--------------------------------------------------------

            if (await imagesServices.GetByIdAsync(id) is null)
                return TypedResults.NotFound();

            await imagesServices.DeleteAsync(id);

            return TypedResults.NoContent();
        }).RequireAuthorization();

        // ======================================================================== api extra

        //filtro foto per stato
        group.MapGet("/filter/{state:string}",  async Task<Ok<IEnumerable<Image>>> (ImagesServices imagesServices, string state) =>
        {
            var list = await imagesServices.GetListFilterAsync(state);
            return TypedResults.Ok(list);
        });

        // prenotata da qualcuno
        group.MapPost("/book/{imageId:int}/{userId:int}", async Task<Results<NoContent, NotFound>>
            (ImagesServices imagesServices, UsersServices usersServices, int imageId, int userId) =>
        {
            User user = await usersServices.GetByIdAsync(userId);
            if (user is null)
                return TypedResults.NotFound();

            await imagesServices.BookImage(imageId, userId);
            return TypedResults.NoContent();
        }).RequireAuthorization();

        // annulla prenotazione qualcuno
        group.MapPost("/unbook/{imageId:int}", async Task<Results<NoContent, NotFound, UnauthorizedHttpResult, ForbidHttpResult>>
            (ImagesServices imagesServices, UsersServices usersServices, HttpContext context, int imageId) =>
        {
            //------------------------- check se admin o collaboratore
            var stringUserId = context.User.FindFirstValue("userId");

            if (!int.TryParse(stringUserId, out int userId))
                return TypedResults.Unauthorized();

            if (!(await usersServices.IsAdmin(userId) || await usersServices.IsCollaborator(userId)))
            {
                // se non è ne admin ne collaboratore
                //se la user che effettua la richiesta è lo stesso che ha prenotato
                if(await imagesServices.GetUserOfBookedImage(imageId) == userId)
                    await imagesServices.UnbookImage(imageId);
                else
                    return TypedResults.Forbid();
            }
            else
            {
                // se è admin o collaboratore
                await imagesServices.UnbookImage(imageId);
            }
            //--------------------------------------------------------
            
            return TypedResults.NoContent();
        }).RequireAuthorization();

        group.MapPost("/setsold/{imageId:int}", async Task<Results<NoContent, NotFound, UnauthorizedHttpResult, ForbidHttpResult>>
            (ImagesServices imagesServices, UsersServices usersServices, HttpContext context, int imageId) =>
        {
            //------------------------- check se admin o collaboratore
            var stringUserId = context.User.FindFirstValue("userId");

            if (!int.TryParse(stringUserId, out int userId))
                return TypedResults.Unauthorized();

            if (!(await usersServices.IsAdmin(userId) || await usersServices.IsCollaborator(userId)))
                return TypedResults.Forbid();
            //--------------------------------------------------------

            await imagesServices.SetSoldImage(imageId);

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
    decimal price
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
        Price = price
    };
};
