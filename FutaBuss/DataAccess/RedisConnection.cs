﻿using StackExchange.Redis;

namespace FutaBuss.DataAccess
{
    public class RedisConnection
    {
        private readonly ConnectionMultiplexer _redis;
        private readonly IDatabase _db;

        public RedisConnection(string connectionString, string username, string password)
        {
            try
            {
                var options = ConfigurationOptions.Parse(connectionString);
                options.User = username;
                options.Password = password;
                _redis = ConnectionMultiplexer.Connect(options);
                _db = _redis.GetDatabase();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Redis connection error: " + ex.Message, ex);
            }
        }

        public void SetString(string key, string value)
        {
            try
            {
                _db.StringSet(key, value);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Redis set string error: " + ex.Message, ex);
            }
        }

        public string? GetString(string key)
        {
            try
            {
                return _db.StringGet(key);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Redis get string error: " + ex.Message, ex);
            }
        }
    }
}
