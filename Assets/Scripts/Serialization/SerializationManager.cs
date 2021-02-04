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
    }
}
