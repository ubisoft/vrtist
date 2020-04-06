using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportUI : MonoBehaviour
{
    [ColorUsage(true, true)]
    public Color activeColor = new Color(0, 38, 64);
    [ColorUsage(true, true)]
    public Color impossibleColor = new Color(64, 0, 0);

    private LineRenderer line = null;
    private Transform plane = null;

    // Start is called before the first frame update
    void Start()
    {
        line = transform.Find("Ray")?.GetComponent<LineRenderer>();
        plane = transform.Find("Target/Plane"); // Target/Plane
    }

    public void SetActiveColor()
    {
        SetEmissiveColor(activeColor);
    }

    public void SetImpossibleColor()
    {
        SetEmissiveColor(impossibleColor);
    }

    private void SetEmissiveColor(Color color)
    {
        if (line != null)
        {
            line.material.SetColor("_EmissiveColor", color);
        }

        if (plane != null)
        {
            plane.GetComponent<MeshRenderer>()?.material.SetColor("_EmissiveColor", color);
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
