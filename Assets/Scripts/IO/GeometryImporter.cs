using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class GeometryImporter : MonoBehaviour
    {
        [SerializeField] private Transform root;
        public GameObject lightPrefab = null;
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
        public bool createLight = false;

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

            if(createLight)
            {
                createLight = false;
                GameObject light = Utils.CreateInstance(lightPrefab, Utils.FindGameObject("Lights").transform);
                new CommandAddGameObject(light).Submit();
            }

            if (load)
            {
                load = false;
                importer.Import(filename, root);
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