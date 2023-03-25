namespace BlobBin.Endpoints;

public class DeleteUser : BaseEndpoint
{
    private readonly Db _db;

    public DeleteUser(Db db) {
        _db = db;
    }

    [HttpDelete("/delete-user")]
    public ActionResult Handle() {
        var user = _db.Users.FirstOrDefault(c => c.Uid == Request.Form["uid"]);
        if (user == default) {
            return Ok();
        }

        if (!PasswordHelper.Verify(Request.Form["password"], user.PasswordHash)) {
            return Unauthorized();
        }

        _db.Users.Remove(user);
        _db.SaveChanges();
        return Ok();
    }
}