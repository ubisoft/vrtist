/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    [CreateAssetMenu(menuName = "VRtist/Settings")]
    public class Settings : ScriptableObject
    {
        public int version = 1;
        public bool displayGizmos = true;
        public bool DisplayGizmos
        {
            get { return displayGizmos; }
            set { displayGizmos = value; onSettingsChanged.Invoke(); }
        }

        public bool displayLocators = true;
        public bool DisplayLocators
        {
            get { return displayLocators; }
            set { displayLocators = value; onSettingsChanged.Invoke(); }
        }
        public bool displayWorldGrid = true;

        public bool DisplayWorldGrid
        {
            get { return displayWorldGrid; }
            set { displayWorldGrid = value; onSettingsChanged.Invoke(); }
        }
        public bool displayAvatars = true;
        public bool DisplayAvatars
        {
            get { return displayAvatars; }
            set { displayAvatars = value; onSettingsChanged.Invoke(); }
        }
        public bool displayFPS = false;
        public bool DisplayFPS
        {
            get { return displayFPS; }
            set { displayFPS = value; onSettingsChanged.Invoke(); }
        }

        public bool display3DCurves = true;
        public bool Display3DCurves
        {
            get { return display3DCurves; }
            set { display3DCurves = value; onSettingsChanged.Invoke(); }
        }
        public float masterVolume = 0f;
        public float ambientVolume = -35f;
        public float uiVolume = 0f;
        public bool rightHanded = true;
        public bool forcePaletteOpen = false;

        public Vector3 palettePosition;
        public Quaternion paletteRotation;
        public bool pinnedPalette = false;

        public Vector3 dopeSheetPosition = new Vector3(0.3f, 1.4f, 0.7f);
        public Quaternion dopeSheetRotation = Quaternion.Euler(7, 52, 0);
        public bool dopeSheetVisible = false;
        public bool DopeSheetVisible
        {
            get { return dopeSheetVisible; }
            set { dopeSheetVisible = value; onSettingsChanged.Invoke(); }
        }

        public Vector3 shotManagerPosition = new Vector3(0.3f, 1.2f, 0.7f);
        public Quaternion shotManagerRotation = Quaternion.Euler(64, 50, 0);
        public bool shotManagerVisible = false;
        public bool ShotManagerVisible
        {
            get { return shotManagerVisible; }
            set { shotManagerVisible = value; onSettingsChanged.Invoke(); }
        }

        public Vector3 cameraPreviewPosition = new Vector3(0.3f, 1.5f, 0.6f);
        public Quaternion cameraPreviewRotation = Quaternion.Euler(-4, 49, 0);
        public bool cameraPreviewVisible = false;
        public bool CameraPreviewVisible
        {
            get { return cameraPreviewVisible; }
            set { cameraPreviewVisible = value; onSettingsChanged.Invoke(); }
        }

        public Vector3 cameraFeedbackDirection = Vector3.forward;
        public float cameraFeedbackScaleValue = 1f;
        public float cameraFeedbackMaxScaleValue = 1.7f;
        public float cameraFeedbackMinScaleValue = 0.4f;

        public bool cameraFeedbackVisible = false;
        public bool CameraFeedbackVisible
        {
            get { return cameraFeedbackVisible; }
            set { cameraFeedbackVisible = value; onSettingsChanged.Invoke(); }
        }

        public float cameraDamping = 50f;

        public bool consoleVisible = false;
        public Vector3 consolePosition = new Vector3(-0.2f, 1.0f, 0.5f);
        public Quaternion consoleRotation = Quaternion.Euler(54, 6, 0);
        public bool ConsoleVisible
        {
            get { return consoleVisible; }
            set { consoleVisible = value; onSettingsChanged.Invoke(); }
        }

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

        public CameraManager.Resolution videoOutputResolution = new CameraManager.Resolution
        {
            width = CameraManager.resolution1080p.width,
            height = CameraManager.resolution1080p.height
        };
        public string videoOutputDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos) + "/VRtist";
        public string snapshotsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + "/VRtist";

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

        public string assetBankDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/VRtist";
        public string lobbyScenePath = null;

        private string projectName = "myScene";
        public string ProjectName
        {
            get { return projectName; }
            set { projectName = value; onSettingsChanged.Invoke(); }
        }

        public static UnityEvent onSettingsChanged = new UnityEvent();
        public void Reset()
        {
            displayGizmos = true;
            displayLocators = true;
            displayWorldGrid = true;
            displayFPS = false;
            masterVolume = 0f;
            ambientVolume = -35f;
            uiVolume = 0f;
            rightHanded = true;
            forcePaletteOpen = false;
            pinnedPalette = false;
            palettePosition = new Vector3(-0.02f, 0.05f, -0.05f);
            paletteRotation = Quaternion.Euler(30f, 0, 0);
            cameraDamping = 50f;
            castShadows = false;
            scaleSpeed = 50f;
            raySliderDrag = 95.0f;

            dopeSheetVisible = false;
            dopeSheetPosition = new Vector3(0.3f, 1.4f, 0.7f);
            dopeSheetRotation = Quaternion.Euler(7, 52, 0);

            shotManagerVisible = false;
            shotManagerPosition = new Vector3(0.3f, 1.2f, 0.7f);
            shotManagerRotation = Quaternion.Euler(64, 50, 0);

            cameraPreviewVisible = false;
            cameraPreviewPosition = new Vector3(0.3f, 1.5f, 0.6f);
            cameraPreviewRotation = Quaternion.Euler(-4, 49, 0);

            cameraFeedbackDirection = Vector3.forward;
            cameraFeedbackScaleValue = 1f;
            cameraFeedbackVisible = false;

            consoleVisible = false;
            consolePosition = new Vector3(-0.2f, 1.0f, 0.5f);
            consoleRotation = Quaternion.Euler(54, 6, 0);

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

            videoOutputResolution = CameraManager.resolution1080p;
            videoOutputDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos) + "/VRtist";
            snapshotsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + "/VRtist";

            assetBankDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/VRtist";
            projectName = "myScene";
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
            if (window.name == "ConsoleHandle")
            {
                consolePosition = window.localPosition;
                consoleRotation = window.localRotation;
            }
        }

        public void LoadWindowPosition(Transform window)
        {
        }

        private string GetJsonFilename()
        {
            return Application.persistentDataPath + "/VRtist.json";
        }

        public void Load()
        {
            LoadJson(GetJsonFilename());
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
