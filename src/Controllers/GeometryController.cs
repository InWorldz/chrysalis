using System.Net;
using System.Threading.Tasks;
using FlatBuffers;
using InWorldz.Arbiter.Serialization;
using InWorldz.Chrysalis.Util;

namespace InWorldz.Chrysalis.Controllers
{
    /// <summary>
    /// Handles incoming requests related to geometry 
    /// </summary>
    internal class GeometryController
    {
        public GeometryController(HttpFrontend frontEnd)
        {
            frontEnd.AddHandler("POST", "/geometry/hp2b", ConvertHalcyonPrimToBabylon);
            frontEnd.AddHandler("POST", "/geometry/hg2b", ConvertHalcyonGroupToBabylon);
        }

        private async Task ConvertHalcyonPrimToBabylon(HttpListenerContext context, HttpListenerRequest request)
        {
            //halcyon gemoetry is coming in as a primitive flatbuffer object
            //as binary in the body. deserialize and convert using the prim exporter
            ByteBuffer body = await StreamUtil.ReadStreamFullyAsync(request.InputStream);
            var prim = HalcyonPrimitive.GetRootAsHalcyonPrimitive(body);
            


        }

        private async Task ConvertHalcyonGroupToBabylon(HttpListenerContext context, HttpListenerRequest request)
        {
            //halcyon gemoetry is coming in as a primitive flatbuffer object
            //as binary in the body. deserialize and convert using the prim exporter
            ByteBuffer body = await StreamUtil.ReadStreamFullyAsync(request.InputStream);
            var prim = HalcyonPrimitive.GetRootAsHalcyonPrimitive(body);


        }
    }
}