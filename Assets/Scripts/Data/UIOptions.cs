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
        public HDRColorVariable sceneHoverColor;// = new Color(2.0f, 0.8f, 0.0f, 1.0f); // hdr yellow
        public ColorVariable attenuatedTextColor;// = new Color(.7,.7,.7);

        private static UIOptions instance = null;
        public static UIOptions Instance
        {
            get
            {
                if (instance == null)
                {
                    UIOptions[] options = Resources.FindObjectsOfTypeAll<UIOptions>();
                    if (options.Length > 0)
                    {
                        instance = options[0];
                    }
                }
                if (instance == null)
                {
                    instance = CreateInstance<UIOptions>(); // in memory
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
            //UIOptions myUIOptions = CreateInstance<UIOptions>();
            string json = File.ReadAllText("user_prefs_ui.json"); // TODO: find where Unity stores user created files.
            JsonUtility.FromJsonOverwrite(json, Instance);
            // + reloadui
        }



        //private void OnEnable()
        //{

        //}

        //private void OnDisable()
        //{

        //}

        //private void OnDestroy()
        //{

        //}
    }
}
