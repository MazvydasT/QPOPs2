﻿namespace JTfy
{
    public class Vec<T> : DataArray<T> where T : notnull
    {
        public Int32 Count { get { return data.Length; } }

        public override Int32 ByteCount { get { return 4 + base.ByteCount; } }

        public override Byte[] Bytes
        {
            get
            {
                var bytesList = new List<Byte>(ByteCount);

                bytesList.AddRange(StreamUtils.ToBytes(Count));

                for (int i = 0; i < Count; ++i)
                {
                    bytesList.AddRange(StreamUtils.ToBytes(data[i]));
                }

                return bytesList.ToArray();
            }
        }

        public Vec(Stream stream)
        {
            data = new T[StreamUtils.ReadInt32(stream)];

            for (int i = 0, c = data.Length; i < c; ++i)
            {
                data[i] = StreamUtils.Read<T>(stream);
            }
        }

        public Vec(T[] data)
        {
            this.data = data;
        }
    }

    public class VecI32 : Vec<Int32>
    {
        public VecI32(Stream stream) : base(stream) { }
        public VecI32(int[] data) : base(data) { }
        public VecI32() : this(Array.Empty<int>()) { }
    }

    public class VecU32 : Vec<UInt32>
    {
        public VecU32(Stream stream) : base(stream) { }
        public VecU32(uint[] data) : base(data) { }
        public VecU32() : this(Array.Empty<uint>()) { }
    }

    public class VecF32 : Vec<Single>
    {
        public VecF32(Stream stream) : base(stream) { }
        public VecF32(float[] data) : base(data) { }
        public VecF32() : this(Array.Empty<float>()) { }
    }

    public class MbString : Vec<UInt16>
    {
        public string Value
        {
            get
            {
                /*var chars = new char[data.Length];
                Buffer.BlockCopy(data, 0, chars, 0, chars.Length * 2);*/

                var chars = data.Select(d => BitConverter.ToChar(BitConverter.GetBytes(d), 0)).ToArray();

                return new String(chars);
            }
        }

        public override string ToString()
        {
            return Value;
        }

        public MbString(Stream stream) : base(stream) { }
        public MbString(UInt16[] data) : base(data) { }
        public MbString() : base(Array.Empty<ushort>()) { }
        public MbString(string value)
            : base(Array.Empty<ushort>())
        {
            var chars = value.ToCharArray();

            /*data = new UInt16[chars.Length];

            Buffer.BlockCopy(chars, 0, data, 0, data.Length * 2);*/

            data = chars.Select(c => BitConverter.ToUInt16(BitConverter.GetBytes(c), 0)).ToArray();
        }
    }
}