using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace VRtist
{

    public class AvatarController : MonoBehaviour
    {
        RectTransform canvas;
        TextMeshProUGUI text;
        List<Material> materials = new List<Material>();

        private void Start()
        {
            canvas = (RectTransform) transform.Find("Canvas");
            if (null == text) { text = gameObject.GetComponentInChildren<TextMeshProUGUI>(); }
            FetchMaterials();
        }

        private void Update()
        {
            // Orient text face to camera
            canvas.LookAt(Camera.main.transform);

            // Constant scale
            float scale = 1f / GlobalState.worldScale;
            transform.localScale = new Vector3(scale, scale, scale);
        }

        private void FetchMaterials()
        {
            if (materials.Count > 0) { return; }

            MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                Material material = meshRenderer.material;
                if (material.name == "Base (Instance)")
                {
                    materials.Add(material);
                }
            }
        }

        public void SetUser(ConnectedUser user)
        {
            if (null == text) { text = gameObject.GetComponentInChildren<TextMeshProUGUI>(); }
            if (text.text != user.name) { text.text = user.name; }

            FetchMaterials();
            user.color.a = 0.5f;
            if (materials[0].GetColor("_BaseColor") != user.color)
            {
                foreach (Material material in materials)
                {
                    material.SetColor("_BaseColor", user.color);
                }
            }

            // TODO scale (VRtist scale)

            // TODO bounding boxes of selected objects

            transform.localPosition = user.eye;
            transform.LookAt(transform.parent.TransformPoint(user.target));
        }
    }
}
