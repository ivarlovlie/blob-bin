namespace BlobBin.Endpoints;

public static class DeleteUser
{
    public static IResult Handle(HttpContext context, Db db) {
        var user = db.Users.FirstOrDefault(c => c.Uid == context.Request.Form["uid"]);
        if (user == default) {
            return Results.Empty;
        }

        if (!PasswordHelper.Verify(context.Request.Form["password"], user.PasswordHash)) {
            return Results.Unauthorized();
        }

        db.Users.Remove(user);
        db.SaveChanges();
        return Results.Ok();
    }
}