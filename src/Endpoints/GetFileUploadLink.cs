namespace BlobBin.Endpoints;

public class GetFileUploadLink : BaseEndpoint
{
    private readonly Db _db;

    public GetFileUploadLink(Db db) {
        _db = db;
    }

    [HttpGet("/upload-link")]
    public ActionResult Handle() {
        var file = new File {
            IP = Request.Headers["X-Forwarded-For"].ToString()
        };
        _db.Files.Add(file);
        _db.SaveChanges();
        return Content(
            Request.GetRequestHost()
            + "/upload/"
            + file.Id
            , "text/plain");
    }
}