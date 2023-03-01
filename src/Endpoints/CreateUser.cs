namespace BlobBin.Endpoints;

public static class CreateUser
{
    public static IResult Handle(HttpContext context, Db db) {
        if (context.Request.Form["uid"].ToString().IsNullOrWhiteSpace()) {
            return Results.BadRequest("No uid");
        }

        if (context.Request.Form["password"].ToString().IsNullOrWhiteSpace()) {
            return Results.BadRequest("No password");
        }

        if (context.Request.Form["password"].ToString().Length < 6) {
            return Results.BadRequest("Password has to be more than 5 characters");
        }

        var user = new User() {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            IP = context.Request.Headers["X-Forwarded-For"].ToString(),
            PasswordHash = PasswordHelper.HashPassword(context.Request.Form["password"]),
            Uid = context.Request.Form["uid"]
        };
        db.Users.Add(user);
        db.SaveChanges();
        return Results.Ok();
    }
}