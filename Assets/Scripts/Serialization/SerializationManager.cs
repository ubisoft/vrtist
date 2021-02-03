using System.IO;

using UnityEngine;

namespace VRtist.Serialization
{
    public static class SerializationManager
    {
        public static void Save(string path, IBlob data, bool deleteFolder = false)
        {
            DirectoryInfo folder = Directory.GetParent(path);
            if (!folder.Exists)
            {
                folder.Create();
            }
            else if (deleteFolder)
            {
                folder.Delete(true);
                folder.Create();
            }

            File.WriteAllBytes(path, data.ToBytes());
        }

        public static void Load(string path, IBlob data)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"Cannot load {path}: no such file.");
                return;
            }

            int index = 0;
            data.FromBytes(File.ReadAllBytes(path), ref index);
        }

        //public static BinaryFormatter GetBinaryFormatter()
        //{
        //    if (formatter != null)
        //    {
        //        return formatter;
        //    }

        //    formatter = new BinaryFormatter();

        //    SurrogateSelector selector = new SurrogateSelector();

        //    Vector2Surrogate vector2Surrogate = new Vector2Surrogate();
        //    Vector3Surrogate vector3Surrogate = new Vector3Surrogate();
        //    Vector4Surrogate vector4Surrogate = new Vector4Surrogate();
        //    QuaternionSurrogate quaternionSurrogate = new QuaternionSurrogate();
        //    ColorSurrogate colorSurrogate = new ColorSurrogate();

        //    Vector3ArraySurrogate v3as = new Vector3ArraySurrogate();

        //    selector.AddSurrogate(typeof(Vector3[]), new StreamingContext(StreamingContextStates.All), v3as);
        //    selector.AddSurrogate(typeof(Vector2), new StreamingContext(StreamingContextStates.All), vector2Surrogate);
        //    selector.AddSurrogate(typeof(Vector3), new StreamingContext(StreamingContextStates.All), vector3Surrogate);
        //    selector.AddSurrogate(typeof(Vector4), new StreamingContext(StreamingContextStates.All), vector4Surrogate);
        //    selector.AddSurrogate(typeof(Quaternion), new StreamingContext(StreamingContextStates.All), quaternionSurrogate);
        //    selector.AddSurrogate(typeof(Color), new StreamingContext(StreamingContextStates.All), colorSurrogate);


        //    formatter.SurrogateSelector = selector;

        //    return formatter;
        //}
    }
}
