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

        public string obj = @"D:\test.obj";
        public bool writeOBJ = false;

        void Update()
        {
            progress = importer.Progress;

            if (load)
            {
                load = false;
                importer.Import(filename, root, IOMetaData.Type.Geometry);
            }
            if (undo == true)
            {
                undo = false;
                CommandManager.Undo();
            }
            if (redo == true)
            {
                redo = false;
                CommandManager.Redo();
            }

            if (serialize == true)
            {
                serialize = false;
                SceneSerializer.Save(json);
            }
            if (deserialize == true)
            {
                deserialize = false;
                SceneSerializer sceneSerializer = SceneSerializer.Load(json);
            }

            if(writeOBJ == true)
            {
                writeOBJ = false;
                foreach (var selectedItem in Selection.selection)
                {
                    OBJExporter.Export(obj, selectedItem.Value);
                    break;
                }
            }
        }
        //===========================

    }
}