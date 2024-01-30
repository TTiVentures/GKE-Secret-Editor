﻿using System.Diagnostics;
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
        Cmd = new();
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
        if (Cmd is null)
        {
            throw new ApplicationException("Cmd is null");
        }

        Cmd.StandardInput.Flush();
        Cmd.StandardInput.Close();

        // Wait for cmd to finish:
        Cmd.WaitForExit();

        var error = Cmd.StandardError.ReadToEnd();

        // Get exit code:
        var exitCode = Cmd.ExitCode;

        // Check exit code error:
        if (exitCode != 0)
        {
            _logger.LogError("An Error Occurred: \n{Error}", error);
        }

        else
        {
            _logger.LogInformation("Operation Completed Successfully");
        }
    }

    public void GetOnCluster()
    {
        if (Cmd is null)
        {
            throw new ApplicationException("Cmd is null");
        }

        Cmd.StandardInput.WriteLine(
            $"gcloud container clusters get-credentials {_properties.Cluster} --zone {_properties.Zone} --project {_properties.Project}");
    }

    public void GetFromKubernetes(string yamlOutputFilePath, string secretsDirectory, string secretName)
    {
        StartCmd();

        GetOnCluster();

        var command = $"kubectl get secret {secretName} --namespace {_properties.Namespace} -o yaml > {yamlOutputFilePath}";
        if (Cmd is null)
        {
            throw new ApplicationException("Cmd is null");
        }

        Cmd.StandardInput.WriteLine(command);
        FlushAndExecute();

        var myConfig =
            YamlDeserializer.Deserializer.Deserialize<ExpandoObject>(File.ReadAllText(yamlOutputFilePath));

        var data = ((dynamic)myConfig).data;

        foreach (var file in data)
        {
            var fileName = file.Key;
            var fileContent = (string)file.Value.ToString()!;

            var filePath = Path.Combine(secretsDirectory, fileName);
            using (var sw = File.CreateText(filePath))
            {
                sw.WriteLine(fileContent.DecodeBase64());
            }

            Console.WriteLine($"File {fileName} downloaded.");
        }
    }

    public void WriteInKubernetes(string yamlOriginalFilePath, string yamlEditedOutputFilePath, string secretsDirectory)
    {
        if (Cmd is null)
        {
            throw new ApplicationException("Cmd is null");
        }

        StartCmd();

        var originalYaml = YamlDeserializer.Deserializer.Deserialize<ExpandoObject>(File.ReadAllText(yamlOriginalFilePath));

        var data = ((dynamic)originalYaml).data;
        var dict = (Dictionary<object, object>)data;

        foreach (var file in dict)
        {
            var fileName = file.Key.ToString()!;
            var filePath = Path.Combine(secretsDirectory, fileName);
            var fileContent = File.ReadAllText(filePath);

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File {fileName} not found.");
                return;
            }
            else
            {
                Console.WriteLine($"File {fileName}");
            }

            dict[fileName] = fileContent.EncodeBase64();
        }

        // Remove metadata before uploading to GKE
        ((Dictionary<object, object>)((dynamic)originalYaml).metadata).Remove("annotations");
        ((Dictionary<object, object>)((dynamic)originalYaml).metadata).Remove("creationTimestamp");
        ((Dictionary<object, object>)((dynamic)originalYaml).metadata).Remove("resourceVersion");
        var yaml = YamlDeserializer.Serializer.Serialize(originalYaml);

        using (var sw = File.CreateText(yamlEditedOutputFilePath))
        {
            sw.WriteLine(yaml);
        }

        GetOnCluster();

        Cmd.StandardInput.WriteLine($"kubectl apply -f {yamlEditedOutputFilePath}");

        FlushAndExecute();
        Console.WriteLine("Secret saved.");
    }
}