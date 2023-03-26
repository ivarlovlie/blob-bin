namespace BlobBin.Endpoints;

public class GetPaste : BaseEndpoint
{
    private readonly Db _db;

    public GetPaste(Db db) {
        _db = db;
    }

    [HttpGet("/p/{id}")]
    [HttpPost("/p/{id}")]
    public async Task<ActionResult> Handle(string id) {
        var paste = _db.Pastes.FirstOrDefault(c => c.PublicId == id.Trim());
        if (paste is not {DeletedAt: null}) return NotFound();
        if (paste.PasswordHash.HasValue()) {
            var password = Request.Method == "POST" ? Request.Form["password"].ToString() : "";
            if (password.IsNullOrWhiteSpace() || !PasswordHelper.Verify(password, paste.PasswordHash)) {
                return Content($"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <link rel="stylesheet" href="/index.css">
    <title>{paste.PublicId} - Authenticate - Blobbin</title>
</head>
<body>
<form action="/p/{paste.PublicId}" method="post">
    <p>Authenticate to access this paste:</p>
    <input type="password" name="password" placeholder="Password">
    <button type="submit">Unlock</button>
</form>
</body>
</html>
""", "text/html");
            }
        }

        if (paste.Singleton) {
            paste.DeletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        if (Tools.ShouldDeleteUpload(paste)) {
            paste.DeletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        if (paste.IsProbablyEncrypted) {
            return Content($"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <link rel="stylesheet" href="/index.css">
    <script>document.getElementsByTagName("html")[0].className += " js";</script>
    <title>{paste.PublicId} - Encrypted - Blobbin</title>
</head>
<noscript>
This file is encrypted, you can decrypt it by copying the following content into a file and running <code>openssl aes-256-cbc -d -salt -pbkdf2 -iter 10000 -in encryptedfilename -out plaintextfilename</code> on the file you made.
<pre>{paste.Content}</pre>
</noscript>
<body class="js">
<p>This paste looks like it is encrypted, if you have the decryption key, you can try to decrypt it down below.</p>
<div style="display:flex;flex-direction:row;width:fit-content;">
<input type="password" placeholder="decryption key" id="decrypt-key">
<button type="button" id="btn-decrypt">decrypt</button>
</div>
<hr>
<pre id="content">{paste.Content}</pre>
<script src="/index.js"></script>
<script src="/decryptor.js"></script>
</body>
</html>
""", "text/html");
        }

        return Content(paste.Content, paste.MimeType, Encoding.UTF8);
    }
}