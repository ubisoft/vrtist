using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

namespace VRtist
{

    public enum MessageType
    {
        JoinRoom = 1,
        CreateRoom,
        LeaveRoom,
        ListRooms,
        Content,
        ClearContent,
        DeleteRoom,
        ClearRoom,
        ListRoomClients,
        _ListClients, // deprecated
        SetClientName,
        SendError,
        ConnectionLost,
        ListAllClients,

        SetClientCustomAttribute,
        _SetRoomCustomAttribute,
        _SetRoomKeepOpen,
        ClientId,
        ClientUpdate,
        _RoomUpdate,
        _RoomDeleted,

        Command = 100,
        Delete,
        Camera,
        Light,
        MeshConnection_Deprecated,
        Rename,
        Duplicate,
        SendToTrash,
        RestoreFromTrash,
        Texture,
        AddCollectionToCollection,
        RemoveCollectionFromCollection,
        AddObjectToCollection,
        RemoveObjectFromCollection,
        AddObjectToScene,
        AddCollectionToScene,
        CollectionInstance,
        Collection,
        CollectionRemoved,
        SetScene,
        GreasePencilMesh,
        GreasePencilMaterial,
        GreasePencilConnection,
        GreasePencilTimeOffset,
        FrameStartEnd,
        Animation,
        RemoveObjectFromScene,
        RemoveCollectionFromScene,
        Scene,
        SceneRemoved,
        AddObjectToDocument,
        ObjectVisibility,
        _GroupBegin,
        _GroupEnd,
        _SceneRenamed,
        AddKeyframe,
        RemoveKeyframe,
        MoveKeyframe,
        _QueryCurrentFrame,
        QueryAnimationData,
        _BlenderDataUpdate,
        CameraAttributes,
        LightAttributes,
        _BlenderDataRemove,
        _BlenderDataRename,
        ClearAnimations,
        CurrentCamera,
        ShotManagerMontageMode,
        ShotManagerContent,
        ShotManagerCurrentShot,
        ShotManagerAction,
        _BlenderDataCreate,

        Optimized_Commands = 200,
        Transform,
        Mesh,
        Material,
        AssignMaterial,
        Frame,
        Play,
        Pause,
        Sky,

        End_Optimized_Commands = 999,
        ClientIdWrapper = 1000
    }

    public class NetCommand
    {
        public byte[] data;
        public MessageType messageType;
        public int id;

        public NetCommand()
        {
        }
        public NetCommand(byte[] d, MessageType mtype, int mid = 0)
        {
            data = d;
            messageType = mtype;
            id = mid;
        }
    }

    public class GPLayer
    {
        public GPLayer(string n)
        {
            name = n;
        }
        public List<GPFrame> frames = new List<GPFrame>();
        string name;
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

    public class NetGeometry
    {
        public static Dictionary<string, Material> materials = new Dictionary<string, Material>();

        public static Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        public static Dictionary<string, List<MaterialParameters>> meshesMaterials = new Dictionary<string, List<MaterialParameters>>();

        public static Dictionary<MaterialType, Material> baseMaterials = new Dictionary<MaterialType, Material>();
        public static Dictionary<string, MaterialParameters> materialsParameters = new Dictionary<string, MaterialParameters>();

        public static Dictionary<string, GreasePencilData> greasePencils = new Dictionary<string, GreasePencilData>();
        public static HashSet<string> materialsFillEnabled = new HashSet<string>();
        public static HashSet<string> materialStrokesEnabled = new HashSet<string>();
        public static Dictionary<string, Dictionary<string, int>> greasePencilLayerIndices = new Dictionary<string, Dictionary<string, int>>();

        public static Dictionary<string, byte[]> textureData = new Dictionary<string, byte[]>();
        public static Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        public static HashSet<string> texturesFlipY = new HashSet<string>();

        public static byte[] StringsToBytes(string[] values, bool storeSize = true)
        {
            int size = sizeof(int);
            for (int i = 0; i < values.Length; i++)
            {
                byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(values[i]);
                size += sizeof(int) + utf8.Length;
            }


            byte[] bytes = new byte[size];
            int index = 0;
            if (storeSize)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(values.Length), 0, bytes, 0, sizeof(int));
                index += sizeof(int);
            }
            for (int i = 0; i < values.Length; i++)
            {
                string value = values[i];
                byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(value);
                Buffer.BlockCopy(BitConverter.GetBytes(utf8.Length), 0, bytes, index, sizeof(int));
                Buffer.BlockCopy(utf8, 0, bytes, index + sizeof(int), value.Length);
                index += sizeof(int) + value.Length;
            }
            return bytes;
        }

        public static byte[] StringToBytes(string value)
        {
            byte[] bytes = new byte[sizeof(int) + value.Length];
            byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(value);
            Buffer.BlockCopy(BitConverter.GetBytes(utf8.Length), 0, bytes, 0, sizeof(int));
            Buffer.BlockCopy(utf8, 0, bytes, sizeof(int), value.Length);
            return bytes;
        }

