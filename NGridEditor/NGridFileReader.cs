using System;

namespace LoLNGRIDConverter
{
    public class NGridFileReader
    {
        private FileWrapper ngridFile;
        private NGrid grid;

        private NGridFileReader(FileWrapper fileWrapper)
        {
            this.ngridFile = fileWrapper;
            this.grid = new NGrid();
        }

        public static NGrid Load(string filePath)
        {
            FileWrapper file = new FileWrapper(filePath);
            NGridFileReader reader = new NGridFileReader(file);
            return reader.ProcessFile();
        }

        private NGrid ProcessFile()
        {
            try
            {
                int major = ngridFile.ReadByte();
                int minor = 0;
                if (major != 2) minor = ngridFile.ReadShort();

                grid.MajorVersion = major;
                grid.MinorVersion = minor;
                grid.MinBounds = ngridFile.ReadVector3();
                grid.MaxBounds = ngridFile.ReadVector3();
                grid.CellSize = ngridFile.ReadFloat();
                grid.CellCountX = ngridFile.ReadInt();
                grid.CellCountZ = ngridFile.ReadInt();

                if (major == 7) ReadCellsVersion7();
                else if (major == 2 || major == 3 || major == 5) ReadCellsVersion5();
                else throw new Exception($"Unsupported NGrid version: {major}");

                ReadHeightSamples();
                ReadHintNodes();

                return grid;
            }
            finally
            {
                ngridFile.Close();
            }
        }

        private void ReadCellsVersion7()
        {
            int totalCellCount = grid.CellCountX * grid.CellCountZ;

            for (int i = 0; i < totalCellCount; i++)
            {
                NavGridCell cell = new NavGridCell { index = i };
                cell.Height = ngridFile.ReadFloat(); ngridFile.ReadInt(); ngridFile.ReadFloat(); ngridFile.ReadInt(); ngridFile.ReadFloat();
                cell.x = ngridFile.ReadShort();
                cell.z = ngridFile.ReadShort();
                ngridFile.ReadInt(); ngridFile.ReadInt(); ngridFile.ReadInt(); ngridFile.ReadFloat(); ngridFile.ReadShort(); ngridFile.ReadShort(); ngridFile.ReadShort(); ngridFile.ReadShort();
                grid.Cells.Add(cell);
            }

            for (int i = 0; i < totalCellCount; i++) grid.Cells[i].visionPathingFlags = (VisionPathingFlags)ngridFile.ReadShort();

            for (int i = 0; i < totalCellCount; i++)
            {
                grid.Cells[i].riverRegionFlags = (RiverRegionFlags)ngridFile.ReadByte();
                int jq = ngridFile.ReadByte();
                grid.Cells[i].jungleQuadrantFlags = (JungleQuadrantFlags)(jq & 0x0f);
                grid.Cells[i].mainRegionFlags = (MainRegionFlags)((jq & ~0x0f) >> 4);
                int nl = ngridFile.ReadByte();
                grid.Cells[i].nearestLaneFlags = (NearestLaneFlags)(nl & 0x0f);
                grid.Cells[i].poiFlags = (POIFlags)((nl & ~0x0f) >> 4);
                int rs = ngridFile.ReadByte();
                grid.Cells[i].ringFlags = (RingFlags)(rs & 0x0f);
                grid.Cells[i].srxFlags = (UnknownSRXFlags)((rs & ~0x0f) >> 4);
            }

            for (int i = 0; i < 8; i++) for (int j = 0; j < 132; j++) ngridFile.ReadByte();
        }

        private void ReadCellsVersion5()
        {
            int totalCellCount = grid.CellCountX * grid.CellCountZ;

            for (int i = 0; i < totalCellCount; i++)
            {
                NavGridCell cell = new NavGridCell { index = i };
                cell.Height = ngridFile.ReadFloat(); ngridFile.ReadInt(); ngridFile.ReadFloat(); ngridFile.ReadInt(); ngridFile.ReadFloat(); ngridFile.ReadInt();
                cell.x = ngridFile.ReadShort();
                cell.z = ngridFile.ReadShort();
                ngridFile.ReadFloat(); ngridFile.ReadFloat(); ngridFile.ReadInt(); ngridFile.ReadInt(); ngridFile.ReadFloat();

                int arrivalDirection = ngridFile.ReadShort();
                int visionPathingFlags = ngridFile.ReadShort();
                int hintNode1 = ngridFile.ReadShort();
                int hintNode2 = ngridFile.ReadShort();

                if (grid.MajorVersion == 2 && hintNode2 == 0)
                {
                    hintNode2 = hintNode1;
                    hintNode1 = visionPathingFlags;
                    visionPathingFlags = (arrivalDirection & ~0xff) >> 8;
                    arrivalDirection &= 0xff;
                }

                cell.visionPathingFlags = (VisionPathingFlags)visionPathingFlags;
                grid.Cells.Add(cell);
            }

            if (grid.MajorVersion == 5)
            {
                for (int i = 0; i < totalCellCount; i++)
                {
                    grid.Cells[i].riverRegionFlags = (RiverRegionFlags)ngridFile.ReadByte();
                    int jq = ngridFile.ReadByte();
                    grid.Cells[i].jungleQuadrantFlags = (JungleQuadrantFlags)(jq & 0x0f);
                    grid.Cells[i].mainRegionFlags = (MainRegionFlags)((jq & ~0x0f) >> 4);
                }
                for (int i = 0; i < 4; i++) for (int j = 0; j < 132; j++) ngridFile.ReadByte();
            }
        }

        private void ReadHeightSamples()
        {
            grid.HeightSampleCountX = ngridFile.ReadInt();
            grid.HeightSampleCountZ = ngridFile.ReadInt();
            grid.HeightSampleOffsetX = ngridFile.ReadFloat();
            grid.HeightSampleOffsetZ = ngridFile.ReadFloat();
            int total = grid.HeightSampleCountX * grid.HeightSampleCountZ;
            for (int i = 0; i < total; i++) grid.HeightSamples.Add(ngridFile.ReadFloat());
        }

        private void ReadHintNodes()
        {
            for (int i = 0; i < 900; i++)
            {
                for (int j = 0; j < 900; j++) ngridFile.ReadFloat();
                ngridFile.ReadShort();
                ngridFile.ReadShort();
            }
        }
    }
}