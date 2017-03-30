using System.Net;
using System.Threading.Tasks;
using FlatBuffers;
using InWorldz.Arbiter.Serialization;
using InWorldz.Chrysalis.Util;
using InWorldz.PrimExporter.ExpLib;
using InWorldz.PrimExporter.ExpLib.ImportExport;
using OpenMetaverse;

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
            var part = Mapper.MapFlatbufferPrimToPart(prim);

            
        }

        private async Task ConvertHalcyonGroupToBabylon(HttpListenerContext context, HttpListenerRequest request)
        {
            //halcyon gemoetry is coming in as a primitive group flatbuffer object
            //as binary in the body. deserialize and convert using the prim exporter
            ByteBuffer body = await StreamUtil.ReadStreamFullyAsync(request.InputStream);

            var group = HalcyonGroup.GetRootAsHalcyonGroup(body);
            var sog = Mapper.MapFlatbufferGroupToSceneObjectGroup(group);

            var displayData = GroupLoader.Instance.GroupDisplayDataFromSOG(UUID.Zero, new GroupLoader.LoaderParams(),
                sog, null, null, null);

            BabylonFlatbufferFormatter formatter = new BabylonFlatbufferFormatter();

            var babylonExport = formatter.Export(displayData);
            var babylonFlatbuffer = babylonExport.FaceBlob;

            ObjectHasher hasher = new ObjectHasher();

            //compute the hash for the group to generate the etag
            ulong hash = 5381;
            foreach (var obj in babylonExport.BaseObjects)
            {
                hash = hasher.GetPrimHash(obj.MaterialHash, obj.ShapeHash);
            }

            context.Response.StatusCode = 200;
            context.Response.AddHeader("Content-Type", "application/octet-stream");
            context.Response.AddHeader("Etag", hash.ToString());

            await context.Response.OutputStream.WriteAsync(babylonFlatbuffer.Item1, babylonFlatbuffer.Item2,
                babylonFlatbuffer.Item3);

            context.Response.Close();
        }
    }
}