using System.Text.Json;
using GKE_Secret_Editor;
using GKE_Secret_Editor.Models;
using GKE_Secret_Editor.Utils;
using Microsoft.Extensions.Logging;

const string yamlOutputPath = @"_data/yamls/output.yaml";
const string yamlEditedOutputPath = @"_data/yamls/output-mod.yaml";
const string jsonOutputPath = @"_data/output.json";
const string propertiesPath = @"_data/properties.json";

FolderExtensions.CreateFolderIfDoesNotExists("_data");
FolderExtensions.CreateFolderIfDoesNotExists("_data/yamls");

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddFilter("Microsoft", LogLevel.Warning)
        .AddFilter("System", LogLevel.Warning)
        .AddFilter("LoggingConsoleApp.Program", LogLevel.Debug)
        .AddConsole();
});
ILogger logger = loggerFactory.CreateLogger<Program>();

if (!File.Exists(propertiesPath))
{
    using var sw = File.CreateText(propertiesPath);

    var newEmptyProperties = new GoogleCloudConnectionProperties();
    sw.WriteLine(JsonSerializer.Serialize(newEmptyProperties, new JsonSerializerOptions { WriteIndented = true }));

    return;
}

var propertiesRaw = File.ReadAllText(propertiesPath);
var properties = JsonSerializer.Deserialize<GoogleCloudConnectionProperties>(propertiesRaw);

if (properties is null)
{
    logger.LogError("Properties is null or the file is corrupted.");
    return;
}

var gcConnection = new CmdGoogleCloudConnection(loggerFactory, properties);


var command = "";
while (command != "q")
{
    Console.WriteLine("\nWhat do you want to do?");
    Console.WriteLine("(d) Download secret");
    Console.WriteLine("(u) Upload secret");
    Console.WriteLine("(q) Quit");
    command = Console.ReadLine();

    try
    {
        switch (command)
        {
            case "d":
                Console.WriteLine("What secret do you want to edit?");
                var secret = Console.ReadLine();

                var secretJson = gcConnection.GetFromKubernetes(yamlOutputPath, secret);
                using (var sw = File.CreateText(jsonOutputPath))
                {
                    sw.WriteLine(secretJson);
                }

                break;
            case "u":
                var content = File.ReadAllText(jsonOutputPath);
                gcConnection.WriteInKubernetes(yamlOutputPath, yamlEditedOutputPath, content);

                break;
            case "q":
                break;
            default:
                Console.WriteLine("In valid command try again.");
                break;
        }
    }
    catch (Exception e)
    {
        logger.LogError(e.Message);
    }
}