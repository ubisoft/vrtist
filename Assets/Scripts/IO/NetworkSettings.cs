using System;
using System.IO;
using UnityEngine;

namespace VRtist
{
    [CreateAssetMenu(fileName = "NetworkSettings", menuName = "VRtist/NetworkSettings")]
    public class NetworkSettings : ScriptableObject
    {
        public string host = "127.0.0.1";
        public int port = 12800;
        public string room = "Local";
        public string master;
        public string userName;
        public Color userColor;

        private string GetJsonFilename()
        {
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string filename = appdata + @"\VRtistNetworkSettings.json";
            return filename;
        }

        public void Load()
        {
            LoadJson(GetJsonFilename());
        }

        public void Save()
        {
            SaveToJson(GetJsonFilename());
        }

        public void SaveToJson(string filename)
        {
            string json = JsonUtility.ToJson(this, true);
            File.WriteAllText(filename, json);
        }

        public void LoadJson(string filename)
        {
            if (File.Exists(filename))
            {
                string json = File.ReadAllText(filename);
                JsonUtility.FromJsonOverwrite(json, this);
            }
        }
    }
}
