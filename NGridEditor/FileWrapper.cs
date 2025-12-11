using System;
using System.IO;
using System.Text;

namespace LoLNGRIDConverter
{
    public class FileWrapper
    {
        private FileStream fileStream;
        private byte[] buffer = new byte[8];

        public FileWrapper(string filePath, bool writeMode = false)
        {
            if (writeMode)
                fileStream = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            else
                fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public void Close() => fileStream?.Close();

        public byte ReadByte() => (byte)fileStream.ReadByte();

        public short ReadShort()
        {
            fileStream.Read(buffer, 0, 2);
            return (short)(buffer[0] | (buffer[1] << 8));
        }

        public int ReadInt()
        {
            fileStream.Read(buffer, 0, 4);
            return buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24);
        }

        public float ReadFloat()
        {
            fileStream.Read(buffer, 0, 4);
            if (!BitConverter.IsLittleEndian) Array.Reverse(buffer, 0, 4);
            return BitConverter.ToSingle(buffer, 0);
        }

        public Vector3 ReadVector3() => new Vector3(ReadFloat(), ReadFloat(), ReadFloat());

        public void WriteByte(byte b) => fileStream.WriteByte(b);

        public void WriteShort(short s)
        {
            buffer[0] = (byte)(s & 0xFF);
            buffer[1] = (byte)((s >> 8) & 0xFF);
            fileStream.Write(buffer, 0, 2);
        }

        public void WriteInt(int i)
        {
            buffer[0] = (byte)(i & 0xFF);
            buffer[1] = (byte)((i >> 8) & 0xFF);
            buffer[2] = (byte)((i >> 16) & 0xFF);
            buffer[3] = (byte)((i >> 24) & 0xFF);
            fileStream.Write(buffer, 0, 4);
        }

        public void WriteFloat(float f)
        {
            byte[] bytes = BitConverter.GetBytes(f);
            if (!BitConverter.IsLittleEndian) Array.Reverse(bytes);
            fileStream.Write(bytes, 0, 4);
        }

        public void WriteVector3(Vector3 v)
        {
            WriteFloat(v.x); WriteFloat(v.y); WriteFloat(v.z);
        }

        public void WriteZeros(int count)
        {
            byte[] zeros = new byte[count];
            fileStream.Write(zeros, 0, count);
        }
    }
}