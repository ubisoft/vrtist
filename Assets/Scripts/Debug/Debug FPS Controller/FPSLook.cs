using UnityEngine;

public class FPSLook : MonoBehaviour
{
    public Transform rotateX;
    private float sensitivity = 400;
    float xRotation = 0f;

    void Update()
    {
        float z = Input.GetAxis("Fire1");
        if (z <= 0)
        {
            Cursor.lockState = CursorLockMode.None;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;

        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.Rotate(Vector3.up * mouseX);
        rotateX.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }
}
