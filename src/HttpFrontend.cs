using System.Collections.Generic;
using System.Net;

namespace chrysalis
{
    class HttpFrontend
    {
        private HttpListener _httpListener;
        private bool _stop = false;

        public HttpFrontend(IEnumerable<string> prefixes)
        {
            _httpListener = new HttpListener();
            foreach (var prefix in prefixes)
            {
                _httpListener.Prefixes.Add(prefix);
            }
        }

        public async void Start()
        {
            _httpListener.Start();

            while (!_stop)
            {
                await _httpListener.GetContextAsync();
            }
        }

        public void Stop()
        {
            _stop = true;
        }

    }
}
