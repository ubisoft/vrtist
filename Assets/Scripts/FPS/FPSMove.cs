using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSMove : MonoBehaviour
{
    public Transform rotateX;
    public float speed = 1f;

    // Update is called once per frame
    void Update()
    {
        float x = Input.GetAxis("Horizontal") * speed;
        float y = Input.GetAxis("Vertical") * speed;

        if (x == 0f && y == 0f)
            return;

        Vector3 move = rotateX.right * x + rotateX.forward * y;
        transform.localPosition += move;
    }
}
