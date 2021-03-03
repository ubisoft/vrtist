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
            SpawnVFX(source, duplicateVFXPrefab, 1.5f);
        }


        public void SpawnDeleteVFX(GameObject source) {
            SpawnVFX(source, deleteVFXPrefab, 1f);
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
            if(positionList.Count == 0) { return; }

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
                    if(mesh.isReadable)
                    {
                        positionList.AddRange(mesh.vertices);
                        normalList.AddRange(mesh.normals);
                    }
                }
            }

            // Try to get data from mesh filter
            else {
                MeshFilter[] meshFilters = source.GetComponentsInChildren<MeshFilter>();
                if(meshFilters.Length > 0) {
                    positionList.Clear();
                    normalList.Clear();
                    foreach(MeshFilter meshFilter in meshFilters) {
                        if(meshFilter.mesh.isReadable)
                        {
                            positionList.AddRange(meshFilter.mesh.vertices);
                            normalList.AddRange(meshFilter.mesh.normals);
                        }
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
            visualEffect.SetVector3("SourcePosition", source.transform.position);
        }
    }
}
