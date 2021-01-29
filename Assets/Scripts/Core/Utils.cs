using System;
using System.IO;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace VRtist
{
    public class Utils
    {
        public static GameObject FindRootGameObject(string name)
        {
            Scene scene = SceneManager.GetActiveScene();
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

        public static GameObject CreatePaint(Transform parent, Color color)
        {
            GameObject intermediateParent = new GameObject();
            intermediateParent.transform.parent = parent;

            GameObject paint = new GameObject();
            paint.transform.parent = intermediateParent.transform;
            paint.name = SyncData.CreateUniqueName("Paint");
            intermediateParent.name = paint.name + "_parent";

            paint.transform.localPosition = Vector3.zero;
            paint.transform.localRotation = Quaternion.identity;
            paint.transform.localScale = Vector3.one;
            paint.tag = "PhysicObject";

            Mesh mesh = new Mesh();
            MeshFilter meshFilter = paint.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            MeshRenderer renderer = paint.AddComponent<MeshRenderer>();
            Material paintMaterial = ResourceManager.GetMaterial(MaterialID.ObjectOpaque);
            renderer.sharedMaterial = paintMaterial;
            renderer.material.SetColor("_BaseColor", color);

            // Update Mixer (TODO: have a VRtist API to do that, not directly Mixer)
            MaterialParameters parameters = new MaterialParameters
            {
                materialType = MaterialID.ObjectOpaque,
                baseColor = color
            };
            MixerUtils.materialsParameters[SyncData.GetMaterialName(paint)] = parameters;

            paint.AddComponent<MeshCollider>();
            paint.AddComponent<PaintController>();

            return paint;
        }

        public static GameObject CreateVolume(Transform parent, Color color)
        {
            GameObject intermediateParent = new GameObject();
            intermediateParent.transform.parent = parent;

            GameObject volume = new GameObject();
            volume.transform.parent = intermediateParent.transform;
            volume.name = SyncData.CreateUniqueName("Volume");
            intermediateParent.name = volume.name + "_parent";

            volume.transform.localPosition = Vector3.zero;
            volume.transform.localRotation = Quaternion.identity;
            volume.transform.localScale = Vector3.one;
            volume.tag = "PhysicObject";

            Mesh mesh = new Mesh
            {
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };
            MeshFilter meshFilter = volume.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            MeshRenderer renderer = volume.AddComponent<MeshRenderer>();
            Material volumeMaterial = ResourceManager.GetMaterial(MaterialID.ObjectOpaque); // TODO: another specific material??
            renderer.sharedMaterial = volumeMaterial;
            renderer.material.SetColor("_BaseColor", color);

            // Update Mixer (TODO: have a VRtist API to do that, not directly Mixer)
            MaterialParameters parameters = new MaterialParameters
            {
                materialType = MaterialID.ObjectOpaque,
                baseColor = color
            };
            MixerUtils.materialsParameters[SyncData.GetMaterialName(volume)] = parameters;

            volume.AddComponent<MeshCollider>();
            volume.AddComponent<VolumeController>();

            return volume;
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

        public static UnityEvent clearScene = new UnityEvent();
        public static void ClearScene()
        {
            clearScene.Invoke();

            Transform root = GlobalState.Instance.world.Find("RightHanded");
            DeleteTransformChildren(root);
            DeleteTransformChildren(SyncData.prefab);
        }

        public static void DeleteTransformChildren(Transform trans)
        {
            Debug.Log("Clear scene");
            Selection.ClearSelection();
            foreach (Transform child in trans)
            {
                if (child.name.StartsWith("__VRtist_"))
                {
                    // There are some game objects that are not user objects and must remain
                    continue;
                }
                Debug.Log("Destroying " + child.name);
                GameObject.Destroy(child.gameObject);
            }
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
    }
}
