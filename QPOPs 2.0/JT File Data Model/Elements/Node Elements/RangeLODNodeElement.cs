namespace JTfy
{
    public class RangeLODNodeElement : LODNodeElement
    {
        public VecF32 RangeLimits { get; set; } = new VecF32();

        public CoordF32 Center { get; set; } = new CoordF32();

        public override int ByteCount { get { return base.ByteCount + RangeLimits.ByteCount + Center.ByteCount; } }

        public override byte[] Bytes
        {
            get
            {
                var bytesList = new List<Byte>(ByteCount);

                bytesList.AddRange(base.Bytes);
                bytesList.AddRange(RangeLimits.Bytes);
                bytesList.AddRange(Center.Bytes);

                return bytesList.ToArray();
            }
        }

        public RangeLODNodeElement(int objectId) : base(objectId) { }

        public RangeLODNodeElement(Stream stream) : base(stream)
        {
            RangeLimits = new VecF32(stream);
            Center = new CoordF32(stream);
        }
    }
}