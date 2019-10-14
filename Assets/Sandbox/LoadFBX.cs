using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assimp;

public class LoadFBX : MonoBehaviour
{
    [SerializeField]
    private int meshCount = 0;

    [SerializeField]
    private GameObject cube = null;

    // Start is called before the first frame update
    void Start()
    {
        Assimp.AssimpContext ctx = new AssimpContext();
        Assimp.Scene aScene = ctx.ImportFile("C:\\Texel_4M.FBX", Assimp.PostProcessSteps.None);
        if (aScene.HasMeshes)
        {
            meshCount = aScene.MeshCount;
            cube.SetActive(true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
