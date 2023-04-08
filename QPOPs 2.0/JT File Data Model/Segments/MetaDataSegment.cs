using System.Diagnostics;

namespace JTfy
{
    public class MetaDataSegment : BaseDataStructure
    {
        public BaseDataStructure MetaDataElement { get; private set; }

        public override int ByteCount { get { return ElementHeader.Size + MetaDataElement.ByteCount; } }

        public override byte[] Bytes
        {
            get
            {
                var bytesList = new List<byte>(ByteCount);

                var objectTypeIdBaseTypePair = ConstUtils.TypeToObjectTypeId[MetaDataElement.GetType()];

                bytesList.AddRange(new ElementHeader(MetaDataElement.ByteCount + GUID.Size + 1, new GUID(objectTypeIdBaseTypePair.Item1), objectTypeIdBaseTypePair.Item2).Bytes);
                bytesList.AddRange(MetaDataElement.Bytes);

                return bytesList.ToArray();
            }
        }

        public MetaDataSegment(BaseDataStructure metaDataElement)
        {
            MetaDataElement = metaDataElement;
        }

        public MetaDataSegment(Stream stream)
        {
            var elementHeader = new ElementHeader(stream);

            var objectTypeIdAsString = elementHeader.ObjectTypeID.ToString();

            //var metaDataElement = ConstUtils.CreateObjectFromTypeId(objectTypeIdAsString, stream);

            //if(metaDataElement != null) MetaDataElement = (BaseDataStructure)metaDataElement;

            if (ConstUtils.ObjectTypeIdToType.TryGetValue(objectTypeIdAsString, out var value))
            {
                var metaDataElement = Activator.CreateInstance(value.Item1, new object[] { stream });

                MetaDataElement = (BaseDataStructure)metaDataElement!;
            }

            else
                throw new NotImplementedException(String.Format("Case not defined for Graph Element Object Type {0}", objectTypeIdAsString));

            Debug.Assert(stream.Length == stream.Position, "End of stream not reached at the end of MetaDataSegment");

            stream.Dispose();
        }
    }
}