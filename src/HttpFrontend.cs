using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
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
        public int MaxConcurrentRequests { get; set; } = 10;

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
        public delegate Task RequestHandler(HttpListenerContext context, HttpListenerRequest request);

        private readonly HttpListener _httpListener;
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        /// <summary>
        /// Collection of handlers based on [HTTP_METHOD, PATH]
        /// </summary>
        private Dictionary<Tuple<string, string>, RequestHandler> _handlers 
            = new Dictionary<Tuple<string, string>, RequestHandler>(); 

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
        public async Task Start()
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
        private async Task<bool> ProcessRequestAsync(HttpListenerContext context)
        {
            //combine path parts until we find a match
            StringBuilder sb = new StringBuilder();

            foreach (var segment in context.Request.Url.Segments)
            {
                sb.Append("/");
                sb.Append(segment);

                RequestHandler handler;
                var search = new Tuple<string, string>(context.Request.HttpMethod, sb.ToString());
                if (_handlers.TryGetValue(search, out handler))
                {
                    //we found a matching handler. call it
                    await handler(context, context.Request);
                    return true;
                }
            }

            //we didn't find a handler
            return false;
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
