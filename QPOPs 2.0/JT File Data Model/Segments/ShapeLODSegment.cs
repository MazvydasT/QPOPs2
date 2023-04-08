using System.Diagnostics;

namespace JTfy
{
    public class ShapeLODSegment : BaseDataStructure
    {
        public BaseDataStructure ShapeLODElement { get; private set; }

        public override int ByteCount
        {
            get { return ElementHeader.Size + ShapeLODElement.ByteCount; }
        }

        public override byte[] Bytes
        {
            get
            {
                var bytesList = new List<byte>(ByteCount);

                var objectTypeIdBaseTypePair = ConstUtils.TypeToObjectTypeId[ShapeLODElement.GetType()];

                bytesList.AddRange(new ElementHeader(ShapeLODElement.ByteCount + GUID.Size + 1, new GUID(objectTypeIdBaseTypePair.Item1), objectTypeIdBaseTypePair.Item2).Bytes);
                bytesList.AddRange(ShapeLODElement.Bytes);

                return bytesList.ToArray();
            }
        }

        public ShapeLODSegment(BaseDataStructure shapeLODElement)
        {
            ShapeLODElement = shapeLODElement;
        }

        public ShapeLODSegment(Stream stream)
        {
            var elementHeader = new ElementHeader(stream);

            var objectTypeIdAsString = elementHeader.ObjectTypeID.ToString();

            //var shapeLODElement = ConstUtils.CreateObjectFromTypeId(objectTypeIdAsString, stream);

            //if (shapeLODElement != null) ShapeLODElement = (BaseDataStructure)shapeLODElement;

            if (ConstUtils.ObjectTypeIdToType.TryGetValue(objectTypeIdAsString, out var value))
            {
                var shapeLODElement = Activator.CreateInstance(value.Item1, new object[] { stream });

                ShapeLODElement = (BaseDataStructure)shapeLODElement!;
            }

            else
                throw new NotImplementedException(String.Format("Case not defined for Shape LOD Element Object Type {0}", objectTypeIdAsString));

            Debug.Assert(stream.Length == stream.Position, "End of stream not reached at the end of ShapeLODSegment");

            stream.Dispose();
        }
    }
}