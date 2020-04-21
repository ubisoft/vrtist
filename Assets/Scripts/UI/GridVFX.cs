using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class GridVFX : MonoBehaviour
{
    private VisualEffect vfx = null;
    private int radiusID = -1;
    private int stepID = -1;
    private int pointSizeID = -1;
    private int snapXID = -1;
    private int snapYID = -1;
    private int snapZID = -1;

    private void Awake()
    {
        vfx = GetComponent<VisualEffect>();
        radiusID = Shader.PropertyToID("Radius");
        stepID = Shader.PropertyToID("Step");
        pointSizeID = Shader.PropertyToID("PointSize");
        snapXID = Shader.PropertyToID("SnapX");
        snapYID = Shader.PropertyToID("SnapY");
        snapZID = Shader.PropertyToID("SnapZ");
    }

    void Start()
    {
        if (vfx == null)
        {
            vfx = GetComponent<VisualEffect>();
        }

        vfx.Play();
    }

    private void OnEnable()
    {
        
    }

    private void OnDisable()
    {
        vfx.Stop();
    }

    public void SetRadius(float r)
    {
        if (vfx != null)
        {
            vfx.SetFloat(radiusID, r);
        }
    }

    public void SetStepSize(float s)
    {
        if (vfx != null)
        {
            vfx.SetFloat(stepID, s);
        }
    }

    public void SetPointSize(float s)
    {
        if (vfx != null)
        {
            vfx.SetFloat(pointSizeID, s);
        }
    }

    public void SetAxis(bool x, bool y, bool z)
    {
        if (vfx != null)
        {
            vfx.SetBool(snapXID, x);
            vfx.SetBool(snapYID, y);
            vfx.SetBool(snapZID, z);
        }
    }
}
