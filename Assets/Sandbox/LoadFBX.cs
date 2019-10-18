using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace VRtist
{
    public class LoadFBX : MonoBehaviour
    {
        // Start is called before the first frame update
        public AssimpIO importer;
        void Start()
        {
            importer.importEventTask += OnGeometryLoaded;
            Import(@"D:\FBX\Batman\batman.obj");
            Import(@"D:\unity\VRSamples\Assets\Models\Cabane\cabane.fbx");
            Import(@"D:\FBX\bidule\faces.fbx");
            Import(@"D:\FBX\fireTruck.fbx");
        }

        void Import(string fileName)
        {
            GameObject go = new GameObject();
            go.transform.parent = gameObject.transform;
            //go.SetActive(false);
            importer.Import(fileName, go);
        }

        static void OnGeometryLoaded(object sender, AssimpIO.ImportTaskEventArgs e)
        {
            e.Root.SetActive(true);
        }

        // Update is called once per frame
        void Update()
        {
        }        
    }

}