using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RosakhutorScraperLib
{
    public sealed class CameraScraper : IDisposable
    {
        private enum HttpRequestHeadersType
        {
            Default = 0,
            DetailedCameraInfo
        }

        #region Fields
        private bool _disposed = false;
        private int _nextPage = 1;
        private bool _lastPageParsed = false;

        private CameraParser _parser;
        private ScraperHttpClient _httpClient;

        private const string uBaseUrlRosakhutor = "https://rosakhutor.com";
        private const string uBaseUrlSochi = "https://sochi.camera";
        private const string uApiInternalListFormat = "https://rosakhutor.com/api/internal/component/about:camera.list?p={0}";
        private const string uApiInternalItemFormat = "https://rosakhutor.com/api/internal/component/about:camera.item/ajax?params[CAMERA_ID]={0}";
        private const string uWidgetUrlFormat = "https://sochi.camera/widget/widget.json?{0}";
        private const string uHlsUrlFormat = "https://sochi.camera:8081/cam_{0}/video.m3u8?token=bisv_dgZK";

        private readonly Dictionary<HttpRequestHeadersType, Dictionary<string, string>> _defaultHeadersCollection;
        #endregion

        #region Methods
        public CameraScraper()
        {
            _parser = new CameraParser();

            int headersTypesCount = Enum.GetNames(typeof(HttpRequestHeadersType)).Length;
            _defaultHeadersCollection = new Dictionary<HttpRequestHeadersType, Dictionary<string, string>>(headersTypesCount);
            for (int i = 0; i < headersTypesCount; ++i)
                _defaultHeadersCollection[(HttpRequestHeadersType)i] = GetHttpRequestHeaders((HttpRequestHeadersType)i);

            _httpClient = ScraperHttpClient.Instance;
            _httpClient.SetConnectionLimit(uBaseUrlRosakhutor, 5);
            _httpClient.SetConnectionLimit(uBaseUrlSochi, 5);
            _httpClient.SetHttpRequestHeaders(_defaultHeadersCollection[HttpRequestHeadersType.Default]);
        }
        public async Task<List<CameraInfo>> ParseNextAsync(bool withDetailedInfo = false)
        {
            if (_lastPageParsed)
                return new List<CameraInfo>(0);

            #region Get cameras names
            string html = await _httpClient.GetStringAsync(string.Format(uApiInternalListFormat, _nextPage++));
            List<string> names = _parser.GetNames(html);
            #endregion

            if (names.Count == 0)
            {
                _lastPageParsed = true;
                return new List<CameraInfo>(0);
            }
            int camerasCount = names.Count;
            Task<string>[] tasks = new Task<string>[camerasCount];

            #region Get cameras widgets
            List<string> ids = _parser.GetIds(html);    
            for (int i = 0; i < camerasCount; ++i)
            {
                tasks[i] = _httpClient.GetStringAsync(string.Format(uApiInternalItemFormat, ids[i]));
            }
            string[] htmls = await Task.WhenAll(tasks);
            List<string> widgets = new List<string>(camerasCount);
            foreach (string h in htmls)
            {
                widgets.Add(_parser.GetWidget(h));
            }
            #endregion

            #region Get cameras detailed info in JSON;
            string[] detailedInfoJSON = null;
            if (withDetailedInfo)
            {
                _httpClient.SetHttpRequestHeaders(_defaultHeadersCollection[HttpRequestHeadersType.DetailedCameraInfo]);
                for (int i = 0; i < camerasCount; ++i)
                {
                    tasks[i] = _httpClient.GetStringAsync(string.Format(uWidgetUrlFormat, widgets[i]));
                }
                detailedInfoJSON = await Task.WhenAll(tasks);
                _httpClient.SetHttpRequestHeaders(_defaultHeadersCollection[HttpRequestHeadersType.Default]);
            }
            #endregion

            #region Return cameras info
            List<CameraInfo> camerasInfo = new List<CameraInfo>(camerasCount);
            for (int i = 0; i < camerasCount; ++i)
            {
                camerasInfo.Add(new CameraInfo 
                { 
                    Name = names[i], 
                    HlsUrl = GetHlsUrl(widgets[i]), 
                    DetailedInfoJSON = detailedInfoJSON?[i]
                 });
            }
            #endregion
            return camerasInfo;
        }
        public void Reset(int nextPage = 1)
        {
            if (nextPage < 1)
                throw new ArgumentOutOfRangeException("nextPage", nextPage, "Page number must be greater than 0!");
            _nextPage = nextPage;
            _lastPageParsed = false;
        }
        private static Dictionary<string, string> GetHttpRequestHeaders(HttpRequestHeadersType type)
        {
            switch (type)
            {
                case HttpRequestHeadersType.DetailedCameraInfo:
                    return new Dictionary<string, string>
                    {
                        { "Accept", "*/*" },
                        { "Connection", "keep-alive" },
                        { "Host", uBaseUrlSochi.Substring(8) },
                        { "Origin", uBaseUrlRosakhutor },
                        { "Referer", uBaseUrlSochi },
                        { "Sec-Fetch-Dest", "empty" },
                        { "Sec-Fetch-Mode", "cors" },
                        { "Sec-Fetch-Site", "cross-site" },
                        { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.159 Safari/537.36 Edg/92.0.902.84" }
                    };
                case HttpRequestHeadersType.Default:
                default:
                    return new Dictionary<string, string>
                    {
                        { "User-Agent", "Mozilla/5.0 (X11; CrOS x86_64 8172.45.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.64 Safari/537.36" }
                    };
            }
        }
        private static string GetHlsUrl(string widget) => string.Format(uHlsUrlFormat, widget);
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                if (disposing)
                {
                    _httpClient?.Dispose();
                }
                _httpClient = null;
            }
        }
        ~CameraScraper() => Dispose(false);
        #endregion
    }
}
