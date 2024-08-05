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
        T Get(string key);
        void Set(string key, T value);
    }

    public interface ICacheProvider<T> : ICache<T>
    {
        void UpdateCache();
        void ForceRefresh();
    }

    public class RedisCacheProvider<T> : ICacheDataProvider<T>
    {
        private readonly IDatabase _database;
        private readonly string _prefix;

        public RedisCacheProvider(IDatabase database, string prefix = "cache:")
        {
            _database = database;
            _prefix = prefix;
        }

        public T Get(string key)
        {
            var value = _database.StringGet(_prefix + key);
            return value.HasValue ? JsonConvert.DeserializeObject<T>(value) : default(T);
        }

        public void Set(string key, T value)
        {
            var serializedValue = JsonConvert.SerializeObject(value);
            _database.StringSet(_prefix + key, serializedValue);
        }
    }

    public abstract class Cache<T> : ICacheProvider<T>
    {
        private readonly ICacheDataProvider<T> _cacheDataProvider;
        private readonly string _key;

        public Cache(ICacheDataProvider<T> cacheDataProvider, string key)
        {
            _cacheDataProvider = cacheDataProvider;
            _key = key;
            Cache = GetFromCache() || GetFromSource();
        }

        public T Cache { get; private set; }

        private T GetFromCache()
        {
            return _cacheDataProvider.Get(_key);
        }

        public abstract T  GetFromSource();

        public void UpdateCache()
        {
            _cacheDataProvider.Set(_key, Cache);
        }

        public void ForceRefresh()
        {
            Cache = GetFromSource();
            UpdateCache();
        }
    }
}