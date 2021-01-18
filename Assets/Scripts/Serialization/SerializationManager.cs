using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace VRtist.Serialization
{
    public class SerializationManager
    {
        public static bool Save(string path, object data)
        {
            BinaryFormatter formatter = GetBinaryFormatter();

            DirectoryInfo folder = Directory.GetParent(path);
            if (!folder.Exists)
            {
                folder.Create();
            }

            using (FileStream file = File.Create(path))
            {
                try
                {
                    formatter.Serialize(file, data);
                }
                catch (SerializationException e)
                {
                    Debug.LogError("Failed to serialize: " + e.Message);
                    return false;
                }
            }

            return true;
        }

        public static object Load(string path)
        {
            if (!File.Exists(path)) { return null; }

            BinaryFormatter formatter = GetBinaryFormatter();

            using (FileStream file = File.OpenRead(path))
            {
                object save = formatter.Deserialize(file);
                return save;
            }
        }

        public static BinaryFormatter GetBinaryFormatter()
        {
            BinaryFormatter formatter = new BinaryFormatter();

            SurrogateSelector selector = new SurrogateSelector();

            Vector2Surrogate vector2Surrogate = new Vector2Surrogate();
            Vector3Surrogate vector3Surrogate = new Vector3Surrogate();
            Vector4Surrogate vector4Surrogate = new Vector4Surrogate();
            QuaternionSurrogate quaternionSurrogate = new QuaternionSurrogate();
            ColorSurrogate colorSurrogate = new ColorSurrogate();
            MeshSurrogate meshSurrogate = new MeshSurrogate();

            selector.AddSurrogate(typeof(Vector2), new StreamingContext(StreamingContextStates.All), vector2Surrogate);
            selector.AddSurrogate(typeof(Vector3), new StreamingContext(StreamingContextStates.All), vector3Surrogate);
            selector.AddSurrogate(typeof(Vector4), new StreamingContext(StreamingContextStates.All), vector4Surrogate);
            selector.AddSurrogate(typeof(Quaternion), new StreamingContext(StreamingContextStates.All), quaternionSurrogate);
            selector.AddSurrogate(typeof(Color), new StreamingContext(StreamingContextStates.All), colorSurrogate);
            selector.AddSurrogate(typeof(Mesh), new StreamingContext(StreamingContextStates.All), meshSurrogate);

            formatter.SurrogateSelector = selector;

            return formatter;
        }
    }
}
