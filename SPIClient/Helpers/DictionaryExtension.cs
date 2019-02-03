using System.Collections.Generic;

namespace SPIClient.Helpers
{
    internal static class DictionaryExtension
    {
        public static void AddRange<TKey, TValue>(this Dictionary<TKey, TValue> dic, Dictionary<TKey, TValue> dicToAdd)
        {
            foreach(var item in dic)
            {
                dicToAdd.Add(item.Key, item.Value);
            }
        }
    }
}
