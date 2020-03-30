using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;


namespace VRtist {

    public class SelectionVFX : MonoBehaviour {
        public ComputeShader computeShader = null;  // compute shader to do stuff on the GPU
        public GameObject duplicateVFXPrefab;
        public GameObject deleteVFXPrefab;
        
        private GameObject source = null;  // The source object from which to create the VFX

        private RenderTexture positionMap = null;  // position map for particles
        private RenderTexture normalMap = null;    // normal map for particles
        private RenderTexture velocityMap = null;  // velocity map for particles

        Matrix4x4 previousTransform = Matrix4x4.identity;  // to compute velocity
        
        List<Vector3> positionList = new List<Vector3>();
        List<Vector3> normalList = new List<Vector3>();

        ComputeBuffer positionBuffer1;  // we will switch compute buffers for previous data
        ComputeBuffer positionBuffer2;  // we use them for computing velocity
        ComputeBuffer normalBuffer;

        RenderTexture tempPositionMap;  // results from the compute shader
        RenderTexture tempVelocityMap;
        RenderTexture tempNormalMap;

        GameObject vfx;                 // current VFX
        VisualEffect visualEffect;      // same
        float duration;
        float elapsedTime = 0f;


        private void Start() {
            // Be sure to create size maps multiple of 8
            positionMap = Utils.CreateRenderTexture(512, 512, 0, RenderTextureFormat.ARGBHalf, false);
            normalMap = Utils.CreateRenderTexture(512, 512, 0, RenderTextureFormat.ARGBHalf, false);
            velocityMap = Utils.CreateRenderTexture(512, 512, 0, RenderTextureFormat.ARGBHalf, false);
        }


        private void SpawnVFX(GameObject source, GameObject vfxPrefab, float duration) {
            this.duration = duration;
            this.source = source;
            vfx = Instantiate(vfxPrefab);
            vfx.transform.parent = transform.parent;
            vfx.transform.localPosition = Vector3.zero;
            vfx.transform.localRotation = Quaternion.identity;
            vfx.transform.localScale = Vector3.one;

            visualEffect = vfx.GetComponent<VisualEffect>();

            Destroy(vfx, duration + 1f);
            Destroy(gameObject, duration + 1f);
        }


        public void SpawnDuplicateVFX(GameObject source) {
            SpawnVFX(source, duplicateVFXPrefab, 3f);
        }


        public void SpawnDeleteVFX(GameObject source) {
            SpawnVFX(source, deleteVFXPrefab, 2f);
        }


        private void OnDestroy() {
            Utils.TryDispose(positionBuffer1);
            Utils.TryDispose(positionBuffer2);
            Utils.TryDispose(normalBuffer);

            Utils.TryDestroy(tempPositionMap);
            Utils.TryDestroy(tempNormalMap);
            Utils.TryDestroy(tempVelocityMap);

            positionBuffer1 = null;
            positionBuffer2 = null;
            normalBuffer = null;

            tempPositionMap = null;
            tempNormalMap = null;
            tempVelocityMap = null;
        }


        private void Update() {
            if(null == source) { return; }

            elapsedTime += Time.deltaTime;

            GetData();
            TransferData();
            UpdateVFX();

            Utils.SwapBuffers(ref positionBuffer1, ref positionBuffer2);
            previousTransform = source.transform.localToWorldMatrix;
        }
        

        private void GetData() {
            // Try to get data from skinned mesh
            SkinnedMeshRenderer[] renderers = source.GetComponentsInChildren<SkinnedMeshRenderer>();
            if(renderers.Length > 0) {
                positionList.Clear();
                normalList.Clear();
                Mesh mesh = new Mesh();
                foreach(SkinnedMeshRenderer renderer in renderers) {
                    renderer.BakeMesh(mesh);
                    positionList.AddRange(mesh.vertices);
                    normalList.AddRange(mesh.normals);
                }
            }

            // Try to get data from mesh filter
            else {
                MeshFilter[] meshFilters = source.GetComponentsInChildren<MeshFilter>();
                if(meshFilters.Length > 0) {
                    positionList.Clear();
                    normalList.Clear();
                    foreach(MeshFilter meshFilter in meshFilters) {
                        positionList.AddRange(meshFilter.mesh.vertices);
                        normalList.AddRange(meshFilter.mesh.normals);
                    }
                }
            }
        }


        public void TransferData() {
            int mapWidth = positionMap.width;
            int mapHeight = positionMap.height;

            int vertexCount = positionList.Count;
            int dataCount = vertexCount * 3;

            // Release temp objects if their data size doesn't match
            if(null != positionBuffer1 && positionBuffer1.count != dataCount) {
                positionBuffer1.Dispose();
                positionBuffer2.Dispose();
                normalBuffer.Dispose();
                positionBuffer1 = null;
                positionBuffer2 = null;
                normalBuffer = null;
            }

            if(null != tempPositionMap && (tempPositionMap.width != mapWidth || tempPositionMap.height != mapHeight)) {
                Destroy(tempPositionMap);
                Destroy(tempVelocityMap);
                Destroy(tempNormalMap);
                tempPositionMap = null;
                tempVelocityMap = null;
                tempNormalMap = null;
            }

            // Lazy initialization
            if(null == positionBuffer1) {
                positionBuffer1 = new ComputeBuffer(dataCount, sizeof(float));
                positionBuffer2 = new ComputeBuffer(dataCount, sizeof(float));
                normalBuffer = new ComputeBuffer(dataCount, sizeof(float));
            }

            if(null == tempPositionMap) {
                tempPositionMap = Utils.CreateRenderTexture(positionMap);
                tempVelocityMap = Utils.CreateRenderTexture(positionMap);
                tempNormalMap = Utils.CreateRenderTexture(positionMap);
            }

            // Set data
            computeShader.SetInt("VertexCount", vertexCount);
            computeShader.SetMatrix("Transform", source.transform.localToWorldMatrix);
            computeShader.SetMatrix("OldTransform", previousTransform);
            computeShader.SetFloat("FrameRate", 1f / Time.deltaTime);

            positionBuffer1.SetData(positionList);
            normalBuffer.SetData(normalList);

            computeShader.SetBuffer(0, "PositionBuffer", positionBuffer1);
            computeShader.SetBuffer(0, "OldPositionBuffer", positionBuffer2);
            computeShader.SetBuffer(0, "NormalBuffer", normalBuffer);

            computeShader.SetTexture(0, "PositionMap", tempPositionMap);
            computeShader.SetTexture(0, "VelocityMap", tempVelocityMap);
            computeShader.SetTexture(0, "NormalMap", tempNormalMap);

            // Compute
            computeShader.Dispatch(0, mapWidth / 8, mapHeight / 8, 1);

            // Retrieve data
            Graphics.CopyTexture(tempPositionMap, positionMap);
            Graphics.CopyTexture(tempVelocityMap, velocityMap);
            Graphics.CopyTexture(tempNormalMap, normalMap);
        }


        private void UpdateVFX() {
            visualEffect.SetTexture("PointCache", positionMap);
            visualEffect.SetFloat("Factor", Mathf.Abs(source.transform.lossyScale.x));
            visualEffect.SetFloat("RemainingTime", duration - elapsedTime);
        }

    }

    /*public class SelectionVFX_old : MonoBehaviour {
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
                pointCache = new Texture2D(1, Mathf.Min(normalizedVertices.Count, SystemInfo.maxTextureSize), TextureFormat.RGBA32, false, true);
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
    }*/
}
