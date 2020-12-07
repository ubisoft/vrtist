using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
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

    [Serializable]
    public class SkySettings
    {
        public Color topColor;
        public Color middleColor;
        public Color bottomColor;
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

    // Shot manager
    public enum ShotManagerAction
    {
        AddShot = 0,
        DeleteShot,
        DuplicateShot,
        MoveShot,
        UpdateShot
    }


    public class ShotManagerActionInfo
    {
        public ShotManagerAction action;
        public int shotIndex = 0;
        public string shotName = "";
        public int shotStart = -1;
        public int shotEnd = -1;
        public string cameraName = "";
        public Color shotColor = Color.black;
        public int moveOffset = 0;
        public int shotEnabled = -1;

        public ShotManagerActionInfo Copy()
        {
            return new ShotManagerActionInfo()
            {
                action = action,
                shotIndex = shotIndex,
                shotName = shotName,
                shotStart = shotStart,
                shotEnd = shotEnd,
                cameraName = cameraName,
                shotColor = shotColor,
                moveOffset = moveOffset,
                shotEnabled = shotEnabled
            };
        }
    }
    public class Shot
    {
        public string name;
        public GameObject camera = null; // TODO, manage game object destroy
        public int start = -1;
        public int end = -1;
        public bool enabled = true;
        public Color color = Color.black;

        public Shot Copy()
        {
            return new Shot { name = name, camera = camera, start = start, end = end, enabled = enabled, color = color };
        }
    }

    // Blender Asset Bank
    public enum BlenderBankAction
    {
        List,
        Import
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

    public class ConnectedUser
    {
        public string id;  // clientId
        public string viewId;
        public string masterId;
        public string name;
        public string room;
        public Vector3 eye;
        public Vector3 target;
        public Color color;
        public Vector3[] corners = new Vector3[4];
    }


    // Material classes
    public enum MaterialType
    {
        Opaque,
        Transparent,
        EditorOpaque,
        EditorTransparent,
        GreasePencil,
        Paint,
    }

    public class MaterialParameters
    {
        public string name;
        public MaterialType materialType;
        public float opacity;
        public string opacityTexturePath;
        public Color baseColor;
        public string baseColorTexturePath;
        public float metallic;
        public string metallicTexturePath;
        public float roughness;
        public string roughnessTexturePath;
        public string normalTexturePath;
        public Color emissionColor;
        public string emissionColorTexturePath;
    }
}
