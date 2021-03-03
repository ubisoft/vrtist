/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

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
