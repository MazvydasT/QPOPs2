//using System.Drawing;

namespace JTfy
{
    public class Color
    {
        public byte A { get; set; }
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }

        public static Color FromArgb(int red, int green, int blue) => FromArgb(255, red, green, blue);
        public static Color FromArgb(int alpha, int red, int green, int blue)
        {
            if (alpha < 0 || alpha > 255) throw new ArgumentException("alpha is not within [0, 255] range");
            if (red < 0 || red > 255) throw new ArgumentException("red is not within [0, 255] range");
            if (green < 0 || green > 255) throw new ArgumentException("green is not within [0, 255] range");
            if (blue < 0 || blue > 255) throw new ArgumentException("blue is not within [0, 255] range");

            return new Color()
            {
                A = (byte)alpha,
                R = (byte)red,
                G = (byte)green,
                B = (byte)blue
            };
        }
    }
}