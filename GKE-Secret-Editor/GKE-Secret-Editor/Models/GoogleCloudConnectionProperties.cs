namespace SecretEditor
{
    public class GoogleCloudConnectionProperties
    {
        public string Cluster { get; private set; } = "";
        public string Zone { get; private set; } = "";
        public string Project { get; private set; } = "";

        public string Namespace { get; private set; } = "";

    }
}