using SecretEditor;
using System.Text.Json;

string yamlOutputPath = @"_data/yamls/output.yaml";
string yamlEditedOutputPath = @"_data/yamls/output-mod.yaml";
string jsonOutputPath = @"_data/output.json";
string propertiesPath = @"_data/properties.json";


CreateFolderIfDoesNotExists("_data");
CreateFolderIfDoesNotExists("_data/yamls");

var GCConnection = new CmdGoogleCloudConnection();
if (File.Exists(propertiesPath))
{
    var properties = File.ReadAllText(propertiesPath);
    GCConnection.Properties = JsonSerializer.Deserialize<GoogleCloudConnectionProperties>(properties);
}
else
{
    var temp = new GoogleCloudConnectionProperties();

    using (StreamWriter sw = File.CreateText(propertiesPath))
    {
        sw.WriteLine(JsonSerializer.Serialize(temp, new JsonSerializerOptions() { WriteIndented = true }));
    }
    return;
}

var command = "";
while (command != "q")
{
    Console.WriteLine("\nWhat do you want to do?");
    Console.WriteLine("(d) Download secret");
    Console.WriteLine("(u) Upload secret");
    Console.WriteLine("(q) Quit");
    command = Console.ReadLine();

    switch (command)
    {
        case "d":
            Console.WriteLine("What secret do you want to edit?");
            var secret = Console.ReadLine();

            var secretJson = GCConnection.GetFromKubernetes(yamlOutputPath, secretName: secret);
            using (StreamWriter sw = File.CreateText(jsonOutputPath))
            {
                sw.WriteLine(secretJson);
            }
            break;
        case "u":
            var content = File.ReadAllText(jsonOutputPath);
            GCConnection.WriteInKubernetes(yamlOutputPath, yamlEditedOutputPath, content);

            break;
        case "q":
            break;
        default:
            Console.WriteLine("In valid command try again.");
            break;
    }
}


void CreateFolderIfDoesNotExists(string path)
{
    bool exists = Directory.Exists(path);

    if (!exists)
    {
        Directory.CreateDirectory(path);
    }
}



