using Newtonsoft.Json.Linq;

namespace SPIClient.Helpers
{
    static class JObjectExtensions
    {
        public static T GetValueOrDefault<T>(this JObject jObject, string attribute, T defaultValue)
        {
            if (jObject == null || !jObject.TryGetValue(attribute, out JToken token))
            {
                return defaultValue;
            }
            else
            {
                return token.ToObject<T>();
            }
        }
    }
}
