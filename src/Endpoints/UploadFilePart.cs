namespace BlobBin.Endpoints;

public class UploadFilePart
{
    public static IResult Handle(HttpContext context, Db db, Guid id) {
        return Results.Ok();
    }
}