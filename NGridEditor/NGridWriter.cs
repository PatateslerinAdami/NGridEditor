using System;

namespace LoLNGRIDConverter
{
    public static class NGridWriter
    {
        public static void Save(NGrid grid, string filePath, int targetVersion)
        {
            FileWrapper file = new FileWrapper(filePath, true);

            try
            {
                file.WriteByte((byte)targetVersion);

                file.WriteShort(0);// Minor version, i think not used?

                file.WriteVector3(grid.MinBounds);
                file.WriteVector3(grid.MaxBounds);
                file.WriteFloat(grid.CellSize);
                file.WriteInt(grid.CellCountX);
                file.WriteInt(grid.CellCountZ);

                if (targetVersion == 7)
                {
                    WriteCellsVersion7(file, grid);
                }
                else if (targetVersion == 5)
                {
                    WriteCellsVersion5(file, grid);
                }
                else if (targetVersion == 3)
                {
                    WriteCellsVersion3(file, grid);
                }
                else
                {
                    throw new Exception($"Writing Version {targetVersion} is not supported.");
                }

                file.WriteInt(grid.HeightSampleCountX);
                file.WriteInt(grid.HeightSampleCountZ);
                file.WriteFloat(grid.HeightSampleOffsetX);
                file.WriteFloat(grid.HeightSampleOffsetZ);

                foreach (float sample in grid.HeightSamples)
                {
                    file.WriteFloat(sample);
                }

                for (int i = 0; i < 900; i++)
                {
                    for (int j = 0; j < 900; j++) file.WriteFloat(0);
                    file.WriteShort(0);
                    file.WriteShort(0);
                }
            }
            finally
            {
                file.Close();
            }
        }

        private static void WriteCellsVersion7(FileWrapper file, NGrid grid)
        {
            foreach (var cell in grid.Cells)
            {
                file.WriteFloat(cell.Height); file.WriteInt(0); file.WriteFloat(1); file.WriteInt(1); file.WriteFloat(0);
                file.WriteShort((short)cell.x);
                file.WriteShort((short)cell.z);
                file.WriteInt(0); file.WriteInt(0); file.WriteInt(0); file.WriteFloat(0);
                file.WriteShort(0); file.WriteShort(0); file.WriteShort(0); file.WriteShort(0);
            }

            foreach (var cell in grid.Cells) file.WriteShort((short)cell.visionPathingFlags);

            foreach (var cell in grid.Cells)
            {
                file.WriteByte((byte)cell.riverRegionFlags);
                int jq = (int)cell.jungleQuadrantFlags & 0x0f;
                int mr = ((int)cell.mainRegionFlags << 4) & 0xf0;
                file.WriteByte((byte)(jq | mr));
                int nl = (int)cell.nearestLaneFlags & 0x0f;
                int poi = ((int)cell.poiFlags << 4) & 0xf0;
                file.WriteByte((byte)(nl | poi));
                int ring = (int)cell.ringFlags & 0x0f;
                int srx = ((int)cell.srxFlags << 4) & 0xf0;
                file.WriteByte((byte)(ring | srx));
            }

            // unknown blocks
            for (int i = 0; i < 8; i++) file.WriteZeros(132);
        }

        private static void WriteCellsVersion5(FileWrapper file, NGrid grid)
        {
            foreach (var cell in grid.Cells)
            {
                file.WriteFloat(cell.Height); file.WriteInt(0); file.WriteFloat(1); file.WriteInt(1); file.WriteFloat(0); file.WriteInt(0);
                file.WriteShort((short)cell.x);
                file.WriteShort((short)cell.z);
                file.WriteFloat(0); file.WriteFloat(0); file.WriteInt(0); file.WriteInt(0); file.WriteFloat(0);

                file.WriteShort(0); 
                file.WriteShort((short)cell.visionPathingFlags); 
                file.WriteShort(0); 
                file.WriteShort(0); 
            }

            foreach (var cell in grid.Cells)
            {
                file.WriteByte((byte)cell.riverRegionFlags);
                int jq = (int)cell.jungleQuadrantFlags & 0x0f;
                int mr = ((int)cell.mainRegionFlags << 4) & 0xf0;
                file.WriteByte((byte)(jq | mr));
            }

            // unknown blocks
            for (int i = 0; i < 4; i++) file.WriteZeros(132);
        }

        private static void WriteCellsVersion3(FileWrapper file, NGrid grid)
        {
            foreach (var cell in grid.Cells)
            {
                file.WriteFloat(cell.Height); file.WriteInt(0); file.WriteFloat(1); file.WriteInt(1); file.WriteFloat(0); file.WriteInt(0);
                file.WriteShort((short)cell.x);
                file.WriteShort((short)cell.z);
                file.WriteFloat(0); file.WriteFloat(0); file.WriteInt(0); file.WriteInt(0); file.WriteFloat(0);

                file.WriteShort(0); 
                file.WriteShort((short)cell.visionPathingFlags);
                file.WriteShort(0); 
                file.WriteShort(0); 
            }
        }
    }
}