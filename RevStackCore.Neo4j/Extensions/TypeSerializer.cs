using System;
using System.IO;
using Newtonsoft.Json;

namespace RevStackCore.Neo4j
{
    public static class TypeSerializer
    {
        public static string ToObjectLiteralString<T>(this T src) where T:class
        {
            var serializer = new JsonSerializer();
            var stringWriter = new StringWriter();
            using (var writer = new JsonTextWriter(stringWriter))
            {
                writer.QuoteName = false;
                serializer.Serialize(writer, src);
            }
            return stringWriter.ToString();
        }
    }
}
