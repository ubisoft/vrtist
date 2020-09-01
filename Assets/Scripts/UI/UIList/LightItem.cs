using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VRtist
{
    public class LightItem : ListItemContent
    {
        [HideInInspector] public GameObject lightObject;
        [HideInInspector] public UIDynamicListItem item;

        public void Start()
        {
            Selection.OnSelectionChanged += OnSelectionChanged;
        }

        public void OnDestroy()
        {
            Selection.OnSelectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged(object sender, SelectionChangedArgs args)
        {
            if (Selection.IsSelected(lightObject))
            {
                SetColor(UIOptions.PushedColor);
            }
            else
            {
                SetColor(UIOptions.BackgroundColor);
            }
        }

        public void SetColor(Color color)
        {
            gameObject.GetComponentInChildren<MeshRenderer>(true).materials[0].SetColor("_BaseColor", color);
        }

        public void SetItemName(string name)
        {
            TextMeshProUGUI text = transform.Find("Canvas/TextPanel/Text").GetComponent<TextMeshProUGUI>();
            text.text = name;
        }

        public void SetLightObject(GameObject lightObject, LightController controller)
        {
            this.lightObject = lightObject;
            SetColor(UIOptions.BackgroundColor);
            SetItemName(lightObject.name);

            Sprite sprite = null;
            switch (controller.lightType)
            {
                case LightType.Directional:
                    sprite = UIUtils.LoadIcon("sun");
                    break;
                case LightType.Spot:
                    sprite = UIUtils.LoadIcon("spot");
                    break;
                case LightType.Point:
                    sprite = UIUtils.LoadIcon("light");
                    break;
            }
            if (null != sprite)
            {
                gameObject.GetComponentInChildren<Image>(true).sprite = sprite;
            }
        }
    }
}
