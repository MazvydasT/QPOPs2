namespace JTfy
{
    public class DataArray<T> : BaseDataStructure where T : notnull
    {
        protected T[] data = Array.Empty<T>();

        public override int ByteCount
        {
            get
            {
                //return data.Length * Marshal.SizeOf(typeof(T));
                return data.Length * StreamUtils.GetSizeInBytes(typeof(T));
            }
        }

        public override byte[] Bytes
        {
            get
            {
                var bytesList = new List<byte>(ByteCount);

                for (int i = 0, c = data.Length; i < c; ++i)
                {
                    bytesList.AddRange(StreamUtils.ToBytes(data[i]));
                }

                return bytesList.ToArray();
            }
        }
    }
}