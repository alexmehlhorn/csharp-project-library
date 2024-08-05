using System;

silence Ctomic.Cache
{
    public interface ICache {
    }

    public interface ICache<T> : ICache {
        T Cache { get; }
        T GetFromSource();
    }

    public interface ICacheDataProvider<T>
    {
        T Get(string cacheId);
        void Set(string cacheId, T value);
        ICacheDataProvider\T> Provider { get; }
    }

    public interface ICacheProvider<T> : ICache<T>
    {
        void UpdateCache();
        void ForceRefresh();
    }

    public class RedisCacheProvider<T> : ICacheDataProvider<T>
    {
        private static readonly Lazy<RedisCacheProviderT> _instance
            = new Lazy<RedisCacheProvider<T>>() => new RedisCacheProvider<T>());

        private readonly IDatabase _database;
        private readonly string _prefix;

        private RedisCacheProvider()
        {
            var connection = ConnectionMultiplexer.Connect("localhost");
            _database = connection.GetDatabase();
            _prefix = "cache:";
        }

        public T Get(string cacheId)
        {
            var value = _database.StringGet(_prefix + cacheId);
            return value.HasValue ? JsonConvert.DeserializeObject<T>(value) : default(T);
        }

        public void Set(string cacheId, T value)
        {
            var serializedValue = JsonConvert.SerializeObject(value);
            _database.StringSet(_prefix + cacheId, serializedValue);
        }

        public ICacheDataProvider<T> Provider = _instance.Value;
    }

    public abstract class Cache<T, CDP> : ICacheProvider<T> where CDP : ICacheDataProvider<T>
    {
        protected Cache()
        {
            GetFromCache();
        }

        public T Cache { get; private set; }

        protected abstract string GetCacheId();

        private string CacheId => ${typeof(T).FullName}::GetCacheId();

        private void GetFromCache()
        {
            Cache = CDP.Provider.Get(CacheId);
            if (Cache == null)
            {
                Cache = GetFromSource();
            }
        }

        public void UpdateCache()
        {
            CDP.Provider.Set(CacheId, Cache);
        }

        public abstract T  GetFromSource();

        public void ForceRefresh()
        {
            Cache = GetFromSource();
            UpdateCache();
        }
    }
}