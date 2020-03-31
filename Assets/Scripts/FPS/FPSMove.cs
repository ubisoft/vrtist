using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSMove : MonoBehaviour
{
    public Transform playerBody;
    public float speed = 1f;

    // Update is called once per frame
    void Update()
    {
        float x = Input.GetAxis("Horizontal") * speed;
        float y = Input.GetAxis("Vertical") * speed;

        Vector3 move = transform.right * x + transform.forward * y;
        playerBody.localPosition += move;
    }
}
