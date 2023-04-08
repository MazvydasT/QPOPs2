namespace JTfy
{
    public static class Convert
    {
        public static byte[] ToBytes<T>(T value) where T : notnull
        {
            var inputTypeName = typeof(T).Name;

            return inputTypeName switch
            {
                "Byte" => new[] { (byte)(object)value },
                "SByte" => new[] { (byte)(object)value },
                "Char" => BitConverter.GetBytes((Char)(object)value),
                "Int16" => BitConverter.GetBytes((Int16)(object)value),
                "UInt16" => BitConverter.GetBytes((UInt16)(object)value),
                "Int32" => BitConverter.GetBytes((Int32)(object)value),
                "UInt32" => BitConverter.GetBytes((UInt32)(object)value),
                "Int64" => BitConverter.GetBytes((Int64)(object)value),
                "UInt64" => BitConverter.GetBytes((UInt64)(object)value),
                "Double" => BitConverter.GetBytes((Double)(object)value),
                "Single" => BitConverter.GetBytes((Single)(object)value),
                _ => throw new NotSupportedException($"Input type <{inputTypeName}> is not one of: Byte, SByte, Char, Int16, UInt16, Int32, UInt32, Int64, UInt64, Double, Single")
            };
        }

        public static byte[] ToBytes<T>(T[] values) where T : notnull => values.SelectMany(ToBytes<T>).ToArray();

        public static T FromBytes<T>(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes), $"{nameof(bytes)} is null");

            var returnTypeName = typeof(T).Name;

            return returnTypeName switch
            {
                "Byte" => bytes.Length == 1 ?
                (T)(object)bytes[0] :
                throw new ArgumentException($"{nameof(bytes)}.Length should be 1 for {nameof(FromBytes)}<Byte>"),

                "SByte" => bytes.Length == 1 ?
                (T)(object)bytes[0] :
                throw new ArgumentException($"{nameof(bytes)}.Length should be 1 for {nameof(FromBytes)}<SByte>"),

                "Char" => bytes.Length == 2 ?
                (T)(object)BitConverter.ToChar(bytes, 0) :
                throw new ArgumentException($"{nameof(bytes)}.Length should be 2 for {nameof(FromBytes)}<Char>"),

                "Int16" => bytes.Length == 2 ?
                (T)(object)BitConverter.ToInt16(bytes, 0) :
                throw new ArgumentException($"{nameof(bytes)}.Length should be 2 for {nameof(FromBytes)}<Int16>"),

                "UInt16" => bytes.Length == 2 ?
                (T)(object)BitConverter.ToUInt16(bytes, 0) :
                throw new ArgumentException($"{nameof(bytes)}.Length should be 2 for {nameof(FromBytes)}<UInt16>"),

                "Int32" => bytes.Length == 4 ?
                (T)(object)BitConverter.ToInt32(bytes, 0) :
                throw new ArgumentException($"{nameof(bytes)}.Length should be 4 for {nameof(FromBytes)}<Int32>"),

                "UInt32" => bytes.Length == 4 ?
                (T)(object)BitConverter.ToUInt32(bytes, 0) :
                throw new ArgumentException($"{nameof(bytes)}.Length should be 4 for {nameof(FromBytes)}<UInt32>"),

                "Int64" => bytes.Length == 8 ?
                (T)(object)BitConverter.ToInt64(bytes, 0) :
                throw new ArgumentException($"{nameof(bytes)}.Length should be 8 for {nameof(FromBytes)}<Int64>"),

                "UInt64" => bytes.Length == 8 ?
                (T)(object)BitConverter.ToUInt64(bytes, 0) :
                throw new ArgumentException($"{nameof(bytes)}.Length should be 8 for {nameof(FromBytes)}<UInt64>"),

                "Double" => bytes.Length == 8 ?
                (T)(object)BitConverter.ToDouble(bytes, 0) :
                throw new ArgumentException($"{nameof(bytes)}.Length should be 8 for {nameof(FromBytes)}<Double>"),

                "Single" => bytes.Length == 4 ?
                (T)(object)BitConverter.ToSingle(bytes, 0) :
                throw new ArgumentException($"{nameof(bytes)}.Length should be 4 for {nameof(FromBytes)}<Single>"),

                _ => throw new NotSupportedException($"Return type <{returnTypeName}> is not one of: Byte, SByte, Char, Int16, UInt16, Int32, UInt32, Int64, UInt64, Double, Single")
            };
        }
    }
}