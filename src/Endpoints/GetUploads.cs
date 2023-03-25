namespace BlobBin.Endpoints;

public class GetUploads : BaseEndpoint
{
    private readonly Db _db;

    public GetUploads(Db db) {
        _db = db;
    }

    [HttpPost("/uploads")]
    public ActionResult Handle() {
        if (Request.Form["uid"].ToString().IsNullOrWhiteSpace()) {
            return BadRequest("No uid");
        }

        var uid = Request.Form["uid"].ToString().AsGuid();

        if (Request.Form["password"].ToString().IsNullOrWhiteSpace()) {
            return BadRequest("No password");
        }

        var user = _db.Users.FirstOrDefault(c => c.Id == uid);
        if (user == default) {
            return NoContent();
        }

        if (!PasswordHelper.Verify(Request.Form["password"], user.PasswordHash)) {
            return Unauthorized();
        }

        return Ok(new {
            Files = _db.Files.Where(c => c.CreatedBy == uid),
            Pastes = _db.Pastes.Where(c => c.CreatedBy == uid)
        });
    }
}