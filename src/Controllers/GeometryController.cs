using System;
using System.Net;
using System.Threading.Tasks;

namespace InWorldz.Chrysalis.Controllers
{
    /// <summary>
    /// Handles incoming requests related to geometry 
    /// </summary>
    internal class GeometryController
    {
        public GeometryController(HttpFrontend frontEnd)
        {
            frontEnd.AddHandler("POST", "/geometry/h2b", ConvertHalcyonGeomToBabylon);
        }

        private async Task ConvertHalcyonGeomToBabylon(HttpListenerContext context, HttpListenerRequest request)
        {
            throw new NotImplementedException();
        }
    }
}