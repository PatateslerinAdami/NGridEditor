using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace LoLNGRIDConverter
{
    public static class WGeoToNGridGenerator
    {
        private struct Tri
        {
            public Vector3 P1, P2, P3;
            public double MinX, MaxX, MinZ, MaxZ;
        }

        public static async Task<NGrid> GenerateAsync(List<WGeoModelData> models, float cellSize, float manualYOffset)
        {
            List<Tri> allTriangles = new List<Tri>();
            double minX = double.MaxValue, maxX = double.MinValue;
            double minZ = double.MaxValue, maxZ = double.MinValue;

            foreach (var model in models)
            {
                var mesh = model.Mesh;
                if (mesh == null || mesh.TriangleIndices.Count == 0) continue;

                for (int i = 0; i < mesh.TriangleIndices.Count; i += 3)
                {
                    int i1 = mesh.TriangleIndices[i];
                    int i2 = mesh.TriangleIndices[i + 1];
                    int i3 = mesh.TriangleIndices[i + 2];

                    var p1 = mesh.Positions[i1];
                    var p2 = mesh.Positions[i2];
                    var p3 = mesh.Positions[i3];

                    minX = Math.Min(minX, Math.Min(p1.X, Math.Min(p2.X, p3.X)));
                    maxX = Math.Max(maxX, Math.Max(p1.X, Math.Max(p2.X, p3.X)));

                    double z1 = -p1.Z; double z2 = -p2.Z; double z3 = -p3.Z;
                    minZ = Math.Min(minZ, Math.Min(z1, Math.Min(z2, z3)));
                    maxZ = Math.Max(maxZ, Math.Max(z1, Math.Max(z2, z3)));

                    Tri t = new Tri
                    {
                        P1 = new Vector3((float)p1.X, (float)p1.Y, (float)z1),
                        P2 = new Vector3((float)p2.X, (float)p2.Y, (float)z2),
                        P3 = new Vector3((float)p3.X, (float)p3.Y, (float)z3)
                    };
                    t.MinX = Math.Min(t.P1.x, Math.Min(t.P2.x, t.P3.x));
                    t.MaxX = Math.Max(t.P1.x, Math.Max(t.P2.x, t.P3.x));
                    t.MinZ = Math.Min(t.P1.z, Math.Min(t.P2.z, t.P3.z));
                    t.MaxZ = Math.Max(t.P1.z, Math.Max(t.P2.z, t.P3.z));
                    allTriangles.Add(t);
                }
            }

            minX = Math.Floor(minX / cellSize) * cellSize;
            minZ = Math.Floor(minZ / cellSize) * cellSize;
            maxX = Math.Ceiling(maxX / cellSize) * cellSize;
            maxZ = Math.Ceiling(maxZ / cellSize) * cellSize;

            minX -= cellSize * 2; minZ -= cellSize * 2;
            maxX += cellSize * 2; maxZ += cellSize * 2;

            double finalMinX = minX; double finalMaxX = maxX;
            double finalMinZ = minZ; double finalMaxZ = maxZ;

            return await Task.Run(() =>
            {
                double bucketSize = 1000.0;
                int bucketCountX = (int)((finalMaxX - finalMinX) / bucketSize) + 1;
                int bucketCountZ = (int)((finalMaxZ - finalMinZ) / bucketSize) + 1;
                List<Tri>[,] buckets = new List<Tri>[bucketCountX, bucketCountZ];

                for (int i = 0; i < bucketCountX; i++)
                    for (int j = 0; j < bucketCountZ; j++)
                        buckets[i, j] = new List<Tri>();

                foreach (var tri in allTriangles)
                {
                    int startX = (int)((tri.MinX - finalMinX) / bucketSize);
                    int endX = (int)((tri.MaxX - finalMinX) / bucketSize);
                    int startZ = (int)((tri.MinZ - finalMinZ) / bucketSize);
                    int endZ = (int)((tri.MaxZ - finalMinZ) / bucketSize);

                    startX = Math.Max(0, startX); endX = Math.Min(bucketCountX - 1, endX);
                    startZ = Math.Max(0, startZ); endZ = Math.Min(bucketCountZ - 1, endZ);

                    for (int x = startX; x <= endX; x++)
                        for (int z = startZ; z <= endZ; z++)
                            buckets[x, z].Add(tri);
                }

                int cellsX = (int)Math.Round((finalMaxX - finalMinX) / cellSize);
                int cellsZ = (int)Math.Round((finalMaxZ - finalMinZ) / cellSize);
                int sampleW = cellsX + 1;
                int sampleH = cellsZ + 1;

                NGrid grid = new NGrid();
                grid.MajorVersion = 7;
                grid.CellSize = cellSize;
                grid.MinBounds = new Vector3((float)finalMinX, 0, (float)finalMinZ);
                grid.MaxBounds = new Vector3((float)finalMaxX, 0, (float)finalMaxZ);
                grid.CellCountX = cellsX;
                grid.CellCountZ = cellsZ;
                grid.HeightSampleCountX = sampleW;
                grid.HeightSampleCountZ = sampleH;
                grid.HeightSampleOffsetX = cellSize;
                grid.HeightSampleOffsetZ = cellSize;

                bool[] sampleHits = new bool[sampleW * sampleH];
                float globalMinY = float.MaxValue;
                float globalMaxY = float.MinValue;

                for (int z = 0; z < sampleH; z++)
                {
                    for (int x = 0; x < sampleW; x++)
                    {
                        float worldX = (float)(finalMinX + (x * cellSize));
                        float worldZ = (float)(finalMinZ + (z * cellSize));

                        int bx = (int)((worldX - finalMinX) / bucketSize);
                        int bz = (int)((worldZ - finalMinZ) / bucketSize);

                        float height = 0;
                        bool found = false;

                        if (bx >= 0 && bx < bucketCountX && bz >= 0 && bz < bucketCountZ)
                        {
                            float bestY = float.MinValue;
                            foreach (var tri in buckets[bx, bz])
                            {
                                if (GetHeightOnTriangle(tri, worldX, worldZ, out float y))
                                {
                                    if (y > bestY) bestY = y;
                                    found = true;
                                }
                            }
                            if (found) height = bestY;
                        }

                        grid.HeightSamples.Add(height);
                        sampleHits[(z * sampleW) + x] = found;

                        if (found)
                        {
                            if (height < globalMinY) globalMinY = height;
                            if (height > globalMaxY) globalMaxY = height;
                        }
                    }
                }


                float voidHeight = (globalMinY == float.MaxValue) ? 0 : globalMinY;

                voidHeight += manualYOffset;

                for (int i = 0; i < grid.HeightSamples.Count; i++)
                {
                    if (sampleHits[i])
                    {
                        grid.HeightSamples[i] += manualYOffset;
                    }
                    else
                    {
                        grid.HeightSamples[i] = voidHeight; 
                    }
                }

                float finalMinY = (globalMinY == float.MaxValue) ? 0 : globalMinY + manualYOffset;
                float finalMaxY = (globalMaxY == float.MinValue) ? 0 : globalMaxY + manualYOffset;

                grid.MinBounds = new Vector3(grid.MinBounds.x, finalMinY, grid.MinBounds.z);
                grid.MaxBounds = new Vector3(grid.MaxBounds.x, finalMaxY, grid.MaxBounds.z);

                for (int z = 0; z < cellsZ; z++)
                {
                    for (int x = 0; x < cellsX; x++)
                    {
                        int sampleIdx = (z * sampleW) + x;
                        float cellHeight = grid.HeightSamples[sampleIdx];

                        NavGridCell cell = new NavGridCell
                        {
                            index = (z * cellsX) + x,
                            x = x,
                            z = z,
                            Height = cellHeight
                        };

                        bool isHit = sampleHits[sampleIdx];
                        if (isHit)
                            cell.visionPathingFlags = VisionPathingFlags.Walkable;
                        else
                            cell.visionPathingFlags = VisionPathingFlags.Wall;

                        grid.Cells.Add(cell);
                    }
                }

                return grid;
            });
        }

        private static bool GetHeightOnTriangle(Tri tri, float x, float z, out float y)
        {
            y = 0;
            float det = (tri.P2.z - tri.P3.z) * (tri.P1.x - tri.P3.x) + (tri.P3.x - tri.P2.x) * (tri.P1.z - tri.P3.z);
            if (Math.Abs(det) < 0.00001f) return false;

            float l1 = ((tri.P2.z - tri.P3.z) * (x - tri.P3.x) + (tri.P3.x - tri.P2.x) * (z - tri.P3.z)) / det;
            float l2 = ((tri.P3.z - tri.P1.z) * (x - tri.P3.x) + (tri.P1.x - tri.P3.x) * (z - tri.P3.z)) / det;
            float l3 = 1.0f - l1 - l2;

            if (l1 >= 0 && l1 <= 1 && l2 >= 0 && l2 <= 1 && l3 >= 0 && l3 <= 1)
            {
                y = l1 * tri.P1.y + l2 * tri.P2.y + l3 * tri.P3.y;
                return true;
            }
            return false;
        }
    }
}