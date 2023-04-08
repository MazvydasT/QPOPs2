﻿namespace JTfy
{
    public class LosslessCompressedRawVertexData : BaseDataStructure
    {
        public int UncompressedDataSize { get { return VertexData.Length; } }
        public int CompressedDataSize { get { return CompressedVertexData.Length; } }

        private byte[] compressedVertexData = Array.Empty<byte>();
        private byte[] vertexData = Array.Empty<byte>();

        public byte[] CompressedVertexData
        {
            get
            {
                if (compressedVertexData.Length == 0)
                {
                    compressedVertexData = CompressionUtils.Compress(vertexData);
                }

                return compressedVertexData;
            }

            set
            {
                compressedVertexData = value;
            }
        }

        public byte[] VertexData
        {
            get
            {
                if (vertexData.Length == 0)
                {
                    vertexData = CompressionUtils.Decompress(compressedVertexData);
                }

                return vertexData;
            }

            set
            {
                vertexData = value;
            }
        }

        public override int ByteCount
        {
            get { return 4 + 4 + CompressedVertexData.Length; }
        }

        public override byte[] Bytes
        {
            get
            {
                var bytesList = new List<byte>(ByteCount);

                bytesList.AddRange(StreamUtils.ToBytes(VertexData.Length));
                bytesList.AddRange(StreamUtils.ToBytes(CompressedVertexData.Length));
                bytesList.AddRange(CompressedVertexData);

                return bytesList.ToArray();
            }
        }

        public LosslessCompressedRawVertexData(byte[] vertexData)
        {
            VertexData = vertexData;
        }

        public LosslessCompressedRawVertexData(Stream stream)
        {
            StreamUtils.ReadInt32(stream); //uncompressedDataSize
            var compressedDataSize = StreamUtils.ReadInt32(stream);

            if (compressedDataSize > 0)
                CompressedVertexData = StreamUtils.ReadBytes(stream, compressedDataSize, false);
            else if (compressedDataSize < 0)
                VertexData = StreamUtils.ReadBytes(stream, Math.Abs(compressedDataSize), false);
            else
                VertexData = CompressedVertexData = new byte[0];
        }
    }
}