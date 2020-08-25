using System.IO;
using UnityEngine;

namespace VRtist
{
    [CreateAssetMenu(menuName = "VRtist/UIOptions")]
    public class UIOptions : ScriptableObject
    {
        [SpaceHeader("Common Colors", 6, 0.3f, 0.3f, 0.3f)]
        public ColorVar foregroundColor = new ColorVar { value = new Color(0.9f, 0.9f, 0.9f, 1.0f) };
        public ColorVar backgroundColor = new ColorVar { value = new Color(0.1742f, 0.5336f, 0.723f, 1.0f) };
        public ColorVar pushedColor = new ColorVar { value = new Color(0.0f, 0.65f, 1.0f, 1.0f) };
        public ColorVar checkedColor = new ColorVar { value = new Color(0.0f, 0.85f, 1.0f, 1.0f) };
        public ColorVar disabledColor = new ColorVar { value = new Color(0.5873f, 0.6170f, 0.6320f) };
        public ColorVar selectedColor = new ColorVar { value = new Color(0.0f, 0.65f, 1.0f, 1.0f) };
        public ColorVar hoveredColor = new ColorVar { value = new Color(0.4f, 0.4f, 0.4f, 1.0f) };

        [SpaceHeader("Specific Class Colors", 6, 0.3f, 0.3f, 0.3f)]
        public ColorVar sliderRailColor = new ColorVar { value = new Color(0.1f, 0.1f, 0.1f, 1.0f) };
        public ColorVar sliderKnobColor = new ColorVar { value = new Color(0.9f, 0.9f, 0.9f, 1.0f) };
        public ColorVar panelColor = new ColorVar { value = new Color(.7f, .7f, .7f) };
        public ColorVar panelHoverColor = new ColorVar { value = new Color(0.4f, 0.4f, 0.4f, 1.0f) };
        public ColorVar grabberBaseColor = new ColorVar { value = new Color(0.9f, 0.9f, 0.9f, 1.0f) };
        public ColorVar grabberHoverColor = new ColorVar { value = new Color(0.1742f, 0.5336f, 0.723f, 1.0f) };
        public ColorVar invisibleColor = new ColorVar { value = new Color(0.0f, 0.0f, 0.0f, 0.0f) };

        [SpaceHeader("Specific Widget Colors", 6, 0.3f, 0.3f, 0.3f)]
        public ColorVar attenuatedTextColor = new ColorVar { value = new Color(.7f,.7f,.7f) };
        public ColorVar sectionTextColor = new ColorVar { value = new Color(.0f, .4739f, 1.0f) };
        public ColorVar closeWindowButtonColor = new ColorVar { value = new Color(.7f, .7f, .7f) };
        public ColorVar pinWindowButtonColor = new ColorVar { value = new Color(.7f, .7f, .7f) };
        public ColorVar exitButtonColor = new ColorVar { value = new Color(.7f, .1f, .1f) };
        public ColorVar focusColor = new ColorVar { value = new Color(.7f, .7f, .7f) };
        [Space(30)]
        public ColorVar sceneHoverColor = new ColorVar() { isHdr = true, value = new Color(2.0f, 0.8f, 0.0f, 1.0f) }; // hdr yellow

        // ReadOnly Properties

        public static Color ForegroundColor { get { return Instance.foregroundColor.value; } }
        public static Color BackgroundColor { get { return Instance.backgroundColor.value; } }
        public static Color PushedColor { get { return Instance.pushedColor.value; } }
        public static Color CheckedColor { get { return Instance.checkedColor.value; } }
        public static Color DisabledColor { get { return Instance.disabledColor.value; } }
        public static Color SelectedColor { get { return Instance.selectedColor.value; } }
        public static Color HoveredColor { get { return Instance.hoveredColor.value; } }
        public static Color SliderRailColor { get { return Instance.sliderRailColor.value; } }
        public static Color SliderKnobColor { get { return Instance.sliderKnobColor.value; } }
        public static Color AttenuatedTextColor { get { return Instance.attenuatedTextColor.value; } }
        public static Color SectionTextColor { get { return Instance.sectionTextColor.value; } }
        public static Color PanelColor { get { return Instance.panelColor.value; } }
        public static Color PanelHoverColor { get { return Instance.panelHoverColor.value; } }
        public static Color CloseWindowButtonColor { get { return Instance.closeWindowButtonColor.value; } }
        public static Color PinWindowButtonColor { get { return Instance.pinWindowButtonColor.value; } }
        public static Color ExitButtonColor { get { return Instance.exitButtonColor.value; } }
        public static Color FocusColor { get { return Instance.focusColor.value; } }
        public static Color GrabberBaseColor { get { return Instance.grabberBaseColor.value; } }
        public static Color GrabberHoverColor { get { return Instance.grabberHoverColor.value; } }
        public static Color InvisibleColor { get { return Instance.invisibleColor.value; } }
        public static Color SceneHoverColor { get { return Instance.sceneHoverColor.value; } }

        public static ColorVar ForegroundColorVar { get { return Instance.foregroundColor; } }
        public static ColorVar BackgroundColorVar { get { return Instance.backgroundColor; } }
        public static ColorVar PushedColorVar { get { return Instance.pushedColor; } }
        public static ColorVar CheckedColorVar { get { return Instance.checkedColor; } }
        public static ColorVar DisabledColorVar { get { return Instance.disabledColor; } }
        public static ColorVar SelectedColorVar { get { return Instance.selectedColor; } }
        public static ColorVar HoveredColorVar { get { return Instance.hoveredColor; } }
        public static ColorVar SliderRailColorVar { get { return Instance.sliderRailColor; } }
        public static ColorVar SliderKnobColorVar { get { return Instance.sliderKnobColor; } }
        public static ColorVar AttenuatedTextColorVar { get { return Instance.attenuatedTextColor; } }
        public static ColorVar SectionTextColorVar { get { return Instance.sectionTextColor; } }
        public static ColorVar PanelColorVar { get { return Instance.panelColor; } }
        public static ColorVar PanelHoverColorVar { get { return Instance.panelHoverColor; } }
        public static ColorVar CloseWindowButtonColorVar { get { return Instance.closeWindowButtonColor; } }
        public static ColorVar PinWindowButtonColorVar { get { return Instance.pinWindowButtonColor; } }
        public static ColorVar ExitButtonColorVar { get { return Instance.exitButtonColor; } }
        public static ColorVar FocusColorVar { get { return Instance.focusColor; } }
        public static ColorVar GrabberBaseColorVar { get { return Instance.grabberBaseColor; } }
        public static ColorVar GrabberHoverColorVar { get { return Instance.grabberHoverColor; } }
        public static ColorVar InvisibleColorVar { get { return Instance.invisibleColor; } }
        public static ColorVar SceneHoverColorVar { get { return Instance.sceneHoverColor; } }

        private static UIOptions instance = null;
        public static UIOptions Instance
        {
            get
            {
                if (instance == null || instance.name != "DefaultUIOptions")
                {
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
