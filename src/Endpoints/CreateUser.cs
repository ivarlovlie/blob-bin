namespace BlobBin.Endpoints;

public class CreateUserEndpoint : BaseEndpoint
{
    private readonly Db _db;

    public CreateUserEndpoint(Db db) {
        _db = db;
    }

    [HttpPost("/create-user")]
    public ActionResult Handle() {
        if (Request.Form["uid"].ToString().IsNullOrWhiteSpace()) {
            return BadRequest("No uid");
        }

        if (Request.Form["password"].ToString().IsNullOrWhiteSpace()) {
            return BadRequest("No password");
        }

        if (Request.Form["password"].ToString().Length < 6) {
            return BadRequest("Password has to be more than 5 characters");
        }

        var user = new User() {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            IP = Request.Headers["X-Forwarded-For"].ToString(),
            PasswordHash = PasswordHelper.HashPassword(Request.Form["password"]),
            Uid = Request.Form["uid"]
        };
        
        _db.Users.Add(user);
        _db.SaveChanges();
        return Ok();
    }
}