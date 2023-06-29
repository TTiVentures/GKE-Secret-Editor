using System.Diagnostics;
using System.Dynamic;
using GKE_Secret_Editor.Models;
using GKE_Secret_Editor.Utils;
using Microsoft.Extensions.Logging;

namespace GKE_Secret_Editor;

public class CmdGoogleCloudConnection
{
    private readonly ILogger<CmdGoogleCloudConnection> _logger;
    private readonly GoogleCloudConnectionProperties _properties;

    public CmdGoogleCloudConnection(ILoggerFactory loggerFactory,
        GoogleCloudConnectionProperties properties)
    {
        _logger = loggerFactory.CreateLogger<CmdGoogleCloudConnection>();
        _properties = properties;
    }

    private Process? Cmd { get; set; }

    private void StartCmd()
    {
        Cmd = new Process();
        Cmd.StartInfo.FileName = "cmd.exe";
        Cmd.StartInfo.RedirectStandardInput = true;
        Cmd.StartInfo.RedirectStandardOutput = true;
        Cmd.StartInfo.RedirectStandardError = true;
        Cmd.StartInfo.CreateNoWindow = true;
        Cmd.StartInfo.UseShellExecute = false;

        Cmd.Start();
    }

    private void FlushAndExecute()
    {
        if (Cmd is null) throw new ApplicationException("Cmd is null");

        Cmd.StandardInput.Flush();
        Cmd.StandardInput.Close();

        // Wait for cmd to finish:
        Cmd.WaitForExit();

        var error = Cmd.StandardError.ReadToEnd();

        // Get exit code:
        var exitCode = Cmd.ExitCode;

        // Check exit code error:
        if (exitCode != 0)
            _logger.LogError("An Error Occurred: \n{Error}", error);

        else
            _logger.LogInformation("Operation Completed Successfully");
    }

    public void GetOnCluster()
    {
        if (Cmd is null) throw new ApplicationException("Cmd is null");

        Cmd.StandardInput.WriteLine(
            $"gcloud container clusters get-credentials {_properties.Cluster} --zone {_properties.Zone} --project {_properties.Project}");
    }

    public string GetFromKubernetes(string outputFileName, string secretName)
    {
        StartCmd();

        GetOnCluster();

        var command = $"kubectl get secret {secretName} --namespace {_properties.Namespace} -o yaml > {outputFileName}";
        if (Cmd is null) throw new ApplicationException("Cmd is null");
        Cmd.StandardInput.WriteLine(command);
        FlushAndExecute();

        var myConfig = YamlDeserializer.Deserializer.Deserialize<ExpandoObject>(File.ReadAllText(outputFileName));

        var data = ((dynamic)myConfig).data;
        var secretBase64 = ((Dictionary<object, object>)data).First().Value.ToString();
        return secretBase64.DecodeBase64();
    }

    public void WriteInKubernetes(string jsonEditedFile, string yamlEditedFile, string content)
    {
        if (Cmd is null) throw new ApplicationException("Cmd is null");

        if (!content.IsValidJson())
        {
            Console.WriteLine("The Json is invalid");
            return;
        }

        StartCmd();

        var myConfig = YamlDeserializer.Deserializer.Deserialize<ExpandoObject>(File.ReadAllText(jsonEditedFile));

        var data = ((dynamic)myConfig).data;
        var dict = (Dictionary<object, object>)data;
        var dictValue = dict.First();

        dict[dictValue.Key] = content.EncodeBase64();

        ((Dictionary<object, object>)((dynamic)myConfig).metadata).Remove("annotations");
        ((Dictionary<object, object>)((dynamic)myConfig).metadata).Remove("creationTimestamp");
        ((Dictionary<object, object>)((dynamic)myConfig).metadata).Remove("resourceVersion");
        var yaml = YamlDeserializer.Serializer.Serialize(myConfig);

        using (var sw = File.CreateText(yamlEditedFile))
        {
            sw.WriteLine(yaml);
        }

        GetOnCluster();

        Cmd.StandardInput.WriteLine($"kubectl apply -f {yamlEditedFile}");

        FlushAndExecute();
        Console.WriteLine("Secret saved.");
    }
}