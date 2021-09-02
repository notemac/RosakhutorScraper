using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace RosakhutorScraperLib
{
    internal sealed class ScraperHttpClient : HttpClient
    {
        #region Fields
        private bool _disposed = false;
        public static ScraperHttpClient Instance;
        #endregion

        #region Methods
        static ScraperHttpClient() => Instance = new ScraperHttpClient();
        private ScraperHttpClient() : base() { }
        internal void SetHttpRequestHeaders(Dictionary<string, string> requestHeaders)
        {
            if (requestHeaders == null) return;
            DefaultRequestHeaders.Clear();
            foreach (var header in requestHeaders)
            {
                DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }
        /// <summary>
        /// Подводные камни HttpClient в .NET: https://habr.com/ru/post/424873/
        /// </summary>
        internal void SetConnectionLimit(string baseUrl, int connectionLimit)
            => ServicePointManager.FindServicePoint(new Uri(baseUrl)).ConnectionLimit = connectionLimit;

        internal new void Dispose() 
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                Instance = null;
                base.Dispose(disposing);
            }
        }
        ~ScraperHttpClient() => Dispose(false);
        #endregion
    }
}
