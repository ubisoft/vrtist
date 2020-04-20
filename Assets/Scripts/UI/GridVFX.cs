using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class GridVFX : MonoBehaviour
{
    private VisualEffect vfx = null;
    private int stepID = -1;
    private int snapXID = -1;
    private int snapYID = -1;
    private int snapZID = -1;

    private void Awake()
    {
        vfx = GetComponent<VisualEffect>();
        stepID = Shader.PropertyToID("Step");
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

    public void SetStepSize(float s)
    {
        if (vfx != null)
        {
            vfx.SetFloat(stepID, s);
        }
    }

    public void SetAxis(bool x, bool y, bool z)
    {
        if (vfx != null)
        {
            vfx.SetBool(snapXID, x);
            vfx.SetBool(snapYID, y);
            vfx.SetBool(snapZID, z);
            //vfx.SetBool("SnapX", x);
            //vfx.SetBool("SnapY", y);
            //vfx.SetBool("SnapZ", z);
        }
    }
}
