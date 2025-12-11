using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LoLNGRIDConverter
{
    public static class NGridRenderer
    {
        public static WriteableBitmap Render(NGrid grid)
        {
            int width = grid.CellCountX;
            int height = grid.CellCountZ;
            WriteableBitmap wbmp = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);

            wbmp.Lock();
            unsafe
            {
                int* pBackBuffer = (int*)wbmp.BackBuffer;

                for (int i = 0; i < grid.Cells.Count; i++)
                {
                    var cell = grid.Cells[i];
                    Color c = GetColorForCell(cell);
                    int pixelColor = (c.A << 24) | (c.R << 16) | (c.G << 8) | c.B;

                    //flip for the bottom -left origin
                    int visualY = height - 1 - cell.z;

                    int targetIndex = (visualY * width) + cell.x;

                    pBackBuffer[targetIndex] = pixelColor;
                }
            }
            wbmp.AddDirtyRect(new System.Windows.Int32Rect(0, 0, width, height));
            wbmp.Unlock();
            return wbmp;
        }

        private static Color GetColorForCell(NavGridCell cell)
        {
            if ((cell.visionPathingFlags & VisionPathingFlags.Wall) != 0)
                return NGridPalette.Wall;

            if ((cell.visionPathingFlags & VisionPathingFlags.StructureWall) != 0)
                return NGridPalette.Structure;

            if ((cell.visionPathingFlags & VisionPathingFlags.Brush) != 0)
                return NGridPalette.Brush;

            if ((cell.riverRegionFlags & RiverRegionFlags.River) != 0)
                return NGridPalette.River;

            return NGridPalette.Walkable;
        }
    }
}