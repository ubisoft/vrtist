using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    public int fps { get; private set; }

    void Update()
    {
        fps = (int) (1f / Time.unscaledDeltaTime);
    }
}
