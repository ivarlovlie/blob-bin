namespace BlobBin.Endpoints;

public static class GetUploads
{
    public static IResult Handle(HttpContext context, Db db) {
        if (context.Request.Form["uid"].ToString().IsNullOrWhiteSpace()) {
            return Results.BadRequest("No uid");
        }

        var uid = context.Request.Form["uid"].ToString().AsGuid();

        if (context.Request.Form["password"].ToString().IsNullOrWhiteSpace()) {
            return Results.BadRequest("No password");
        }

        var user = db.Users.FirstOrDefault(c => c.Id == uid);
        if (user == default) {
            return Results.Empty;
        }

        if (!PasswordHelper.Verify(context.Request.Form["password"], user.PasswordHash)) {
            return Results.Unauthorized();
        }

        return Results.Json(new {
            Files = db.Files.Where(c => c.CreatedBy == uid),
            Pastes = db.Pastes.Where(c => c.CreatedBy == uid)
        });
    }
}