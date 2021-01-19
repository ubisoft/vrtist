using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSMove : MonoBehaviour
{
    public Transform rotateX;
    private float speed = 10f;

    // Update is called once per frame
    void Update()
    {
        float x = Input.GetAxis("Horizontal") * speed * Time.deltaTime;
        float y = Input.GetAxis("Vertical") * speed * Time.deltaTime;

        if (x == 0f && y == 0f)
            return;

        Vector3 move = rotateX.right * x + rotateX.forward * y;
        transform.localPosition += move;
    }
}
