﻿namespace JTfy
{
    public class BaseShapeNodeElement : BaseNodeElement
    {
        public BBoxF32 TransformedBBox { get; set; } = new();

        public BBoxF32 UntransformedBBox { get; private set; }

        public float Area { get; set; }

        public CountRange VertexCountRange { get; private set; }

        public CountRange NodeCountRange { get; set; } = new CountRange();

        public CountRange PolygonCountRange { get; set; } = new CountRange();

        public int Size { get; set; }

        public float CompressionLevel { get; private set; }


        public override int ByteCount
        {
            get
            {
                return base.ByteCount + TransformedBBox.ByteCount + UntransformedBBox.ByteCount + 4 + VertexCountRange.ByteCount + NodeCountRange.ByteCount + PolygonCountRange.ByteCount + 4 + 4;
            }
        }

        public override byte[] Bytes
        {
            get
            {
                var bytesList = new List<byte>(ByteCount);

                bytesList.AddRange(base.Bytes);

                bytesList.AddRange(TransformedBBox.Bytes);
                bytesList.AddRange(UntransformedBBox.Bytes);
                bytesList.AddRange(StreamUtils.ToBytes(Area));
                bytesList.AddRange(VertexCountRange.Bytes);
                bytesList.AddRange(NodeCountRange.Bytes);
                bytesList.AddRange(PolygonCountRange.Bytes);
                bytesList.AddRange(StreamUtils.ToBytes(Size));
                bytesList.AddRange(StreamUtils.ToBytes(CompressionLevel));

                return bytesList.ToArray();
            }
        }

        public BaseShapeNodeElement(GeometricSet geometricSet, int objectId)
            : base(objectId)
        {
            TransformedBBox = new BBoxF32();
            UntransformedBBox = geometricSet.UntransformedBoundingBox;
            Area = geometricSet.Area;
            VertexCountRange = new CountRange(geometricSet.Positions.Length);
            NodeCountRange = new CountRange(1);
            PolygonCountRange = new CountRange(geometricSet.TriangleCount);
            Size = geometricSet.Size;
            CompressionLevel = 0;
        }

        public BaseShapeNodeElement(BBoxF32 untransformedBBox, CountRange vertexCountRange, int objectId)
            : base(objectId)
        {
            UntransformedBBox = untransformedBBox;
            VertexCountRange = vertexCountRange;
            CompressionLevel = 0;
        }

        public BaseShapeNodeElement(Stream stream)
            : base(stream)
        {
            TransformedBBox = new BBoxF32(stream);
            UntransformedBBox = new BBoxF32(stream);
            Area = StreamUtils.ReadFloat(stream);
            VertexCountRange = new CountRange(stream);
            NodeCountRange = new CountRange(stream);
            PolygonCountRange = new CountRange(stream);
            Size = StreamUtils.ReadInt32(stream);
            CompressionLevel = StreamUtils.ReadFloat(stream);
        }
    }
}