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

using TMPro;

using UnityEditor;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace VRtist
{
    [ExecuteInEditMode]
    [SelectionBase]
    public class UIGrabber : UIElement
    {
        [HideInInspector] public int? uid;
        public bool rotateOnHover = true;
        public GameObject prefab;

        [SpaceHeader("Callbacks", 6, 0.8f, 0.8f, 0.8f)]
        public GameObjectHashChangedEvent onEnterUI3DObject = new GameObjectHashChangedEvent();
        public GameObjectHashChangedEvent onExitUI3DObject = new GameObjectHashChangedEvent();
        public UnityEvent onHoverEvent = new UnityEvent();
        public UnityEvent onClickEvent = new UnityEvent();
        public UnityEvent onReleaseEvent = new UnityEvent();

        private static GameObject textThumbnailPrefab;
        private static GameObject imageThumbnailPrefab;

        private static Quaternion thumbnailRotation = Quaternion.Euler(25f, -35f, 0f);

        private string lazyImagePath = null;
        private bool lazyLoaded = false;
        public bool isValid = true;

        void Start()
        {
            if (prefab)
            {
                if (ToolsUIManager.Instance != null)
                {
                    ToolsUIManager.Instance.RegisterUI3DObject(prefab);
                    uid = prefab.GetHashCode();
                    transform.localRotation = thumbnailRotation;
                }
            }

            // Load thumbnail prefabs
            if (null == textThumbnailPrefab)
            {
                textThumbnailPrefab = Resources.Load<GameObject>("Prefabs/UI/AssetBankGenericItem");
                imageThumbnailPrefab = Resources.Load<GameObject>("Prefabs/UI/AssetBankImageItem");
            }
        }

        private void OnValidate()
        {
            NeedsRebuild = true;
        }

        private void OnEnable()
        {
            // Load lazy thumbnail
            if (null != lazyImagePath && lazyImagePath != "" && !lazyLoaded)
            {
                LoadThumbnail(lazyImagePath);
                lazyLoaded = true;
            }
        }

        private void Update()
        {
            if (NeedsRebuild)
            {
                UpdateLocalPosition();
                ResetColor();
                NeedsRebuild = false;
            }
        }

        public override void ResetColor()
        {
            base.ResetColor();
        }

        // Handles multi-mesh and multi-material per mesh.
        public override void SetColor(Color color)
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
#else
            if (Application.isPlaying)
#endif
            {
                MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer meshRenderer in meshRenderers)
                {
                    Material[] materials = meshRenderer.materials;
                    foreach (Material material in meshRenderer.materials)
                    {
                        material.SetColor("_BaseColor", color);
                    }
                }
            }
        }

        #region Create Thumbnail helpers
        static void LoadPrefabs()
        {
            // Load thumbnail prefabs
            if (null == textThumbnailPrefab)
            {
                textThumbnailPrefab = Resources.Load<GameObject>("Prefabs/UI/AssetBankGenericItem");
                imageThumbnailPrefab = Resources.Load<GameObject>("Prefabs/UI/AssetBankImageItem");
            }
        }

        public static GameObject CreateTextThumbnail(string text, UnityAction<int> onEnter, UnityAction<int> onExit)
        {
            LoadPrefabs();
            GameObject thumbnail = Instantiate(textThumbnailPrefab);
            thumbnail.transform.Find("Canvas/Panel/Name").GetComponent<TextMeshProUGUI>().text = text;
            UIGrabber uiGrabber = thumbnail.GetComponent<UIGrabber>();
            uiGrabber.uid = thumbnail.GetHashCode();
            uiGrabber.rotateOnHover = false;
            uiGrabber.onEnterUI3DObject.AddListener(onEnter);
            uiGrabber.onExitUI3DObject.AddListener(onExit);
            return thumbnail;
        }

        public static GameObject CreateImageThumbnail(Sprite image, UnityAction<int> onEnter, UnityAction<int> onExit)
        {
            LoadPrefabs();
            GameObject thumbnail = Instantiate(imageThumbnailPrefab);
            thumbnail.transform.Find("Canvas/Panel/Image").GetComponent<Image>().sprite = image;
            UIGrabber uiGrabber = thumbnail.GetComponent<UIGrabber>();
            uiGrabber.uid = thumbnail.GetHashCode();
            uiGrabber.rotateOnHover = false;
            uiGrabber.onEnterUI3DObject.AddListener(onEnter);
            uiGrabber.onExitUI3DObject.AddListener(onExit);
            return thumbnail;
        }

        public static GameObject CreateLazyImageThumbnail(string path, UnityAction<int> onEnter, UnityAction<int> onExit)
        {
            LoadPrefabs();
            GameObject thumbnail = Instantiate(imageThumbnailPrefab);
            UIGrabber uiGrabber = thumbnail.GetComponent<UIGrabber>();
            uiGrabber.lazyImagePath = path;
            uiGrabber.uid = thumbnail.GetHashCode();
            uiGrabber.rotateOnHover = false;
            uiGrabber.onEnterUI3DObject.AddListener(onEnter);
            uiGrabber.onExitUI3DObject.AddListener(onExit);
            return thumbnail;
        }

        private void LoadThumbnail(string path)
        {
            Sprite sprite = Utils.LoadSprite(path);
            if (null == sprite)
            {
                sprite = UIUtils.LoadIcon("warning");
                isValid = false;
            }
            transform.Find("Canvas/Panel/Image").GetComponent<Image>().sprite = sprite;
        }

        public static GameObject Create3DThumbnail(GameObject thumbnail, UnityAction<int> onEnter, UnityAction<int> onExit)
        {
            UIGrabber uiGrabber = thumbnail.GetComponent<UIGrabber>();
            if (null == uiGrabber)
            {
                uiGrabber = thumbnail.AddComponent<UIGrabber>();
            }
            uiGrabber.uid = thumbnail.GetHashCode();
            uiGrabber.rotateOnHover = true;
            uiGrabber.onEnterUI3DObject.AddListener(onEnter);
            uiGrabber.onExitUI3DObject.AddListener(onExit);
            thumbnail.transform.localRotation = thumbnailRotation;

            MeshRenderer[] meshRenderers = thumbnail.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            return thumbnail;
        }
        #endregion

        #region ray
        public override void OnRayEnter()
        {
            base.OnRayEnter();

            GoFrontAnimation();

            if (uid != null)
            {
                onEnterUI3DObject.Invoke((int)uid);
            }

            WidgetBorderHapticFeedback();
        }

        public override void OnRayEnterClicked()
        {
            base.OnRayEnterClicked();

            GoFrontAnimation();

            if (uid != null)
            {
                onEnterUI3DObject.Invoke((int)uid);
            }

            WidgetBorderHapticFeedback();
        }

        public override void OnRayHover(Ray ray)
        {
            base.OnRayHover(ray);

            onHoverEvent.Invoke();

            if (rotateOnHover) { RotateAnimation(); }
        }

        public override void OnRayHoverClicked()
        {
            base.OnRayHoverClicked();

            onHoverEvent.Invoke();

            if (rotateOnHover) { RotateAnimation(); }
        }

        public override void OnRayExit()
        {
            base.OnRayExit();

            GoBackAnimation();

            if (rotateOnHover) { ResetRotation(); }

            if (uid != null)
            {
                onExitUI3DObject.Invoke((int)uid);
            }

            WidgetBorderHapticFeedback();
        }

        public override void OnRayExitClicked()
        {
            base.OnRayExitClicked();

            GoBackAnimation();

            if (uid != null)
            {
                onExitUI3DObject.Invoke((int)uid);
            }

            WidgetBorderHapticFeedback();
        }

        public override void OnRayClick()
        {
            base.OnRayClick();
            onClickEvent.Invoke();
        }

        public override void OnRayReleaseInside()
        {
            base.OnRayReleaseInside();
            onReleaseEvent.Invoke();
        }

        public override bool OnRayReleaseOutside()
        {
            if (rotateOnHover) { ResetRotation(); }
            return base.OnRayReleaseOutside();
        }

        public void GoFrontAnimation()
        {
            transform.localPosition += new Vector3(0f, 0f, -0.02f); // avance vers nous, dnas le repere de la page (local -Z)
            transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        }

        public void GoBackAnimation()
        {
            transform.localPosition += new Vector3(0f, 0f, +0.02f); // recule, dnas le repere de la page (local +Z)
            transform.localScale = Vector3.one;
        }

        public void RotateAnimation()
        {
            transform.localRotation *= Quaternion.Euler(0f, -3f, 0f); // rotate autour du Y du repere du parent (penche a 25, -35, 0)
        }

        public void ResetRotation()
        {
            if (rotateOnHover)
            {
                transform.localRotation = thumbnailRotation;
            }
        }

        #endregion
    }
}
