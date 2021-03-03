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

using UnityEngine;

namespace VRtist.Mixer
{
    public class MixerUser : User
    {
        public string viewId;
        public string masterId;
        public Vector3[] corners = new Vector3[4];
    }

    // Commands data types
    public class FrameStartEnd
    {
        public int start;
        public int end;
    }
    public class MontageModeInfo
    {
        public bool montage;
    }

    // Grease Pencil related classes
    public class GPLayer
    {
        public GPLayer(string _)
        {
        }
        public List<GPFrame> frames = new List<GPFrame>();
        public bool visible;
    }
    public class GPFrame
    {
        public GPFrame(int f)
        {
            frame = f;
        }
        public List<GPStroke> strokes = new List<GPStroke>();
        public int frame;
    }
    public class GPStroke
    {
        public Vector3[] vertices;
        public int[] triangles;
        public MaterialParameters materialParameters;
    }

    public class GreasePencilData
    {
        public Dictionary<int, Tuple<Mesh, List<MaterialParameters>>> meshes = new Dictionary<int, Tuple<Mesh, List<MaterialParameters>>>();
        public int frameOffset = 0;
        public float frameScale = 1f;
        public bool hasCustomRange = false;
        public int rangeStartFrame;
        public int rangeEndFrame;

        public void AddMesh(int frame, Tuple<Mesh, List<MaterialParameters>> mesh)
        {
            meshes[frame] = mesh;
        }
    }

    public class AssignMaterialInfo
    {
        public string objectName;
        public string materialName;
    }
    public class CameraInfo
    {
        public Transform transform;
    }
    public class LightInfo
    {
        public Transform transform;
    }

    public class AddToCollectionInfo
    {
        public string collectionName;
        public Transform transform;
    }

    public class AddObjectToSceneInfo
    {
        public Transform transform;
    }

    public class RenameInfo
    {
        public Transform srcTransform;
        public string newName;
    }

    public class DuplicateInfos
    {
        public GameObject srcObject;
        public GameObject dstObject;
    }

    public class MeshInfos
    {
        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;
        public Transform meshTransform;
    }

    public class DeleteInfo
    {
        public Transform meshTransform;
    }

    public class SendToTrashInfo
    {
        public Transform transform;
    }
    public class RestoreFromTrashInfo
    {
        public Transform transform;
        public Transform parent;
    }
    public class ClearAnimationInfo
    {
        public GameObject gObject;
    }

    // Blender Asset Bank
    public enum BlenderBankAction
    {
        ListRequest,
        ListResponse,
        ImportRequest,
        ImportResponse
    }

    public class BlenderBankInfo
    {
        public BlenderBankAction action;
        public string name;
    }

    // Animation
    public class SetKeyInfo
    {
        public string objectName;
        public AnimatableProperty property;
        public AnimationKey key;
    };

    public class MoveKeyInfo
    {
        public string objectName;
        public AnimatableProperty property;
        public int frame;
        public int newFrame;
    }

    public class MaterialParameters
    {
        public string name;
        public MaterialID materialType;
        public float opacity;
        public string opacityTexturePath = "";
        public Color baseColor;
        public string baseColorTexturePath = "";
        public float metallic;
        public string metallicTexturePath = "";
        public float roughness;
        public string roughnessTexturePath = "";
        public string normalTexturePath = "";
        public Color emissionColor;
        public string emissionColorTexturePath = "";
    }
}
