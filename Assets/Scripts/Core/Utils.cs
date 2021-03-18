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
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace VRtist
{
    public struct MaterialValue
    {
        public Color color;
        public float roughness;
        public float metallic;
    }

    public class Utils
    {
        public static string blenderHiddenParent = "__Blender_Hidden_Parent_Matrix";
        public static string blenderCollectionInstanceOffset = "__Blender_Collection_Instance_Offset";
        static int gameObjectNameId = 0;
        static readonly long timestamp = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;

        public static GameObject FindRootGameObject(string name)
        {
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == name)
                {
                    return roots[i];
                }
            }
            return null;
        }

        public static GameObject FindWorld()
        {
            return FindRootGameObject("World");
        }

        public static Volume FindVolume()
        {
            GameObject volumes = Utils.FindRootGameObject("Volumes");
            return volumes.transform.Find("VolumePostProcess").GetComponent<Volume>();
        }

        public static Volume FindLobbyVolume()
        {
            GameObject volumes = Utils.FindRootGameObject("Volumes");
            return volumes.transform.Find("VolumeLobby").GetComponent<Volume>();
        }

        public static Volume FindCameraPostProcessVolume()
        {
            GameObject volumes = Utils.FindRootGameObject("Volumes");
            return volumes.transform.Find("VolumePostProcessCamera").GetComponent<Volume>();
        }

        public static GameObject FindGameObject(string name)
        {
            GameObject world = Utils.FindWorld();
            if (!world)
                return null;

            int childrenCount = world.transform.childCount;
            for (int i = 0; i < childrenCount; i++)
            {
                GameObject child = world.transform.GetChild(i).gameObject;
                if (child.name == name)
                    return child;
            }

            return null;
        }

        public static bool GetTransformRelativePathTo(Transform child, Transform root, out string path)
        {
            path = "";
            Transform current = child;
            while (null != current.parent && current.name != root.name)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            if (path.Length > 0) { path = path.Substring(0, path.Length - 1); }  // remove trailing slash
            return current != null;
        }

        public static string BuildTransformPath(GameObject gobject)
        {
            string res = "";
            while (gobject.GetComponent<ParametersController>() == null)
            {
                res = "/" + gobject.name + res;
                gobject = gobject.transform.parent.gameObject;
            }

            if (res.Length > 0)
                res = res.Substring(1, res.Length - 1);

            return res;
        }

        private static readonly Regex readableNameRegex = new Regex(@"(?<basename>.+?)\.(?<hash>.+?)\.(?<number>\d+)", RegexOptions.Compiled);
        public static string GetReadableName(string name)
        {
            string readableName = name;
            MatchCollection matches = readableNameRegex.Matches(name);
            if (matches.Count == 1)
            {
                GroupCollection groups = matches[0].Groups;
                string baseName = groups["basename"].ToString();
                int number = Int32.Parse(groups["number"].Value);
                readableName = $"{baseName}.{number}";
            }
            return readableName;
        }

        public static string GetPath(Transform t)
        {
            if (null == t)
                return "";

            string path = t.name;
            Transform parent = t.parent;
            while (null != parent)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        public static string GetBaseName(string name)
        {
            string res = name;
            for (int i = 0; i < 2; i++)
            {
                int index = res.LastIndexOf('.');
                if (index >= 0)
                    res = res.Substring(0, index);
            }

            return res;
        }

        public static string CreateUniqueName(string baseName)
        {
            baseName = GetBaseName(baseName);

            if (baseName.Length > 48)
                baseName = baseName.Substring(0, 48);

            string name = baseName + "." + String.Format("{0:X}", (Dns.GetHostName() + timestamp.ToString()).GetHashCode()) + "." + gameObjectNameId.ToString();
            gameObjectNameId++;
            return name;
        }

        public static string GetMaterialName(GameObject gobject)
        {
            return "Mat_" + gobject.name;
        }

        public static RenderTexture CreateRenderTexture(int width, int height, int depth, RenderTextureFormat format, bool randomWrite)
        {
            RenderTexture renderTexture = new RenderTexture(width, height, depth, format)
            {
                enableRandomWrite = randomWrite
            };
            renderTexture.Create();
            return renderTexture;
        }
        public static RenderTexture CreateRenderTexture(RenderTexture source)
        {
            return CreateRenderTexture(source.width, source.height, 0, source.format, true);
        }

        public static MaterialValue GetMaterialValue(GameObject gobject)
        {
            MeshRenderer renderer = gobject.GetComponentInChildren<MeshRenderer>();
            MaterialValue value = new MaterialValue();
            if (null != renderer)
            {
                value.color = renderer.material.GetColor("_BaseColor");
                if (renderer.material.HasProperty("_Smoothness")) { value.roughness = 1f - renderer.material.GetFloat("_Smoothness"); }
                else { value.roughness = renderer.material.GetFloat("_Roughness"); }
                value.metallic = renderer.material.GetFloat("_Metallic");
            }
            return value;
        }

        public static void SetMaterialValue(GameObject gobject, MaterialValue value)
        {
            Material opaqueMat = Resources.Load<Material>("Materials/ObjectOpaque");
            Material transpMat = Resources.Load<Material>("Materials/ObjectTransparent");
            MeshRenderer[] renderers = gobject.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer renderer in renderers)
            {
                int i = 0;
                Material[] newMaterials = renderer.materials;
                foreach (Material oldMaterial in renderer.materials)
                {
                    bool previousMaterialWasTransparent = oldMaterial.HasProperty("_Opacity") && oldMaterial.GetFloat("_Opacity") < 0.99f;
                    bool newMaterialIsTransparent = value.color.a < 1.0f;
                    if (previousMaterialWasTransparent != newMaterialIsTransparent)
                    {
                        // swap material type
                        if (newMaterialIsTransparent)
                        {
                            newMaterials[i] = new Material(transpMat);
                        }
                        else
                        {
                            newMaterials[i] = new Material(opaqueMat);
                        }
                    }
                    //else
                    //{
                    //    newMaterials[i] = oldMaterial;
                    //}

                    Material newMaterial = newMaterials[i++];

                    newMaterial.SetColor("_BaseColor", value.color);
                    if (newMaterial.HasProperty("_Opacity")) { newMaterial.SetFloat("_Opacity", value.color.a); }
                    if (newMaterial.HasProperty("_Smoothness")) { newMaterial.SetFloat("_Smoothness", 1f - value.roughness); }
                    else { newMaterial.SetFloat("_Roughness", value.roughness); }
                    newMaterial.SetFloat("_Metallic", value.metallic);
                }
                renderer.materials = newMaterials; // set array
            }
        }

        public static void TryDispose(IDisposable obj)
        {
            if (null == obj) { return; }
            obj.Dispose();
        }
        public static void TryDestroy(UnityEngine.Object obj)
        {
            if (null == obj) { return; }
            UnityEngine.Object.Destroy(obj);
        }

        public static void SwapBuffers(ref ComputeBuffer buf1, ref ComputeBuffer buf2)
        {
            var temp = buf1;
            buf1 = buf2;
            buf2 = temp;
        }

        public static Texture2D CopyRenderTextureToTexture(RenderTexture renderTexture)
        {
            TextureCreationFlags flags = TextureCreationFlags.None;
            Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, renderTexture.graphicsFormat, flags);

            RenderTexture activeRT = RenderTexture.active;
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();
            //Graphics.CopyTexture(renderTexture, texture); doesn't work :(
            RenderTexture.active = activeRT;

            return texture;
        }

        public static void SavePNG(Texture2D texture, string path)
        {
            byte[] data = texture.EncodeToPNG();
            CreatePath(path);
            File.WriteAllBytes(path, data);
        }

        public static Texture2D LoadTexture(string path, bool linear = false)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("Invalid path: path is empty or null");
                return null;
            }
            if (!File.Exists(path))
            {
                Debug.LogWarning($"No such file: {path} does not exist");
                return null;
            }

            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, true, linear);
            texture.LoadImage(bytes);
            return texture;
        }

        public static Sprite LoadSprite(string path)
        {
            Texture2D texture = LoadTexture(path);
            if (null == texture) { return null; }
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            return sprite;
        }

        public static void FillTexture(Texture2D texture, Color color)
        {
            Color[] pixels = texture.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply();
        }

        public static void DeleteTransformChildren(Transform trans)
        {
            Debug.Log("Clear scene");
            Selection.ClearSelection();

            // sync reparent before async destroy
            GameObject tmp = new GameObject();
            for (int i = trans.childCount - 1; i >= 0; i--)
            {
                Transform child = trans.GetChild(i);
                if (child.name.StartsWith("__VRtist_"))
                    continue;

                child.parent = tmp.transform;
            }

            GameObject.Destroy(tmp);
        }

        public static void CreatePath(string path)
        {
            string filename = Path.GetFileName(path);
            DirectoryInfo folder;
            if (filename.Length > 0)
                folder = Directory.GetParent(path);
            else
                folder = new DirectoryInfo(path);
            if (!folder.Exists)
            {
                folder.Create();
            }
        }

        public static void Reparent(Transform t, Transform parent)
        {
            Vector3 position = t.localPosition;
            Quaternion rotation = t.localRotation;
            Vector3 scale = t.localScale;

            t.parent = parent;
            t.localPosition = position;
            t.localRotation = rotation;
            t.localScale = scale;
        }
    }
}
