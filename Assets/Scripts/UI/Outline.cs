using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Outline : MonoBehaviour
{
    private Material[] materials = null;
    private Transform world;

    // Start is called before the first frame update
    void Start()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if(!renderer)
            renderer = GetComponentInChildren<MeshRenderer>();
        if(renderer)
            materials = renderer.materials;

        world = transform.parent;
        while (world.parent)
        {
            world = world.parent;
        }
        Update();
    }

    // Update is called once per frame
    void Update()
    {
        if (materials == null)
            return;
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i].SetFloat("_ScaleFactor", world.localScale.x);
            //materials[i].SetFloat("_Thickness", transform.parent.localScale.x * 1f);
        }
    }
}
