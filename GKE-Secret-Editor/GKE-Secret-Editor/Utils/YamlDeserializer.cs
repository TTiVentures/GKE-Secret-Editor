using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SecretEditor
{
    public static class YamlDeserializer
    {
        public static IDeserializer Deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        public static ISerializer Serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }
}
