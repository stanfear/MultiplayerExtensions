using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultiplayerExtensions.Utilities
{
    public static class WebUtils
    {
        private static HttpClient? _httpClient;

        public static HttpClient HttpClient
        {
            get
            {
                if (_httpClient == null)
                    _httpClient = new HttpClient();
                return _httpClient;
            }
        }

        public static async Task<byte[]> DownloadAsBytesAsync(Uri uri, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await HttpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }
    }
}
