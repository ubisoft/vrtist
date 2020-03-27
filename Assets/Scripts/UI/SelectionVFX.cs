using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;


namespace VRtist {

    public class SelectionVFX : MonoBehaviour {
        private static GameObject duplicateVFXPrefab;
        private static GameObject deleteVFXPrefab;

        private const float duplicateVFXTime = 3f;
        private const float deleteVFXTime = 2f;

        private Texture2D pointCache;
        private GameObject vfx;
        private bool vfxRunning = false;

        private float boundingBoxSize;


        public void SpawnDuplicateVFX() {
            if(!vfxRunning) {
                if(null == duplicateVFXPrefab) {
                    duplicateVFXPrefab = Resources.Load<GameObject>("VFX/Duplicate");
                }
                StartCoroutine(RunVFX(duplicateVFXPrefab, duplicateVFXTime, true));
            }
        }


        public void SpawnDeleteVFX() {
            if(!vfxRunning) {
                if(null == deleteVFXPrefab) {
                    deleteVFXPrefab = Resources.Load<GameObject>("VFX/Delete");
                }
                StartCoroutine(RunVFX(deleteVFXPrefab, deleteVFXTime, false));
            }
        }


        private void UpdateBoundingBoxSize(GameObject gobj) {
            // Uniform axis-aligned bounding box in local space (no transforms applied)
            MeshFilter[] meshFilters = gobj.GetComponentsInChildren<MeshFilter>();
            Bounds bounds = new Bounds();
            foreach(MeshFilter meshFilter in meshFilters) {
                bounds.Encapsulate(meshFilter.mesh.bounds);
            }
            boundingBoxSize = Mathf.Max(bounds.extents.x * 2, bounds.extents.y * 2, bounds.extents.z * 2);
        }


        private void UpdatePointCache(GameObject gobj) {
            Vector3[] vertices;
            MeshFilter[] meshFilters = gobj.GetComponentsInChildren<MeshFilter>();
            List<Color> normalizedVertices = new List<Color>();  // color auto-clamp
            foreach(MeshFilter meshFilter in meshFilters) {
                vertices = meshFilter.mesh.vertices;
                for(int i = 0; i < vertices.Length; ++i) {
                    // Transform world-space vertex coordinates to normalized vertex position in the bounding box
                    vertices[i] = (gobj.transform.InverseTransformPoint(meshFilter.gameObject.transform.TransformPoint(vertices[i])) + new Vector3(boundingBoxSize * 0.5f, 0f, boundingBoxSize * 0.5f)) / boundingBoxSize;
                    normalizedVertices.Add(new Color(vertices[i].x, vertices[i].y, vertices[i].z));
                }
            }
            if(null == pointCache || pointCache.width != normalizedVertices.Count) {
                pointCache = new Texture2D(1, normalizedVertices.Count, TextureFormat.RGBA32, false, true);
                pointCache.filterMode = FilterMode.Point;
            }
            pointCache.SetPixels(normalizedVertices.ToArray());
            pointCache.Apply();
        }


        private IEnumerator RunVFX(GameObject prefab, float waitTime, bool toParent) {
            vfxRunning = true;

            vfx = Instantiate(prefab);
            if(toParent) {
                vfx.transform.parent = transform;
                vfx.transform.localPosition = Vector3.zero;
                vfx.transform.localRotation = Quaternion.identity;
                vfx.transform.localScale = Vector3.one;
            } else {
                vfx.transform.position = transform.position;
                vfx.transform.rotation = transform.rotation;
                vfx.transform.localScale = transform.lossyScale;  // local = lossy: ok
            }

            UpdateBoundingBoxSize(gameObject);
            UpdatePointCache(gameObject);
            VisualEffect visualEffect = vfx.GetComponent<VisualEffect>();
            visualEffect.SetFloat("Factor", Mathf.Abs(transform.lossyScale.x));  // RightHanded has scale.x set to -1
            visualEffect.SetTexture("PointCache", pointCache);
            visualEffect.SetFloat("BoundingBoxSize", boundingBoxSize);

            yield return new WaitForSeconds(waitTime);

            Destroy(vfx);
            vfxRunning = false;
        }
    }
}