        public static byte[] TriangleIndicesToBytes(int[] vectors)
        {
            byte[] bytes = new byte[sizeof(int) * vectors.Length + sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(vectors.Length / 3), 0, bytes, 0, sizeof(int));
            int index = sizeof(int);
            for (int i = 0; i < vectors.Length; i++)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(vectors[i]), 0, bytes, index, sizeof(int));
                index += sizeof(int);
            }
            return bytes;
        }

        public static byte[] Vector3ToBytes(Vector3[] vectors)
        {
            byte[] bytes = new byte[3 * sizeof(float) * vectors.Length + sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(vectors.Length), 0, bytes, 0, sizeof(int));
            int index = sizeof(int);
            for (int i = 0; i < vectors.Length; i++)
            {
                Vector3 vector = vectors[i];
                Buffer.BlockCopy(BitConverter.GetBytes(vector.x), 0, bytes, index + 0, sizeof(float));
                Buffer.BlockCopy(BitConverter.GetBytes(vector.y), 0, bytes, index + sizeof(float), sizeof(float));
                Buffer.BlockCopy(BitConverter.GetBytes(vector.z), 0, bytes, index + 2 * sizeof(float), sizeof(float));
                index += 3 * sizeof(float);
            }
            return bytes;
        }

        public static Color GetColor(byte[] data, ref int currentIndex)
        {
            float[] buffer = new float[4];
            int size = 4 * sizeof(float);
            Buffer.BlockCopy(data, currentIndex, buffer, 0, size);
            currentIndex += size;
            return new Color(buffer[0], buffer[1], buffer[2], buffer[3]);
        }

        public static byte[] ColorToBytes(Color color)
        {
            byte[] bytes = new byte[4 * sizeof(float)];

            Buffer.BlockCopy(BitConverter.GetBytes(color.r), 0, bytes, 0, sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(color.g), 0, bytes, sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(color.b), 0, bytes, 2 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(color.a), 0, bytes, 3 * sizeof(float), sizeof(float));
            return bytes;
        }

        public static Vector3 GetVector3(byte[] data, ref int currentIndex)
        {
            float[] buffer = new float[3];
            int size = 3 * sizeof(float);
            Buffer.BlockCopy(data, currentIndex, buffer, 0, size);
            currentIndex += size;
            return new Vector3(buffer[0], buffer[1], buffer[2]);
        }

        public static byte[] Vector3ToBytes(Vector3 vector)
        {
            byte[] bytes = new byte[3 * sizeof(float)];

            Buffer.BlockCopy(BitConverter.GetBytes(vector.x), 0, bytes, 0, sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(vector.y), 0, bytes, sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(vector.z), 0, bytes, 2 * sizeof(float), sizeof(float));
            return bytes;
        }

        public static Vector3 GetVector4(byte[] data, ref int currentIndex)
        {
            float[] buffer = new float[4];
            int size = 4 * sizeof(float);
            Buffer.BlockCopy(data, currentIndex, buffer, 0, size);
            currentIndex += size;
            return new Vector4(buffer[0], buffer[1], buffer[2], buffer[3]);
        }

        public static byte[] Vector4ToBytes(Vector4 vector)
        {
            byte[] bytes = new byte[4 * sizeof(float)];

            Buffer.BlockCopy(BitConverter.GetBytes(vector.x), 0, bytes, 0 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(vector.y), 0, bytes, 1 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(vector.z), 0, bytes, 2 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(vector.w), 0, bytes, 3 * sizeof(float), sizeof(float));
            return bytes;
        }

        public static Vector2 GetVector2(byte[] data, ref int currentIndex)
        {
            float[] buffer = new float[2];
            int size = 2 * sizeof(float);
            Buffer.BlockCopy(data, currentIndex, buffer, 0, size);
            currentIndex += size;
            return new Vector2(buffer[0], buffer[1]);
        }

        public static byte[] Vector2ToBytes(Vector2[] vectors)
        {
            byte[] bytes = new byte[2 * sizeof(float) * vectors.Length + sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(vectors.Length), 0, bytes, 0, sizeof(int));
            int index = sizeof(int);
            for (int i = 0; i < vectors.Length; i++)
            {
                Vector2 vector = vectors[i];
                Buffer.BlockCopy(BitConverter.GetBytes(vector.x), 0, bytes, index + 0, sizeof(float));
                Buffer.BlockCopy(BitConverter.GetBytes(vector.y), 0, bytes, index + sizeof(float), sizeof(float));
                index += 2 * sizeof(float);
            }
            return bytes;
        }

        public static Matrix4x4 GetMatrix(byte[] data, ref int index)
        {
            float[] matrixBuffer = new float[16];

            int size = 4 * 4 * sizeof(float);
            Buffer.BlockCopy(data, index, matrixBuffer, 0, size);
            Matrix4x4 m = new Matrix4x4(new Vector4(matrixBuffer[0], matrixBuffer[1], matrixBuffer[2], matrixBuffer[3]),
                                        new Vector4(matrixBuffer[4], matrixBuffer[5], matrixBuffer[6], matrixBuffer[7]),
                                        new Vector4(matrixBuffer[8], matrixBuffer[9], matrixBuffer[10], matrixBuffer[11]),
                                        new Vector4(matrixBuffer[12], matrixBuffer[13], matrixBuffer[14], matrixBuffer[15])
                                        );
            index += size;
            return m;
        }

        public static byte[] MatrixToBytes(Matrix4x4 matrix)
        {
            byte[] column0Buffer = Vector4ToBytes(matrix.GetColumn(0));
            byte[] column1Buffer = Vector4ToBytes(matrix.GetColumn(1));
            byte[] column2Buffer = Vector4ToBytes(matrix.GetColumn(2));
            byte[] column3Buffer = Vector4ToBytes(matrix.GetColumn(3));
            List<byte[]> buffers = new List<byte[]> { column0Buffer, column1Buffer, column2Buffer, column3Buffer };
            return ConcatenateBuffers(buffers);
        }


        public static Quaternion GetQuaternion(byte[] data, ref int currentIndex)
        {
            float[] buffer = new float[4];
            int size = 4 * sizeof(float);
            Buffer.BlockCopy(data, currentIndex, buffer, 0, size);
            currentIndex += size;
            return new Quaternion(buffer[0], buffer[1], buffer[2], buffer[3]);
        }
        public static byte[] QuaternionToBytes(Quaternion quaternion)
        {
            byte[] bytes = new byte[4 * sizeof(float)];

            Buffer.BlockCopy(BitConverter.GetBytes(quaternion.x), 0, bytes, 0, sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(quaternion.y), 0, bytes, 1 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(quaternion.z), 0, bytes, 2 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(quaternion.w), 0, bytes, 3 * sizeof(float), sizeof(float));
            return bytes;
        }

        public static bool GetBool(byte[] data, ref int currentIndex)
        {
            int[] buffer = new int[1];
            Buffer.BlockCopy(data, currentIndex, buffer, 0, sizeof(int));
            currentIndex += sizeof(int);
            return buffer[0] == 1 ? true : false;
        }

        public static byte[] BoolToBytes(bool value)
        {
            byte[] bytes = new byte[sizeof(int)];
            int v = value ? 1 : 0;
            Buffer.BlockCopy(BitConverter.GetBytes(v), 0, bytes, 0, sizeof(int));
            return bytes;
        }

        public static int GetInt(byte[] data, ref int currentIndex)
        {
            int[] buffer = new int[1];
            Buffer.BlockCopy(data, currentIndex, buffer, 0, sizeof(int));
            currentIndex += sizeof(int);
            return buffer[0];
        }

        public static byte[] IntToBytes(int value)
        {
            byte[] bytes = new byte[sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(value), 0, bytes, 0, sizeof(int));
            return bytes;
        }

        public static float GetFloat(byte[] data, ref int currentIndex)
        {
            float[] buffer = new float[1];
            Buffer.BlockCopy(data, currentIndex, buffer, 0, sizeof(float));
            currentIndex += sizeof(float);
            return buffer[0];
        }

        public static byte[] FloatToBytes(float value)
        {
            byte[] bytes = new byte[sizeof(float)];
            Buffer.BlockCopy(BitConverter.GetBytes(value), 0, bytes, 0, sizeof(float));
            return bytes;
        }

        public static byte[] ConcatenateBuffers(List<byte[]> buffers)
        {
            int totalLength = 0;
            foreach (byte[] buffer in buffers)
            {
                totalLength += buffer.Length;
            }
            byte[] resultBuffer = new byte[totalLength];
            int index = 0;
            foreach (byte[] buffer in buffers)
            {
                int size = buffer.Length;
                Buffer.BlockCopy(buffer, 0, resultBuffer, index, size);
                index += size;
            }
            return resultBuffer;
        }

        public static void BuildClientId(byte[] data)
        {
            string clientId = ConvertToString(data);
            GlobalState.SetClientId(clientId);
        }

        public static void Rename(Transform root, byte[] data)
        {
            int bufferIndex = 0;
            string[] srcPath = GetString(data, ref bufferIndex).Split('/');
            string[] dstPath = GetString(data, ref bufferIndex).Split('/');

            string srcName = srcPath[srcPath.Length - 1];
            string dstName = dstPath[dstPath.Length - 1];

            SyncData.Rename(srcName, dstName);
        }

        public static void Delete(Transform prefab, byte[] data)
        {
            int bufferIndex = 0;
            string[] ObjectPath = GetString(data, ref bufferIndex).Split('/');
            string objectName = ObjectPath[ObjectPath.Length - 1];

            SyncData.Delete(objectName);
        }

        public static void Duplicate(Transform prefab, byte[] data)
        {
            int bufferIndex = 0;
            Transform srcPath = FindPath(prefab, data, ref bufferIndex);
            if (srcPath == null)
                return;

            string name = GetString(data, ref bufferIndex);

            Matrix4x4 mat = GetMatrix(data, ref bufferIndex);
            Vector3 position, scale;
            Quaternion rotation;
            Maths.DecomposeMatrix(mat, out position, out rotation, out scale);

            GameObject newGameObject = SyncData.Duplicate(srcPath.gameObject, name);
            newGameObject.transform.localPosition = position;
            newGameObject.transform.localRotation = rotation;
            newGameObject.transform.localScale = scale;
        }

        public static void BuildSendToTrash(Transform root, byte[] data)
        {
            int bufferIndex = 0;
            Transform objectPath = FindPath(root, data, ref bufferIndex);
            if (null == objectPath)
                return;
            objectPath.parent.parent = Utils.GetTrash().transform;

            Node node = SyncData.nodes[objectPath.name];
            node.RemoveInstance(objectPath.gameObject);
        }
        public static void BuildRestoreFromTrash(Transform root, byte[] data)
        {
            int bufferIndex = 0;
            string objectName = GetString(data, ref bufferIndex);
            Transform parent = FindPath(root, data, ref bufferIndex);
            Transform trf = Utils.GetTrash().transform.Find(objectName + "_parent");
            if (null != trf)
            {
                trf.parent = parent;

                Node node = SyncData.nodes[objectName];
                node.AddInstance(trf.GetChild(0).gameObject);
            }
        }

        public static void BuildTexture(byte[] data)
        {
            int bufferIndex = 0;
            string path = GetString(data, ref bufferIndex);

            int size = GetInt(data, ref bufferIndex);

            byte[] buffer = new byte[size];
            Buffer.BlockCopy(data, bufferIndex, buffer, 0, size);

            textureData[path] = buffer;
        }

        public static void BuildCollection(byte[] data)
        {
            int bufferIndex = 0;
            string collectionName = GetString(data, ref bufferIndex);
            bool visible = GetBool(data, ref bufferIndex);
            Vector3 offset = GetVector3(data, ref bufferIndex);

            bool tempVisible = GetBool(data, ref bufferIndex);

            SyncData.AddCollection(collectionName, offset, visible, tempVisible);
        }

        public static void BuildCollectionRemoved(byte[] data)
        {
            int bufferIndex = 0;
            string collectionName = GetString(data, ref bufferIndex);

            SyncData.RemoveCollection(collectionName);
        }

        public static void BuildAddCollectionToCollection(Transform root, byte[] data)
        {
            int bufferIndex = 0;
            string parentCollectionName = GetString(data, ref bufferIndex);
            string collectionName = GetString(data, ref bufferIndex);

            SyncData.AddCollectionToCollection(parentCollectionName, collectionName);

        }

        public static void BuildRemoveCollectionFromCollection(Transform root, byte[] data)
        {
            int bufferIndex = 0;
            string parentCollectionName = GetString(data, ref bufferIndex);
            string collectionName = GetString(data, ref bufferIndex);

            SyncData.RemoveCollectionFromCollection(parentCollectionName, collectionName);
        }

        public static void BuildAddObjectToCollection(Transform root, byte[] data)
        {
            int bufferIndex = 0;
            string collectionName = GetString(data, ref bufferIndex);
            string objectName = GetString(data, ref bufferIndex);

            SyncData.AddObjectToCollection(collectionName, objectName);
        }

        public static void BuildRemoveObjectFromCollection(Transform root, byte[] data)
        {
            int bufferIndex = 0;
            string collectionName = GetString(data, ref bufferIndex);
            string objectName = GetString(data, ref bufferIndex);

            SyncData.RemoveObjectFromCollection(collectionName, objectName);
        }

        public static void BuildCollectionInstance(byte[] data)
        {
            int bufferIndex = 0;
            Transform transform = BuildPath(data, ref bufferIndex, true);
            string collectionName = GetString(data, ref bufferIndex);

            SyncData.AddCollectionInstance(transform, collectionName);
        }

        public static void BuildAddObjectToDocument(Transform root, byte[] data)
        {
            int bufferIndex = 0;
            string sceneName = GetString(data, ref bufferIndex);
            if (sceneName != SyncData.currentSceneName)
                return;
            string objectName = GetString(data, ref bufferIndex);
            SyncData.AddObjectToDocument(root, objectName, "/");
        }

        public static void BuilAddCollectionToScene(Transform root, byte[] data)
        {
            int bufferIndex = 0;
            string sceneName = GetString(data, ref bufferIndex);
            string collectionName = GetString(data, ref bufferIndex);
            SyncData.sceneCollections.Add(collectionName);
        }

        public static void BuilSetScene(byte[] data)
        {
            int bufferIndex = 0;
            string sceneName = GetString(data, ref bufferIndex);
            SyncData.SetScene(sceneName);
        }

        public static MaterialParameters DefaultMaterial()
        {
            string name = "defaultMaterial";

            if (materialsParameters.TryGetValue(name, out MaterialParameters materialParameters))
                return materialParameters;

            MaterialType materialType;
#if UNITY_EDITOR
            materialType = MaterialType.EditorOpaque;
#else
            materialType = MaterialType.Opaque;
#endif
            materialParameters = new MaterialParameters();
            materialsParameters[name] = materialParameters;
            materialParameters.name = name;
            materialParameters.materialType = materialType;
            materialParameters.opacity = 0;
            materialParameters.opacityTexturePath = "";
            materialParameters.baseColor = new Color(0.8f, 0.8f, 0.8f);
            materialParameters.baseColorTexturePath = "";
            materialParameters.metallic = 0f;
            materialParameters.metallicTexturePath = "";
            materialParameters.roughness = 0.5f;
            materialParameters.roughnessTexturePath = "";
            materialParameters.normalTexturePath = "";
            materialParameters.emissionColor = new Color(0, 0, 0); ;
            materialParameters.emissionColorTexturePath = "";

            return materialParameters;
        }

        public static Texture2D CreateSmallImage()
        {
            Texture2D smallImage = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            smallImage.LoadRawTextureData(new byte[] { 0, 0, 0, 255 });
            return smallImage;
        }

        public static Texture2D LoadTextureOIIO(string filePath, bool isLinear)
        {
            // TODO: need to flip? Repere bottom left, Y-up
            int ret = OIIOAPI.oiio_open_image(filePath);
            if (ret == 0)
            {
                Debug.LogWarning("Could not open image " + filePath + " with OIIO.");
                return null;
            }

            int width = -1;
            int height = -1;
            int nchannels = -1;
            OIIOAPI.BASETYPE format = OIIOAPI.BASETYPE.NONE;
            ret = OIIOAPI.oiio_get_image_info(ref width, ref height, ref nchannels, ref format);
            if (ret == 0)
            {
                Debug.LogWarning("Could not get info about image " + filePath + " with OIIO");
                return null;
            }

            TexConv conv = new TexConv();
            bool canConvert = Format2Format(format, nchannels, ref conv);
            if (!canConvert)
            {
                Debug.LogWarning("Could not create image from format: " + conv.format + " with option: " + conv.options);
                return CreateSmallImage();
            }
            // TMP
            else if (conv.options.HasFlag(TextureConversionOptions.SHORT_TO_FLOAT) || conv.options.HasFlag(TextureConversionOptions.SHORT_TO_INT))
            {
                Debug.LogWarning("Could not create image from format: " + conv.format + " with option: " + conv.options);
                return CreateSmallImage();
            }

            Texture2D image = new Texture2D(width, height, conv.format, true, isLinear); // with mips

            var pixels = image.GetRawTextureData();
            GCHandle handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
            ret = OIIOAPI.oiio_fill_image_data(handle.AddrOfPinnedObject(), conv.options.HasFlag(TextureConversionOptions.RGB_TO_RGBA) ? 1 : 0);
            if (ret == 1)
            {
                image.LoadRawTextureData(pixels);
                image.Apply();
            }
            else
            {
                Debug.LogWarning("Could not fill texture data of " + filePath + " with OIIO.");
                return null;
            }

            return image;
        }

        enum TextureConversionOptions
        {
            NO_CONV = 0,
            RGB_TO_RGBA = 1,
            SHORT_TO_INT = 2,
            SHORT_TO_FLOAT = 4,
        }; // TODO: fill, enhance

        class TexConv
        {
            public TextureFormat format;
            public TextureConversionOptions options;
        };

        private static bool Format2Format(OIIOAPI.BASETYPE format, int nchannels, ref TexConv result)
        {
            // TODO: handle compressed formats.

            result.format = TextureFormat.RGBA32;
            result.options = TextureConversionOptions.NO_CONV;

            switch (format)
            {
                case OIIOAPI.BASETYPE.UCHAR:
                case OIIOAPI.BASETYPE.CHAR:
                    switch (nchannels)
                    {
                        case 1: result.format = TextureFormat.R8; break;
                        case 2: result.format = TextureFormat.RG16; break;
                        case 3: result.format = TextureFormat.RGB24; break;
                        case 4: result.format = TextureFormat.RGBA32; break;
                        default: return false;
                    }
                    break;

                case OIIOAPI.BASETYPE.USHORT:
                    switch (nchannels)
                    {
                        case 1: result.format = TextureFormat.R16; break;
                        case 2: result.format = TextureFormat.RGFloat; result.options = TextureConversionOptions.SHORT_TO_FLOAT; break;
                        case 3: result.format = TextureFormat.RGBAFloat; result.options = TextureConversionOptions.SHORT_TO_FLOAT | TextureConversionOptions.RGB_TO_RGBA; break;
                        case 4: result.format = TextureFormat.RGBAFloat; result.options = TextureConversionOptions.SHORT_TO_FLOAT; break;
                        // R16_G16, R16_G16_B16 and R16_G16_B16_A16 do not exist
                        default: return false;
                    }
                    break;

                case OIIOAPI.BASETYPE.HALF:
                    switch (nchannels)
                    {
                        case 1: result.format = TextureFormat.RHalf; break;
                        case 2: result.format = TextureFormat.RGHalf; break;
                        case 3: result.format = TextureFormat.RGBAHalf; result.options = TextureConversionOptions.NO_CONV | TextureConversionOptions.RGB_TO_RGBA; break; // RGBHalf is NOT SUPPORTED
                        case 4: result.format = TextureFormat.RGBAHalf; break;
                        default: return false;
                    }
                    break;

                case OIIOAPI.BASETYPE.FLOAT:
                    switch (nchannels)
                    {
                        case 1: result.format = TextureFormat.RFloat; break;
                        case 2: result.format = TextureFormat.RGFloat; break;
                        case 3: result.format = TextureFormat.RGBAFloat; result.options = TextureConversionOptions.NO_CONV | TextureConversionOptions.RGB_TO_RGBA; break;// RGBFloat is NOT SUPPORTED
                        case 4: result.format = TextureFormat.RGBAFloat; break;
                        default: return false;
                    }
                    break;

                default: return false;
            }

            return true;
        }

        public static Texture2D LoadTextureDXT(string filePath, bool isLinear)
        {
            byte[] ddsBytes = System.IO.File.ReadAllBytes(filePath);

            byte[] format = { ddsBytes[84], ddsBytes[85], ddsBytes[86], ddsBytes[87], 0 };
            string sFormat = System.Text.Encoding.UTF8.GetString(format);
            TextureFormat textureFormat;

            if (sFormat != "DXT1")
                textureFormat = TextureFormat.DXT1;
            else if (sFormat != "DXT5")
                textureFormat = TextureFormat.DXT5;
            else return null;

            byte ddsSizeCheck = ddsBytes[4];
            if (ddsSizeCheck != 124)
                throw new Exception("Invalid DDS DXTn texture. Unable to read");  //this header byte should be 124 for DDS image files

            int height = ddsBytes[13] * 256 + ddsBytes[12];
            int width = ddsBytes[17] * 256 + ddsBytes[16];

            int DDS_HEADER_SIZE = 128;
            byte[] dxtBytes = new byte[ddsBytes.Length - DDS_HEADER_SIZE];
            Buffer.BlockCopy(ddsBytes, DDS_HEADER_SIZE, dxtBytes, 0, ddsBytes.Length - DDS_HEADER_SIZE);

            Texture2D texture = new Texture2D(width, height, textureFormat, true, isLinear);
            texture.LoadRawTextureData(dxtBytes);
            texture.Apply();

            return texture;
        }

        public static Texture2D LoadTextureFromBuffer(byte[] data, bool isLinear)
        {
            Texture2D tex = new Texture2D(2, 2, TextureFormat.ARGB32, true, isLinear);
            bool res = tex.LoadImage(data);
            if (!res)
                return null;

            return tex;
        }

        public static Texture2D GetTexture(string filePath, bool isLinear)
        {
            if (textureData.ContainsKey(filePath))
            {
                byte[] data = textureData[filePath];
                textureData.Remove(filePath);
                return LoadTexture(filePath, data, isLinear);
            }
            if (textures.ContainsKey(filePath))
            {
                return textures[filePath];
            }
            return null;
        }

        public static Texture2D LoadTexture(string filePath, byte[] data, bool isLinear)
        {
            string directory = Path.GetDirectoryName(filePath);
            string withoutExtension = Path.GetFileNameWithoutExtension(filePath);
            string ddsFile = directory + "/" + withoutExtension + ".dds";

            if (File.Exists(ddsFile))
            {
                Texture2D t = LoadTextureDXT(ddsFile, isLinear);
                if (null != t)
                {
                    textures[filePath] = t;
                    texturesFlipY.Add(filePath);
                    return t;
                }
            }

            if (File.Exists(filePath))
            {
                //byte[] bytes = System.IO.File.ReadAllBytes(filePath);
                //Texture2D t = LoadTextureFromBuffer(bytes, isLinear);
                Texture2D t = LoadTextureOIIO(filePath, isLinear);
                if (null != t)
                {
                    textures[filePath] = t;
                    texturesFlipY.Add(filePath);
                    return t;
                }
            }

            Texture2D texture = LoadTextureFromBuffer(data, isLinear);
            if (null != texture)
                textures[filePath] = texture;
            /*
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(data, 0, data.Length);
                }
            }
            catch
            {
                Debug.LogWarning("Could not write : " + filePath);
            }
            */

            return texture;
        }

        public static Material GetMaterial(MaterialType materialType)
        {
            if (baseMaterials.Count == 0)
            {
                baseMaterials.Add(MaterialType.Opaque, Resources.Load<Material>("Materials/BlenderImport"));
                baseMaterials.Add(MaterialType.Transparent, Resources.Load<Material>("Materials/BlenderImportTransparent"));
                baseMaterials.Add(MaterialType.EditorOpaque, Resources.Load<Material>("Materials/BlenderImportEditor"));
                baseMaterials.Add(MaterialType.EditorTransparent, Resources.Load<Material>("Materials/BlenderImportTransparentEditor"));
                baseMaterials.Add(MaterialType.GreasePencil, Resources.Load<Material>("Materials/GreasePencilMat"));
                baseMaterials.Add(MaterialType.Paint, Resources.Load<Material>("Materials/Paint"));
            }
            return baseMaterials[materialType];
        }

        public static void BuildMaterial(byte[] data)
        {
            int currentIndex = 0;
            string name = GetString(data, ref currentIndex);
            float opacity = GetFloat(data, ref currentIndex);
            string opacityTexturePath = GetString(data, ref currentIndex);

            if (!materialsParameters.TryGetValue(name, out MaterialParameters materialParameters))
            {
                MaterialType materialType;
#if UNITY_EDITOR
                materialType = (opacityTexturePath.Length > 0 || opacity < 1.0f)
                    ? MaterialType.EditorTransparent : MaterialType.EditorOpaque;
#else
                materialType = (opacityTexturePath.Length > 0 || opacity < 1.0f)
                    ? MaterialType.Transparent : MaterialType.Opaque;
#endif
                materialParameters = new MaterialParameters();
                materialParameters.name = name;
                materialParameters.materialType = materialType;
                materialsParameters[name] = materialParameters;
            }


            materialParameters.opacity = opacity;
            materialParameters.opacityTexturePath = opacityTexturePath;
            materialParameters.baseColor = GetColor(data, ref currentIndex);
            materialParameters.baseColorTexturePath = GetString(data, ref currentIndex);
            materialParameters.metallic = GetFloat(data, ref currentIndex);
            materialParameters.metallicTexturePath = GetString(data, ref currentIndex);
            materialParameters.roughness = GetFloat(data, ref currentIndex);
            materialParameters.roughnessTexturePath = GetString(data, ref currentIndex);
            materialParameters.normalTexturePath = GetString(data, ref currentIndex);
            materialParameters.emissionColor = GetColor(data, ref currentIndex);
            materialParameters.emissionColorTexturePath = GetString(data, ref currentIndex);
        }

        public static void ApplyMaterialParameters(MeshRenderer meshRenderer, List<MaterialParameters> meshMaterials)
        {
            MaterialParameters[] materialParameters = meshMaterials.ToArray();
            Material[] materials = new Material[materialParameters.Length];
            for (int i = 0; i < materialParameters.Length; i++)
            {
                materials[i] = GetMaterial(materialParameters[i].materialType);
            }

            Material[] materialsToDestroy = meshRenderer.materials;
            for (int i = 0; i < materialsToDestroy.Length; i++)
                GameObject.Destroy(materialsToDestroy[i]);

            meshRenderer.sharedMaterials = materials;
            Material[] instanceMaterials = meshRenderer.materials;
            for (int i = 0; i < materialParameters.Length; i++)
            {
                ApplyMaterialParameters(instanceMaterials[i], materialParameters[i]);
            }
            meshRenderer.materials = instanceMaterials;
        }

        public static void ApplyMaterialParameters(Material material, MaterialParameters parameters)
        {
            if (parameters.materialType == MaterialType.Paint)
            {
                material.SetColor("_BaseColor", parameters.baseColor);
                return;
            }

            if (parameters.materialType == MaterialType.GreasePencil)
            {
                material.SetColor("_UnlitColor", parameters.baseColor);
                return;
            }

            //
            // OPACITY
            //
            material.SetFloat("_Opacity", parameters.opacity);
            if (parameters.opacityTexturePath.Length > 0)
            {
                Texture2D tex = GetTexture(parameters.opacityTexturePath, true);
                if (tex != null)
                {
                    material.SetFloat("_UseOpacityMap", 1f);
                    material.SetTexture("_OpacityMap", tex);
                    if (texturesFlipY.Contains(parameters.opacityTexturePath))
                        material.SetVector("_UvScale", new Vector4(1, -1, 0, 0));
                }
            }

            //
            // BASE COLOR
            //
            Color baseColor = parameters.baseColor;
            material.SetColor("_BaseColor", baseColor);
            string baseColorTexturePath = parameters.baseColorTexturePath;
            if (baseColorTexturePath.Length > 0)
            {
                Texture2D tex = GetTexture(baseColorTexturePath, false);
                if (tex != null)
                {
                    material.SetFloat("_UseColorMap", 1f);
                    material.SetTexture("_ColorMap", tex);
                    if (texturesFlipY.Contains(baseColorTexturePath))
                        material.SetVector("_UvScale", new Vector4(1, -1, 0, 0));
                }

            }

            //
            // METALLIC
            //
            float metallic = parameters.metallic;
            material.SetFloat("_Metallic", metallic);
            string metallicTexturePath = parameters.metallicTexturePath;
            if (metallicTexturePath.Length > 0)
            {
                Texture2D tex = GetTexture(metallicTexturePath, true);
                if (tex != null)
                {
                    material.SetFloat("_UseMetallicMap", 1f);
                    material.SetTexture("_MetallicMap", tex);
                    if (texturesFlipY.Contains(metallicTexturePath))
                        material.SetVector("_UvScale", new Vector4(1, -1, 0, 0));
                }
            }

            //
            // ROUGHNESS
            //
            float roughness = parameters.roughness;
            material.SetFloat("_Roughness", roughness);
            string roughnessTexturePath = parameters.roughnessTexturePath;
            if (roughnessTexturePath.Length > 0)
            {
                Texture2D tex = GetTexture(roughnessTexturePath, true);
                if (tex != null)
                {
                    material.SetFloat("_UseRoughnessMap", 1f);
                    material.SetTexture("_RoughnessMap", tex);
                    if (texturesFlipY.Contains(roughnessTexturePath))
                        material.SetVector("_UvScale", new Vector4(1, -1, 0, 0));
                }
            }

            //
            // NORMAL
            //
            string normalTexturePath = parameters.normalTexturePath;
            if (normalTexturePath.Length > 0)
            {
                Texture2D tex = GetTexture(normalTexturePath, true);
                if (tex != null)
                {
                    material.SetFloat("_UseNormalMap", 1f);
                    material.SetTexture("_NormalMap", tex);
                    if (texturesFlipY.Contains(normalTexturePath))
                        material.SetVector("_UvScale", new Vector4(1, -1, 0, 0));
                }
            }

            //
            // EMISSION
            //
            Color emissionColor = parameters.emissionColor;
            material.SetColor("_EmissiveColor", baseColor);
            string emissionColorTexturePath = parameters.emissionColorTexturePath;
            if (emissionColorTexturePath.Length > 0)
            {
                Texture2D tex = GetTexture(emissionColorTexturePath, false);
                if (tex != null)
                {
                    material.SetFloat("_UseEmissiveMap", 1f);
                    material.SetTexture("_EmissiveMap", tex);
                    if (texturesFlipY.Contains(emissionColorTexturePath))
                        material.SetVector("_UvScale", new Vector4(1, -1, 0, 0));
                }
            }

        }

        public static void BuildAssignMaterial(byte[] data)
        {
            int currentIndex = 0;
            string objectName = GetString(data, ref currentIndex);
            string materialName = GetString(data, ref currentIndex);

            if (!materialsParameters.TryGetValue(materialName, out MaterialParameters materialParameters))
            {
                Debug.LogError("Could not assign material " + materialName + " to " + objectName);
                return;
            }

            Material material = GetMaterial(materialParameters.materialType);
            Node prefabNode = SyncData.nodes[objectName];
            MeshRenderer[] renderers = prefabNode.prefab.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length > 0)
            {
                foreach (MeshRenderer renderer in renderers)
                {
                    renderer.sharedMaterial = material;
                    Material instanceMaterial = renderer.material;
                    ApplyMaterialParameters(instanceMaterial, materialParameters);
                    renderer.material = instanceMaterial;
                }
                foreach (Tuple<GameObject, string> item in prefabNode.instances)
                {
                    MeshRenderer[] rends = item.Item1.GetComponentsInChildren<MeshRenderer>();
                    if (rends.Length > 0)
                    {
                        foreach (MeshRenderer rend in rends)
                        {
                            rend.sharedMaterial = material;
                            Material instanceMaterial = rend.material;
                            ApplyMaterialParameters(instanceMaterial, materialParameters);
                            rend.material = instanceMaterial;
                        }
                    }
                }
            }
        }

        public static Transform FindPath(Transform root, byte[] data, ref int bufferIndex)
        {
            string path = NetGeometry.GetString(data, ref bufferIndex);
            if (path == "")
                return root;

            char[] separator = { '/' };
            string[] splitted = path.Split(separator);
            Transform parent = root;
            foreach (string subPath in splitted)
            {
                Transform transform = SyncData.FindChild(parent, subPath);
                if (transform == null)
                {
                    return null;
                }
                parent = transform;
            }
            return parent;
        }

        public static string ConvertToString(byte[] data)
        {
            return System.Text.Encoding.UTF8.GetString(data, 0, data.Length);
        }

        public static string GetString(byte[] data, ref int bufferIndex)
        {
            int strLength = (int) BitConverter.ToUInt32(data, bufferIndex);
            string str = System.Text.Encoding.UTF8.GetString(data, bufferIndex + 4, strLength);
            bufferIndex = bufferIndex + strLength + 4;
            return str;
        }

        public static string GetPathName(Transform root, Transform transform)
        {
            if (root == transform)
                return "";

            string result = transform.name;
            while (transform.parent && transform.parent.parent && transform.parent.parent != root)
            {
                transform = transform.parent.parent; // skip blender pseudo-parent
                result = transform.name + "/" + result;
            }
            return result;
        }



        public static Transform BuildPath(byte[] data, ref int bufferIndex, bool includeLeaf)
        {
            string path = GetString(data, ref bufferIndex);
            if (!includeLeaf)
            {
                int index = path.LastIndexOf('/');
                if (index == -1)
                    return null;
                path = path.Substring(0, index);
            }
            return SyncData.GetOrCreatePrefabPath(path);
        }

        public static void BuildObjectVisibility(byte[] data)
        {
            int currentIndex = 0;
            string objectName = GetString(data, ref currentIndex);
            bool visible = GetBool(data, ref currentIndex) ? false : true;
            bool hideSelect = GetBool(data, ref currentIndex);
            bool visibleInRender = GetBool(data, ref currentIndex) ? false : true;
            bool tempVisible = GetBool(data, ref currentIndex) ? false : true;

            if (SyncData.nodes.ContainsKey(objectName))
            {
                Node node = SyncData.nodes[objectName];
                node.visible = visible;
                node.tempVisible = tempVisible;
                SyncData.ApplyVisibilityToInstances(node.prefab.transform);
            }
        }

        public static Transform BuildTransform(Transform prefab, byte[] data)
        {
            int currentIndex = 0;

            Transform transform = BuildPath(data, ref currentIndex, true);

            Matrix4x4 parentInverse = GetMatrix(data, ref currentIndex);
            Matrix4x4 basis = GetMatrix(data, ref currentIndex);
            Matrix4x4 local = GetMatrix(data, ref currentIndex);

            Vector3 t, s;
            Quaternion r;
            Maths.DecomposeMatrix(parentInverse, out t, out r, out s);
            transform.parent.localPosition = t;
            transform.parent.localRotation = r;
            transform.parent.localScale = s;

            Matrix4x4 localMatrix = parentInverse.inverse * local;
            Maths.DecomposeMatrix(localMatrix, out t, out r, out s);
            transform.localPosition = t;
            transform.localRotation = r;
            transform.localScale = s;

            if (SyncData.nodes.TryGetValue(transform.name, out Node node))
            {
                bool recording = GlobalState.Instance.recordState == GlobalState.RecordState.Recording;
                bool gripped = GlobalState.Instance.selectionGripped;
                foreach (Tuple<GameObject, string> instance in node.instances)
                {
                    GameObject obj = instance.Item1;
                    if ((recording || gripped) && Selection.IsSelected(obj))
                        continue;
                    obj.transform.localPosition = transform.localPosition;
                    obj.transform.localRotation = transform.localRotation;
                    obj.transform.localScale = transform.localScale;
                }
            }

            return transform;
        }


        /* --------------------------------------------------------------------------------------------
         * 
         *   COMMANDS
         * 
         * -------------------------------------------------------------------------------------------*/

        public static NetCommand BuildObjectVisibilityCommand(Transform root, Transform transform)
        {
            bool tempVisible = true;
            string parentName = "";
            if (SyncData.nodes.ContainsKey(transform.name))
            {
                Node node = SyncData.nodes[transform.name];
                tempVisible = node.tempVisible;
                if (null != node.parent)
                    parentName = node.parent.prefab.name + "/";
            }
            byte[] name = StringToBytes(parentName + transform.name);
            byte[] hideBuffer = BoolToBytes(transform.gameObject.activeSelf ? false : true);
            byte[] hideSelect = BoolToBytes(false);
            byte[] hideInViewport = BoolToBytes(false);
            byte[] hideGet = BoolToBytes(tempVisible ? false : true);

            List<byte[]> buffers = new List<byte[]> { name, hideBuffer, hideSelect, hideInViewport, hideGet };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.ObjectVisibility);
            return command;
        }
        public static NetCommand BuildTransformCommand(Transform root, Transform transform)
        {
            bool tempVisible = true;
            string parentName = "";
            if (SyncData.nodes.ContainsKey(transform.name))
            {
                Node node = SyncData.nodes[transform.name];
                tempVisible = node.tempVisible;
                if (null != node.parent)
                    parentName = node.parent.prefab.name + "/";
            }
            byte[] name = StringToBytes(parentName + transform.name);
            Matrix4x4 parentMatrix = Matrix4x4.TRS(transform.parent.localPosition, transform.parent.localRotation, transform.parent.localScale);
            Matrix4x4 basisMatrix = Matrix4x4.TRS(transform.localPosition, transform.localRotation, transform.localScale);
            byte[] invertParentMatrixBuffer = MatrixToBytes(parentMatrix);
            byte[] basisMatrixBuffer = MatrixToBytes(basisMatrix);
            byte[] localMatrixBuffer = MatrixToBytes(parentMatrix * basisMatrix);

            List<byte[]> buffers = new List<byte[]> { name, invertParentMatrixBuffer, basisMatrixBuffer, localMatrixBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Transform);
            return command;
        }

        public static NetCommand BuildMaterialCommand(Material material)
        {
            byte[] name = StringToBytes(material.name);
            float op = 1f;
            if (material.HasProperty("_Opacity"))
                op = material.GetFloat("_Opacity");
            byte[] opacity = FloatToBytes(op);
            byte[] opacityMapTexture = StringToBytes("");
            byte[] baseColor = ColorToBytes(material.GetColor("_BaseColor"));
            byte[] baseColorTexture = StringToBytes("");
            byte[] metallic = FloatToBytes(material.GetFloat("_Metallic"));
            byte[] metallicTexture = StringToBytes("");
            byte[] roughness = FloatToBytes(material.HasProperty("_Smoothness") ? 1f - material.GetFloat("_Smoothness") : material.GetFloat("_Roughness"));
            byte[] roughnessTexture = StringToBytes("");
            byte[] normalMapTexture = StringToBytes("");
            byte[] emissionColor = ColorToBytes(Color.black);
            byte[] emissionColorTexture = StringToBytes("");

            List<byte[]> buffers = new List<byte[]> { name, opacity, opacityMapTexture, baseColor, baseColorTexture, metallic, metallicTexture, roughness, roughnessTexture, normalMapTexture, emissionColor, emissionColorTexture };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Material);
            return command;
        }

        public static NetCommand BuildAssignMaterialCommand(AssignMaterialInfo info)
        {
            byte[] objectName = StringToBytes(info.objectName);
            byte[] materialName = StringToBytes(info.materialName);
            List<byte[]> buffers = new List<byte[]> { objectName, materialName };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.AssignMaterial);
            return command;
        }

        public static NetCommand BuildCameraCommand(Transform root, CameraInfo cameraInfo)
        {
            Transform current = cameraInfo.transform;
            string path = current.name;
            while (current.parent && current.parent != root)
            {
                current = current.parent;
                path = current.name + "/" + path;
            }
            byte[] bpath = StringToBytes(path);

            CameraController cameraController = cameraInfo.transform.GetComponent<CameraController>();
            byte[] bname = StringToBytes(cameraController.controllerName);

            Camera cam = cameraInfo.transform.GetComponentInChildren<Camera>(true);
            int sensorFit = (int) cam.gateFit;

            byte[] paramsBuffer = new byte[6 * sizeof(float) + 1 * sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(cameraController.focal), 0, paramsBuffer, 0 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(cameraController.near), 0, paramsBuffer, 1 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(cameraController.far), 0, paramsBuffer, 2 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(1.8f), 0, paramsBuffer, 3 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(sensorFit), 0, paramsBuffer, 4 * sizeof(float), sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(cam.sensorSize.x), 0, paramsBuffer, 4 * sizeof(float) + sizeof(int), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(cam.sensorSize.y), 0, paramsBuffer, 5 * sizeof(float) + sizeof(int), sizeof(float));

            List<byte[]> buffers = new List<byte[]> { bpath, bname, paramsBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Camera);
            return command;
        }

        public static NetCommand BuildLightCommand(Transform root, LightInfo lightInfo)
        {
            Transform current = lightInfo.transform;
            string path = current.name;
            while (current.parent && current.parent != root)
            {
                current = current.parent;
                path = current.name + "/" + path;
            }
            byte[] bpath = StringToBytes(path);

            Light light = lightInfo.transform.GetComponentInChildren<Light>();
            int shadow = light.shadows != LightShadows.None ? 1 : 0;
            Color color = light.color;

            float spotSize = 0;
            float spotBlend = 0;

            float worldScale = root.parent.localScale.x;

            LightController lightController = lightInfo.transform.GetComponentInChildren<LightController>();
            byte[] bname = StringToBytes(lightController.controllerName);

            float power = lightController.GetPower();

            float intensity = lightController.intensity;

            switch (light.type)
            {
                case LightType.Spot:
                    spotSize = light.spotAngle / 180f * 3.14f;
                    spotBlend = 1f - (light.innerSpotAngle / 100f);
                    break;
            }

            byte[] paramsBuffer = new byte[2 * sizeof(int) + 7 * sizeof(float)];
            Buffer.BlockCopy(BitConverter.GetBytes((int) light.type), 0, paramsBuffer, 0 * sizeof(int), sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(shadow), 0, paramsBuffer, 1 * sizeof(int), sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(light.color.r), 0, paramsBuffer, 2 * sizeof(int), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(light.color.g), 0, paramsBuffer, 2 * sizeof(int) + 1 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(light.color.b), 0, paramsBuffer, 2 * sizeof(int) + 2 * sizeof(float), sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(light.color.a), 0, paramsBuffer, 2 * sizeof(int) + 3 * sizeof(float), sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(power), 0, paramsBuffer, 2 * sizeof(int) + 4 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(spotSize), 0, paramsBuffer, 2 * sizeof(int) + 5 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(spotBlend), 0, paramsBuffer, 2 * sizeof(int) + 6 * sizeof(float), sizeof(float));

            List<byte[]> buffers = new List<byte[]> { bpath, bname, paramsBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Light);
            return command;

        }

        public static NetCommand BuildSkyCommand(SkySettings skyInfo)
        {
            byte[] skyNameBuffer = StringToBytes("Sky"); // optimized commands need a name
            byte[] topBuffer = ColorToBytes(skyInfo.topColor);
            byte[] middleBuffer = ColorToBytes(skyInfo.middleColor);
            byte[] bottomBuffer = ColorToBytes(skyInfo.bottomColor);

            List<byte[]> buffers = new List<byte[]> { skyNameBuffer, topBuffer, middleBuffer, bottomBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Sky);
            return command;
        }

        public static NetCommand BuildRenameCommand(Transform root, RenameInfo rename)
        {
            string src = GetPathName(root, rename.srcTransform);
            byte[] srcPath = StringToBytes(src);
            byte[] dstName = StringToBytes(rename.newName);
            Debug.Log($"{rename.srcTransform.name}: {src} --> {rename.newName}");

            List<byte[]> buffers = new List<byte[]> { srcPath, dstName };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Rename);
            return command;
        }

        public static NetCommand BuildDuplicateCommand(Transform root, DuplicateInfos duplicate)
        {
            byte[] srcPath = StringToBytes(GetPathName(root, duplicate.srcObject.transform));
            byte[] dstName = StringToBytes(duplicate.dstObject.name);

            Transform transform = duplicate.dstObject.transform;
            byte[] matrixBuffer = MatrixToBytes(Matrix4x4.TRS(transform.localPosition, transform.localRotation, transform.localScale));

            List<byte[]> buffers = new List<byte[]> { srcPath, dstName, matrixBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Duplicate);
            return command;
        }

        public static NetCommand BuildSendToTrashCommand(Transform root, SendToTrashInfo sendToTrash)
        {
            byte[] path = StringToBytes(GetPathName(root, sendToTrash.transform));
            List<byte[]> buffers = new List<byte[]> { path };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.SendToTrash);
            return command;
        }

        public static NetCommand BuildRestoreFromTrashCommand(Transform root, RestoreFromTrashInfo restoreFromTrash)
        {
            string parentPath = GetPathName(root, restoreFromTrash.parent);

            byte[] nameBuffer = StringToBytes(restoreFromTrash.transform.name);
            byte[] pathBuffer = StringToBytes(parentPath);

            List<byte[]> buffers = new List<byte[]> { nameBuffer, pathBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.RestoreFromTrash);
            return command;
        }


        public static NetCommand BuildMeshCommand(Transform root, MeshInfos meshInfos)
        {
            Mesh mesh = meshInfos.meshFilter.mesh;
            byte[] name = StringToBytes(mesh.name);

            byte[] baseMeshSize = IntToBytes(0);

            byte[] positions = Vector3ToBytes(mesh.vertices);

            int[] baseTriangles = mesh.triangles;
            Vector3[] baseNormals = mesh.normals;
            Vector3[] splittedNormals = new Vector3[baseTriangles.Length];
            for (int i = 0; i < splittedNormals.Length; i++)
            {
                int id = baseTriangles[i];
                splittedNormals[i] = baseNormals[id];

            }
            byte[] normals = Vector3ToBytes(splittedNormals);

            Vector2[] baseUVs = mesh.uv;
            Vector2[] splittedUVs = null;
            if (null != mesh.uv && mesh.uv.Length > 0)
            {
                splittedUVs = new Vector2[baseTriangles.Length];
                for (int i = 0; i < splittedNormals.Length; i++)
                {
                    int id = baseTriangles[i];
                    splittedUVs[i] = baseUVs[id];
                }
            }
            else
            {
                splittedUVs = new Vector2[0];
            }
            byte[] uvs = Vector2ToBytes(splittedUVs);

            int[] materialIndices = null;
            if (mesh.subMeshCount <= 1)
            {
                materialIndices = new int[0];
            }
            else
            {
                materialIndices = new int[baseTriangles.Length / 3];
                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    SubMeshDescriptor subMesh = mesh.GetSubMesh(i);
                    for (int j = subMesh.indexStart / 3; j < (subMesh.indexStart + subMesh.indexCount) / 3; j++)
                    {
                        materialIndices[j] = i;
                    }
                }

            }

            byte[] materialIndicesBuffer = new byte[materialIndices.Length * sizeof(int) + sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(materialIndices.Length), 0, materialIndicesBuffer, 0, sizeof(int));
            Buffer.BlockCopy(materialIndices, 0, materialIndicesBuffer, sizeof(int), materialIndices.Length * sizeof(int));

            byte[] triangles = TriangleIndicesToBytes(baseTriangles);

            Material[] materials = meshInfos.meshRenderer.materials;
            string[] materialNames = new string[materials.Length];
            int index = 0;
            foreach (Material material in materials)
            {
                materialNames[index++] = material.name;
            }
            byte[] materialsBuffer = StringsToBytes(materialNames);

            Transform transform = meshInfos.meshTransform;
            string path = GetPathName(root, transform);
            byte[] pathBuffer = StringToBytes(path);

            byte[] bakedMeshSize = IntToBytes(positions.Length + normals.Length + uvs.Length + materialIndicesBuffer.Length + triangles.Length);

            // necessary to satisfy baked mesh server format
            //////////////////////////////////////////////////
            int materialCount = materials.Length;
            byte[] materialLinksBuffer = new byte[sizeof(int) * materialCount];
            index = 0;
            for (int i = 0; i < materialCount; i++)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(1), 0, materialLinksBuffer, index, sizeof(int));
                index += sizeof(int);
            }
            byte[] materialLinkNamesBuffer = StringsToBytes(materialNames, false);
            //////////////////////////////////////////////////

            List<byte[]> buffers = new List<byte[]> { pathBuffer, name, baseMeshSize, bakedMeshSize, positions, normals, uvs, materialIndicesBuffer, triangles, materialsBuffer, materialLinksBuffer, materialLinkNamesBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Mesh);
            return command;
        }

        public static NetCommand BuildAddCollecitonCommand(string collectionName)
        {
            byte[] collectionNameBuffer = StringToBytes(collectionName);
            byte[] visible = BoolToBytes(true);
            byte[] offset = Vector3ToBytes(Vector3.zero);
            byte[] temporaryVisible = BoolToBytes(true);
            List<byte[]> buffers = new List<byte[]> { collectionNameBuffer, visible, offset, temporaryVisible };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Collection);
            return command;
        }

        public static NetCommand BuildAddCollectionToScene(string collectionName)
        {
            byte[] sceneNameBuffer = StringToBytes(SyncData.currentSceneName);
            byte[] collectionNameBuffer = StringToBytes(collectionName);
            List<byte[]> buffers = new List<byte[]> { sceneNameBuffer, collectionNameBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.AddCollectionToScene);
            SyncData.sceneCollections.Add(collectionName);
            return command;
        }


        public static NetCommand BuildAddObjectToCollecitonCommand(AddToCollectionInfo info)
        {
            byte[] collectionNameBuffer = StringToBytes(info.collectionName);
            byte[] objectNameBuffer = StringToBytes(info.transform.name);

            List<byte[]> buffers = new List<byte[]> { collectionNameBuffer, objectNameBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.AddObjectToCollection);
            return command;
        }

        public static NetCommand BuildAddObjectToScene(AddObjectToSceneInfo info)
        {
            byte[] sceneNameBuffer = StringToBytes(SyncData.currentSceneName);
            byte[] objectNameBuffer = StringToBytes(info.transform.name);
            List<byte[]> buffers = new List<byte[]> { sceneNameBuffer, objectNameBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.AddObjectToDocument);
            return command;
        }

        public static NetCommand BuildDeleteCommand(Transform root, DeleteInfo deleteInfo)
        {
            Transform transform = deleteInfo.meshTransform;
            string path = GetPathName(root, transform);
            byte[] pathBuffer = StringToBytes(path);

            List<byte[]> buffers = new List<byte[]> { pathBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Delete);
            return command;
        }

        public static void BuildAnimation(Transform root, byte[] data)
        {
            int currentIndex = 0;
            string objectName = GetString(data, ref currentIndex);
            string animationChannel = GetString(data, ref currentIndex);
            int keyChannelIndex = BitConverter.ToInt32(data, currentIndex);
            currentIndex += 4;

            int keyCount = (int) BitConverter.ToUInt32(data, currentIndex);
            currentIndex += 4;

            int[] intBuffer = new int[keyCount];
            float[] floatBuffer = new float[keyCount];
            int[] interpolationBuffer = new int[keyCount];

            Buffer.BlockCopy(data, currentIndex, intBuffer, 0, keyCount * sizeof(int));
            Buffer.BlockCopy(data, currentIndex + keyCount * sizeof(int), floatBuffer, 0, keyCount * sizeof(float));
            Buffer.BlockCopy(data, currentIndex + (keyCount * sizeof(int)) + (keyCount * sizeof(float)), interpolationBuffer, 0, keyCount * sizeof(int));

            //AnimationKey[] keys = new AnimationKey[keyCount];
            //Buffer.BlockCopy(data, currentIndex, keys, 0, (int)keyCount * 2 * sizeof(float));

            List<AnimationKey> keys = new List<AnimationKey>();
            for (int i = 0; i < keyCount; i++)
            {
                keys.Add(new AnimationKey(intBuffer[i], floatBuffer[i], (Interpolation) interpolationBuffer[i]));
            }

            Node node = SyncData.nodes[objectName];
            GlobalState.Instance.AddAnimationChannel(node.prefab, animationChannel, keyChannelIndex, keys);

            // Apply to instances
            foreach (Tuple<GameObject, string> t in node.instances)
            {
                GameObject gobj = t.Item1;
                GlobalState.Instance.AddAnimationChannel(gobj, animationChannel, keyChannelIndex, keys);
            }
        }

        public static void BuildCameraAttributes(Transform root, byte[] data)
        {
            int currentIndex = 0;
            string cameraName = GetString(data, ref currentIndex);

            Node node = SyncData.nodes[cameraName];
            CameraController cameraController = node.prefab.GetComponent<CameraController>();
            cameraController.focal = GetFloat(data, ref currentIndex);

            //float aperture = GetFloat(data, ref currentIndex);
            //float focus_distance = GetFloat(data, ref currentIndex);

            // Apply to instances
            bool recording = GlobalState.Instance.recordState == GlobalState.RecordState.Recording;
            foreach (Tuple<GameObject, string> t in node.instances)
            {
                GameObject gobj = t.Item1;
                if (recording && Selection.IsSelected(gobj))
                    continue;

                CameraController controller = gobj.GetComponent<CameraController>();
                controller.focal = cameraController.focal;
                //GlobalState.Instance.FireAnimationChanged(gobj);
            }
        }

        public static void BuildLightAttributes(Transform root, byte[] data)
        {
            int currentIndex = 0;
            string lightName = GetString(data, ref currentIndex);

            Node node = SyncData.nodes[lightName];

            LightController lightController = node.prefab.GetComponent<LightController>();
            float power = GetFloat(data, ref currentIndex);
            lightController.SetPower(power);
            lightController.color = GetColor(data, ref currentIndex);


            // Apply to instances
            bool recording = GlobalState.Instance.recordState == GlobalState.RecordState.Recording;
            foreach (Tuple<GameObject, string> t in node.instances)
            {
                GameObject gobj = t.Item1;
                if (recording && Selection.IsSelected(gobj))
                    continue;

                LightController controller = gobj.GetComponent<LightController>();
                controller.intensity = lightController.intensity;
                controller.color = lightController.color;
            }
        }

        public static void BuildCamera(Transform root, byte[] data)
        {
            int currentIndex = 0;
            Transform transform = BuildPath(data, ref currentIndex, false);
            if (transform == null)
                transform = root;

            currentIndex = 0;
            string leafName = GetString(data, ref currentIndex);
            int index = leafName.LastIndexOf('/');
            if (index != -1)
            {
                leafName = leafName.Substring(index + 1, leafName.Length - index - 1);
            }
            string name = GetString(data, ref currentIndex);

            GameObject camGameObject = null;
            Node node = null;
            if (!SyncData.nodes.ContainsKey(leafName))
            {
                camGameObject = Utils.CreateInstance(Resources.Load("Prefabs/Camera") as GameObject, transform, leafName, isPrefab: true);
                node = SyncData.CreateNode(name);
                node.prefab = camGameObject;
                CameraController camera = camGameObject.GetComponentInChildren<CameraController>(true);
                camera.controllerName = name;

                //camGameObject.transform.GetChild(0).Rotate(0f, 180f, 0f);
                //camGameObject.transform.GetChild(0).localScale = new Vector3(-1, 1, 1);
            }
            else // TODO: found a case where a camera was found (don't know when it was created???), but had no Camera child object.
            {
                node = SyncData.nodes[leafName];
                camGameObject = node.prefab;
            }

            float focal = BitConverter.ToSingle(data, currentIndex);
            float near = BitConverter.ToSingle(data, currentIndex + sizeof(float));
            float far = BitConverter.ToSingle(data, currentIndex + 2 * sizeof(float));
            float aperture = BitConverter.ToSingle(data, currentIndex + 3 * sizeof(float));
            currentIndex += 4 * sizeof(float);

            Camera.GateFitMode gateFit = (Camera.GateFitMode) BitConverter.ToInt32(data, currentIndex);
            if (gateFit == Camera.GateFitMode.None)
                gateFit = Camera.GateFitMode.Horizontal;
            currentIndex += sizeof(Int32);

            float sensorWidth = BitConverter.ToSingle(data, currentIndex);
            currentIndex += sizeof(float);
            float sensorHeight = BitConverter.ToSingle(data, currentIndex);
            currentIndex += sizeof(float);

            Camera cam = camGameObject.GetComponentInChildren<Camera>(true);

            // TMP fix for a weird case.
            if (cam == null)
                return;

            CameraController cameraController = camGameObject.GetComponent<CameraController>();
            cameraController.focal = focal;

            cam.focalLength = focal;
            cam.gateFit = gateFit;
            cam.focalLength = focal;
            cam.sensorSize = new Vector2(sensorWidth, sensorHeight);

            //GlobalState.Instance.FireAnimationChanged(camGameObject);
        }

        public static void BuildLight(Transform root, byte[] data)
        {
            int currentIndex = 0;
            Transform transform = BuildPath(data, ref currentIndex, false);
            if (transform == null)
                transform = root;

            currentIndex = 0;
            string leafName = GetString(data, ref currentIndex);
            int index = leafName.LastIndexOf('/');
            if (index != -1)
            {
                leafName = leafName.Substring(index + 1, leafName.Length - index - 1);
            }
            string name = GetString(data, ref currentIndex);

            LightType lightType = (LightType) BitConverter.ToInt32(data, currentIndex);
            currentIndex += sizeof(Int32);

            GameObject lightGameObject = null;
            Node node = null;
            if (!SyncData.nodes.ContainsKey(leafName))
            {
                switch (lightType)
                {
                    case LightType.Directional:
                        lightGameObject = Utils.CreateInstance(Resources.Load("Prefabs/Sun") as GameObject, transform, leafName);
                        break;
                    case LightType.Point:
                        lightGameObject = Utils.CreateInstance(Resources.Load("Prefabs/Point") as GameObject, transform, leafName);
                        break;
                    case LightType.Spot:
                        lightGameObject = Utils.CreateInstance(Resources.Load("Prefabs/Spot") as GameObject, transform, leafName);
                        break;
                    default:
                        return;
                }
                node = SyncData.CreateNode(leafName);
                node.prefab = lightGameObject;
                LightController light = lightGameObject.GetComponentInChildren<LightController>(true);
                light.controllerName = name;
            }
            else
            {
                node = SyncData.nodes[leafName];
                lightGameObject = node.prefab;
            }

            // Read data
            int shadow = BitConverter.ToInt32(data, currentIndex);
            currentIndex += sizeof(Int32);

            float ColorR = BitConverter.ToSingle(data, currentIndex);
            float ColorG = BitConverter.ToSingle(data, currentIndex + sizeof(float));
            float ColorB = BitConverter.ToSingle(data, currentIndex + 2 * sizeof(float));
            float ColorA = BitConverter.ToSingle(data, currentIndex + 3 * sizeof(float));
            Color lightColor = new Color(ColorR, ColorG, ColorB, ColorA);
            currentIndex += 4 * sizeof(float);

            float power = BitConverter.ToSingle(data, currentIndex);
            currentIndex += sizeof(float);
            float spotSize = BitConverter.ToSingle(data, currentIndex);
            currentIndex += sizeof(float);
            float spotBlend = BitConverter.ToSingle(data, currentIndex);
            currentIndex += sizeof(float);

            // Set data to all instances
            LightController lightController = lightGameObject.GetComponentInChildren<LightController>(true);
            if (!lightController)
                return;
            lightController.lightType = lightType;
            lightController.color = lightColor;
            lightController.SetPower(power);
            if (lightType == LightType.Spot)
            {
                lightController.range = 5f;
                lightController.outerAngle = spotSize * 180f / 3.14f;
                lightController.innerAngle = (1f - spotBlend) * 100f;
            }
            lightController.castShadows = shadow != 0 ? true : false;

            foreach (Tuple<GameObject, string> t in node.instances)
            {
                GameObject gobj = t.Item1;
                LightController lightContr = gobj.GetComponentInChildren<LightController>(true);

                lightContr.CopyParameters(lightController);
            }

            //GlobalState.Instance.FireAnimationChanged(lightGameObject);
        }

        public static void BuildSky(byte[] data)
        {
            int currentIndex = 0;
            string skyName = GetString(data, ref currentIndex);
            Color topColor = GetColor(data, ref currentIndex);
            Color middleColor = GetColor(data, ref currentIndex);
            Color bottomColor = GetColor(data, ref currentIndex);

            Sky.SetSkyColors(new SkySettings { topColor = topColor, middleColor = middleColor, bottomColor = bottomColor });
        }

        public static NetCommand BuildSendClearAnimations(ClearAnimationInfo info)
        {
            NetCommand command = new NetCommand(StringToBytes(info.gObject.name), MessageType.ClearAnimations);
            return command;
        }

        public static NetCommand BuildSendMontageMode(bool montage)
        {
            NetCommand command = new NetCommand(BoolToBytes(montage), MessageType.ShotManagerMontageMode);
            return command;
        }

        public static NetCommand BuildSendShotManagerAction(ShotManagerActionInfo info)
        {
            NetCommand command = null;
            List<byte[]> buffers;
            byte[] shotName;
            byte[] start;
            byte[] end;
            byte[] camera;
            byte[] color;
            byte[] enabled;

            byte[] action = IntToBytes((int) info.action);
            byte[] shotIndex = IntToBytes(info.shotIndex);

            switch (info.action)
            {
                case ShotManagerAction.AddShot:
                    {
                        byte[] nextShotIndex = IntToBytes(info.shotIndex + 1);
                        shotName = StringToBytes(info.shotName);
                        start = IntToBytes(info.shotStart);
                        end = IntToBytes(info.shotEnd);
                        camera = StringToBytes(info.cameraName);
                        color = ColorToBytes(info.shotColor);
                        buffers = new List<byte[]> { action, nextShotIndex, shotName, start, end, camera, color };
                        command = new NetCommand(ConcatenateBuffers(buffers), MessageType.ShotManagerAction);
                    }
                    break;
                case ShotManagerAction.DeleteShot:
                    buffers = new List<byte[]> { action, shotIndex };
                    command = new NetCommand(ConcatenateBuffers(buffers), MessageType.ShotManagerAction);
                    break;
                case ShotManagerAction.DuplicateShot:
                    shotName = StringToBytes(info.shotName);
                    buffers = new List<byte[]> { action, shotIndex, shotName };
                    command = new NetCommand(ConcatenateBuffers(buffers), MessageType.ShotManagerAction);
                    break;
                case ShotManagerAction.MoveShot:
                    byte[] offset = IntToBytes(info.moveOffset);
                    buffers = new List<byte[]> { action, shotIndex, offset };
                    command = new NetCommand(ConcatenateBuffers(buffers), MessageType.ShotManagerAction);
                    break;
                case ShotManagerAction.UpdateShot:
                    start = IntToBytes(info.shotStart);
                    end = IntToBytes(info.shotEnd);
                    camera = StringToBytes(info.cameraName);
                    color = ColorToBytes(info.shotColor);
                    enabled = IntToBytes(info.shotEnabled);
                    buffers = new List<byte[]> { action, shotIndex, start, end, camera, color, enabled };
                    command = new NetCommand(ConcatenateBuffers(buffers), MessageType.ShotManagerAction);
                    break;
            }
            return command;
        }

        public static NetCommand BuildSendPlayerTransform(ConnectedUser playerInfo)
        {
            string json = JsonHelper.CreateJsonPlayerInfo(playerInfo);
            if (null == json) { return null; }
            byte[] buffer = StringToBytes(json);
            NetCommand command = new NetCommand(buffer, MessageType.SetClientCustomAttribute);
            return command;
        }

        public static MeshFilter GetOrCreateMeshFilter(GameObject obj)
        {
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            if (meshFilter == null)
                meshFilter = obj.AddComponent<MeshFilter>();
            return meshFilter;
        }

        public static MeshRenderer GetOrCreateMeshRenderer(GameObject obj)
        {
            MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                meshRenderer = obj.AddComponent<MeshRenderer>();
            return meshRenderer;
        }
        public static MeshCollider GetOrCreateMeshCollider(GameObject obj)
        {
            MeshCollider meshCollider = obj.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                meshCollider = obj.AddComponent<MeshCollider>();
                //meshCollider.convex = true;
            }
            return meshCollider;
        }

        public static Transform ConnectMesh(Transform transform, Mesh mesh)
        {
            GameObject gobject = transform.gameObject;

            gobject.tag = "PhysicObject";

            MeshFilter filter = GetOrCreateMeshFilter(gobject);
            string meshName = mesh.name;

            //foreach (MeshFilter filter in meshInstances[meshName])
            {
                filter.mesh = mesh;
                GameObject obj = filter.gameObject;
                MeshRenderer meshRenderer = GetOrCreateMeshRenderer(obj);

                ApplyMaterialParameters(meshRenderer, meshesMaterials[meshName]);
                GetOrCreateMeshCollider(obj);

                if (SyncData.nodes.ContainsKey(obj.name))
                {
                    foreach (Tuple<GameObject, string> t in SyncData.nodes[obj.name].instances)
                    {
                        GameObject instance = t.Item1;
                        MeshFilter instanceMeshFilter = GetOrCreateMeshFilter(instance);
                        instanceMeshFilter.mesh = mesh;

                        MeshRenderer instanceMeshRenderer = GetOrCreateMeshRenderer(instance);
                        ApplyMaterialParameters(instanceMeshRenderer, meshesMaterials[meshName]);

                        MeshCollider meshCollider = GetOrCreateMeshCollider(instance);
                        meshCollider.sharedMesh = null;
                        meshCollider.sharedMesh = mesh;
                    }
                }
            }

            MeshCollider collider = gobject.GetComponent<MeshCollider>();
            if (null != collider)
            {
                collider.sharedMesh = null;
                collider.sharedMesh = mesh;
            }

            return transform;
        }

        public static Mesh BuildMesh(byte[] data)
        {
            int currentIndex = 0;
            Transform transform = BuildPath(data, ref currentIndex, true);
            string meshName = GetString(data, ref currentIndex);

            int baseMeshDataSize = (int) BitConverter.ToUInt32(data, currentIndex);
            currentIndex += 4 + baseMeshDataSize;

            int bakedMeshDataSize = (int) BitConverter.ToUInt32(data, currentIndex);
            currentIndex += 4;
            if (bakedMeshDataSize == 0)
                return null;

            int rawVerticesCount = (int) BitConverter.ToUInt32(data, currentIndex);
            currentIndex += 4;
            int size = rawVerticesCount * sizeof(float) * 3;
            Vector3[] rawVertices = new Vector3[rawVerticesCount];
            float[] float3Values = new float[rawVerticesCount * 3];
            Buffer.BlockCopy(data, currentIndex, float3Values, 0, size);
            int idx = 0;
            for (int i = 0; i < rawVerticesCount; i++)
            {
                rawVertices[i].x = float3Values[idx++];
                rawVertices[i].y = float3Values[idx++];
                rawVertices[i].z = float3Values[idx++];
            }
            currentIndex += size;

            int normalsCount = (int) BitConverter.ToUInt32(data, currentIndex);
            currentIndex += 4;
            size = normalsCount * sizeof(float) * 3;
            Vector3[] normals = new Vector3[normalsCount];
            float3Values = new float[normalsCount * 3];
            Buffer.BlockCopy(data, currentIndex, float3Values, 0, size);
            idx = 0;
            for (int i = 0; i < normalsCount; i++)
            {
                normals[i].x = float3Values[idx++];
                normals[i].y = float3Values[idx++];
                normals[i].z = float3Values[idx++];
            }
            currentIndex += size;

            UInt32 UVsCount = BitConverter.ToUInt32(data, currentIndex);
            currentIndex += 4;

            size = (int) UVsCount * sizeof(float) * 2;
            Vector2[] uvs = new Vector2[UVsCount];
            Buffer.BlockCopy(data, currentIndex, float3Values, 0, size);
            idx = 0;
            for (int i = 0; i < UVsCount; i++)
            {
                uvs[i].x = float3Values[idx++];
                uvs[i].y = float3Values[idx++];
            }
            currentIndex += size;

            int materialIndicesCount = (int) BitConverter.ToUInt32(data, currentIndex);
            currentIndex += 4;
            int[] materialIndices = new int[materialIndicesCount];
            size = materialIndicesCount * sizeof(int);
            Buffer.BlockCopy(data, currentIndex, materialIndices, 0, size);
            currentIndex += size;

            int rawIndicesCount = (int) BitConverter.ToUInt32(data, currentIndex) * 3;
            currentIndex += 4;
            int[] rawIndices = new int[rawIndicesCount];
            size = rawIndicesCount * sizeof(int);
            Buffer.BlockCopy(data, currentIndex, rawIndices, 0, size);
            currentIndex += size;

            Vector3[] vertices = new Vector3[rawIndicesCount];
            for (int i = 0; i < rawIndicesCount; i++)
            {
                vertices[i] = rawVertices[rawIndices[i]];
            }

            int materialCount = (int) BitConverter.ToUInt32(data, currentIndex);
            currentIndex += 4;
            List<MaterialParameters> meshMaterialParameters = new List<MaterialParameters>();
            if (materialCount == 0)
            {
                meshMaterialParameters.Add(DefaultMaterial());
                materialCount = 1;
            }
            else
            {
                for (int i = 0; i < materialCount; i++)
                {
                    int materialNameSize = (int) BitConverter.ToUInt32(data, currentIndex);
                    string materialName = System.Text.Encoding.UTF8.GetString(data, currentIndex + 4, materialNameSize);
                    currentIndex += materialNameSize + 4;

                    if (materialsParameters.TryGetValue(materialName, out MaterialParameters materialParameters))
                    {
                        meshMaterialParameters.Add(materialParameters);
                    }
                    else
                    {
                        meshMaterialParameters.Add(DefaultMaterial());
                    }
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = meshName;
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            if (materialCount == 1) // only one submesh
            {
                int[] indices = new int[rawIndicesCount];
                for (int i = 0; i < rawIndicesCount; i++)
                {
                    indices[i] = i;
                }

                mesh.triangles = indices;
            }
            else
            {
                List<int>[] subIndicesArray = new List<int>[materialCount];
                for (int i = 0; i < materialCount; i++)
                {
                    subIndicesArray[i] = new List<int>();
                }

                for (int i = 0; i < materialIndicesCount; i++)
                {
                    int materialIndex = materialIndices[i];
                    List<int> subIndices = subIndicesArray[materialIndex];
                    int index = 3 * i;
                    subIndices.Add(index);
                    subIndices.Add(index + 1);
                    subIndices.Add(index + 2);
                }

                mesh.subMeshCount = materialCount;
                for (int i = 0; i < materialCount; i++)
                {
                    mesh.SetTriangles(subIndicesArray[i].ToArray(), i);
                }

            }

            mesh.RecalculateBounds();
            meshes[meshName] = mesh;
            meshesMaterials[meshName] = meshMaterialParameters;

            ConnectMesh(transform, mesh);
            return mesh;
        }

        public static void BuildGreasePencil(byte[] data)
        {
            int currentIndex = 0;
            string greasePencilPath = GetString(data, ref currentIndex);
            string greasePencilName = GetString(data, ref currentIndex);
            string[] path = greasePencilPath.Split('/');
            path[path.Length - 1] = greasePencilName;
            Transform prefab = SyncData.GetOrCreatePrefabPath(String.Join("/", path));

            SyncData.greasePencilsNameToPrefab[greasePencilName] = prefab.name;
        }


        private static bool IsFillEnabled(string materialName)
        {
            string name = materialName + "_fill";
            return materialsFillEnabled.Contains(name);
        }

        private static bool IsStrokeEnabled(string materialName)
        {
            string name = materialName + "_stroke";
            return materialStrokesEnabled.Contains(name);
        }

        private static void CreateStroke(float[] points, int numPoints, int lineWidth, Vector3 offset, ref GPStroke subMesh)
        {
            FreeDraw freeDraw = new FreeDraw();
            for (int i = 0; i < numPoints; i++)
            {
                Vector3 position = new Vector3(points[i * 5 + 0] + offset.x, points[i * 5 + 1] + offset.y, points[i * 5 + 2] + offset.z);
                float ratio = lineWidth * 0.0006f * points[i * 5 + 3];  // pressure
                freeDraw.AddRawControlPoint(position, ratio);
            }
            subMesh.vertices = freeDraw.vertices;
            subMesh.triangles = freeDraw.triangles;
        }

        private static void CreateFill(float[] points, int numPoints, Vector3 offset, ref GPStroke subMesh)
        {
            Vector3[] p3D = new Vector3[numPoints];
            for (int i = 0; i < numPoints; i++)
            {
                p3D[i].x = points[i * 5 + 0];
                p3D[i].y = points[i * 5 + 1];
                p3D[i].z = points[i * 5 + 2];
            }

            Vector3 x = Vector3.right;
            Vector3 y = Vector3.up;
            Vector3 z = Vector3.forward;
            Matrix4x4 mat = Matrix4x4.identity;
            if (numPoints >= 3)
            {
                Vector3 p0 = p3D[0];
                Vector3 p1 = p3D[numPoints / 3];
                Vector3 p2 = p3D[2 * numPoints / 3];

                x = (p1 - p0).normalized;
                y = (p2 - p1).normalized;
                if (x != y)
                {
                    z = Vector3.Cross(x, y).normalized;
                    x = Vector3.Cross(y, z).normalized;
                    Vector4 pos = new Vector4(p0.x, p0.y, p0.z, 1);
                    mat = new Matrix4x4(x, y, z, pos);
                }
            }
            Matrix4x4 invMat = mat.inverse;

            Vector3[] p3D2 = new Vector3[numPoints];
            for (int i = 0; i < numPoints; i++)
            {
                p3D2[i] = invMat.MultiplyPoint(p3D[i]);
            }


            Vector2[] p = new Vector2[numPoints];
            for (int i = 0; i < numPoints; i++)
            {
                p[i].x = p3D2[i].x;
                p[i].y = p3D2[i].y;
            }

            Vector2[] outputVertices;
            int[] indices;

            Triangulator.Triangulator.Triangulate(p, Triangulator.WindingOrder.CounterClockwise, out outputVertices, out indices);

            Vector3[] positions = new Vector3[outputVertices.Length];
            for (int i = 0; i < outputVertices.Length; i++)
            {
                positions[i] = mat.MultiplyPoint(new Vector3(outputVertices[i].x, outputVertices[i].y)) + offset;
            }

            subMesh.vertices = positions;
            subMesh.triangles = indices;
        }

        /*
        private static void CreateFill(float[] points, int numPoints, Vector3 offset, ref GPStroke subMesh)
        {
            Vector3[] p3D = new Vector3[numPoints];
            for (int i = 0; i < numPoints; i++)
            {
                int invI = i;// numPoints - i - 1;
                p3D[invI].x = points[i * 5 + 0];
                p3D[invI].y = points[i * 5 + 1];
                p3D[invI].z = points[i * 5 + 2];
            }

            Vector3 x = Vector3.right;
            Vector3 y = Vector3.up;
            Vector3 z = Vector3.forward;
            Matrix4x4 mat = Matrix4x4.identity;
            if (numPoints >= 3)
            {
                Vector3 p0 = p3D[0];
                Vector3 p1 = p3D[numPoints / 3];
                Vector3 p2 = p3D[2 * numPoints / 3];

                x = (p1 - p0).normalized;
                y = (p2 - p1).normalized;
                z = Vector3.Cross(x, y).normalized;
                x = Vector3.Cross(y, z).normalized;
                Vector4 pos = new Vector4(p0.x, p0.y, p0.z, 1);
                mat = new Matrix4x4(x, y, z, pos);
            }
            Matrix4x4 invMat = mat.inverse;

            List<Vector3> p3D2 = new List<Vector3>();
            for (int i = 0; i < numPoints; i++)
            {
                p3D2.Add(invMat.MultiplyPoint(p3D[i]));
            }

            List<Triangle> triangles = Triangulator2.TriangulateConcavePolygon(p3D2);
            Vector3[] positions = new Vector3[triangles.Count * 3];
            int[] indices = new int[triangles.Count * 3];

            int indice = 0;
            foreach(Triangle triangle in triangles)
            {
                positions[indice] = mat.MultiplyPoint(triangle.v1.position);
                positions[indice + 1] = mat.MultiplyPoint(triangle.v2.position);
                positions[indice + 2] = mat.MultiplyPoint(triangle.v3.position);

                indices[indice] = indice;
                indices[indice + 1] = indice + 1;
                indices[indice + 2] = indice + 2;
                indice += 3;
            }

            subMesh.vertices = positions;
            subMesh.triangles = indices;
        }
        */

        public static MaterialParameters BuildGreasePencilMaterial(string materialName, Color color)
        {
            if (!materialsParameters.TryGetValue(materialName, out MaterialParameters materialParameters))
            {
                materialParameters = new MaterialParameters();
                materialParameters.materialType = MaterialType.GreasePencil;
                materialParameters.name = materialName;
                materialsParameters[materialName] = materialParameters;
            }

            materialParameters.baseColor = color;

            return materialParameters;
        }


        public static void BuildGreasePencilMaterial(byte[] data)
        {
            int currentIndex = 0;
            string materialName = GetString(data, ref currentIndex);
            bool strokeEnabled = GetBool(data, ref currentIndex);
            string strokeMode = GetString(data, ref currentIndex);
            string strokeStyle = GetString(data, ref currentIndex);
            Color strokeColor = GetColor(data, ref currentIndex);
            bool strokeOverlap = GetBool(data, ref currentIndex);
            bool fillEnabled = GetBool(data, ref currentIndex);
            string fillStyle = GetString(data, ref currentIndex);
            Color fillColor = GetColor(data, ref currentIndex);

            string materialStrokeName = materialName + "_stroke";
            string materialFillName = materialName + "_fill";
            MaterialParameters strokeMaterial = BuildGreasePencilMaterial(materialStrokeName, strokeColor);
            MaterialParameters fillMaterial = BuildGreasePencilMaterial(materialFillName, fillColor);

            // stroke enable
            if (strokeEnabled)
            {
                materialStrokesEnabled.Add(materialStrokeName);
            }
            else
            {
                if (materialStrokesEnabled.Contains(materialStrokeName))
                    materialStrokesEnabled.Remove(materialStrokeName);
            }

            // fill
            if (fillEnabled)
            {
                materialsFillEnabled.Add(materialFillName);
            }
            else
            {
                if (materialsFillEnabled.Contains(materialFillName))
                    materialsFillEnabled.Remove(materialFillName);
            }
        }

        public static void BuildStroke(byte[] data, ref int currentIndex, string[] materialNames, int layerIndex, int strokeIndex, ref GPFrame frame)
        {
            int materialIndex = GetInt(data, ref currentIndex);
            int lineWidth = GetInt(data, ref currentIndex);
            int numPoints = GetInt(data, ref currentIndex);
            float[] points = new float[5 * numPoints];

            int dataSize = 5 * sizeof(float) * numPoints;
            Buffer.BlockCopy(data, currentIndex, points, 0, dataSize);
            currentIndex += dataSize;

            float layerOffset = 0.001f * layerIndex;
            float strokeOffset = 0.0001f * strokeIndex;

            if ((materialIndex < materialNames.Length) && IsStrokeEnabled(materialNames[materialIndex]))
            {
                Vector3 offset = new Vector3(0.0f, -(strokeOffset + layerOffset), 0.0f);
                GPStroke subMesh = new GPStroke();
                CreateStroke(points, numPoints, lineWidth, offset, ref subMesh);
                subMesh.materialParameters = materialsParameters[materialNames[materialIndex] + "_stroke"];
                frame.strokes.Add(subMesh);
            }

            if ((materialIndex < materialNames.Length) && IsFillEnabled(materialNames[materialIndex]))
            {
                Vector3 offset = new Vector3(0.0f, -(strokeOffset + layerOffset), 0.0f);
                GPStroke subMesh = new GPStroke();
                CreateFill(points, numPoints, offset, ref subMesh);
                subMesh.materialParameters = materialsParameters[materialNames[materialIndex] + "_fill"];
                frame.strokes.Add(subMesh);
            }
        }

        public static void BuildFrame(byte[] data, ref int currentIndex, string[] materialNames, int layerIndex, ref GPLayer layer, int frameIndex)
        {
            int frameNumber = GetInt(data, ref currentIndex);
            if (frameIndex == 0)
                frameNumber = 0;
            GPFrame frame = new GPFrame(frameNumber);
            layer.frames.Add(frame);

            int strokeCount = GetInt(data, ref currentIndex);
            for (int strokeIndex = 0; strokeIndex < strokeCount; strokeIndex++)
            {
                BuildStroke(data, ref currentIndex, materialNames, layerIndex, strokeIndex, ref frame);
            }
        }


        public static void BuildLayer(byte[] data, ref int currentIndex, string[] materialNames, int layerIndex, ref List<GPLayer> layers)
        {
            string layerName = GetString(data, ref currentIndex);
            bool hidden = GetBool(data, ref currentIndex);
            GPLayer layer = new GPLayer(layerName);
            layer.visible = !hidden;
            layers.Add(layer);

            int frameCount = GetInt(data, ref currentIndex);
            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                BuildFrame(data, ref currentIndex, materialNames, layerIndex, ref layer, frameIndex);
            }
        }

        public static Tuple<Mesh, List<MaterialParameters>> BuildGPFrameMesh(List<GPStroke> strokes)
        {
            // Build mesh from sub-meshes
            int vertexCount = 0;
            foreach (var meshMaterial in strokes)
            {
                vertexCount += meshMaterial.vertices.Length;
            }

            Vector3[] vertices = new Vector3[vertexCount];
            int currentVertexIndex = 0;

            foreach (var subMesh in strokes)
            {
                Array.Copy(subMesh.vertices, 0, vertices, currentVertexIndex, subMesh.vertices.Length);
                currentVertexIndex += subMesh.vertices.Length;
            }

            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.subMeshCount = strokes.Count;
            mesh.vertices = vertices;

            int currentSubMesh = 0;
            List<MaterialParameters> mats = new List<MaterialParameters>();

            Tuple<Mesh, List<MaterialParameters>> result = new Tuple<Mesh, List<MaterialParameters>>(mesh, mats);

            int currentIndexIndex = 0;
            foreach (var subMesh in strokes)
            {
                int verticesCount = subMesh.vertices.Length;
                int[] triangles = new int[subMesh.triangles.Length];
                for (int i = 0; i < subMesh.triangles.Length; i++)
                {
                    triangles[i] = subMesh.triangles[i] + currentIndexIndex;
                }

                mesh.SetTriangles(triangles, currentSubMesh++);
                mats.Add(subMesh.materialParameters);

                currentIndexIndex += verticesCount;
            }
            return result;
        }

        static SortedSet<int> GetFrames(List<GPLayer> layers)
        {
            SortedSet<int> frames = new SortedSet<int>();
            foreach (GPLayer layer in layers)
            {
                foreach (GPFrame frame in layer.frames)
                    frames.Add(frame.frame);
            }

            return frames;
        }

        static List<GPFrame> GetGPFrames(List<GPLayer> layers, int f)
        {
            List<GPFrame> frames = new List<GPFrame>();
            foreach (GPLayer layer in layers)
            {
                if (!layer.visible)
                    continue;
                for (int i = layer.frames.Count - 1; i >= 0; --i)
                {
                    GPFrame gpframe = layer.frames[i];
                    if (gpframe.frame <= f)
                    {
                        frames.Add(gpframe);
                        break;
                    }
                }
            }
            return frames;
        }

        static List<GPStroke> GetStrokes(List<GPFrame> frames)
        {
            List<GPStroke> strokes = new List<GPStroke>();
            foreach (GPFrame frame in frames)
            {
                foreach (GPStroke stroke in frame.strokes)
                    strokes.Add(stroke);
            }
            return strokes;
        }

        public static void BuildGreasePencilMesh(byte[] data)
        {
            int currentIndex = 0;
            string name = GetString(data, ref currentIndex);

            int materialCount = GetInt(data, ref currentIndex);
            string[] materialNames = new string[materialCount];
            for (int i = 0; i < materialCount; i++)
            {
                materialNames[i] = GetString(data, ref currentIndex);
            }

            List<GPStroke> subMeshes = new List<GPStroke>();
            List<GPLayer> layers = new List<GPLayer>();

            int layerCount = GetInt(data, ref currentIndex);
            for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
            {
                BuildLayer(data, ref currentIndex, materialNames, layerIndex, ref layers);
            }

            SortedSet<int> frames = GetFrames(layers);

            GreasePencilData gpdata = new GreasePencilData();
            greasePencils[name] = gpdata;

            if (frames.Count == 0)
                return;
            foreach (int frame in frames)
            {
                List<GPFrame> gpframes = GetGPFrames(layers, frame);
                List<GPStroke> strokes = GetStrokes(gpframes);

                Tuple<Mesh, List<MaterialParameters>> meshData = BuildGPFrameMesh(strokes);

                meshData.Item1.RecalculateBounds();
                gpdata.AddMesh(frame, new Tuple<Mesh, List<MaterialParameters>>(meshData.Item1, meshData.Item2));
            }
        }
        public static void BuildGreasePencilConnection(byte[] data)
        {
            int currentIndex = 0;
            Transform transform = BuildPath(data, ref currentIndex, true);
            string greasePencilName = GetString(data, ref currentIndex);

            GameObject gobject = transform.gameObject;
            GreasePencilBuilder greasePencilBuilder = gobject.GetComponent<GreasePencilBuilder>();
            if (null == greasePencilBuilder)
                greasePencilBuilder = gobject.AddComponent<GreasePencilBuilder>();

            GreasePencil greasePencil = gobject.GetComponent<GreasePencil>();
            if (null == greasePencil)
                greasePencil = gobject.AddComponent<GreasePencil>();

            GreasePencilData gpdata = greasePencils[greasePencilName];
            greasePencil.data = gpdata;

            Node prefab = SyncData.nodes[transform.name];
            foreach (var item in prefab.instances)
            {
                GreasePencil greasePencilInstance = item.Item1.GetComponent<GreasePencil>();
                greasePencilInstance.data = gpdata;
                greasePencilInstance.ForceUpdate();
            }

            gobject.tag = "PhysicObject";
        }

        public static void BuildGreasePencilTimeOffset(byte[] data)
        {
            int currentIndex = 0;
            string name = GetString(data, ref currentIndex);
            GreasePencilData gpData = greasePencils[name];
            gpData.frameOffset = GetInt(data, ref currentIndex);
            gpData.frameScale = GetFloat(data, ref currentIndex);
            gpData.hasCustomRange = GetBool(data, ref currentIndex);
            gpData.rangeStartFrame = GetInt(data, ref currentIndex);
            gpData.rangeEndFrame = GetInt(data, ref currentIndex);

        }

        public static void BuildPlay()
        {
            GlobalState.Instance.SetPlaying(true);
        }

        public static void BuildPause()
        {
            GlobalState.Instance.SetPlaying(false);
        }

        public static void BuildFrame(byte[] data)
        {
            int index = 0;
            int frame = GetInt(data, ref index);

            GlobalState.currentFrame = frame;
        }

        public static NetCommand BuildSendFrameCommand(int frame)
        {
            byte[] masterIdBuffer = NetGeometry.StringToBytes(GlobalState.networkUser.masterId);
            byte[] messageTypeBuffer = NetGeometry.IntToBytes((int) MessageType.Frame);
            byte[] frameBuffer = NetGeometry.IntToBytes(frame);
            List<byte[]> buffers = new List<byte[]> { masterIdBuffer, messageTypeBuffer, frameBuffer };
            byte[] buffer = ConcatenateBuffers(buffers);
            return new NetCommand(buffer, MessageType.ClientIdWrapper);
        }

        public static NetCommand BuildSendFrameStartEndCommand(int start, int end)
        {
            byte[] startBuffer = NetGeometry.IntToBytes((int) start);
            byte[] endBuffer = NetGeometry.IntToBytes((int) end);
            List<byte[]> buffers = new List<byte[]> { startBuffer, endBuffer };
            byte[] buffer = ConcatenateBuffers(buffers);
            return new NetCommand(buffer, MessageType.FrameStartEnd);
        }

        public static NetCommand BuildSendSetKey(SetKeyInfo data)
        {
            byte[] objectNameBuffer = StringToBytes(data.objectName);
            byte[] channelNameBuffer = StringToBytes(data.channelName);
            byte[] channelIndexBuffer = IntToBytes(data.channelIndex);
            byte[] frameBuffer = IntToBytes(data.frame);
            byte[] valueBuffer = FloatToBytes(data.value);
            byte[] interpolationBuffer = IntToBytes((int) data.interpolation);
            List<byte[]> buffers = new List<byte[]> { objectNameBuffer, channelNameBuffer, channelIndexBuffer, frameBuffer, valueBuffer, interpolationBuffer };
            byte[] buffer = ConcatenateBuffers(buffers);
            return new NetCommand(buffer, MessageType.AddKeyframe);
        }

        public static NetCommand BuildSendRemoveKey(SetKeyInfo data)
        {
            byte[] objectNameBuffer = StringToBytes(data.objectName);
            byte[] channelNameBuffer = StringToBytes(data.channelName);
            byte[] channelIndexBuffer = IntToBytes(data.channelIndex);
            byte[] frameBuffer = IntToBytes(data.frame);
            List<byte[]> buffers = new List<byte[]> { objectNameBuffer, channelNameBuffer, channelIndexBuffer, frameBuffer };
            byte[] buffer = ConcatenateBuffers(buffers);
            return new NetCommand(buffer, MessageType.RemoveKeyframe);
        }

        public static NetCommand BuildSendMoveKey(MoveKeyInfo data)
        {
            byte[] objectNameBuffer = StringToBytes(data.objectName);
            byte[] channelNameBuffer = StringToBytes(data.channelName);
            byte[] channelIndexBuffer = IntToBytes(data.channelIndex);
            byte[] frameBuffer = IntToBytes(data.frame);
            byte[] newFrameBuffer = IntToBytes(data.newFrame);
            List<byte[]> buffers = new List<byte[]> { objectNameBuffer, channelNameBuffer, channelIndexBuffer, frameBuffer, newFrameBuffer };
            byte[] buffer = ConcatenateBuffers(buffers);
            return new NetCommand(buffer, MessageType.MoveKeyframe);
        }

        public static NetCommand BuildSendQueryAnimationData(string name)
        {
            return new NetCommand(StringToBytes(name), MessageType.QueryAnimationData);
        }

        public static void BuildFrameStartEnd(byte[] data)
        {
            int index = 0;
            int start = GetInt(data, ref index);
            int end = GetInt(data, ref index);
            GlobalState.startFrame = start;
            GlobalState.endFrame = end;
        }

        public static void BuildCurrentCamera(byte[] data)
        {
            int index = 0;
            string cameraName = GetString(data, ref index);
            if (cameraName.Length == 0)
            {
                Selection.SetActiveCamera(null);
            }
            else
            {
                Node prefabNode = SyncData.nodes[cameraName];
                // We only have one instance of any camera in the scene                
                CameraController controller = prefabNode.instances[0].Item1.GetComponent<CameraController>();
                if (null != controller) { Selection.SetActiveCamera(controller); }
            }
        }

        public static void BuildMontageMode(byte[] data)
        {

        }

        public static void BuildShotManagerCurrentShot(byte[] data)
        {
            int index = 0;
            int shotIndex = GetInt(data, ref index);
            ShotManager.Instance.CurrentShot = shotIndex;
        }

        public static void BuildShotManagerMontageMode(byte[] data)
        {
            int index = 0;
            bool montageMode = GetBool(data, ref index);
            ShotManager.Instance.MontageMode = montageMode;
        }
        public static void BuildShotManager(byte[] data)
        {
            ShotManager.Instance.Clear();

            int index = 0;
            int shotCount = GetInt(data, ref index);
            for (int i = 0; i < shotCount; ++i)
            {
                string shotName = GetString(data, ref index);
                string cameraName = GetString(data, ref index);
                int start = GetInt(data, ref index);
                int end = GetInt(data, ref index);
                bool enabled = GetBool(data, ref index);

                GameObject camera = null;
                if (cameraName.Length > 0 && SyncData.nodes.ContainsKey(cameraName))
                    camera = SyncData.nodes[cameraName].instances[0].Item1;

                Shot shot = new Shot { name = shotName, camera = camera, start = start, end = end, enabled = enabled };
                ShotManager.Instance.AddShot(shot);
            }

            ShotManager.Instance.FireChanged();
        }

        public static void BuildClientAttribute(byte[] data)
        {
            int index = 0;
            string json = GetString(data, ref index);
            // JArray exception... :(
            //var d = JsonConvert.DeserializeObject<Dictionary<string, JsonClientId>>(jsonAttr);

            ClientInfo client = JsonHelper.GetClientInfo(json);

            if (client.id.IsValid)
            {
                // Ignore info on ourself
                if (client.id.value == GlobalState.networkUser.id) { return; }

                if (client.room.IsValid)
                {
                    // A client may leave the room
                    if (client.room.value == "null")
                    {
                        GlobalState.RemoveConnectedUser(client.id.value);
                        return;
                    }

                    // Ignore other room messages
                    if (client.room.value != GlobalState.networkUser.room) { return; }

                    // Add client to the list of connected users in our room
                    if (!GlobalState.HasConnectedUser(client.id.value))
                    {
                        ConnectedUser newUser = new ConnectedUser();
                        newUser.id = client.id.value;
                        GlobalState.AddConnectedUser(newUser);
                    }
                }

                // Get client connected to our room
                if (!GlobalState.HasConnectedUser(client.id.value)) { return; }
                ConnectedUser user = GlobalState.GetConnectedUser(client.id.value);

                // Retrieve the viewId (one of possible - required to send data)
                if (client.viewId.IsValid && null == GlobalState.networkUser.viewId)
                {
                    GlobalState.networkUser.viewId = client.viewId.value;
                }

                if (client.userName.IsValid) { user.name = client.userName.value; }
                if (client.userColor.IsValid) { user.color = client.userColor.value; }

                bool changed = false;

                // Get its eye position
                if (client.eye.IsValid)
                {
                    user.eye = client.eye.value;
                    changed = true;
                }

                // Get its target look at
                if (client.target.IsValid)
                {
                    user.target = client.target.value;
                    changed = true;
                }

                if (changed) { GlobalState.UpdateConnectedUser(user); }
            }
        }

        public static void BuildListAllClients(byte[] data)
        {
            int index = 0;
            string json = GetString(data, ref index);
            List<ClientInfo> clients = JsonHelper.GetClientsInfo(json);
            foreach (ClientInfo client in clients)
            {
                // Invalid client
                if (!client.id.IsValid) { continue; }

                // Ignore ourself
                if (client.id.value == GlobalState.networkUser.id) { continue; }

                if (client.room.IsValid)
                {
                    // Only consider clients in our room
                    if (client.room.value != GlobalState.networkUser.room) { continue; }

                    // Retrieve the viewId (one of possible - required to send data)
                    if (client.viewId.IsValid && null == GlobalState.networkUser.viewId)
                    {
                        GlobalState.networkUser.viewId = client.viewId.value;
                    }

                    // Add client to the list of connected users in our room
                    if (!GlobalState.HasConnectedUser(client.id.value))
                    {
                        ConnectedUser newUser = new ConnectedUser();
                        newUser.id = client.id.value;
                        if (client.userName.IsValid) { newUser.name = client.userName.value; }
                        if (client.userColor.IsValid) { newUser.color = client.userColor.value; }
                        if (client.eye.IsValid) { newUser.eye = client.eye.value; }
                        if (client.target.IsValid) { newUser.target = client.target.value; }
                        GlobalState.AddConnectedUser(newUser);
                    }
                }
            }
        }

        public static void BuildShotManagerAction(byte[] data)
        {
            int index = 0;
            ShotManagerAction action = (ShotManagerAction) GetInt(data, ref index);
            int shotIndex = GetInt(data, ref index);

            switch (action)
            {
                case ShotManagerAction.AddShot:
                    {
                        string shotName = GetString(data, ref index);
                        int start = GetInt(data, ref index);
                        int end = GetInt(data, ref index);
                        string cameraName = GetString(data, ref index);
                        Color color = GetColor(data, ref index);
                        GameObject cam = null;
                        if (cameraName.Length > 0)
                            cam = SyncData.nodes[cameraName].instances[0].Item1;
                        Shot shot = new Shot { name = shotName, camera = cam, color = color, start = start, end = end };
                        ShotManager.Instance.InsertShot(shotIndex, shot);
                        ShotManager.Instance.FireChanged();
                    }
                    break;
                case ShotManagerAction.DeleteShot:
                    ShotManager.Instance.RemoveShot(shotIndex);
                    ShotManager.Instance.FireChanged();
                    break;
                case ShotManagerAction.DuplicateShot:
                    {
                        string shotName = GetString(data, ref index);
                        ShotManager.Instance.DuplicateShot(shotIndex);
                        ShotManager.Instance.FireChanged();
                    }
                    break;
                case ShotManagerAction.MoveShot:
                    {
                        int offset = GetInt(data, ref index);
                        ShotManager.Instance.MoveCurrentShot(offset);
                        ShotManager.Instance.FireChanged();
                    }
                    break;
                case ShotManagerAction.UpdateShot:
                    {
                        int start = GetInt(data, ref index);
                        int end = GetInt(data, ref index);
                        string cameraName = GetString(data, ref index);
                        Color color = GetColor(data, ref index);
                        int enabled = GetInt(data, ref index);

                        Shot shot = ShotManager.Instance.shots[shotIndex];
                        if (start != -1)
                            shot.start = start;
                        if (end != -1)
                            shot.end = end;
                        if (cameraName.Length > 0)
                        {
                            GameObject cam = SyncData.nodes[cameraName].instances[0].Item1;
                            shot.camera = cam;
                        }
                        if (color.r != -1)
                        {
                            shot.color = color;
                        }
                        if (enabled != -1)
                        {
                            shot.enabled = enabled == 1 ? true : false;
                        }

                        ShotManager.Instance.UpdateShot(shotIndex, shot);
                        ShotManager.Instance.FireChanged();
                    }
                    break;
            }

        }
    }

    public class NetworkClient : MonoBehaviour
    {
        private static NetworkClient _instance;
        public Transform root;
        public Transform prefab;

        Thread thread = null;
        bool alive = true;
        bool connected = false;

        Socket socket = null;
        List<NetCommand> receivedCommands = new List<NetCommand>();
        List<NetCommand> pendingCommands = new List<NetCommand>();

        public void Awake()
        {
            _instance = this;
        }

        public static NetworkClient GetInstance()
        {
            return _instance;
        }

        void OnDestroy()
        {
            Join();
        }

        void Start()
        {
            Connect();
            SyncData.Init(prefab, root);
            StartCoroutine(ProcessIncomingCommands());
        }

        IPAddress GetIpAddressFromHostname(string hostname)
        {
            string[] splitted = hostname.Split('.');
            if (splitted.Length == 4)
            {
                bool error = false;
                byte[] baddr = new byte[4];
                for (int i = 0; i < 4; i++)
                {
                    int val;
                    if (Int32.TryParse(splitted[i], out val) && val >= 0 && val <= 255)
                    {
                        baddr[i] = (byte) val;
                    }
                    else
                    {
                        error = true;
                        break;
                    }
                }
                if (!error)
                    return new IPAddress(baddr);
            }

            IPAddress ipAddress = null;

            IPHostEntry ipHostInfo = Dns.GetHostEntry(hostname);
            if (ipHostInfo.AddressList.Length == 0)
                return ipAddress;

#pragma warning disable CS0162 // Unreachable code detected
            for (int i = ipHostInfo.AddressList.Length - 1; i >= 0; i--)
#pragma warning restore CS0162 // Unreachable code detected
            {
                IPAddress addr = ipHostInfo.AddressList[i];
                ipAddress = addr;
                break;
            }

            return ipAddress;
        }

        public void Connect()
        {
            connected = false;
            string[] args = System.Environment.GetCommandLineArgs();
            string room = GlobalState.Instance.networkSettings.room;
            string hostname = GlobalState.Instance.networkSettings.host;
            int port = GlobalState.Instance.networkSettings.port;
            string master = GlobalState.Instance.networkSettings.master;
            string userName = GlobalState.Instance.networkSettings.userName;
            Color userColor = GlobalState.Instance.networkSettings.userColor;

            // Read command line
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--room") { room = args[i + 1]; }
                if (args[i] == "--hostname") { hostname = args[i + 1]; }
                if (args[i] == "--port") { Int32.TryParse(args[i + 1], out port); }
                if (args[i] == "--master") { master = args[i + 1]; }
                if (args[i] == "--username") { userName = args[i + 1]; }
                if (args[i] == "--usercolor") { ColorUtility.TryParseHtmlString(args[i + 1], out userColor); }
            }
            GlobalState.networkUser.room = room;
            GlobalState.networkUser.masterId = master;
            GlobalState.networkUser.name = userName;
            GlobalState.networkUser.color = userColor;

            IPAddress ipAddress = GetIpAddressFromHostname(hostname);
            if (null == ipAddress)
                return;

            IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

            // Create a TCP/IP  socket.  
            socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Connect the socket to the remote endpoint. Catch any errors.  
            try
            {
                socket.Connect(remoteEP);
            }
            catch (ArgumentNullException ane)
            {
                Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                return;
            }
            catch (SocketException se)
            {
                Console.WriteLine("SocketException : {0}", se.ToString());
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception : {0}", e.ToString());
                return;
            }

            JoinRoom(room);
            connected = true;

            NetCommand command = new NetCommand(new byte[0], MessageType.ClientId);
            AddCommand(command);

            NetCommand commandListClients = new NetCommand(new byte[0], MessageType.ListAllClients);
            AddCommand(commandListClients);

            thread = new Thread(new ThreadStart(Run));
            thread.Start();
        }

        public void Join()
        {
            if (thread == null)
                return;
            alive = false;
            thread.Join();
            socket.Disconnect(false);
        }

        NetCommand ReadMessage()
        {
            int count = socket.Available;
            if (count < 14)
                return null;

            byte[] header = new byte[14];
            socket.Receive(header, 0, 14, SocketFlags.None);

            var size = BitConverter.ToInt64(header, 0);
            var commandId = BitConverter.ToInt32(header, 8);
            //Debug.Log("Received Command Id " + commandId);
            var mtype = BitConverter.ToUInt16(header, 8 + 4);

            byte[] data = new byte[size];
            long remaining = size;
            long current = 0;
            while (remaining > 0)
            {
                int sizeRead = socket.Receive(data, (int) current, (int) remaining, SocketFlags.None);
                current += sizeRead;
                remaining -= sizeRead;
            }


            NetCommand command = new NetCommand(data, (MessageType) mtype);
            return command;
        }

        void WriteMessage(NetCommand command)
        {
            byte[] sizeBuffer = BitConverter.GetBytes((Int64) command.data.Length);
            byte[] commandId = BitConverter.GetBytes((Int32) command.id);
            byte[] typeBuffer = BitConverter.GetBytes((Int16) command.messageType);
            List<byte[]> buffers = new List<byte[]> { sizeBuffer, commandId, typeBuffer, command.data };

            socket.Send(NetGeometry.ConcatenateBuffers(buffers));
        }

        void AddCommand(NetCommand command)
        {
            lock (this)
            {
                pendingCommands.Add(command);
            }
        }

        public void SendObjectVisibility(Transform transform)
        {
            NetCommand command = NetGeometry.BuildObjectVisibilityCommand(root, transform);
            AddCommand(command);
        }

        public void SendTransform(Transform transform)
        {
            NetCommand command = NetGeometry.BuildTransformCommand(root, transform);
            AddCommand(command);
        }

        public void SendMesh(MeshInfos meshInfos)
        {
            NetCommand command = NetGeometry.BuildMeshCommand(root, meshInfos);
            AddCommand(command);
        }

        public void SendDelete(DeleteInfo deleteInfo)
        {
            NetCommand command = NetGeometry.BuildDeleteCommand(root, deleteInfo);
            AddCommand(command);
        }

        public void SendMaterial(Material material)
        {
            NetCommand command = NetGeometry.BuildMaterialCommand(material);
            AddCommand(command);
        }

        public void SendAssignMaterial(AssignMaterialInfo info)
        {
            NetCommand command = NetGeometry.BuildAssignMaterialCommand(info);
            AddCommand(command);
        }

        public void SendCamera(CameraInfo cameraInfo)
        {
            NetCommand command = NetGeometry.BuildCameraCommand(root, cameraInfo);
            AddCommand(command);
        }
        public void SendLight(LightInfo lightInfo)
        {
            NetCommand command = NetGeometry.BuildLightCommand(root, lightInfo);
            AddCommand(command);
        }
        public void SendSky(SkySettings skyInfo)
        {
            NetCommand command = NetGeometry.BuildSkyCommand(skyInfo);
            AddCommand(command);
        }
        public void SendRename(RenameInfo rename)
        {
            NetCommand command = NetGeometry.BuildRenameCommand(root, rename);
            AddCommand(command);
        }

        public void SendFrame(FrameInfo frame)
        {
            NetCommand command = NetGeometry.BuildSendFrameCommand(frame.frame);
            AddCommand(command);
            //GlobalState.currentFrame = frame.frame;
        }

        public void SendFrameStartEnd(FrameStartEnd range)
        {
            NetCommand command = NetGeometry.BuildSendFrameStartEndCommand(range.start, range.end);
            AddCommand(command);
        }

        public void SendPlay()
        {
            byte[] masterIdBuffer = NetGeometry.StringToBytes(GlobalState.networkUser.masterId);
            byte[] messageTypeBuffer = NetGeometry.IntToBytes((int) MessageType.Play);
            byte[] dataBuffer = new byte[0];
            List<byte[]> buffers = new List<byte[]> { masterIdBuffer, messageTypeBuffer, dataBuffer };
            byte[] buffer = NetGeometry.ConcatenateBuffers(buffers);
            AddCommand(new NetCommand(buffer, MessageType.ClientIdWrapper));
        }

        public void SendPause()
        {
            byte[] masterIdBuffer = NetGeometry.StringToBytes(GlobalState.networkUser.masterId);
            byte[] messageTypeBuffer = NetGeometry.IntToBytes((int) MessageType.Pause);
            byte[] dataBuffer = new byte[0];
            List<byte[]> buffers = new List<byte[]> { masterIdBuffer, messageTypeBuffer, dataBuffer };
            byte[] buffer = NetGeometry.ConcatenateBuffers(buffers);
            AddCommand(new NetCommand(buffer, MessageType.ClientIdWrapper));
        }

        public void SendAddKeyframe(SetKeyInfo data)
        {
            NetCommand command = NetGeometry.BuildSendSetKey(data);
            AddCommand(command);
        }

        public void SendRemoveKeyframe(SetKeyInfo data)
        {
            NetCommand command = NetGeometry.BuildSendRemoveKey(data);
            AddCommand(command);
        }

        public void SendMoveKeyframe(MoveKeyInfo data)
        {
            NetCommand command = NetGeometry.BuildSendMoveKey(data);
            AddCommand(command);
        }

        public void SendQueryObjectData(string name)
        {
            NetCommand command = NetGeometry.BuildSendQueryAnimationData(name);
            AddCommand(command);
        }
        public void SendDuplicate(DuplicateInfos duplicate)
        {
            NetCommand command = NetGeometry.BuildDuplicateCommand(root, duplicate);
            AddCommand(command);
        }

        public void SendToTrash(SendToTrashInfo sendToTrash)
        {
            NetCommand command = NetGeometry.BuildSendToTrashCommand(root, sendToTrash);
            AddCommand(command);
        }

        public void RestoreFromTrash(RestoreFromTrashInfo restoreFromTrash)
        {
            NetCommand command = NetGeometry.BuildRestoreFromTrashCommand(root, restoreFromTrash);
            AddCommand(command);
        }

        public void SendAddObjectToColleciton(AddToCollectionInfo addToCollectionInfo)
        {
            string collectionName = addToCollectionInfo.collectionName;
            if (!SyncData.collectionNodes.ContainsKey(collectionName))
            {
                NetCommand addCollectionCommand = NetGeometry.BuildAddCollecitonCommand(collectionName);
                AddCommand(addCollectionCommand);
            }

            NetCommand commandAddObjectToCollection = NetGeometry.BuildAddObjectToCollecitonCommand(addToCollectionInfo);
            AddCommand(commandAddObjectToCollection);
            if (!SyncData.sceneCollections.Contains(collectionName))
            {
                NetCommand commandAddCollectionToScene = NetGeometry.BuildAddCollectionToScene(collectionName);
                AddCommand(commandAddCollectionToScene);
            }
        }

        public void SendAddObjectToScene(AddObjectToSceneInfo addObjectToScene)
        {
            NetCommand command = NetGeometry.BuildAddObjectToScene(addObjectToScene);
            AddCommand(command);
        }

        public void SendClearAnimations(ClearAnimationInfo info)
        {
            NetCommand command = NetGeometry.BuildSendClearAnimations(info);
            AddCommand(command);
        }

        public void SendMontageMode(MontageModeInfo data)
        {
            NetCommand command = NetGeometry.BuildSendMontageMode(data.montage);
            AddCommand(command);
        }

        public void SendShotManagerAction(ShotManagerActionInfo info)
        {
            NetCommand command = NetGeometry.BuildSendShotManagerAction(info);
            AddCommand(command);
        }

        public void SendPlayerTransform(ConnectedUser info)
        {
            NetCommand command = NetGeometry.BuildSendPlayerTransform(info);
            if (null != command) { AddCommand(command); }
        }

        public void JoinRoom(string roomName)
        {
            NetCommand command = new NetCommand(System.Text.Encoding.UTF8.GetBytes(roomName), MessageType.JoinRoom);
            AddCommand(command);

            string json = JsonHelper.CreateJsonClientNameAndColor(GlobalState.networkUser.name, GlobalState.networkUser.color);
            NetCommand commandClientInfo = new NetCommand(NetGeometry.StringToBytes(json), MessageType.SetClientCustomAttribute);
            AddCommand(commandClientInfo);
        }

        void Send(byte[] data)
        {
            lock (this)
            {
                socket.Send(data);
            }
        }

        public DateTime before = DateTime.Now;
        void Run()
        {
            while (alive)
            {
                NetCommand command = ReadMessage();
                if (command != null)
                {
                    if (command.messageType == MessageType.ClientId ||
                        command.messageType == MessageType.ClientUpdate ||
                        command.messageType == MessageType.ListAllClients ||
                        command.messageType > MessageType.Command)
                    {
                        lock (this)
                        {
                            if (command.messageType == MessageType.ClientIdWrapper)
                            {
                                UnpackFromClientId(command, ref receivedCommands);
                            }
                            else
                            {
                                receivedCommands.Add(command);
                            }
                        }
                    }
                }

                lock (this)
                {
                    if (pendingCommands.Count > 0)
                    {
                        foreach (NetCommand pendingCommand in pendingCommands)
                        {
                            WriteMessage(pendingCommand);
                        }
                        pendingCommands.Clear();
                    }
                }
            }
        }

        public bool UnpackFromClientId(NetCommand command, ref List<NetCommand> commands)
        {
            int index = 0;
            string masterId = NetGeometry.GetString(command.data, ref index);

            // For debug purpose (unity in editor mode when networkSettings.master is empty)
            if (null == GlobalState.networkUser.masterId || GlobalState.networkUser.masterId.Length == 0)
                GlobalState.networkUser.masterId = masterId;

            if (masterId != GlobalState.networkUser.masterId)
                return false;

            int remainingData = command.data.Length - index;
            while (remainingData > 0)
            {
                int dataLength = NetGeometry.GetInt(command.data, ref index);
                remainingData -= dataLength + sizeof(int);

                dataLength -= sizeof(int);
                int messageType = NetGeometry.GetInt(command.data, ref index);
                byte[] newBuffer = new byte[dataLength];

                Buffer.BlockCopy(command.data, index, newBuffer, 0, dataLength);
                index += dataLength;

                NetCommand newCommand = new NetCommand(newBuffer, (MessageType) messageType);
                commands.Add(newCommand);
            }

            return true;
        }

        public int i = 0;
        public List<NetCommand> commands = new List<NetCommand>();
        int commandProcessedCount = 0;

        IEnumerator ProcessIncomingCommands()
        {
            while (true)
            {
                lock (this)
                {
                    commands.AddRange(receivedCommands);
                    receivedCommands.Clear();
                }

                if (commands.Count == 0)
                    yield return null;

                DateTime before = DateTime.Now;
                bool prematuredExit = false;
                foreach (NetCommand command in commands)
                {
                    commandProcessedCount++;

                    try
                    {
                        switch (command.messageType)
                        {
                            case MessageType.ClientId:
                                NetGeometry.BuildClientId(command.data);
                                break;
                            case MessageType.Mesh:
                                NetGeometry.BuildMesh(command.data);
                                break;
                            case MessageType.Transform:
                                NetGeometry.BuildTransform(prefab, command.data);
                                break;
                            case MessageType.ObjectVisibility:
                                NetGeometry.BuildObjectVisibility(command.data);
                                break;
                            case MessageType.Material:
                                NetGeometry.BuildMaterial(command.data);
                                break;
                            case MessageType.AssignMaterial:
                                NetGeometry.BuildAssignMaterial(command.data);
                                break;
                            case MessageType.Camera:
                                NetGeometry.BuildCamera(prefab, command.data);
                                break;
                            case MessageType.Animation:
                                NetGeometry.BuildAnimation(prefab, command.data);
                                break;
                            case MessageType.CameraAttributes:
                                NetGeometry.BuildCameraAttributes(prefab, command.data);
                                break;
                            case MessageType.LightAttributes:
                                NetGeometry.BuildLightAttributes(prefab, command.data);
                                break;
                            case MessageType.Light:
                                NetGeometry.BuildLight(prefab, command.data);
                                break;
                            case MessageType.Sky:
                                NetGeometry.BuildSky(command.data);
                                break;
                            case MessageType.Delete:
                                NetGeometry.Delete(prefab, command.data);
                                break;
                            case MessageType.Rename:
                                NetGeometry.Rename(prefab, command.data);
                                break;
                            case MessageType.Duplicate:
                                NetGeometry.Duplicate(prefab, command.data);
                                break;
                            case MessageType.SendToTrash:
                                NetGeometry.BuildSendToTrash(root, command.data);
                                break;
                            case MessageType.RestoreFromTrash:
                                NetGeometry.BuildRestoreFromTrash(root, command.data);
                                break;
                            case MessageType.Texture:
                                NetGeometry.BuildTexture(command.data);
                                break;
                            case MessageType.Collection:
                                NetGeometry.BuildCollection(command.data);
                                break;
                            case MessageType.CollectionRemoved:
                                NetGeometry.BuildCollectionRemoved(command.data);
                                break;
                            case MessageType.AddCollectionToCollection:
                                NetGeometry.BuildAddCollectionToCollection(prefab, command.data);
                                break;
                            case MessageType.RemoveCollectionFromCollection:
                                NetGeometry.BuildRemoveCollectionFromCollection(prefab, command.data);
                                break;
                            case MessageType.AddObjectToCollection:
                                NetGeometry.BuildAddObjectToCollection(prefab, command.data);
                                break;
                            case MessageType.RemoveObjectFromCollection:
                                NetGeometry.BuildRemoveObjectFromCollection(prefab, command.data);
                                break;
                            case MessageType.CollectionInstance:
                                NetGeometry.BuildCollectionInstance(command.data);
                                break;
                            case MessageType.AddObjectToDocument:
                                NetGeometry.BuildAddObjectToDocument(root, command.data);
                                break;
                            case MessageType.AddCollectionToScene:
                                NetGeometry.BuilAddCollectionToScene(root, command.data);
                                break;
                            case MessageType.SetScene:
                                NetGeometry.BuilSetScene(command.data);
                                break;
                            case MessageType.GreasePencilMaterial:
                                NetGeometry.BuildGreasePencilMaterial(command.data);
                                break;
                            case MessageType.GreasePencilMesh:
                                NetGeometry.BuildGreasePencilMesh(command.data);
                                break;
                            case MessageType.GreasePencilConnection:
                                NetGeometry.BuildGreasePencilConnection(command.data);
                                break;
                            case MessageType.GreasePencilTimeOffset:
                                NetGeometry.BuildGreasePencilTimeOffset(command.data);
                                break;
                            case MessageType.Play:
                                NetGeometry.BuildPlay();
                                break;
                            case MessageType.Pause:
                                NetGeometry.BuildPause();
                                break;
                            case MessageType.Frame:
                                NetGeometry.BuildFrame(command.data);
                                break;
                            case MessageType.FrameStartEnd:
                                NetGeometry.BuildFrameStartEnd(command.data);
                                break;
                            case MessageType.CurrentCamera:
                                NetGeometry.BuildCurrentCamera(command.data);
                                break;
                            case MessageType.ShotManagerMontageMode:
                                NetGeometry.BuildShotManagerMontageMode(command.data);
                                break;
                            case MessageType.ShotManagerContent:
                                NetGeometry.BuildShotManager(command.data);
                                break;
                            case MessageType.ShotManagerCurrentShot:
                                NetGeometry.BuildShotManagerCurrentShot(command.data);
                                break;
                            case MessageType.ShotManagerAction:
                                NetGeometry.BuildShotManagerAction(command.data);
                                break;

                            case MessageType.ClientUpdate:
                                NetGeometry.BuildClientAttribute(command.data);
                                break;
                            case MessageType.ListAllClients:
                                NetGeometry.BuildListAllClients(command.data);
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        string message = $"Network exception (Command#{i}) Type {command.messageType}\n{e}";
                        Debug.LogError(message);
                    }
                    i++;

                    DateTime after = DateTime.Now;
                    TimeSpan duration = after.Subtract(before);
                    if (duration.Milliseconds > 20)
                    {
                        commands.RemoveRange(0, commandProcessedCount);
                        prematuredExit = true;
                        break;
                    }
                }
                if (!prematuredExit)
                    commands.Clear();
                commandProcessedCount = 0;
                yield return null;
            }
        }

        public void SendEvent<T>(MessageType messageType, T data)
        {
            if (!connected) { return; }
            switch (messageType)
            {
                case MessageType.Transform:
                    SendTransform(data as Transform); break;
                case MessageType.Mesh:
                    SendMesh(data as MeshInfos); break;
                case MessageType.Delete:
                    SendDelete(data as DeleteInfo); break;
                case MessageType.Material:
                    SendMaterial(data as Material); break;
                case MessageType.AssignMaterial:
                    SendAssignMaterial(data as AssignMaterialInfo); break;
                case MessageType.Camera:
                    SendCamera(data as CameraInfo);
                    SendTransform((data as CameraInfo).transform);
                    break;
                case MessageType.Light:
                    SendLight(data as LightInfo);
                    SendTransform((data as LightInfo).transform);
                    break;
                case MessageType.Sky:
                    SendSky(data as SkySettings);
                    break;
                case MessageType.Rename:
                    SendRename(data as RenameInfo); break;
                case MessageType.Duplicate:
                    SendDuplicate(data as DuplicateInfos); break;
                case MessageType.SendToTrash:
                    SendToTrash(data as SendToTrashInfo); break;
                case MessageType.RestoreFromTrash:
                    RestoreFromTrash(data as RestoreFromTrashInfo); break;
                case MessageType.AddObjectToCollection:
                    SendAddObjectToColleciton(data as AddToCollectionInfo); break;
                case MessageType.AddObjectToScene:
                    SendAddObjectToScene(data as AddObjectToSceneInfo); break;
                case MessageType.Frame:
                    SendFrame(data as FrameInfo); break;
                case MessageType.FrameStartEnd:
                    SendFrameStartEnd(data as FrameStartEnd); break;
                case MessageType.Play:
                    SendPlay(); break;
                case MessageType.Pause:
                    SendPause(); break;
                case MessageType.AddKeyframe:
                    SendAddKeyframe(data as SetKeyInfo); break;
                case MessageType.RemoveKeyframe:
                    SendRemoveKeyframe(data as SetKeyInfo); break;
                case MessageType.MoveKeyframe:
                    SendMoveKeyframe(data as MoveKeyInfo); break;
                case MessageType.QueryAnimationData:
                    SendQueryObjectData(data as string); break;
                case MessageType.ClearAnimations:
                    SendClearAnimations(data as ClearAnimationInfo); break;
                case MessageType.ShotManagerMontageMode:
                    SendMontageMode(data as MontageModeInfo); break;
                case MessageType.ShotManagerAction:
                    SendShotManagerAction(data as ShotManagerActionInfo); break;
            }
        }
    }
}
