namespace GKE_Secret_Editor.Utils;

public static class FolderExtensions
{
    public static void CreateFolderIfDoesNotExists(string path)
    {
        var exists = Directory.Exists(path);

        if (!exists)
        {
            Directory.CreateDirectory(path);
        }
    }

    public static void DeleteFolderContentIfExists(string folderPath)
    {
        if (Directory.Exists(folderPath))
        {
            foreach (var file in Directory.GetFiles(folderPath))
            {
                File.Delete(file);
            }
        }
    }
}