using FlatBuffers;
using System.IO;
using System.Threading.Tasks;

namespace InWorldz.Chrysalis.Util
{
    internal class StreamUtil
    {
        private const int ChunkSz = 4096;

        public static async Task<ByteBuffer> ReadStreamFullyAsync(Stream inputStream)
        {
            MemoryStream fullStream = new MemoryStream();

            byte[] chunk = new byte[ChunkSz];
            int bytesRead;
            while ((bytesRead = await inputStream.ReadAsync(chunk, 0, ChunkSz)) == ChunkSz)
            {
                await fullStream.WriteAsync(chunk, 0, bytesRead);
            }

            return new ByteBuffer(fullStream.ToArray());
        }
    }
}
