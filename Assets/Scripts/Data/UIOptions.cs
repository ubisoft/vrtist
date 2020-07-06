using System.IO;
using UnityEngine;

namespace VRtist
{
    [CreateAssetMenu(menuName = "VRtist/UIOptions")]
    public class UIOptions : ScriptableObject
    {
        public ColorVariable foregroundColor; // = new Color(0.9f, 0.9f, 0.9f, 1.0f);
        public ColorVariable backgroundColor; // new Color(0.1742f, 0.5336f, 0.723f, 1.0f);
        public ColorVariable pushedColor; // new Color(0.0f, 0.65f, 1.0f, 1.0f);
        public ColorVariable checkedColor; // new Color(0.0f, 0.85f, 1.0f, 1.0f);
        public ColorVariable disabledColor;// = new Color(0.5873f, 0.6170f, 0.6320f); // middle grey blue
        public ColorVariable sliderRailColor;// = new Color(0.1f, 0.1f, 0.1f, 1.0f); // darker grey.
        public ColorVariable sliderKnobColor;// = new Color(0.9f, 0.9f, 0.9f, 1.0f); // lighter grey.
        public ColorVariable attenuatedTextColor;// = new Color(.7,.7,.7);
        public ColorVariable panelColor;
        public ColorVariable closeWindowButtonColor;
        public ColorVariable pinWindowButtonColor;
        public ColorVariable focusColor;
        [Space(30)]
        public HDRColorVariable sceneHoverColor;// = new Color(2.0f, 0.8f, 0.0f, 1.0f); // hdr yellow

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
        public static Color SceneHoverColor { get { return Instance.sceneHoverColor.value; } }
        public static Color FocusColor { get { return Instance.focusColor.value; } }

        private static UIOptions instance = null;
        public static UIOptions Instance
        {
            get
            {
                if (instance == null || instance.name != "DefaultUIOptions")
                {
                    instance = Resources.Load<UIOptions>("Data/UI/DefaultUIOptions");
                    // NOTE: le FindObjectsOfTypeAll retourne une liste vide parfois!!!
                    //UIOptions[] options = Resources.FindObjectsOfTypeAll<UIOptions>();
                    //if (options.Length > 0)
                    //{
                    //    for (int i = 0; i < options.Length; ++i)
                    //    {
                    //        if (options[i].name == "DefaultUIOptions")
                    //        {
                    //            instance = options[i];
                    //            break;
                    //        }
                    //    }
                    //}
                }
                return instance;

                // Je me suis retrouve avec 2 UIOptions, le premier etant celui cree en memoire, et pas mon asset!!
                //UIOptions[] options = Resources.FindObjectsOfTypeAll<UIOptions>();
                //if (options.Length > 0)
                //{
                //    for (int i = 0; i < options.Length; ++i)
                //    {
                //        instance = options[i];
                //        if (options[i].name == "DefaultUIOptions")
                //            return options[i];
                //    }
                //}
                //return instance;

                //if (instance == null)
                //{
                //    UIOptions[] options = Resources.FindObjectsOfTypeAll<UIOptions>();
                //    if (options.Length > 0)
                //    {
                //        instance = options[0];
                //    }
                //}
                //if (instance == null)
                //{
                //    instance = CreateInstance<UIOptions>(); // in memory
                //}
                //return instance;
            }
        }

        public void SavePreferences()
        {

        }

        public void LoadPreferences()
        {
            // Load user preference from JSON
            //UIOptions myUIOptions = CreateInstance<UIOptions>();
            string json = File.ReadAllText("user_prefs_ui.json"); // TODO: find where Unity stores user created files.
            JsonUtility.FromJsonOverwrite(json, Instance);
            // + reloadui
        }
    }
}
