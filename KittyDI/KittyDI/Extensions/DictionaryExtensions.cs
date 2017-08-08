using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KittyDI.Extensions
{
  internal static class DictionaryExtensions
  {
    internal static void SmartAdd<TKey, TValue>(this IDictionary<TKey, List<TValue>> dictionary, TKey key, TValue newValue)
    {
      if (!dictionary.ContainsKey(key))
      {
        dictionary.Add(key, new List<TValue>());
      }

      dictionary[key].Add(newValue);
    }
  }
}
