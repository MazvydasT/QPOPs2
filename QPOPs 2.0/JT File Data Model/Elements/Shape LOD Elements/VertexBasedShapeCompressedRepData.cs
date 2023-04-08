﻿namespace JTfy
{
    public class VertexBasedShapeCompressedRepData : BaseDataStructure
    {
        public Int16 VersionNumber { get; private set; }
        public byte NormalBinding { get; private set; }
        public byte TextureCoordBinding { get; private set; }
        public byte ColourBinding { get; private set; }
        public QuantizationParameters QuantizationParameters { get; private set; }
        public List<int> PrimitiveListIndices
        {
            get
            {
                var triStripsCount = TriStrips.Length;

                var primitiveListIndices = new List<int>(triStripsCount + 1);

                for (int i = 0; i < triStripsCount; ++i)
                {
                    var triStrip = TriStrips[i];

                    primitiveListIndices.Add(triStrip[0]);

                    if (i + 1 == triStripsCount)
                        primitiveListIndices.Add(triStrip[^1] + 1);
                }

                return primitiveListIndices;
            }
        }
        //public LossyQuantizedRawVertexData LossyQuantizedRawVertexData { get; private set; }
        public LosslessCompressedRawVertexData LosslessCompressedRawVertexData { get; private set; }

        public float[][] Positions { get; private set; }
        public float[][] Normals { get; private set; }
        public int[][] TriStrips { get; private set; }

        private byte[] primitiveListIndicesInt32CompressedDataPacketBytes = Array.Empty<byte>();
        private byte[] PrimitiveListIndicesInt32CompressedDataPacketBytes
        {
            get
            {
                if (primitiveListIndicesInt32CompressedDataPacketBytes.Length == 0)
                {
                    primitiveListIndicesInt32CompressedDataPacketBytes = Int32CompressedDataPacket.Encode(PrimitiveListIndices.ToArray(), Int32CompressedDataPacket.PredictorType.Stride1);
                }

                return primitiveListIndicesInt32CompressedDataPacketBytes;
            }
        }

        public override int ByteCount
        {
            get
            {
                return 2 + 1 + 1 + 1 + QuantizationParameters.ByteCount + PrimitiveListIndicesInt32CompressedDataPacketBytes.Length + LosslessCompressedRawVertexData.ByteCount;
            }
        }

        public override byte[] Bytes
        {
            get
            {
                var bytesList = new List<byte>(ByteCount);

                bytesList.AddRange(StreamUtils.ToBytes(VersionNumber));
                bytesList.Add(NormalBinding);
                bytesList.Add(TextureCoordBinding);
                bytesList.Add(ColourBinding);
                bytesList.AddRange(QuantizationParameters.Bytes);
                bytesList.AddRange(PrimitiveListIndicesInt32CompressedDataPacketBytes);
                bytesList.AddRange(LosslessCompressedRawVertexData.Bytes);

                return bytesList.ToArray();
            }
        }

        public VertexBasedShapeCompressedRepData(int[][] triStrips, float[][] vertexPositions, float[][]? vertexNormals = null)
        {
            VersionNumber = 1;
            NormalBinding = (byte)(vertexNormals == null ? 0 : 1);
            TextureCoordBinding = 0;
            ColourBinding = 0;
            QuantizationParameters = new QuantizationParameters(0, 0, 0, 0);



            // Next we need to make sure that first index of each tristrip is bigger by 1 than last index of previous tristrip
            // Next we need to extract first index of each tristrip also add last index of last tristrip + 1

            var newVertexIndex = 0;

            var newTriStrips = new int[triStrips.Length][];
            var newVertexPositions = new List<float[]>(vertexPositions.Length);
            var newVertexNormals = new List<float[]>(vertexNormals?.Length ?? 0);

            for (int triStripIndex = 0, triStripCount = triStrips.Length; triStripIndex < triStripCount; ++triStripIndex)
            {
                var triStrip = triStrips[triStripIndex];
                var indicesCount = triStrip.Length;

                var newTriStrip = new int[indicesCount];

                for (int i = 0; i < indicesCount; ++i)
                {
                    newTriStrip[i] = newVertexIndex++;

                    var vertexIndex = triStrip[i];

                    newVertexPositions.Add(vertexPositions[vertexIndex]);
                    if (vertexNormals != null) newVertexNormals.Add(vertexNormals[vertexIndex]);
                }

                newTriStrips[triStripIndex] = newTriStrip;
            }

            TriStrips = newTriStrips;

            TriStrips = newTriStrips;
            Positions = newVertexPositions.ToArray();
            Normals = vertexNormals != null ? newVertexNormals.ToArray() : Array.Empty<float[]>();

            //Next build vertex data from positons and normals eg x y z, xn yn zn -> repeat
            //Next convert vertex data to byte array

            var vertexData = new List<byte>((3 * 4 * newVertexPositions.Count) * (vertexNormals == null ? 1 : 2));

            for (int i = 0, c = newVertexPositions.Count; i < c; ++i)
            {
                if (vertexNormals != null)
                {
                    var vertexNormal = Normals[i];

                    vertexData.AddRange(StreamUtils.ToBytes(vertexNormal[0]));
                    vertexData.AddRange(StreamUtils.ToBytes(vertexNormal[1]));
                    vertexData.AddRange(StreamUtils.ToBytes(vertexNormal[2]));
                }

                var vertexPosition = Positions[i];

                vertexData.AddRange(StreamUtils.ToBytes(vertexPosition[0]));
                vertexData.AddRange(StreamUtils.ToBytes(vertexPosition[1]));
                vertexData.AddRange(StreamUtils.ToBytes(vertexPosition[2]));
            }

            LosslessCompressedRawVertexData = new LosslessCompressedRawVertexData(vertexData.ToArray());
        }

        public VertexBasedShapeCompressedRepData(Stream stream)
        {
            VersionNumber = StreamUtils.ReadInt16(stream);
            NormalBinding = StreamUtils.ReadByte(stream);
            TextureCoordBinding = StreamUtils.ReadByte(stream);
            ColourBinding = StreamUtils.ReadByte(stream);
            QuantizationParameters = new QuantizationParameters(stream);

            var primitiveListIndices = Int32CompressedDataPacket.GetArrayI32(stream, Int32CompressedDataPacket.PredictorType.Stride1);

            MemoryStream vertexDataStream;
            if (QuantizationParameters.BitsPerVertex == 0)
            {
                LosslessCompressedRawVertexData = new LosslessCompressedRawVertexData(stream);

                vertexDataStream = new MemoryStream(LosslessCompressedRawVertexData.VertexData);
            }

            else
            {
                throw new NotImplementedException("LossyQuantizedRawVertexData NOT IMPLEMENTED");
            }

            var readNormals = NormalBinding == 1;
            var readTextureCoords = TextureCoordBinding == 1;
            var readColours = ColourBinding == 1;

            var vertexEntrySize = 3 + (readNormals ? 3 : 0) + (readTextureCoords ? 2 : 0) + (readColours ? 3 : 0);
            var vertexEntryCount = (vertexDataStream.Length / 4) / vertexEntrySize;

            var vertexPositions = new float[vertexEntryCount][];
            var vertexNormals = readNormals ? new float[vertexEntryCount][] : Array.Empty<float[]>();
            var vertexColours = readColours ? new float[vertexEntryCount][] : Array.Empty<float[]>();
            var vertexTextureCoordinates = readTextureCoords ? new float[vertexEntryCount][] : Array.Empty<float[]>();

            for (int i = 0; i < vertexEntryCount; ++i)
            {
                if (readTextureCoords)
                    vertexTextureCoordinates[i] = new float[] { StreamUtils.ReadFloat(vertexDataStream), StreamUtils.ReadFloat(vertexDataStream) };

                if (readColours)
                    vertexColours[i] = new float[] { StreamUtils.ReadFloat(vertexDataStream), StreamUtils.ReadFloat(vertexDataStream), StreamUtils.ReadFloat(vertexDataStream) };

                if (readNormals)
                    vertexNormals[i] = new float[] { StreamUtils.ReadFloat(vertexDataStream), StreamUtils.ReadFloat(vertexDataStream), StreamUtils.ReadFloat(vertexDataStream) };

                vertexPositions[i] = new float[] { StreamUtils.ReadFloat(vertexDataStream), StreamUtils.ReadFloat(vertexDataStream), StreamUtils.ReadFloat(vertexDataStream) };
            }

            Positions = vertexPositions;
            Normals = vertexNormals;

            var triStripCount = primitiveListIndices.Length - 1;
            var triStrips = new int[triStripCount][];

            for (int triStripIndex = 0; triStripIndex < triStripCount; ++triStripIndex)
            {
                var startIndex = primitiveListIndices[triStripIndex];
                var endIndex = primitiveListIndices[triStripIndex + 1];

                var indicesCount = endIndex - startIndex;
                var indices = new int[indicesCount];

                for (int i = 0; i < indicesCount; ++i)
                {
                    indices[i] = startIndex + i;
                }

                triStrips[triStripIndex] = indices;
            }

            TriStrips = triStrips;
        }
    }
}