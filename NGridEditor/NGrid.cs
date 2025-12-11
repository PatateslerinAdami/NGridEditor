using System.Collections.Generic;

namespace LoLNGRIDConverter
{
    public class NGrid
    {
        public int MajorVersion { get; set; }
        public int MinorVersion { get; set; }

        public Vector3 MinBounds { get; set; }
        public Vector3 MaxBounds { get; set; }

        public float CellSize { get; set; }
        public int CellCountX { get; set; }
        public int CellCountZ { get; set; }

        public List<NavGridCell> Cells { get; set; } = new List<NavGridCell>();

        public int HeightSampleCountX { get; set; }
        public int HeightSampleCountZ { get; set; }
        public float HeightSampleOffsetX { get; set; }
        public float HeightSampleOffsetZ { get; set; }
        public List<float> HeightSamples { get; set; } = new List<float>();

        public NavGridCell GetCell(int x, int z)
        {
            if (x < 0 || x >= CellCountX || z < 0 || z >= CellCountZ) return null;
            return Cells[(z * CellCountX) + x];
        }
    }
}