using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VRtist
{
    [CreateAssetMenu(menuName = "VRtist/Settings")]
    public class Settings : ScriptableObject
    {
        public int version = 1;
        public bool displayGizmos = true;
        public bool displayWorldGrid = true;
        public bool displayAvatars = true;
        public bool displayFPS = false;
        public bool display3DCurves = true;
        public float masterVolume = 0f;
        public float ambientVolume = -35f;
        public float uiVolume = 0f;
        public bool rightHanded = true;
        public bool forcePaletteOpen = false;

        public Vector3 palettePosition;
        public Quaternion paletteRotation;
        public bool pinnedPalette = false;

        public Vector3 dopeSheetPosition = Vector3.zero;
        public Quaternion dopeSheetRotation = Quaternion.identity;
        public bool dopeSheetVisible = false;

        public Vector3 shotManagerPosition = Vector3.zero;
        public Quaternion shotManagerRotation = Quaternion.identity;
        public bool shotManagerVisible = false;

        public Vector3 cameraPreviewPosition = Vector3.zero;
        public Quaternion cameraPreviewRotation = Quaternion.identity;
        public bool cameraPreviewVisible = false;

        public Vector3 cameraFeedbackPosition = Vector3.zero;
        public Quaternion cameraFeedbackRotation = Quaternion.identity;
        public Vector3 cameraFeedbackScale = new Vector3(160, 90, 100);
        public float cameraFeedbackScaleValue = 1f;
        public bool cameraFeedbackVisible = false;
        public float cameraDamping = 50f;

        public bool consoleVisible = false;
        public Vector3 consolePosition = Vector3.zero;
        public Quaternion consoleRotation = Quaternion.identity;

        public SkySettings sky = new SkySettings
        {
            topColor = new Color(212f / 255f, 212f / 255f, 212f / 255f),
            middleColor = new Color(195f / 255f, 195f / 255f, 195f / 255f),
            bottomColor = new Color(113f / 255f, 113f / 255f, 113f / 255f)
        };
        public List<SkySettings> skies = new List<SkySettings>() { 
            new SkySettings
            {
                topColor = new Color(212f / 255f, 212f / 255f, 212f / 255f),
                middleColor = new Color(195f / 255f, 195f / 255f, 195f / 255f),
                bottomColor = new Color(113f / 255f, 113f / 255f, 113f / 255f)
            } 
        };
        
        public bool castShadows = false;

        [Range(1.0f, 100.0f)]
        public float scaleSpeed = 50f;

        [Range(1f, 100f)]
        public float raySliderDrag = 97.0f;
        public float RaySliderDrag { get { return 1.0f - (raySliderDrag / 100.0f); } }

        [Range(1f, 100f)]
        public float rayHueDrag = 85.0f;
        public float RayHueDrag { get { return 1.0f - (rayHueDrag / 100.0f); } }

        public AnimationCurve focalCurve;

        public Interpolation interpolation = Interpolation.Linear;

        public void Reset()
        {
            displayGizmos = true;
            displayWorldGrid = true;
            displayFPS = false;
            masterVolume = 0f;
            ambientVolume = -35f;
            uiVolume = 0f;
            rightHanded = true;
            forcePaletteOpen = false;
            pinnedPalette = false;
            cameraDamping = 50f;
            castShadows = false;
            scaleSpeed = 50f;
            raySliderDrag = 95.0f;

            dopeSheetVisible = false;
            dopeSheetPosition = Vector3.zero;
            dopeSheetRotation = Quaternion.identity;

            shotManagerVisible = false;
            shotManagerPosition = Vector3.zero;
            shotManagerRotation = Quaternion.identity;

            cameraPreviewVisible = false;
            cameraPreviewPosition = Vector3.zero;
            cameraPreviewRotation = Quaternion.identity;

            cameraFeedbackPosition = Vector3.zero;
            cameraFeedbackRotation = Quaternion.identity;
            cameraFeedbackScale = Vector3.one;
            cameraFeedbackScaleValue = 1f;
            cameraFeedbackVisible = false;

            consoleVisible = false;
            consolePosition = Vector3.zero;
            consoleRotation = Quaternion.identity;

            interpolation = Interpolation.Linear;

            sky = new SkySettings
            {
                topColor = new Color(212f / 255f, 212f / 255f, 212f / 255f),
                middleColor = new Color(195f / 255f, 195f / 255f, 195f / 255f),
                bottomColor = new Color(113f / 255f, 113f / 255f, 113f / 255f)
            };
            skies = new List<SkySettings>() {
                new SkySettings
                {
                    topColor = new Color(212f / 255f, 212f / 255f, 212f / 255f),
                    middleColor = new Color(195f / 255f, 195f / 255f, 195f / 255f),
                    bottomColor = new Color(113f / 255f, 113f / 255f, 113f / 255f)
                }
            };
        }

        public void SaveWindowPosition(Transform window)
        {
            if (window.name == "PaletteHandle")
            {
                palettePosition = window.localPosition;
                paletteRotation = window.localRotation;
            }
            if (window.name == "DopesheetHandle")
            {
                dopeSheetPosition = window.localPosition;
                dopeSheetRotation = window.localRotation;
            }
            if (window.name == "ShotManagerHandle")
            {
                shotManagerPosition = window.localPosition;
                shotManagerRotation = window.localRotation;
            }
            if (window.name == "CameraPreviewHandle")
            {
                cameraPreviewPosition = window.localPosition;
                cameraPreviewRotation = window.localRotation;
            }
            /*
            if (window.name == "CameraFeedback")
            {
                cameraFeedbackPosition = window.localPosition;
                cameraFeedbackRotation = window.localRotation;
                cameraFeedbackScale = window.localScale;
            }
            */
            if (window.name == "ConsoleHandle")
            {
                consolePosition = window.localPosition;
                consoleRotation = window.localRotation;
            }
        }

        public void LoadWindowPosition(Transform window)
        {
            if (window.name == "PaletteHandle")
            {
                window.localPosition = palettePosition;
                window.localRotation = paletteRotation;
            }
            if (window.name == "DopesheetHandle")
            {
                window.localPosition = dopeSheetPosition;
                window.localRotation = dopeSheetRotation;
            }
            if (window.name == "ShotManagerHandle")
            {
                window.localPosition = shotManagerPosition;
                window.localRotation = shotManagerRotation;
            }
            if (window.name == "CameraPreviewHandle")
            {
                window.localPosition = cameraPreviewPosition;
                window.localRotation = cameraPreviewRotation;
            }
            /*
            if (window.name == "CameraFeedback")
            {
                window.localPosition = cameraFeedbackPosition;
                window.localRotation = cameraFeedbackRotation;
                window.localScale = cameraFeedbackScale;
            }
            */
            if (window.name == "ConsoleHandle")
            {
                window.localPosition = consolePosition;
                window.localRotation = consoleRotation;
            }
        }

        private string GetJsonFilename()
        {
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string filename = appdata + @"\VRtist.json";
            return filename;
        }

        public void Load()
        {
            LoadJson(GetJsonFilename());
            pinnedPalette = false;
        }

        public void Save()
        {
            SaveToJson(GetJsonFilename());
        }
        public void SaveToJson(string filename)
        {
            string json = JsonUtility.ToJson(this, true);
            File.WriteAllText(filename, json);
        }

        public void LoadJson(string filename)
        {
            if (File.Exists(filename))
            {
                string json = File.ReadAllText(filename);
                JsonUtility.FromJsonOverwrite(json, this);
            }
        }
    }

}