using System;
using System.Runtime.Caching;

namespace GeoDecisions.Esb.Client.Core
{
    internal static class InternalCache
    {
        public static T Get<T>(string key, Func<string, T> whenNotFound)
        {
            MemoryCache cache = MemoryCache.Default;

            var cacheItem = (T) cache[key];

            if (cacheItem == null)
            {
                var policy = new CacheItemPolicy();

                cacheItem = whenNotFound(key);

                //policy.ChangeMonitors.Add

                cache.Set(key, cacheItem, policy);
            }

            return cacheItem;
        }

        public static void Set<T>(string key, T obj)
        {
            MemoryCache cache = MemoryCache.Default;
            var policy = new CacheItemPolicy();
            cache.Set(key, obj, policy);
        }
    }
}