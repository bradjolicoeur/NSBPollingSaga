using Microsoft.Extensions.Caching.Memory;
using System;
using TargetAPI.Models;

namespace TargetAPI.Repository
{
    public class RequestRepository : IRequestRepository
    {
        private readonly IMemoryCache _cache;

        public RequestRepository(IMemoryCache memoryCache)
        {
            _cache = memoryCache;
        }

        /// <summary>
        /// add request to the cache so we can check on it
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public RequestState Insert(ValueRequest model)
        {
            // Look for cache key.
            if (model.ClaimCheckToken == null || !_cache.TryGetValue(model.ClaimCheckToken, out ValueModel cacheEntry))
            {
                // Key not in cache, so get data.
                cacheEntry = model;
                cacheEntry.ClaimCheckToken = Guid.NewGuid().ToString();
                cacheEntry.CompleteBy = DateTime.UtcNow.AddSeconds(90);

                // Set cache options.
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    // Keep in cache for this time, reset time if accessed.
                    .SetSlidingExpiration(TimeSpan.FromMinutes(10));

                // Save data in cache.
                _cache.Set(model.ClaimCheckToken, cacheEntry, cacheEntryOptions);
            }

            return new RequestState { ClaimCheckToken = cacheEntry.ClaimCheckToken,
                                    Completed = false, Success = true,
                                    Message = $"Request started for {model.RequestId}" };
        }

        /// <summary>
        /// See if the request is completed yet
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public RequestState CheckRequest(string claimCheckToken)
        {
            var state = new RequestState { ClaimCheckToken = claimCheckToken };

            // Look for cache key.
            if (!_cache.TryGetValue(claimCheckToken, out ValueModel cacheEntry))
            {
                state.Completed = true;
                state.Success = false;
                state.Message = $"Request No longer Valid for {claimCheckToken}";
            }
            else
            {
                state.Success = true;

                if (cacheEntry.CompleteBy <= DateTime.UtcNow)
                {
                    state.Completed = true;
                    state.Message = $"Request completed for {claimCheckToken}";
                }
                else
                {
                    state.Completed = false;
                    state.Message = $"Request still processing for {claimCheckToken}";
                }

            }

            return state;
        }
    }
}
