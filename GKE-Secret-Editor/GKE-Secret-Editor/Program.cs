using System.Text.Json;
using GKE_Secret_Editor;
using GKE_Secret_Editor.Models;
using GKE_Secret_Editor.Utils;
using Microsoft.Extensions.Logging;

const string workDirectory = "_data";
const string yamlsDirectory = workDirectory + "/yamls";
const string secretsDirectory = workDirectory + "/secrets";

const string yamlOutputFilePath = yamlsDirectory + "/output.yaml";
const string yamlEditedOutputFilePath = yamlsDirectory + "/output-mod.yaml";

const string propertiesPath = workDirectory + "/properties.json";

FolderExtensions.CreateFolderIfDoesNotExists(workDirectory);
FolderExtensions.CreateFolderIfDoesNotExists(yamlsDirectory);
FolderExtensions.CreateFolderIfDoesNotExists(secretsDirectory);

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
AppDomain.CurrentDomain.ProcessExit += BeforeExit;

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

                if (string.IsNullOrEmpty(secret))
                {
                    Console.WriteLine("Secret name is empty.");
                    break;
                }

                gcConnection.GetFromKubernetes(yamlOutputFilePath, secretsDirectory, secret);
                break;
            case "u":
                gcConnection.WriteInKubernetes(yamlOutputFilePath, yamlEditedOutputFilePath, secretsDirectory);
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

void BeforeExit(object sender, EventArgs e)
{
    Console.WriteLine("Cleaning up...");
    // Remove the two folders
    FolderExtensions.DeleteFolderIfExists(yamlsDirectory);
    FolderExtensions.DeleteFolderIfExists(secretsDirectory);

    Console.WriteLine("Done.");
}