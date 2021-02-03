using System;
using System.Collections.Generic;

using UnityEngine;

namespace VRtist.Serialization
{
    public static class Converter
    {
        // Converts string array to byte buffer
        public static byte[] StringsToBytes(string[] values, bool storeSize = true)
        {
            int size = sizeof(int);
            for (int i = 0; i < values.Length; i++)
            {
                byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(values[i]);
                size += sizeof(int) + utf8.Length;
            }


            byte[] bytes = new byte[size];
            int index = 0;
            if (storeSize)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(values.Length), 0, bytes, 0, sizeof(int));
                index += sizeof(int);
            }
            for (int i = 0; i < values.Length; i++)
            {
                string value = values[i];
                byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(value);
                Buffer.BlockCopy(BitConverter.GetBytes(utf8.Length), 0, bytes, index, sizeof(int));
                Buffer.BlockCopy(utf8, 0, bytes, index + sizeof(int), value.Length);
                index += sizeof(int) + value.Length;
            }
            return bytes;
        }

        // Converts string to byte buffer
        public static byte[] StringToBytes(string value)
        {
            if (null == value)
            {
                return IntToBytes(0);
            }

            byte[] bytes = new byte[sizeof(int) + value.Length];
            byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(value);
            Buffer.BlockCopy(BitConverter.GetBytes(utf8.Length), 0, bytes, 0, sizeof(int));
            Buffer.BlockCopy(utf8, 0, bytes, sizeof(int), value.Length);
            return bytes;
        }

        public static string ConvertToString(byte[] data)
        {
            return System.Text.Encoding.UTF8.GetString(data, 0, data.Length);
        }

        public static string GetString(byte[] data, ref int bufferIndex)
        {
            int strLength = GetInt(data, ref bufferIndex);
            if (strLength == 0) { return ""; }

            string str = System.Text.Encoding.UTF8.GetString(data, bufferIndex, strLength);
            bufferIndex = bufferIndex + strLength;
            return str;
        }

        public static List<string> GetStrings(byte[] data, ref int index)
        {
            int count = (int)BitConverter.ToUInt32(data, index);
            index += 4;
            List<string> strings = new List<string>();
            for (int i = 0; i < count; ++i)
            {
                strings.Add(GetString(data, ref index));
            }
            return strings;
        }

        // Converts triangle indice array to byte buffer
        public static byte[] TriangleIndicesToBytes(int[] vectors)
        {
            byte[] bytes = new byte[sizeof(int) * vectors.Length + sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(vectors.Length / 3), 0, bytes, 0, sizeof(int));
            int index = sizeof(int);
            for (int i = 0; i < vectors.Length; i++)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(vectors[i]), 0, bytes, index, sizeof(int));
                index += sizeof(int);
            }
            return bytes;
        }

        // Converts byte buffer to Color
        public static Color GetColor(byte[] data, ref int currentIndex)
        {
            float[] buffer = new float[4];
            int size = 4 * sizeof(float);
            Buffer.BlockCopy(data, currentIndex, buffer, 0, size);
            currentIndex += size;
            return new Color(buffer[0], buffer[1], buffer[2], buffer[3]);
        }

        // Converts Color to byte buffer
        public static byte[] ColorToBytes(Color color)
        {
            byte[] bytes = new byte[4 * sizeof(float)];

            Buffer.BlockCopy(BitConverter.GetBytes(color.r), 0, bytes, 0, sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(color.g), 0, bytes, sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(color.b), 0, bytes, 2 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(color.a), 0, bytes, 3 * sizeof(float), sizeof(float));
            return bytes;
        }

        // Convert byte buffer to Vector3
        public static Vector3 GetVector3(byte[] data, ref int currentIndex)
        {
            float[] buffer = new float[3];
            int size = 3 * sizeof(float);
            Buffer.BlockCopy(data, currentIndex, buffer, 0, size);
            currentIndex += size;
            return new Vector3(buffer[0], buffer[1], buffer[2]);
        }

        // Convert Vector3 to byte buffer
        public static byte[] Vector3ToBytes(Vector3 vector)
        {
            byte[] bytes = new byte[3 * sizeof(float)];

            Buffer.BlockCopy(BitConverter.GetBytes(vector.x), 0, bytes, 0, sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(vector.y), 0, bytes, sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(vector.z), 0, bytes, 2 * sizeof(float), sizeof(float));
            return bytes;
        }

        // Converts Vector3 array to byte buffer
        public static byte[] Vectors3ToBytes(Vector3[] vectors)
        {
            byte[] bytes = new byte[3 * sizeof(float) * vectors.Length + sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(vectors.Length), 0, bytes, 0, sizeof(int));
            int index = sizeof(int);
            for (int i = 0; i < vectors.Length; i++)
            {
                Vector3 vector = vectors[i];
                Buffer.BlockCopy(BitConverter.GetBytes(vector.x), 0, bytes, index + 0, sizeof(float));
                Buffer.BlockCopy(BitConverter.GetBytes(vector.y), 0, bytes, index + sizeof(float), sizeof(float));
                Buffer.BlockCopy(BitConverter.GetBytes(vector.z), 0, bytes, index + 2 * sizeof(float), sizeof(float));
                index += 3 * sizeof(float);
            }
            return bytes;
        }

        // Convert byte buffer to Vector3
        public static Vector3[] GetVectors3(byte[] data, ref int currentIndex)
        {
            int count = (int)BitConverter.ToUInt32(data, currentIndex);
            currentIndex += sizeof(int);
            Vector3[] vectors = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float[] buffer = new float[3];
                int size = 3 * sizeof(float);
                Buffer.BlockCopy(data, currentIndex, buffer, 0, size);
                currentIndex += size;
                vectors[i] = new Vector3(buffer[0], buffer[1], buffer[2]);
            }
            return vectors;
        }

        // Convert byte buffer to Vector4
        public static Vector4 GetVector4(byte[] data, ref int currentIndex)
        {
            float[] buffer = new float[4];
            int size = 4 * sizeof(float);
            Buffer.BlockCopy(data, currentIndex, buffer, 0, size);
            currentIndex += size;
            return new Vector4(buffer[0], buffer[1], buffer[2], buffer[3]);
        }

        public static Vector4[] GetVectors4(byte[] data, ref int currentIndex)
        {
            int count = (int)BitConverter.ToUInt32(data, currentIndex);
            currentIndex += sizeof(int);
            Vector4[] vectors = new Vector4[count];
            for (int i = 0; i < count; i++)
            {
                float[] buffer = new float[4];
                int size = 4 * sizeof(float);
                Buffer.BlockCopy(data, currentIndex, buffer, 0, size);
                currentIndex += size;
                vectors[i] = new Vector4(buffer[0], buffer[1], buffer[2], buffer[3]);
            }
            return vectors;
        }

        // Convert Vector4 to byte buffer
        public static byte[] Vector4ToBytes(Vector4 vector)
        {
            byte[] bytes = new byte[4 * sizeof(float)];

            Buffer.BlockCopy(BitConverter.GetBytes(vector.x), 0, bytes, 0 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(vector.y), 0, bytes, 1 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(vector.z), 0, bytes, 2 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(vector.w), 0, bytes, 3 * sizeof(float), sizeof(float));
            return bytes;
        }

        // Convert byte buffer to Vector2
        public static Vector2 GetVector2(byte[] data, ref int currentIndex)
        {
            float[] buffer = new float[2];
            int size = 2 * sizeof(float);
            Buffer.BlockCopy(data, currentIndex, buffer, 0, size);
            currentIndex += size;
            return new Vector2(buffer[0], buffer[1]);
        }

        public static Vector2[] GetVectors2(byte[] data, ref int currentIndex)
        {
            int count = (int)BitConverter.ToUInt32(data, currentIndex);
            currentIndex += sizeof(int);
            Vector2[] vectors = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                float[] buffer = new float[2];
                int size = 2 * sizeof(float);
                Buffer.BlockCopy(data, currentIndex, buffer, 0, size);
                currentIndex += size;
                vectors[i] = new Vector2(buffer[0], buffer[1]);
            }
            return vectors;
        }

        // Convert Vector2 to byte buffer
        public static byte[] Vector2ToBytes(Vector2 vector)
        {
            byte[] bytes = new byte[2 * sizeof(float)];

            Buffer.BlockCopy(BitConverter.GetBytes(vector.x), 0, bytes, 0, sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(vector.y), 0, bytes, sizeof(float), sizeof(float));
            return bytes;
        }

        // Convert Vector2 array to byte buffer
        public static byte[] Vectors2ToBytes(Vector2[] vectors)
        {
            byte[] bytes = new byte[2 * sizeof(float) * vectors.Length + sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(vectors.Length), 0, bytes, 0, sizeof(int));
            int index = sizeof(int);
            for (int i = 0; i < vectors.Length; i++)
            {
                Vector2 vector = vectors[i];
                Buffer.BlockCopy(BitConverter.GetBytes(vector.x), 0, bytes, index + 0, sizeof(float));
                Buffer.BlockCopy(BitConverter.GetBytes(vector.y), 0, bytes, index + sizeof(float), sizeof(float));
                index += 2 * sizeof(float);
            }
            return bytes;
        }

        // Convert byte buffer to Matrix4x4
        public static Matrix4x4 GetMatrix(byte[] data, ref int index)
        {
            float[] matrixBuffer = new float[16];

            int size = 4 * 4 * sizeof(float);
            Buffer.BlockCopy(data, index, matrixBuffer, 0, size);
            Matrix4x4 m = new Matrix4x4(new Vector4(matrixBuffer[0], matrixBuffer[1], matrixBuffer[2], matrixBuffer[3]),
                                        new Vector4(matrixBuffer[4], matrixBuffer[5], matrixBuffer[6], matrixBuffer[7]),
                                        new Vector4(matrixBuffer[8], matrixBuffer[9], matrixBuffer[10], matrixBuffer[11]),
                                        new Vector4(matrixBuffer[12], matrixBuffer[13], matrixBuffer[14], matrixBuffer[15])
                                        );
            index += size;
            return m;
        }

        // Convert Matrix4x4 to byte buffer
        public static byte[] MatrixToBytes(Matrix4x4 matrix)
        {
            byte[] column0Buffer = Vector4ToBytes(matrix.GetColumn(0));
            byte[] column1Buffer = Vector4ToBytes(matrix.GetColumn(1));
            byte[] column2Buffer = Vector4ToBytes(matrix.GetColumn(2));
            byte[] column3Buffer = Vector4ToBytes(matrix.GetColumn(3));
            List<byte[]> buffers = new List<byte[]> { column0Buffer, column1Buffer, column2Buffer, column3Buffer };
            return ConcatenateBuffers(buffers);
        }

        // Convert byte buffer to Quaternion
        public static Quaternion GetQuaternion(byte[] data, ref int currentIndex)
        {
            float[] buffer = new float[4];
            int size = 4 * sizeof(float);
            Buffer.BlockCopy(data, currentIndex, buffer, 0, size);
            currentIndex += size;
            return new Quaternion(buffer[0], buffer[1], buffer[2], buffer[3]);
        }

        // Convert Quaternion to byte buffer
        public static byte[] QuaternionToBytes(Quaternion quaternion)
        {
            byte[] bytes = new byte[4 * sizeof(float)];

            Buffer.BlockCopy(BitConverter.GetBytes(quaternion.x), 0, bytes, 0, sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(quaternion.y), 0, bytes, 1 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(quaternion.z), 0, bytes, 2 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(quaternion.w), 0, bytes, 3 * sizeof(float), sizeof(float));
            return bytes;
        }

        // convert byte buffer to bool
        public static bool GetBool(byte[] data, ref int currentIndex)
        {
            int[] buffer = new int[1];
            Buffer.BlockCopy(data, currentIndex, buffer, 0, sizeof(int));
            currentIndex += sizeof(int);
            return buffer[0] == 1;
        }

        // convert bool to byte buffer
        public static byte[] BoolToBytes(bool value)
        {
            byte[] bytes = new byte[sizeof(int)];
            int v = value ? 1 : 0;
            Buffer.BlockCopy(BitConverter.GetBytes(v), 0, bytes, 0, sizeof(int));
            return bytes;
        }

        // convert byte buffer to int
        public static int GetInt(byte[] data, ref int currentIndex)
        {
            int[] buffer = new int[1];
            Buffer.BlockCopy(data, currentIndex, buffer, 0, sizeof(int));
            currentIndex += sizeof(int);
            return buffer[0];
        }

        // convert byte buffer to ints array
        public static int[] GetInts(byte[] data, ref int currentIndex)
        {
            int count = (int)BitConverter.ToUInt32(data, currentIndex);
            currentIndex += sizeof(int);
            int[] buffer = new int[count];
            Buffer.BlockCopy(data, currentIndex, buffer, 0, count * sizeof(int));
            currentIndex += count * sizeof(int);
            return buffer;
        }

        // convert int to byte buffer
        public static byte[] IntToBytes(int value)
        {
            byte[] bytes = new byte[sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(value), 0, bytes, 0, sizeof(int));
            return bytes;
        }

        public static byte[] IntsToBytes(int[] values)
        {
            byte[] bytes = new byte[sizeof(int) * values.Length + sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(values.Length), 0, bytes, 0, sizeof(int));
            int index = sizeof(int);
            for (int i = 0; i < values.Length; i++)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(values[i]), 0, bytes, index, sizeof(int));
                index += sizeof(int);
            }
            return bytes;
        }

        // convert byte buffer to float
        public static float GetFloat(byte[] data, ref int currentIndex)
        {
            float[] buffer = new float[1];
            Buffer.BlockCopy(data, currentIndex, buffer, 0, sizeof(float));
            currentIndex += sizeof(float);
            return buffer[0];
        }

        // convert byte buffer to floats array
        public static float[] GetFloats(byte[] data, ref int currentIndex)
        {
            int count = (int)BitConverter.ToUInt32(data, currentIndex);
            currentIndex += sizeof(int);
            float[] buffer = new float[count];
            Buffer.BlockCopy(data, currentIndex, buffer, 0, count * sizeof(float));
            currentIndex += count * sizeof(float);
            return buffer;
        }

        // convert float to byte buffer
        public static byte[] FloatToBytes(float value)
        {
            byte[] bytes = new byte[sizeof(float)];
            Buffer.BlockCopy(BitConverter.GetBytes(value), 0, bytes, 0, sizeof(float));
            return bytes;
        }

        public static byte[] FloatsToBytes(float[] values)
        {
            byte[] bytes = new byte[sizeof(float) * values.Length + sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(values.Length), 0, bytes, 0, sizeof(int));
            int index = sizeof(int);
            for (int i = 0; i < values.Length; i++)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(values[i]), 0, bytes, index, sizeof(float));
                index += sizeof(float);
            }
            return bytes;
        }

        // concatenate byte buffers
        public static byte[] ConcatenateBuffers(List<byte[]> buffers)
        {
            int totalLength = 0;
            foreach (byte[] buffer in buffers)
            {
                totalLength += buffer.Length;
            }
            byte[] resultBuffer = new byte[totalLength];
            int index = 0;
            foreach (byte[] buffer in buffers)
            {
                int size = buffer.Length;
                Buffer.BlockCopy(buffer, 0, resultBuffer, index, size);
                index += size;
            }
            return resultBuffer;
        }
    }
}
