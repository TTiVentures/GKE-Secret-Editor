using System.Diagnostics;
using System.Dynamic;

namespace SecretEditor
{
    public class CmdGoogleCloudConnection
    {
        public Process Cmd { get; private set; }
        public GoogleCloudConnectionProperties Properties { get; set; } = new();

        private void StartCmd()
        {
            Cmd = new Process();
            Cmd.StartInfo.FileName = "cmd.exe";
            Cmd.StartInfo.RedirectStandardInput = true;
            Cmd.StartInfo.RedirectStandardOutput = true;
            Cmd.StartInfo.CreateNoWindow = true;
            Cmd.StartInfo.UseShellExecute = false;
            Cmd.Start();
        }

        private void FlushAndExecute()
        {
            Cmd.StandardInput.Flush();
            Cmd.StandardInput.Close();
            Cmd.WaitForExit();
        }

        public void GetOnCluster()
        {
            Cmd.StandardInput.WriteLine($"gcloud container clusters get-credentials {Properties.Cluster} --zone {Properties.Zone} --project {Properties.Project}");
        }

        public string GetFromKubernetes(string outputFileName, string secretName)
        {
            StartCmd();

            GetOnCluster();
            Cmd.StandardInput.WriteLine($"kubectl get secret {secretName} --namespace {Properties.Namespace} -o yaml > {outputFileName}");

            FlushAndExecute();

            var myConfig = YamlDeserializer.Deserializer.Deserialize<ExpandoObject>(File.ReadAllText(outputFileName));

            var data = ((dynamic)myConfig).data;
            var secretBase64 = ((Dictionary<object, object>)data).First().Value.ToString();
            return secretBase64.DecodeBase64();
        }

        public void WriteInKubernetes(string jsonEditedFile, string yamlEditedFile, string content)
        {
            if (!content.IsValidJson())
            {
                Console.WriteLine("The Json is invalid");
                return;
            }

            StartCmd();

            var myConfig = YamlDeserializer.Deserializer.Deserialize<ExpandoObject>(File.ReadAllText(jsonEditedFile));

            var data = ((dynamic)myConfig).data;
            var dict = ((Dictionary<object, object>)data);
            var dictValue = dict.First();

            dict[dictValue.Key] = content.EncodeBase64();

            ((Dictionary<object, object>)((dynamic)myConfig).metadata).Remove("annotations");
            ((Dictionary<object, object>)((dynamic)myConfig).metadata).Remove("creationTimestamp");
            ((Dictionary<object, object>)((dynamic)myConfig).metadata).Remove("resourceVersion");
            var yaml = YamlDeserializer.Serializer.Serialize(myConfig);

            using (StreamWriter sw = File.CreateText(yamlEditedFile))
            {
                sw.WriteLine(yaml);
            }

            GetOnCluster();

            Cmd.StandardInput.WriteLine($"kubectl apply -f {yamlEditedFile}");

            FlushAndExecute();
            Console.WriteLine("Secret saved.");
        }
    }
}
