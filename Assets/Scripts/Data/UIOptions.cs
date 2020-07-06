using System.IO;
using UnityEngine;

namespace VRtist
{
    [CreateAssetMenu(menuName = "VRtist/UIOptions")]
    public class UIOptions : ScriptableObject
    {
        public ColorVar foregroundColor = new ColorVar { value = new Color(0.9f, 0.9f, 0.9f, 1.0f) };
        public ColorVar backgroundColor = new ColorVar { value = new Color(0.1742f, 0.5336f, 0.723f, 1.0f) };
        public ColorVar pushedColor = new ColorVar { value = new Color(0.0f, 0.65f, 1.0f, 1.0f) };
        public ColorVar checkedColor = new ColorVar { value = new Color(0.0f, 0.85f, 1.0f, 1.0f) };
        public ColorVar disabledColor = new ColorVar { value = new Color(0.5873f, 0.6170f, 0.6320f) };
        public ColorVar sliderRailColor = new ColorVar { value = new Color(0.1f, 0.1f, 0.1f, 1.0f) };
        public ColorVar sliderKnobColor = new ColorVar { value = new Color(0.9f, 0.9f, 0.9f, 1.0f) };
        public ColorVar attenuatedTextColor = new ColorVar { value = new Color(.7f,.7f,.7f) };
        public ColorVar panelColor = new ColorVar { value = new Color(.7f, .7f, .7f) };
        public ColorVar closeWindowButtonColor = new ColorVar { value = new Color(.7f, .7f, .7f) };
        public ColorVar pinWindowButtonColor = new ColorVar { value = new Color(.7f, .7f, .7f) };
        public ColorVar focusColor = new ColorVar { value = new Color(.7f, .7f, .7f) };
        [Space(30)]
        public ColorVar sceneHoverColor = new ColorVar() { isHdr = true, value = new Color(2.0f, 0.8f, 0.0f, 1.0f) }; // hdr yellow

        // ReadOnly Properties

        public static Color ForegroundColor { get { return Instance.foregroundColor.value; } }
        public static Color BackgroundColor { get { return Instance.backgroundColor.value; } }
        public static Color PushedColor { get { return Instance.pushedColor.value; } }
        public static Color CheckedColor { get { return Instance.checkedColor.value; } }
        public static Color DisabledColor { get { return Instance.disabledColor.value; } }
        public static Color SliderRailColor { get { return Instance.sliderRailColor.value; } }
        public static Color SliderKnobColor { get { return Instance.sliderKnobColor.value; } }
        public static Color AttenuatedTextColor { get { return Instance.attenuatedTextColor.value; } }
        public static Color PanelColor { get { return Instance.panelColor.value; } }
        public static Color CloseWindowButtonColor { get { return Instance.closeWindowButtonColor.value; } }
        public static Color PinWindowButtonColor { get { return Instance.pinWindowButtonColor.value; } }
        public static Color FocusColor { get { return Instance.focusColor.value; } }
        public static Color SceneHoverColor { get { return Instance.sceneHoverColor.value; } }

        public static ColorVar ForegroundColorVar { get { return Instance.foregroundColor; } }
        public static ColorVar BackgroundColorVar { get { return Instance.backgroundColor; } }
        public static ColorVar PushedColorVar { get { return Instance.pushedColor; } }
        public static ColorVar CheckedColorVar { get { return Instance.checkedColor; } }
        public static ColorVar DisabledColorVar { get { return Instance.disabledColor; } }
        public static ColorVar SliderRailColorVar { get { return Instance.sliderRailColor; } }
        public static ColorVar SliderKnobColorVar { get { return Instance.sliderKnobColor; } }
        public static ColorVar AttenuatedTextColorVar { get { return Instance.attenuatedTextColor; } }
        public static ColorVar PanelColorVar { get { return Instance.panelColor; } }
        public static ColorVar CloseWindowButtonColorVar { get { return Instance.closeWindowButtonColor; } }
        public static ColorVar PinWindowButtonColorVar { get { return Instance.pinWindowButtonColor; } }
        public static ColorVar FocusColorVar { get { return Instance.focusColor; } }
        public static ColorVar SceneHoverColorVar { get { return Instance.sceneHoverColor; } }

        private static UIOptions instance = null;
        public static UIOptions Instance
        {
            get
            {
                if (instance == null || instance.name != "DefaultUIOptions")
                {
                    // NOTES:
                    // Code de reference: UIOptions[] options = Resources.FindObjectsOfTypeAll<UIOptions>();
                    // -> Il arrive souvent que options soit vide, alors que l'asset existe!!

                    instance = Resources.Load<UIOptions>("Data/UI/DefaultUIOptions");
                }
                return instance;
            }
        }

        public void SavePreferences()
        {

        }

        public void LoadPreferences()
        {
            // Load user preference from JSON
            instance = CreateInstance<UIOptions>();
            string json = File.ReadAllText("user_prefs_ui.json"); // TODO: find where Unity stores user created files.
            JsonUtility.FromJsonOverwrite(json, Instance);
            // + reloadui
        }
    }
}
