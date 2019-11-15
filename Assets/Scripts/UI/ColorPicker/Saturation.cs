using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class Saturation : MonoBehaviour
    {
        ColorPicker colorPicker;
        Color baseColor;
        Vector2 cursorPosition = new Vector2(0.5f, 0.5f);

        public Transform cursor;

        void Awake()
        {
            colorPicker = GetComponentInParent<ColorPicker>();
        }

        public void SetBaseColor(Color clr)
        {
            baseColor = clr;
            var renderer = GetComponent<Renderer>();
            renderer.material.SetColor("_Color", clr);
        }

        public Vector2 GetSaturation()
        {
            return cursorPosition;
        }

        public void SetSaturation(Vector2 sat)
        {
            cursorPosition = sat;
            cursor.localPosition = new Vector3(sat.x - 0.5f, sat.y - 0.5f, cursor.localPosition.z);            
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.gameObject.name != "Cursor")
                return;

            Vector3 colliderSphereCenter = other.gameObject.GetComponent<SphereCollider>().center;
            colliderSphereCenter = other.gameObject.transform.localToWorldMatrix.MultiplyPoint(colliderSphereCenter);

            Vector3 position = transform.worldToLocalMatrix.MultiplyPoint(colliderSphereCenter);
            
            float x = position.x + 1f * 0.5f;
            float y = position.y + 1f * 0.5f;
            x = Mathf.Clamp(x, 0, 1);
            y = Mathf.Clamp(y, 0, 1);
            SetSaturation( new Vector2(x, y) );

            colorPicker.OnColorChanged();
        }
    }
}