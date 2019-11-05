using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace VRtist
{
    public class LoadFBX : MonoBehaviour
    {
        public AssimpIO importer;

        public string filename = @"D:\unity\VRSamples\Assets\Models\Cabane\cabane.fbx";
        public bool load = false;
        public bool undo = false;
        public bool redo = false;

        public float progress = 0f;

        void Start()
        {
            importer.importEventTask += OnGeometryLoaded;            
        }


        static void OnGeometryLoaded(object sender, AssimpIO.ImportTaskEventArgs e)
        {
            new CommandAddGameObject(e.Root.gameObject).Submit(); 
        }

        // Update is called once per frame
        void Update()
        {
            progress = importer.Progress;

            if (load)
            {
                importer.Import(filename, gameObject.transform, IOMetaData.Type.Geometry);
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
        }
    }

}