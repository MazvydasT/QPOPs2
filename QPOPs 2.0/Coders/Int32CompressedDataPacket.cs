﻿namespace JTfy
{
    public static class Int32CompressedDataPacket
    {
        public enum CODECType
        {
            Null = 0,
            Bitlength = 1,
            Huffman = 2,
            Arithmetic = 3
        }

        public enum PredictorType
        {
            /** Predictor type 'Lag1' */
            Lag1 = 0,

            /** Predictor type 'Lag2' */
            Lag2 = 1,

            /** Predictor type 'Stride1' */
            Stride1 = 2,

            /** Predictor type 'Stride2' */
            Stride2 = 3,

            /** Predictor type 'StripIndex' */
            StripIndex = 4,

            /** Predictor type 'Ramp' */
            Ramp = 5,

            /** Predictor type 'Xor1' */
            Xor1 = 6,

            /** Predictor type 'Xor2' */
            Xor2 = 7,

            /** Predictor type 'NULL' */
            NULL = 8
        }

        public static byte[] Encode(int[] data, PredictorType predictorType = PredictorType.NULL)
        {
            var packedValue = PackUnpack(data, predictorType, false);
            var encodedValues = EncodeValues(packedValue);

            return encodedValues;
        }

        public static Int32[] GetArrayI32(Stream stream, PredictorType predictorType = PredictorType.NULL)
        {
            var decodedSymbols = DecodeBytes(stream);
            var unpackedValues = PackUnpack(decodedSymbols, predictorType);

            return unpackedValues;
        }

        private static int[] DecodeBytes(Stream stream)
        {
            var codecType = (CODECType)StreamUtils.ReadByte(stream);

            Int32ProbabilityContexts? int32ProbabilityContexts = null;
            //int outOfBandValueCount;
            //int[] outOfBandValues;

            if (codecType == CODECType.Huffman || codecType == CODECType.Arithmetic)
            {
                throw new NotImplementedException("Huffman && Arithmetic codec NOT IMPLEMENTED");

                /*int32ProbabilityContexts = new Int32ProbabilityContexts(stream);
                outOfBandValueCount = StreamUtils.ReadInt32(stream);

                if (outOfBandValueCount > 0)
                {
                    outOfBandValues = DecodeBytes(stream);
                }*/
            }

            if (codecType != CODECType.Null)
            {
                var codeTextLength = StreamUtils.ReadInt32(stream);
                var valueElementCount = StreamUtils.ReadInt32(stream);
                //var symbolCount = valueElementCount;

                if (int32ProbabilityContexts != null && int32ProbabilityContexts.ProbabilityContextTableEntries.Length > 1)
                {
                    StreamUtils.ReadInt32(stream); //symbolCount
                }

                var wordsToRead = StreamUtils.ReadInt32(stream);
                var codeText = new uint[wordsToRead];
                for (int i = 0; i < wordsToRead; ++i)
                {
                    UInt32 codeTextWord;

                    if (StreamUtils.DataIsLittleEndian) // Convert to BigEndian
                    {
                        var bytes = StreamUtils.ReadBytes(stream, 4, true);
                        Array.Reverse(bytes);

                        /*var result = new UInt32[1];
                        Buffer.BlockCopy(bytes, 0, result, 0, 4);

                        codeTextWord = result[0];*/

                        codeTextWord = Convert.FromBytes<UInt32>(bytes);
                    }

                    else
                    {
                        codeTextWord = StreamUtils.ReadUInt32(stream);
                    }

                    codeText[i] = codeTextWord;
                }

                switch (codecType)
                {
                    case CODECType.Bitlength:
                        return BitlengthCoder.Decode(codeText, valueElementCount, codeTextLength);

                    case CODECType.Huffman:
                        throw new NotImplementedException("Huffman codec NOT IMPLEMENTED");

                    case CODECType.Arithmetic:
                        throw new NotImplementedException("Huffman codec NOT IMPLEMENTED");
                }
            }

            else
            {
                var integersToRead = StreamUtils.ReadInt32(stream);

                var decodedSymbols = new int[integersToRead];

                for (int i = 0; i < integersToRead; ++i)
                {
                    decodedSymbols[i] = StreamUtils.ReadInt32(stream);
                }

                return decodedSymbols;
            }

            return Array.Empty<int>();
        }

        private static byte[] EncodeValues(int[] values)
        {
            var valuesLength = values.Length;
            var byteCount = 1 + 4 + valuesLength * 4; // (byte)codecType + (int)integersToRead + (int[])values

            var bytesList = new List<byte>(byteCount)
            {
                (byte)CODECType.Null
            };
            bytesList.AddRange(StreamUtils.ToBytes(valuesLength));

            for (int i = 0; i < valuesLength; ++i)
            {
                bytesList.AddRange(StreamUtils.ToBytes(values[i]));
            }

            return bytesList.ToArray();
        }

        public static Int32[] PackUnpack(Int32[] residuals, PredictorType predictorType, bool unpack = true)
        {
            if (predictorType == PredictorType.NULL) return residuals;

            var unpackedResiduals = new Int32[residuals.Length];

            for (int i = 0, c = residuals.Length; i < c; ++i) // The first four values are not handeled
            {
                if (i < 4)
                {
                    unpackedResiduals[i] = residuals[i];
                }

                else
                {
                    var iPredicted = PredictValue(unpack ? unpackedResiduals : residuals, i, predictorType);

                    unpackedResiduals[i] = (predictorType == PredictorType.Xor1 || predictorType == PredictorType.Xor2 ? residuals[i] ^ iPredicted : residuals[i] + ((unpack ? 1 : -1) * iPredicted));
                }
            }

            return unpackedResiduals;
        }

        private static Int32 PredictValue(Int32[] unpackedValues, int index, PredictorType predictorType)
        {
            var v1 = unpackedValues[index - 1];
            var v2 = unpackedValues[index - 2];
            var v4 = unpackedValues[index - 4];

            return predictorType switch
            {
                PredictorType.Lag2 or PredictorType.Xor2 => v2,
                PredictorType.Stride1 => v1 + (v1 - v2),
                PredictorType.Stride2 => v2 + (v2 - v4),
                PredictorType.StripIndex => v2 - v4 < 8 && v2 - v4 > -8 ? v2 + (v2 - v4) : v2 + 2,
                PredictorType.Ramp => index,
                _ => v1
            };
        }
    }
}
