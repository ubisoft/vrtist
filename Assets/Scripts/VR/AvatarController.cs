using System.Collections.Generic;

using TMPro;

using UnityEngine;

namespace VRtist
{
    public class AvatarController : MonoBehaviour, IGizmo
    {
        RectTransform canvas;
        TextMeshProUGUI text;
        readonly List<Material> materials = new List<Material>();

        private void Start()
        {
            canvas = (RectTransform)transform.Find("Canvas");
            if (null == text) { text = gameObject.GetComponentInChildren<TextMeshProUGUI>(); }
            FetchMaterials();
        }

        private void Update()
        {
            // Orient text face to camera
            canvas.LookAt(Camera.main.transform);

            // Constant scale
            //float scale = 1f / GlobalState.WorldScale;
            //transform.localScale = new Vector3(scale, scale, scale);
        }

        public void SetGizmoVisible(bool value)
        {
            // Hide geometry
            MeshFilter[] meshFilters = gameObject.GetComponentsInChildren<MeshFilter>(true);
            foreach (MeshFilter meshFilter in meshFilters)
            {
                meshFilter.gameObject.SetActive(value);
            }

            // Hide UI
            Canvas[] canvases = gameObject.GetComponentsInChildren<Canvas>(true);
            foreach (Canvas canvas in canvases)
            {
                canvas.gameObject.SetActive(value);
            }
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

        public void SetUser(User user)
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

            transform.localPosition = user.position;
            transform.LookAt(transform.parent.TransformPoint(user.target));
        }
    }
}
