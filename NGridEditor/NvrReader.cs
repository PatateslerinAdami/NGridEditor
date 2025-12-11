using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace LoLNGRIDConverter
{
    public static class NvrReader
    {
        private class NvrMaterial
        {
            public string TextureName; 
            public int Type;
            public uint Flags;
        }

        public static List<WGeoModelData> Load(string filePath)
        {
            var models = new List<WGeoModelData>();

            using (var fs = File.Open(filePath, FileMode.Open, FileAccess.Read))
            using (var br = new BinaryReader(fs))
            {
                byte[] magic = br.ReadBytes(4);
                string magicStr = Encoding.ASCII.GetString(magic);
                if (magicStr != "NVR\0")
                    throw new Exception("Invalid NVR file signature.");

                ushort major = br.ReadUInt16();
                ushort minor = br.ReadUInt16();

                int materialCount = br.ReadInt32();
                int vertexBufferCount = br.ReadInt32();
                int indexBufferCount = br.ReadInt32();
                int meshCount = br.ReadInt32();
                int nodeCount = br.ReadInt32();

                var materials = new List<NvrMaterial>();
                for (int i = 0; i < materialCount; i++)
                {
                    materials.Add(ReadMaterial(br, major, minor));
                }

                var vbPositions = new List<long>();
                var vbSizes = new List<int>();

                for (int i = 0; i < vertexBufferCount; i++)
                {
                    vbPositions.Add(br.BaseStream.Position + 4); 
                    int size = br.ReadInt32();
                    vbSizes.Add(size);
                    br.BaseStream.Seek(size, SeekOrigin.Current);
                }

                var indexBuffers = new List<byte[]>();
                var indexFormats = new List<bool>();
                for (int i = 0; i < indexBufferCount; i++)
                {
                    int size = br.ReadInt32();
                    int format = br.ReadInt32();
                    byte[] data = br.ReadBytes(size);
                    indexBuffers.Add(data);
                    indexFormats.Add(format == 0x65);
                }

                for (int i = 0; i < meshCount; i++)
                {
                    if (major == 8 && minor == 1) br.ReadUInt32();
                    else { br.ReadUInt32(); br.ReadUInt32(); }

                    br.BaseStream.Seek(16, SeekOrigin.Current); 
                    Vector3D minBox = new Vector3D(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    Vector3D maxBox = new Vector3D(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

                    int materialId = br.ReadInt32();

                    int vbId = br.ReadInt32();
                    int startVertex = br.ReadInt32();
                    int vertexCount = br.ReadInt32();
                    int ibId = br.ReadInt32();
                    int startIndex = br.ReadInt32();
                    int indexCount = br.ReadInt32();

                    br.BaseStream.Seek(24, SeekOrigin.Current);

                    if (materialId < 0 || materialId >= materials.Count) continue;
                    if (vertexCount <= 0 || indexCount <= 0) continue;

                    var mat = materials[materialId];
                    long vbStart = vbPositions[vbId];

                    int stride = DetectStride(br, vbStart, startVertex, vertexCount, minBox, maxBox);
                    if (stride == 0) stride = GetVertexStride(mat.Type, mat.Flags);

                    long vertexOffset = vbStart + ((long)startVertex * stride);
                    long savePos = br.BaseStream.Position;

                    br.BaseStream.Seek(vertexOffset, SeekOrigin.Begin);

                    Point3D[] positions = new Point3D[vertexCount];
                    Point[] uvs = new Point[vertexCount];

                    for (int v = 0; v < vertexCount; v++)
                    {
                        float x = br.ReadSingle();
                        float y = br.ReadSingle();
                        float z = br.ReadSingle();
                        positions[v] = new Point3D(x, y, -z);

                        br.BaseStream.Seek(12, SeekOrigin.Current);

                        float u = br.ReadSingle();
                        float vCoord = br.ReadSingle();
                        uvs[v] = new Point(u, vCoord);

                        if (stride > 32) br.BaseStream.Seek(stride - 32, SeekOrigin.Current);
                    }

                    var indices = new Int32Collection(indexCount);
                    byte[] ibData = indexBuffers[ibId];
                    bool is16Bit = indexFormats[ibId];

                    using (var ibStream = new MemoryStream(ibData))
                    using (var ibReader = new BinaryReader(ibStream))
                    {
                        int indexSize = is16Bit ? 2 : 4;
                        ibStream.Seek(startIndex * indexSize, SeekOrigin.Begin);

                        for (int idx = 0; idx < indexCount; idx++)
                        {
                            int val = is16Bit ? ibReader.ReadUInt16() : ibReader.ReadInt32();
                            int normalizedIndex = val - startVertex;
                            if (normalizedIndex >= 0 && normalizedIndex < vertexCount)
                            {
                                indices.Add(normalizedIndex);
                            }
                        }
                    }

                    MeshGeometry3D mesh = new MeshGeometry3D();
                    foreach (var p in positions) mesh.Positions.Add(p);
                    foreach (var uv in uvs) mesh.TextureCoordinates.Add(uv);
                    foreach (var iVal in indices) mesh.TriangleIndices.Add(iVal);
                    mesh.Freeze();

                    models.Add(new WGeoModelData
                    {
                        Mesh = mesh,
                        TextureName = Path.GetFileName(mat.TextureName)
                    });

                    br.BaseStream.Seek(savePos, SeekOrigin.Begin);
                }
            }
            return models;
        }

        private static int DetectStride(BinaryReader br, long vbStart, int startVertex, int vertexCount, Vector3D min, Vector3D max)
        {
            if (min.X == 0 && max.X == 0 && min.Y == 0 && max.Y == 0) return 0;
            double epsilon = 5.0;
            Rect3D box = new Rect3D(
                Math.Min(min.X, max.X) - epsilon,
                Math.Min(min.Y, max.Y) - epsilon,
                Math.Min(min.Z, max.Z) - epsilon,
                Math.Abs(max.X - min.X) + (epsilon * 2),
                Math.Abs(max.Y - min.Y) + (epsilon * 2),
                Math.Abs(max.Z - min.Z) + (epsilon * 2)
            );

            int[] candidates = new int[] { 44, 40, 36, 48, 52, 32 };
            long originalPos = br.BaseStream.Position;

            foreach (int s in candidates)
            {
                bool valid = true;
                int[] checkIndices = new int[] { 0, 1, vertexCount / 2, vertexCount - 1 };
                foreach (int i in checkIndices)
                {
                    if (i >= vertexCount) continue;
                    long offset = vbStart + ((long)(startVertex + i) * s);
                    if (offset + 12 > br.BaseStream.Length) { valid = false; break; }

                    br.BaseStream.Seek(offset, SeekOrigin.Begin);
                    float x = br.ReadSingle();
                    float y = br.ReadSingle();
                    float z = br.ReadSingle();

                    if (!box.Contains(new Point3D(x, y, z))) { valid = false; break; }
                }
                if (valid) { br.BaseStream.Seek(originalPos, SeekOrigin.Begin); return s; }
            }
            br.BaseStream.Seek(originalPos, SeekOrigin.Begin);
            return 0;
        }

        private static NvrMaterial ReadMaterial(BinaryReader br, ushort major, ushort minor)
        {
            var mat = new NvrMaterial();
            if (major == 8 && minor == 1)
            {
                string materialName = ReadFixedString(br, 260); 
                mat.Type = br.ReadInt32();

                br.BaseStream.Seek(16, SeekOrigin.Current); 
                string diffuseTex = ReadFixedString(br, 260); 
                br.BaseStream.Seek(64, SeekOrigin.Current); 

                mat.TextureName = !string.IsNullOrEmpty(diffuseTex) ? diffuseTex : materialName;

                for (int i = 1; i < 8; i++) br.BaseStream.Seek(340, SeekOrigin.Current);
            }
            else
            {
                string materialName = ReadFixedString(br, 260);
                mat.Type = br.ReadInt32();
                mat.Flags = br.ReadUInt32();

                mat.TextureName = null;

                for (int i = 0; i < 8; i++)
                {
                    br.BaseStream.Seek(16, SeekOrigin.Current); 
                    string texName = ReadFixedString(br, 260);
                    br.BaseStream.Seek(64, SeekOrigin.Current); 

                    if (string.IsNullOrEmpty(mat.TextureName) && !string.IsNullOrEmpty(texName))
                    {
                        mat.TextureName = texName;
                    }
                }

                if (string.IsNullOrEmpty(mat.TextureName))
                {
                    mat.TextureName = materialName;
                }
            }
            return mat;
        }

        private static int GetVertexStride(int type, uint flags)
        {
            if (type == 3) return 44;
            if ((flags & 16) != 0) return 40;
            return 36;
        }

        private static string ReadFixedString(BinaryReader br, int length)
        {
            byte[] bytes = br.ReadBytes(length);
            int nullIndex = Array.IndexOf(bytes, (byte)0);
            if (nullIndex < 0) nullIndex = length;
            return Encoding.ASCII.GetString(bytes, 0, nullIndex);
        }
    }
}