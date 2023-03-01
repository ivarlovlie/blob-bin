namespace BlobBin.Endpoints;

public static class UpdateUser
{
    public static IResult Handle(HttpContext context, Db db) {
        var user = db.Users.FirstOrDefault(c => c.Uid == context.Request.Form["uid"]);
        if (user == default) {
            return Results.Empty;
        }

        if (!PasswordHelper.Verify(context.Request.Form["password"], user.PasswordHash)) {
            return Results.Unauthorized();
        }

        if (context.Request.Form["newuid"].ToString().IsNullOrWhiteSpace()) {
            return Results.BadRequest("No new uid was specified");
        }

        user.Uid = context.Request.Form["newuid"];

        db.SaveChanges();
        return Results.Ok();
    }
}