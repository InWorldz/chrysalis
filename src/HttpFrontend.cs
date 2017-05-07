using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InWorldz.Chrysalis
{
    /// <summary>
    /// Frontend to a simple HTTP server
    /// </summary>
    internal class HttpFrontend
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
        private readonly Dictionary<Tuple<string, string>, RequestHandler> _handlers 
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
        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            if (context.Request.Url.Segments.Length == 1 && context.Request.Url.Segments[0] == "/")
            {
                //this is a special case asking for the root
                StringBuilder sb = new StringBuilder("/");
                if (await TryExecuteHandler(context, sb)) return;
            }
            else
            {
                //remove path parts until we find a match
                for (int i = context.Request.Url.Segments.Length; i > 1; i--)
                {
                    StringBuilder sb = new StringBuilder();
                    for (int segIdx = 0; segIdx < i; segIdx++)
                    { 
                        sb.Append(context.Request.Url.Segments[segIdx]);
                    }

                    //always end in /
                    if (sb[sb.Length - 1] != '/')
                    {
                        sb.Append("/");
                    }

                    if (await TryExecuteHandler(context, sb)) return;
                }
            }
            

            //we didn't find a handler
            context.Response.StatusCode = 404;
            context.Response.Close();
        }

        private async Task<bool> TryExecuteHandler(HttpListenerContext context, StringBuilder sb)
        {
            RequestHandler handler;
            var search = new Tuple<string, string>(context.Request.HttpMethod, sb.ToString());
            if (_handlers.TryGetValue(search, out handler))
            {
                //we found a matching handler. call it
                await handler(context, context.Request);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Stops HTTP services
        /// </summary>
        public void Stop()
        {
            _tokenSource.Cancel();
        }

        /// <summary>
        /// Adds a new HTTP handler
        /// </summary>
        /// <param name="method">The HTTP method</param>
        /// <param name="path">The minimal URL path that will trigger the handler</param>
        /// <param name="handler">The handler to process the request</param>
        public void AddHandler(string method, string path, RequestHandler handler)
        {
            if (!path.EndsWith("/")) path += "/";
            _handlers.Add(new Tuple<string, string>(method, path), handler);
        }

    }
}
