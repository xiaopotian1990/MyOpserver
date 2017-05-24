using System;
using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.Redis
{
    public class RedisModule : StatusModule
    {
        public static bool Enabled => Instances.Count > 0;
        public static List<RedisInstance> Instances { get; }
        public static List<RedisConnectionInfo> Connections { get; }

        static RedisModule()
        {
            Connections = LoadRedisConnections();
            Instances = Connections
                .Select(rci => new RedisInstance(rci))
                .Where(rsi => rsi.TryAddToGlobalPollers())
                .ToList();
        }

        public override bool IsMember(string node)
        {
            foreach (var i in Instances)
            {
                // TODO: Dictionary
                if (string.Equals(i.Host, node, StringComparison.InvariantCultureIgnoreCase)) return true;
            }
            return false;
        }

        /// <summary>
        /// 加载Redis连接
        /// </summary>
        /// <returns></returns>
        private static List<RedisConnectionInfo> LoadRedisConnections()
        {
            var result = new List<RedisConnectionInfo>();
            //默认实例
            var defaultServerInstances = Current.Settings.Redis.Defaults.Instances;
            //Servers节点的子集
            var allServerInstances = Current.Settings.Redis.AllServers.Instances;

            foreach (var s in Current.Settings.Redis.Servers)
            {
                var count = result.Count;
                // Add instances that belong to any servers 为Servers实例添加子集
                allServerInstances?.ForEach(gi => result.Add(new RedisConnectionInfo(s.Name, gi)));

                // Add instances defined on this server 添加定义在Servers里的实例
                if (s.Instances.Count > 0)
                    s.Instances.ForEach(i => result.Add(new RedisConnectionInfo(s.Name, i)));

                // If we have no instances added at this point, defaults it is! 如果没有任何实例则添加默认的
                if (defaultServerInstances != null && count == result.Count)
                    defaultServerInstances.ForEach(gi => result.Add(new RedisConnectionInfo(s.Name, gi)));
            }
            return result;
        }
    }
}
