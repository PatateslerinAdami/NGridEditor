using System.Windows.Media;

namespace LoLNGRIDConverter
{
    public static class NGridPalette
    {
        public static Color Walkable = Colors.White;       // (255, 255, 255)
        public static Color Wall = Colors.Black;           // (0, 0, 0)
        public static Color Brush = Colors.Lime;           // (0, 255, 0) 
        public static Color River = Colors.Blue;           // (0, 0, 255)
        public static Color Structure = Colors.Gray;       // (128, 128, 128)

        public static bool AreColorsEqual(Color a, byte r, byte g, byte b)
        {
            return a.R == r && a.G == g && a.B == b;
        }
    }
}