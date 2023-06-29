namespace GKE_Secret_Editor.Utils;

public static class FolderExtensions
{
    public static void CreateFolderIfDoesNotExists(string path)
    {
        var exists = Directory.Exists(path);

        if (!exists) Directory.CreateDirectory(path);
    }
}