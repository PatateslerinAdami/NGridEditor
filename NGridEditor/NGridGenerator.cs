using LoLNGRIDConverter;
using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LoLNGRIDConverter
{
    public static class NGridGenerator
    {
        public static NGrid FromBitmap(
            string flagImagePath,
            float cellSize,
            Vector3 minBounds,
            Vector3 explicitMaxBounds,
            float sampleOffsetX,
            float sampleOffsetZ,
            int? templateSampleCountX,
            int? templateSampleCountZ)
        {
            BitmapImage flagBmp = new BitmapImage(new Uri(flagImagePath));
            int width = flagBmp.PixelWidth;
            int height = flagBmp.PixelHeight;

            NGrid grid = new NGrid();
            grid.MajorVersion = 7;
            grid.MinorVersion = 0;
            grid.CellSize = cellSize;
            grid.CellCountX = width;
            grid.CellCountZ = height;
            grid.MinBounds = minBounds;

            if (explicitMaxBounds.x != 0 || explicitMaxBounds.z != 0)
            {
                grid.MaxBounds = explicitMaxBounds;
            }
            else
            {
                grid.MaxBounds = new Vector3(
                    minBounds.x + (width * cellSize),
                    minBounds.y,
                    minBounds.z + (height * cellSize)
                );
            }

            grid.Cells = new List<NavGridCell>();
            FormatConvertedBitmap convertedFlagBmp = new FormatConvertedBitmap(flagBmp, PixelFormats.Bgra32, null, 0);
            int flagStride = width * 4;
            byte[] flagPixels = new byte[height * flagStride];
            convertedFlagBmp.CopyPixels(flagPixels, flagStride, 0);

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    NavGridCell cell = new NavGridCell { index = (z * width) + x, x = x, z = z };

                    int visualY = height - 1 - z;
                    int idx = (visualY * flagStride) + (x * 4);
                    byte b = flagPixels[idx], g = flagPixels[idx + 1], r = flagPixels[idx + 2];

                    if (NGridPalette.AreColorsEqual(NGridPalette.Wall, r, g, b)) cell.visionPathingFlags = VisionPathingFlags.Wall;
                    else if (NGridPalette.AreColorsEqual(NGridPalette.Brush, r, g, b)) cell.visionPathingFlags = VisionPathingFlags.Brush;
                    else if (NGridPalette.AreColorsEqual(NGridPalette.Structure, r, g, b)) cell.visionPathingFlags = VisionPathingFlags.StructureWall;
                    else if (NGridPalette.AreColorsEqual(NGridPalette.River, r, g, b)) cell.riverRegionFlags = RiverRegionFlags.River;
                    else cell.visionPathingFlags = VisionPathingFlags.Walkable;

                    grid.Cells.Add(cell);
                }
            }

            if (templateSampleCountX.HasValue && templateSampleCountZ.HasValue)
            {
                grid.HeightSampleCountX = templateSampleCountX.Value;
                grid.HeightSampleCountZ = templateSampleCountZ.Value;
            }
            else
            {
                grid.HeightSampleCountX = width + 1;
                grid.HeightSampleCountZ = height + 1;
            }

            grid.HeightSampleOffsetX = sampleOffsetX;
            grid.HeightSampleOffsetZ = sampleOffsetZ;

            grid.HeightSamples = new List<float>();

            int totalSamples = grid.HeightSampleCountX * grid.HeightSampleCountZ;
            for (int i = 0; i < totalSamples; i++)
            {
                grid.HeightSamples.Add(minBounds.y);
            }

            return grid;
        }
    }
}