namespace BlobBin.Endpoints;

public static class GetFileUploadLink
{
    public static IResult Handle(HttpContext context, Db db) {
        var file = new File {
            IP = context.Request.Headers["X-Forwarded-For"].ToString()
        };
        db.Files.Add(file);
        db.SaveChanges();
        return Results.Text(
            context.Request.GetRequestHost()
            + "/upload/"
            + file.Id
        );
    }
}