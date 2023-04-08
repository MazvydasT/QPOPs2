﻿//using System.Drawing;

namespace JTfy
{
    public class GeometricSet
    {
        public float[][] Positions { get; private set; } = Array.Empty<float[]>();

        public float[][] Normals { get; set; } = Array.Empty<float[]>();

        public int[][] TriStrips { get; private set; } = Array.Empty<int[]>();

        public Color Colour { get; set; } = RandomGenUtils.NextColour();

        public int ID { get; set; } = IdGenUtils.NextId;

        public int TriangleCount
        {
            get
            {
                var vertexCount = 0;

                for (int i = 0, c = TriStrips.Length; i < c; ++i)
                {
                    vertexCount += TriStrips[i].Length - 2;
                }

                return vertexCount;
            }
        }

        public float Area
        {
            get
            {
                double area = 0;

                for (int triStripIndex = 0, triStripCount = TriStrips.Length; triStripIndex < triStripCount; ++triStripIndex)
                {
                    var triStrip = TriStrips[triStripIndex];

                    for (int i = 0, c = triStrip.Length - 2; i < c; ++i)
                    {
                        area += CalcUtils.GetTriangleArea(
                            Positions[triStrip[i]],
                            Positions[triStrip[i + 1]],
                            Positions[triStrip[i + 2]]
                        );
                    }
                }

                return (float)area;
            }
        }

        public int Size
        {
            get
            {
                int size = Positions.Length * 4 * (Normals.Length == 0 ? 1 : 2);

                for (int i = 0, c = TriStrips.Length; i < c; ++i)
                {
                    size += TriStrips[i].Length * 4;
                }

                return size;
            }
        }

        public CoordF32 Center
        {
            get
            {
                var boundingBox = UntransformedBoundingBox;
                var maxCorner = boundingBox.MaxCorner;
                var minCorner = boundingBox.MinCorner;

                return new CoordF32(
                    maxCorner.X - minCorner.X,
                    maxCorner.Y - minCorner.Y,
                    maxCorner.Z - minCorner.Z
                );
            }
        }

        public BBoxF32 UntransformedBoundingBox
        {
            get
            {
                float minX = 0, minY = 0, minZ = 0, maxX = 0, maxY = 0, maxZ = 0;

                for (int i = 0, c = Positions.Length; i < c; ++i)
                {
                    var position = Positions[i];
                    var x = position[0];
                    var y = position[1];
                    var z = position[2];

                    if (i == 0)
                    {
                        minX = maxX = x;
                        minY = maxY = y;
                        minZ = maxZ = z;
                    }

                    else
                    {
                        if (x < minX) minX = x;
                        if (y < minY) minY = y;
                        if (z < minZ) minZ = z;

                        if (x > maxX) maxX = x;
                        if (y > maxY) maxY = y;
                        if (z > maxZ) maxZ = z;
                    }
                }

                return new BBoxF32(minX, minY, minZ, maxX, maxY, maxZ);
            }
        }

        public GeometricSet(int[][] triStrips, float[][] positions)
        {
            TriStrips = triStrips;
            Positions = positions;
        }

        public override string ToString()
        {
            /*var stringList = new List<string>(Positions.Length * (Normals == null ? 1 : 2) + TriStrips.Length);

            for (int i = 0, c = Positions.Length; i < c; ++i)
            {
                stringList.Add(String.Join(",", Positions[i]));

                if (Normals != null)
                {
                    stringList.Add(String.Join(",", Normals[i]));
                }
            }

            for (int i = 0, c = TriStrips.Length; i < c; ++i)
            {
                stringList.Add(String.Join(",", TriStrips[i]));
            }

            return String.Join("|", stringList) + Colour.ToString();*/

            return ID.ToString();
        }
    }
}