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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class GridVFX : MonoBehaviour
{
    private VisualEffect vfx = null;

    private int radiusID = -1;
    private int stepID = -1;
    private int oldStepID = -1;
    private int pointSizeID = -1;
    private int snapXID = -1;
    private int snapYID = -1;
    private int snapZID = -1;
    private int targetPositionID = -1;

    private void Awake()
    {
        vfx = GetComponent<VisualEffect>();
        radiusID = Shader.PropertyToID("Radius");
        stepID = Shader.PropertyToID("Step");
        oldStepID = Shader.PropertyToID("OldStep");
        pointSizeID = Shader.PropertyToID("PointSize");
        snapXID = Shader.PropertyToID("SnapX");
        snapYID = Shader.PropertyToID("SnapY");
        snapZID = Shader.PropertyToID("SnapZ");
        targetPositionID = Shader.PropertyToID("TargetPosition");
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

    public void Restart()
    {
        if (vfx != null)
        {
            vfx.Reinit();
        }
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
        }
    }

    public void SetTargetPosition(Vector3 pos)
    {
        if (vfx != null)
        {
            vfx.SetVector3(targetPositionID, pos);
        }
    }
}
