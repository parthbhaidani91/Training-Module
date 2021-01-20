using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Ctail.Training.Plugins.Helper
{
    public static class PluginExtensions
    {
        public static string Serialize<T>(this T obj)
        {
            var ser = new DataContractJsonSerializer(obj.GetType(), new DataContractJsonSerializerSettings
            {
                UseSimpleDictionaryFormat = true
            });

            using (var ms = new MemoryStream())
            {
                ser.WriteObject(ms, obj);
                ms.Position = 0;
                using (var reader = new StreamReader(ms))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public static T Deserialize<T>(this string str) where T : class
        {
            var ser = new DataContractJsonSerializer(typeof(T), new DataContractJsonSerializerSettings
            {
                UseSimpleDictionaryFormat = true
            });
            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(str)))
            {
                return (T)ser.ReadObject(ms);
            }
        }

    }
}
