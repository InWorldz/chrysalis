using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace chrysalis
{
    /// <summary>
    /// Frontend to a simple HTTP server
    /// </summary>
    class HttpFrontend
    {
        /// <summary>
        /// The maximum number of requests allowed in-flight
        /// </summary>
        public int MaxConcurrentRequests { get; set; }

        /// <summary>
        /// Delegate type to handle unhandled exceptions
        /// </summary>
        /// <param name="e">The exception that was thrown</param>
        public delegate void UnhandledExceptionHandler(Exception e);

        /// <summary>
        /// Registration for delegates to be called in the event of an unhandled exception
        /// </summary>
        public event UnhandledExceptionHandler OnUnhandledException;

        /// <summary>
        /// Handler functions that can respond to requests
        /// </summary>
        /// <param name="context">The HTTP context object</param>
        /// <param name="request">The request object</param>
        public delegate void RequestHandler(HttpListenerContext context, HttpListenerRequest request);

        private readonly HttpListener _httpListener;
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        /// <summary>
        /// Collection of handlers based on [HTTP_METHOD, PATH]
        /// </summary>
        private Dictionary<Tuple<string, string>, RequestHandler> _handlers = new Dictionary<Tuple<string, string>, RequestHandler>(); 

        /// <summary>
        /// Constructs a new HttpFrontend
        /// </summary>
        /// <param name="prefixes"></param>
        public HttpFrontend(IEnumerable<string> prefixes)
        {
            _httpListener = new HttpListener();
            foreach (var prefix in prefixes)
            {
                _httpListener.Prefixes.Add(prefix);
            }
        }

        /// <summary>
        /// Starts HTTP services
        /// </summary>
        public async void Start()
        {
            _httpListener.Start();

            var requests = new HashSet<Task>();
            for (int i = 0; i < MaxConcurrentRequests; i++)
            {
                requests.Add(_httpListener.GetContextAsync());
            }
                
            while (!_tokenSource.Token.IsCancellationRequested)
            {
                Task t = await Task.WhenAny(requests);
                requests.Remove(t);

                if (t is Task<HttpListenerContext>)
                {
                    var context = (t as Task<HttpListenerContext>).Result;
                    requests.Add(ProcessRequestAsync(context));
                    requests.Add(_httpListener.GetContextAsync());
                }
                else
                {
                    if (t.IsFaulted)
                    {
                        OnUnhandledException?.Invoke(t.Exception);
                    }
                }
                
            }
        }

        /// <summary>
        /// Processes a single request from the HTTP listener
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task ProcessRequestAsync(HttpListenerContext context)
        {

        }

        /// <summary>
        /// Stops HTTP services
        /// </summary>
        public void Stop()
        {
            _tokenSource.Cancel();
        }

    }
}
