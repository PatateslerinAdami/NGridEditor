using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace LoLNGRIDConverter
{
    public class WGeoModelData
    {
        public MeshGeometry3D Mesh { get; set; }
        public string TextureName { get; set; }
    }

    public static class WGeoReader
    {
        public static List<WGeoModelData> Load(string filePath)
        {
            var models = new List<WGeoModelData>();

            using (var fs = File.Open(filePath, FileMode.Open, FileAccess.Read))
            using (var br = new BinaryReader(fs))
            {
                byte[] magic = br.ReadBytes(4);
                if (Encoding.ASCII.GetString(magic) != "WGEO")
                    throw new Exception("Invalid WGEO file signature.");

                uint version = br.ReadUInt32();
                int modelCount = br.ReadInt32();
                uint faceCount = br.ReadUInt32();

                for (int i = 0; i < modelCount; i++)
                {
                    models.Add(ReadMesh(br));
                }
            }
            return models;
        }

        private static WGeoModelData ReadMesh(BinaryReader br)
        {
            string texturePath = ReadFixedString(br, 260);
            string materialPath = ReadFixedString(br, 64);

            br.ReadBytes(16 + 24);

            int vertexCount = br.ReadInt32();
            int indexCount = br.ReadInt32();

            Point3D[] positions = new Point3D[vertexCount];
            Point[] uvs = new Point[vertexCount];

            for (int i = 0; i < vertexCount; i++)
            {
                float x = br.ReadSingle();
                float y = br.ReadSingle();
                float z = br.ReadSingle();
                float u = br.ReadSingle();
                float v = br.ReadSingle();

                positions[i] = new Point3D(x, y, -z);
                uvs[i] = new Point(u, v);
            }
            bool is32Bit = indexCount > 65535;

            Int32Collection indices = new Int32Collection(indexCount);
            for (int i = 0; i < indexCount; i++)
            {
                if (is32Bit) indices.Add((int)br.ReadUInt32());
                else indices.Add((int)br.ReadUInt16());
            }

            MeshGeometry3D mesh = new MeshGeometry3D();
            foreach (var p in positions) mesh.Positions.Add(p);
            foreach (var uv in uvs) mesh.TextureCoordinates.Add(uv);
            foreach (var idx in indices) mesh.TriangleIndices.Add(idx);

            string cleanTexName = Path.GetFileName(texturePath);

            return new WGeoModelData { Mesh = mesh, TextureName = cleanTexName };
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