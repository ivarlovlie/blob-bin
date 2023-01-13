namespace BlobBin;

public static class Util
{
    public static string GetFilesDirectoryPath(bool createIfNotExists = false) {
        var filesDirectoryPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "AppData",
            "files"
        );
        if (createIfNotExists) Directory.CreateDirectory(filesDirectoryPath);
        return filesDirectoryPath;
    }
}