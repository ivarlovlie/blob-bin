namespace BlobBin.Endpoints;

public class UpdateUser : BaseEndpoint
{
    private readonly Db _db;

    public UpdateUser(Db db) {
        _db = db;
    }

    [HttpPost("/update-user")]
    public ActionResult Handle() {
        var user = _db.Users.FirstOrDefault(c => c.Uid == Request.Form["uid"]);
        if (user == default) {
            return NoContent();
        }

        if (!PasswordHelper.Verify(Request.Form["password"], user.PasswordHash)) {
            return Unauthorized();
        }

        if (Request.Form["newuid"].ToString().IsNullOrWhiteSpace()) {
            return BadRequest("No new uid was specified");
        }

        user.Uid = Request.Form["newuid"];

        _db.SaveChanges();
        return Ok();
    }
}