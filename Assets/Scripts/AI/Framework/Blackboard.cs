using System.Collections.Generic;

namespace Game.AI
{
    /// <summary>
    /// 行为树节点之间共享数据的黑板。
    /// 数据按字符串键存储；同名键会覆盖旧值。
    /// </summary>
    //
    // 功能：实现各独立行为树节点之间的数据共享。
    public class Blackboard
    {
        private readonly Dictionary<string, object> values = new Dictionary<string, object>();
        public void Set(string key, object value)
        {
            values[key] = value;
        }
        /// <summary>
        /// 获取指定键的数据；键不存在时返回 null，
        /// 存在但类型不匹配时抛出类型转换异常。
        /// </summary>
        // 项目内应优先使用 EnemyBlackboardKeys 等常量，避免字符串拼写错误。
        public T Get<T>(string key) where T : class
        {
            return values.TryGetValue(key, out object value) ? (T)value : null;
        }
        public bool Has(string key) => values.ContainsKey(key);
        public void Clear() => values.Clear();
    }
}