using System;
using System.Linq;
using System.Net;
using System.Text;
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
        private ObjectHasher _hasher = new ObjectHasher();

        public GeometryController(HttpFrontend frontEnd)
        {
            frontEnd.AddHandler("POST", "/geometry/hp2b", ConvertHalcyonPrimToBabylon);
            frontEnd.AddHandler("POST", "/geometry/hg2b", ConvertHalcyonGroupToBabylon);
            frontEnd.AddHandler("POST", "/geometry/grouphash", GetHalcyonGroupHash);
            frontEnd.AddHandler("POST", "/geometry/primhash", GetHalcyonPrimHash);
        }

        private async Task GetHalcyonPrimHash(HttpListenerContext context, HttpListenerRequest request)
        {
            //halcyon geometry is coming in as a primitive flatbuffer object
            //as binary in the body. deserialize and hash
            ByteBuffer body = await StreamUtil.ReadStreamFullyAsync(request.InputStream);

            ulong hash = 0;
            //The hashing is CPU intensive. Use a thread task
            await Task.Run(() =>
            {
                var prim = HalcyonPrimitive.GetRootAsHalcyonPrimitive(body);
                var part = Mapper.MapFlatbufferPrimToPart(prim);

                hash = _hasher.GetPrimHash(part.Shape);
            });

            context.Response.StatusCode = 200;
            context.Response.AddHeader("Content-Type", "text/plain");
            byte[] hashText = Encoding.UTF8.GetBytes(hash.ToString());

            await context.Response.OutputStream.WriteAsync(hashText, 0, hashText.Length);

            await context.Response.OutputStream.FlushAsync();
            context.Response.Close();
        }

        private async Task GetHalcyonGroupHash(HttpListenerContext context, HttpListenerRequest request)
        {
            //halcyon geometry is coming in as a primitive flatbuffer object
            //as binary in the body. deserialize and hash
            ByteBuffer body = await StreamUtil.ReadStreamFullyAsync(request.InputStream);

            ulong hash = 0;
            //The hashing is CPU intensive. Use a thread task
            await Task.Run(() =>
            {
                var group = HalcyonGroup.GetRootAsHalcyonGroup(body);
                var sog = Mapper.MapFlatbufferGroupToSceneObjectGroup(group);

                hash = _hasher.GetGroupHash(sog);
            });

            context.Response.StatusCode = 200;
            context.Response.AddHeader("Content-Type", "text/plain");
            byte[] hashText = Encoding.UTF8.GetBytes(hash.ToString());

            await context.Response.OutputStream.WriteAsync(hashText, 0, hashText.Length);

            await context.Response.OutputStream.FlushAsync();
            context.Response.Close();
        }

        private async Task ConvertHalcyonPrimToBabylon(HttpListenerContext context, HttpListenerRequest request)
        {
            //halcyon gemoetry is coming in as a primitive flatbuffer object
            //as binary in the body. deserialize and convert using the prim exporter
            ByteBuffer body = await StreamUtil.ReadStreamFullyAsync(request.InputStream);

            ExportResult babylonExport = null;
            Tuple<byte[], int, int> babylonFlatbuffer = null;

            //The actual conversion is CPU intensive. Use a thread task
            await Task.Run(() =>
            {
                var prim = HalcyonPrimitive.GetRootAsHalcyonPrimitive(body);
                var part = Mapper.MapFlatbufferPrimToPart(prim);

                var displayData = GroupLoader.Instance.ExtractPrimMesh(part, new GroupLoader.LoaderParams(), null);
                BabylonFlatbufferFormatter formatter = new BabylonFlatbufferFormatter();

                babylonExport = formatter.Export(displayData);
                babylonFlatbuffer = babylonExport.FaceBlob;
            });

            context.Response.StatusCode = 200;
            context.Response.AddHeader("Content-Type", "application/octet-stream");
            context.Response.AddHeader("Etag", babylonExport.Hash.ToString());

            await context.Response.OutputStream.WriteAsync(babylonFlatbuffer.Item1, babylonFlatbuffer.Item2,
                babylonFlatbuffer.Item3);

            await context.Response.OutputStream.FlushAsync();
            context.Response.Close();
        }

        private async Task ConvertHalcyonGroupToBabylon(HttpListenerContext context, HttpListenerRequest request)
        {
            //halcyon gemoetry is coming in as a primitive group flatbuffer object
            //as binary in the body. deserialize and convert using the prim exporter
            ByteBuffer body = await StreamUtil.ReadStreamFullyAsync(request.InputStream);

            ExportResult babylonExport = null;
            Tuple<byte[], int, int> babylonFlatbuffer = null;

            //The actual conversion is CPU intensive. Use a thread task
            await Task.Run(() =>
            {
                var group = HalcyonGroup.GetRootAsHalcyonGroup(body);
                var sog = Mapper.MapFlatbufferGroupToSceneObjectGroup(group);

                var displayData = GroupLoader.Instance.GroupDisplayDataFromSOG(UUID.Zero, new GroupLoader.LoaderParams(),
                    sog, null, null, null);

                BabylonFlatbufferFormatter formatter = new BabylonFlatbufferFormatter();

                babylonExport = formatter.Export(displayData);
                babylonFlatbuffer = babylonExport.FaceBlob;
            });

            context.Response.StatusCode = 200;
            context.Response.AddHeader("Content-Type", "application/octet-stream");
            context.Response.AddHeader("Etag", babylonExport.Hash.ToString());

            await context.Response.OutputStream.WriteAsync(babylonFlatbuffer.Item1, babylonFlatbuffer.Item2,
                babylonFlatbuffer.Item3);

            context.Response.Close();
        }
    }
}