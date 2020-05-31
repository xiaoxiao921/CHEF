using System.IO;
using Newtonsoft.Json.Linq;

namespace CHEF
{
    internal static class Config
    {
        private static JObject _json;

        internal static void Init()
        {
            _json = JObject.Parse(File.ReadAllText("config.json"));
        }

        internal static T Get<T>(string key)
        {
            return _json[key].Value<T>();
        }
    }
}
