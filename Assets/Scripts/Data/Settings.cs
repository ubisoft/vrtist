using System;
using System.Collections;
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
        public bool displayFPS = false;
        public float masterVolume = 0f;
        public float ambientVolume = -35f;
        public float uiVolume = 0f;
        public bool rightHanded = true;
        public bool forcePaletteOpen = false;

        public Vector3 palettePosition;
        public Quaternion paletteRotation;
        public bool pinnedPalette = false;

        public Vector3 dopeSheetPosition;
        public Quaternion dopeSheetRotation;
        public bool dopeSheetVisible = false;

        public Vector3 cameraPreviewPosition;
        public Quaternion cameraPreviewRotation;
        public bool cameraPreviewVisible = false;

        public Vector3 cameraFeedbackPosition = Vector3.zero;
        public Quaternion cameraFeedbackRotation = Quaternion.identity;
        public Vector3 cameraFeedbackScale = new Vector3(160, 90, 100);
        public float cameraFeedbackScaleValue = 1f;

        public bool cameraFeedbackVisible = false;
        public float cameraDamping = 50f;

        public bool castShadows = false;

        [Range(1.0f, 100.0f)]
        public float scaleSpeed = 50f;
    
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
            dopeSheetVisible = false;
            cameraPreviewVisible = false;
            cameraFeedbackVisible = false;
            cameraDamping = 50f;
            castShadows = false;
            scaleSpeed = 50f;

            cameraFeedbackPosition = Vector3.zero;
            cameraFeedbackRotation = Quaternion.identity;
            cameraFeedbackScale = Vector3.one;
            cameraFeedbackScaleValue = 1f;
            cameraFeedbackVisible = false;
        }

        public void SetWindowPosition(Transform window)
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