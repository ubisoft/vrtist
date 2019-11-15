using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class ColorPicker : MonoBehaviour
    {
        Color currentColor;
        public Color CurrentColor
        {
            get { return currentColor; }
            set { SetColor(value);  }
        }

        public Hue hue;
        public Saturation saturation;
        public GameObject preview;

        void Start()
        {
            OnColorChanged();
            CurrentColor = new Color(0.2f, 0.5f, 0.6f);
        }

        public void SetColor(Color color)
        {
            currentColor = color;
            float h, s, v;
            Color.RGBToHSV(color, out h, out s, out v);
            hue.SetHue(h);

            Color baseColor = Color.HSVToRGB(h, 1f, 1f);
            saturation.SetBaseColor(baseColor);
            saturation.SetSaturation(new Vector2(s, v));

            SetPreviewColor(color);
        }

        public void OnColorChanged()
        {
            float h = hue.GetHue();
            Vector2 sat = saturation.GetSaturation();

            Color baseColor = Color.HSVToRGB(h, 1f, 1f);
            currentColor = Color.Lerp(Color.white, baseColor, sat.x);
            currentColor = Color.Lerp(Color.black, currentColor, sat.y);

            SetPreviewColor(currentColor);
            saturation.SetBaseColor(baseColor);
        }

        public void SetPreviewColor(Color color)
        {
            var renderer = preview.GetComponent<Renderer>();
            renderer.material.SetColor("_Color", color);
        }

    }
}