using UnityEngine;
using UnityEngine.VFX;

[ExecuteInEditMode]
public class WindowAnchorVFX : MonoBehaviour
{
    public Transform targetA = null;
    public Transform targetB = null;
    private VisualEffect vfx = null;

    // Start is called before the first frame update
    void Start()
    {
        vfx = GetComponent<VisualEffect>();
    }

    private void Update()
    {
        if (targetA != null && targetB != null && vfx != null && vfx.isActiveAndEnabled)
        {
            vfx.SetVector3("TargetA_position", targetA.position);
            vfx.SetVector3("TargetA_angles", targetA.eulerAngles);
            vfx.SetVector3("TargetA_scale", targetA.localScale);

            vfx.SetVector3("TargetB_position", targetB.position);
            vfx.SetVector3("TargetB_angles", targetB.eulerAngles);
            vfx.SetVector3("TargetB_scale", targetB.localScale);
        }
    }

    public void StartVFX()
    {
        if (vfx != null)
        {
            vfx.Play();
        }
    }

    public void StopVFX()
    {
        if (vfx != null)
        {
            vfx.Stop();
        }
    }
}
