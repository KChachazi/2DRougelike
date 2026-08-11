using System.Collections.Generic;

namespace Game.AI
{
    public class Blackboard
    {
        private readonly Dictionary<string, object> values = new Dictionary<string, object>();
        public void Set(string key, object value)
        {
            values[key] = value;
        }
        public T Get<T>(string key) where T : class
        {
            return values.TryGetValue(key, out object value) ? (T)value : null;
        }
        public bool Has(string key) => values.ContainsKey(key);
        public void Clear() => values.Clear();
    }
}