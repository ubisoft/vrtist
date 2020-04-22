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
    private int rotationAxisXID = -1;
    private int rotationAxisYID = -1;
    private int rotationAxisZID = -1;
    private int anglesID = -1;

    private void Awake()
    {
        vfx = GetComponent<VisualEffect>();
        radiusID = Shader.PropertyToID("Radius");
        stepID = Shader.PropertyToID("Step");
        pointSizeID = Shader.PropertyToID("PointSize");
        snapXID = Shader.PropertyToID("SnapX");
        snapYID = Shader.PropertyToID("SnapY");
        snapZID = Shader.PropertyToID("SnapZ");
        rotationAxisXID = Shader.PropertyToID("RotationAxisX");
        rotationAxisYID = Shader.PropertyToID("RotationAxisY");
        rotationAxisZID = Shader.PropertyToID("RotationAxisZ");
        anglesID = Shader.PropertyToID("Angles");

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

    public void SetAngles(Vector3 angles)
    {
        if (vfx != null)
        {
            vfx.SetVector3(anglesID, angles);
        }
    }

    public void SetRotationAxis(Vector3 ax, Vector3 ay, Vector3 az)
    {
        if (vfx != null)
        {
            vfx.SetVector3(rotationAxisXID, ax);
            vfx.SetVector3(rotationAxisYID, ay);
            vfx.SetVector3(rotationAxisZID, az);
        }
    }

    public void SetTargetPositionWorld(Vector3 p)
    {
        if (vfx != null)
        {
            vfx.SetVector3(Shader.PropertyToID("TargetPositionWorld"), p);
        }
    }
}
