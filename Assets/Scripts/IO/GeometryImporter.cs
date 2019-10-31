using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class GeometryImporter : MonoBehaviour
    {
        [SerializeField] private Transform root;
        private AssimpIO importer = null;

        void Start()
        {
            importer = GetComponent<AssimpIO>();
            importer.importEventTask += OnGeometryLoaded;
        }

        static void OnGeometryLoaded(object sender, AssimpIO.ImportTaskEventArgs e)
        {
            if (e.Error)
                return;

            new CommandAddGameObject(e.Root.gameObject).Submit();
        }


        // This is for debug purpose
        //===========================
        public string filename = @"D:\unity\VRtist\Build\cabane.fbx";
        public bool load = false;
        public bool undo = false;
        public bool redo = false;
        public float progress = 0f;

        public string json = @"D:\test.json";
        public bool serialize = false;
        public bool deserialize = false;

        void Update()
        {
            progress = importer.Progress;

            if (load)
            {
                importer.Import(filename, root);
                load = false;
            }
            if (undo == true)
            {
                CommandManager.Undo();
                undo = false;
            }
            if (redo == true)
            {
                CommandManager.Redo();
                redo = false;
            }

            if (serialize == true)
            {
                SceneSerializer sceneSerializer = new SceneSerializer();
                sceneSerializer.Save(json);
                serialize = false;
            }
            if (deserialize == true)
            {
                SceneSerializer sceneSerializer = SceneSerializer.Load(json);
            }
        }
        //===========================

    }
}