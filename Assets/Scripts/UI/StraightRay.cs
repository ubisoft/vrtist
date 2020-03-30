using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StraightRay : MonoBehaviour
{
    [ColorUsage(true, true)]
    public Color defaultColor = new Color(0, 38, 64); // = Color(0.0f, 0.60f, 1.0f) + intensity 6;
    [ColorUsage(true, true)]
    public Color activeColor = new Color(64, 12, 0); // = Color(1.0f, 0.18f, 0.0f) + intensify 6;
    
    private LineRenderer line = null;
    private Transform endPoint = null;
    private Material rayMat = null;
    private Material endMat = null;
    
    private void Start()
    {
        line = GetComponent<LineRenderer>();
        if (line == null)
        {
            Debug.LogWarning("Cannot find the LineRenderer component in the StraightRay");
        }

        endPoint = transform.Find("RayEnd");
        if (endPoint == null)
        {
            Debug.LogWarning("Cannot find the RayEnd object under the StraightRay");
        }

        rayMat = line.material;
        endMat = endPoint.gameObject.GetComponent<MeshRenderer>().material;
    }

    public void SetStartPosition(Vector3 start)
    {
        if (line != null)
        {
            line.SetPosition(0, start);
        }
    }

    public void SetEndPosition(Vector3 end)
    {
        if (line != null)
        {
            line.SetPosition(1, end);
        }

        if (endPoint != null)
        {
            endPoint.position = end;
        }
    }

    public void SetPositions(Vector3 begin, Vector3 end)
    {
        if (line != null)
        {
            line.SetPosition(0, begin);
            line.SetPosition(1, end);
        }

        if (endPoint != null)
        {
            endPoint.position = end;
        }
    }

    public void SetDefaultColor()
    {
        SetColor(defaultColor);
    }

    public void SetActiveColor()
    {
        SetColor(activeColor);
    }

    public void SetColor(Color color)
    {
        if (rayMat != null)
        {
            rayMat.SetColor("_EmissiveColor", color);

            // NOTE: only setting the color does not change the shader...
            //rayMat.SetColor("_EmissiveColorLDR", color);
            //rayMat.SetFloat("_EmissiveIntensity", 50.0f); // 50 nits, 6.0 HDR, 8.0 EV100
        }

        if (endMat != null)
        {
            endMat.SetColor("_EmissiveColor", color);

            //endMat.SetColor("_EmissiveColorLDR", color);
            //endMat.SetFloat("_EmissiveIntensity", 50.0f);
        }
    }
}
