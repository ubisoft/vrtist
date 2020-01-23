using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;

namespace VRtist
{
    public enum MessageType
    {
        JoinRoom = 1,
        CreateRoom,
        LeaveRoom,

        Command = 100,
        Transform,
        Delete,
        Mesh,
        Material,
        Camera,
        Light,
        MeshConnection,
        Rename,
        Duplicate,
        SendToTrash,
        RestoreFromTrash,
        Texture,
    }

    public class NetCommand
    {
        public byte[] data;
        public MessageType messageType;
        public int id;

        public NetCommand()
        {
        }
        public NetCommand(byte[] d, MessageType mtype, int mid = 0 )
        {
            data = d;
            messageType = mtype;
            id = mid;
        }
    }
    
    public class NetGeometry
    {

        public static Dictionary<string, Material> materials = new Dictionary<string, Material>();
        public static Material currentMaterial = null;

        public static Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        public static Dictionary<string, Material[]> meshesMaterials = new Dictionary<string, Material[]>();
        public static Dictionary<string, HashSet<MeshFilter>> meshInstances = new Dictionary<string, HashSet<MeshFilter>>();

        public static Dictionary<string, byte[]> textureData = new Dictionary<string, byte[]>();
        public static Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        public static HashSet<string> texturesFlipY = new HashSet<string>();

        public static byte[] StringsToBytes(string[] values)
        {
            int size = sizeof(int);
            for (int i = 0; i < values.Length; i++)
            {
                byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(values[i]);
                size += sizeof(int) + utf8.Length;
            }
                

            byte[] bytes = new byte[size];
            Buffer.BlockCopy(BitConverter.GetBytes(values.Length), 0, bytes, 0, sizeof(int));
            int index = sizeof(int);
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
            return buffer[0] == 0 ? false : true;
        }

        public static byte[] boolToBytes(bool value)
        {
            byte[] bytes = new byte[sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(value), 0, bytes, 0, sizeof(int));
            return bytes;
        }

        public static int GetInt(byte[] data, ref int currentIndex)
        {
            int[] buffer = new int[1];
            Buffer.BlockCopy(data, currentIndex, buffer, 0, sizeof(int));
            currentIndex += sizeof(int);
            return buffer[0];
        }

        public static byte[] intToBytes(int value)
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

        public static void Rename(Transform root, byte[] data)
        {
            int bufferIndex = 0;
            Transform srcTrf = FindPath(root, data, 0, out bufferIndex);
            if (srcTrf == null)
                return;
            string dstPath = GetString(data, bufferIndex, out bufferIndex);
            if (dstPath.Length == 0)
                return;
            string[] splittedDstPath = dstPath.Split('/');
            string newName = splittedDstPath[splittedDstPath.Length - 1];
            srcTrf.gameObject.name = newName;
        }

        public static void Delete(Transform root, byte[] data)
        {
            int bufferIndex = 0;
            Transform trf = FindPath(root, data, 0, out bufferIndex);
            if (trf == null)
                return;

            MeshFilter[] meshFilters = trf.GetComponentsInChildren<MeshFilter>();
            for(int i = 0; i < meshFilters.Length; i++)
            {
                MeshFilter meshFilter = meshFilters[i];
                GameObject subObject = meshFilters[i].gameObject;

                bool next = false;
                foreach(var meshInstance in meshInstances)
                {
                    string meshInstanceName = meshInstance.Key;
                    HashSet<MeshFilter> meshFilterInstance = meshInstance.Value;
                    foreach(MeshFilter mf in meshFilterInstance)
                    {
                        if(mf == meshFilter)
                        {
                            meshFilterInstance.Remove(meshFilter);
                            if(meshFilterInstance.Count == 0)
                            {
                                meshInstances.Remove(meshInstanceName);
                                meshesMaterials.Remove(meshInstanceName);
                                meshes.Remove(meshInstanceName);
                            }
                            next = true;
                            break;
                        }
                    }
                    if (next)
                        break;
                }
            }

            Selection.RemoveFromSelection(trf.gameObject);
            GameObject.Destroy(trf.gameObject);
        }

        public static void Duplicate(Transform root, byte[] data)
        {
            int bufferIndex = 0;
            Transform srcPath = FindPath(root, data, 0, out bufferIndex);
            if (srcPath == null)
                return;

            string dstName = GetString(data, bufferIndex, out bufferIndex);
            Vector3 position = GetVector3(data, ref bufferIndex);
            Quaternion rotation = GetQuaternion(data, ref bufferIndex);
            Vector3 scale = GetVector3(data, ref bufferIndex);

            GameObject newGameObject = GameObject.Instantiate(srcPath.gameObject, srcPath.parent);
            newGameObject.name = dstName;
            newGameObject.transform.localPosition = position;
            newGameObject.transform.localRotation = rotation;
            newGameObject.transform.localScale = scale;
        }

        public static void BuildSendToTrash(Transform root, byte[] data)
        {
            int bufferIndex = 0;
            Transform objectPath = FindPath(root, data, 0, out bufferIndex);
            if (null == objectPath)
                return;
            objectPath.parent = Utils.GetTrash().transform;
        }
        public static void BuildRestoreFromTrash(Transform root, byte[] data)
        {
            int bufferIndex = 0;
            string objectName = GetString(data, 0, out bufferIndex);
            string dstPath = GetString(data, bufferIndex, out bufferIndex);
            Transform trf = Utils.GetTrash().transform.Find(objectName);
            if (null != trf)
            {
                Transform parent = BuildPath(root, dstPath,true);
                trf.parent = parent;
            }
        }

        public static void BuildTexture(byte[] data)
        {
            int bufferIndex = 0;
            string path = GetString(data, 0, out bufferIndex);

            int size = GetInt(data, ref bufferIndex);

            byte[] buffer = new byte[size];
            Buffer.BlockCopy(data, bufferIndex, buffer, 0, size);

            textureData[path] = buffer;
        }

        public static Material DefaultMaterial()
        {
            string name = "defaultMaterial";
            if (materials.ContainsKey(name))
                return materials[name];

            Shader hdrplit = Shader.Find("HDRP/Lit");
            Material material = new Material(hdrplit);
            material.name = name;
            material.SetColor("_BaseColor", new Color(0.8f, 0.8f, 0.8f));
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.5f);
            materials[name] = material;

            return material;
        }

        public static Texture2D LoadTextureOIIO(string filePath, bool isLinear)
        {
            // TODO: need to flip? Repere bottom left, Y-up
            int ret = OIIOAPI.oiio_open_image(filePath);
            if (ret == 0)
            {
                Debug.Log("Could not open image " + filePath + " with OIIO.");
                return null;
            }

            int width = -1;
            int height = -1;
            int nchannels = -1;
            OIIOAPI.BASETYPE format = OIIOAPI.BASETYPE.NONE;
            ret = OIIOAPI.oiio_get_image_info(ref width, ref height, ref nchannels, ref format);
            if (ret == 0)
            {
                Debug.Log("Could not get info about image " + filePath + " with OIIO");
                return null;
            }

            TextureFormat textureFormat = Format2Format(format, nchannels);
            Texture2D image = new Texture2D(width, height, textureFormat, true, isLinear); // with mips

            // TODO: find out why I did that!!!
            int do_rgb_to_rgba = 0;
            if ((format == OIIOAPI.BASETYPE.FLOAT && nchannels == 3)
                || (format == OIIOAPI.BASETYPE.HALF && nchannels == 3))
            {
                do_rgb_to_rgba = 1;
            }

            var pixels = image.GetRawTextureData();
            GCHandle handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
            ret = OIIOAPI.oiio_fill_image_data(handle.AddrOfPinnedObject(), do_rgb_to_rgba);
            if (ret == 1)
            {
                image.LoadRawTextureData(pixels);
                image.Apply();
            }
            else
            {
                Debug.Log("Could not fill texture data of " + filePath + " with OIIO.");
                return null;
            }

            return image;
        }

        private static TextureFormat Format2Format(OIIOAPI.BASETYPE format, int nchannels)
        {
            // TODO: handle compressed formats.

            TextureFormat defaultFormat = TextureFormat.RGBA32;

            switch (format)
            {
                case OIIOAPI.BASETYPE.UCHAR:
                case OIIOAPI.BASETYPE.CHAR:
                    {
                        switch (nchannels)
                        {
                            case 1: return TextureFormat.R8;
                            case 2: return TextureFormat.RG16;
                            case 3: return TextureFormat.RGB24;
                            case 4: return TextureFormat.RGBA32;
                            default: return defaultFormat;
                        }
                    }

                case OIIOAPI.BASETYPE.HALF:
                    {
                        switch (nchannels)
                        {
                            case 1: return TextureFormat.RHalf;
                            case 2: return TextureFormat.RGHalf;
                            case 3: return TextureFormat.RGBAHalf; // RGBHalf is NOT SUPPORTED
                            case 4: return TextureFormat.RGBAHalf;
                            default: return defaultFormat;
                        }
                    }

                case OIIOAPI.BASETYPE.FLOAT:
                    {
                        switch (nchannels)
                        {
                            case 1: return TextureFormat.RFloat;
                            case 2: return TextureFormat.RGFloat;
                            case 3: return TextureFormat.RGBAFloat; // RGBFloat is NOT SUPPORTED
                            case 4: return TextureFormat.RGBAFloat;
                            default: return defaultFormat;
                        }
                    }

                default: return defaultFormat;
            }
        }

        public static Texture2D LoadTextureDXT(string filePath, bool isLinear)
        {
            byte[] ddsBytes = System.IO.File.ReadAllBytes(filePath);

            byte[] format = { ddsBytes[84],ddsBytes[85],ddsBytes[86],ddsBytes[87], 0 };
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
            if(textureData.ContainsKey(filePath))
            {
                byte[] data = textureData[filePath];
                textureData.Remove(filePath);
                return LoadTexture(filePath, data, isLinear);
            }
            if(textures.ContainsKey(filePath))
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

            //if (File.Exists(ddsFile))
            //{
            //    Texture2D t = LoadTextureDXT(ddsFile, isLinear);
            //    if (null != t)
            //    {
            //        textures[filePath] = t;
            //        texturesFlipY.Add(filePath);
            //        return t;
            //    }
            //}

            if (File.Exists(filePath))
            {
                //byte[] bytes = System.IO.File.ReadAllBytes(filePath);
                //Texture2D t = LoadTextureFromBuffer(bytes, isLinear);
                Texture2D t = LoadTextureOIIO(filePath, isLinear);
                if(null != t)
                {
                    textures[filePath] = t;
                    texturesFlipY.Add(filePath);
                    return t;
                }
            }

            Texture2D texture = LoadTextureFromBuffer(data, isLinear);
            if(null != texture)
                textures[filePath] = texture;
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

            return texture;
        }


        public static void BuildMaterial(byte[] data)
        {
            int currentIndex = 0;

            string name = GetString(data, currentIndex, out currentIndex);
            float opacity = GetFloat(data, ref currentIndex);
            string opacityTexturePath = GetString(data, currentIndex, out currentIndex);

            Material material;
            if (materials.ContainsKey(name))
                material = materials[name];
            else
            {
                Shader importShader = (opacityTexturePath.Length > 0 || opacity < 1.0f)
                    ? Shader.Find("VRtist/BlenderImportTransparent")
                    : Shader.Find("VRtist/BlenderImport");
                material = new Material(importShader);
                material.name = name;
                material.enableInstancing = true;
                materials[name] = material;
            }

            //
            // OPACITY
            //
            material.SetFloat("_Opacity", opacity);
            if (opacityTexturePath.Length > 0)
            {
                Texture2D tex = GetTexture(opacityTexturePath, true);
                if (tex != null)
                {
                    material.SetFloat("_UseOpacityMap", 1f);
                    material.SetTexture("_OpacityMap", tex);
                    if (texturesFlipY.Contains(opacityTexturePath))
                        material.SetVector("_UvScale", new Vector4(1, -1, 0, 0));
                }
            }

            //
            // BASE COLOR
            //
            Color baseColor = GetColor(data, ref currentIndex);
            material.SetColor("_BaseColor", baseColor);
            string baseColorTexturePath = GetString(data, currentIndex, out currentIndex);
            if (baseColorTexturePath.Length > 0)
            {
                Texture2D tex = GetTexture(baseColorTexturePath, false);
                if(tex != null)
                {
                    material.SetFloat("_UseColorMap", 1f);
                    material.SetTexture("_ColorMap", tex);
                    if(texturesFlipY.Contains(baseColorTexturePath))
                        material.SetVector("_UvScale", new Vector4(1, -1, 0, 0));
                }

            }

            //
            // METALLIC
            //
            float metallic = GetFloat(data, ref currentIndex);
            material.SetFloat("_Metallic", metallic);
            string metallicTexturePath = GetString(data, currentIndex, out currentIndex);
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
            float roughness = GetFloat(data, ref currentIndex);
            material.SetFloat("_Roughness", roughness);
            string roughnessTexturePath = GetString(data, currentIndex, out currentIndex);
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
            string normalTexturePath = GetString(data, currentIndex, out currentIndex);
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
            Color emissionColor = GetColor(data, ref currentIndex);
            material.SetColor("_EmissiveColor", baseColor);
            string emissionColorTexturePath = GetString(data, currentIndex, out currentIndex);
            if (emissionColorTexturePath.Length > 0)
            {
                Texture2D tex = GetTexture(emissionColorTexturePath, true);
                if (tex != null)
                {
                    material.SetFloat("_UseEmissiveMap", 1f);
                    material.SetTexture("_EmissiveMap", tex);
                    if (texturesFlipY.Contains(emissionColorTexturePath))
                        material.SetVector("_UvScale", new Vector4(1, -1, 0, 0));
                }
            }


            currentMaterial = material;
        }

        public static Transform FindPath(Transform root, byte[] data, int startIndex, out int bufferIndex)
        {
            int pathLength = (int)BitConverter.ToUInt32(data, startIndex);
            string path = System.Text.Encoding.UTF8.GetString(data, 4, pathLength);
            bufferIndex = startIndex + pathLength + 4;

            char[] separator = { '/' };
            string[] splitted = path.Split(separator, 1);
            Transform parent = root;
            foreach (string subPath in splitted)
            {
                Transform transform = parent.Find(subPath);
                if (transform == null)
                {
                    return null;
                }
                parent = transform;
            }
            return parent;
        }

        public static string GetString(byte[] data, int startIndex, out int bufferIndex)
        {
            int strLength = (int)BitConverter.ToUInt32(data, startIndex);
            string str = System.Text.Encoding.UTF8.GetString(data, startIndex + 4, strLength);
            bufferIndex = startIndex + strLength + 4;
            return str;
        }

        public static string GetPathName(Transform root, Transform transform)
        {
            string result = transform.name;
            while(transform.parent && transform.parent != root)
            {
                transform = transform.parent;
                result = transform.name + "/" + result;
            }
            return result;
        }

        public static Transform BuildPath(Transform root, string path, bool includeLeaf)
        {
            string[] splitted = path.Split('/');
            Transform parent = root;
            int length = includeLeaf ? splitted.Length : splitted.Length - 1;
            for (int i = 0; i < length; i++)
            {
                string subPath = splitted[i];
                Transform transform = parent.Find(subPath);
                if (transform == null)
                {
                    transform = new GameObject(subPath).transform;
                    transform.parent = parent;
                }
                parent = transform;
            }
            return parent;
        }

        public static Transform BuildPath(Transform root, byte[] data, int startIndex, bool includeLeaf, out int bufferIndex)
        {
            string path = GetString(data, startIndex, out bufferIndex);
            return BuildPath(root, path, includeLeaf);
        }
        public static Transform BuildTransform(Transform root, byte[] data)
        {
            int currentIndex = 0;
            Transform transform = BuildPath(root, data, 0, true, out currentIndex);

            float[] buffer = new float[4];
            int size = 3 * sizeof(float);

            Buffer.BlockCopy(data, currentIndex, buffer, 0, size);
            currentIndex += size;
            transform.localPosition = new Vector3(buffer[0], buffer[1], buffer[2]);

            size = 4 * sizeof(float);
            Buffer.BlockCopy(data, currentIndex, buffer, 0, size);
            currentIndex += size;
            transform.localRotation = new Quaternion(buffer[0], buffer[1], buffer[2], buffer[3]);

            size = 3 * sizeof(float);
            Buffer.BlockCopy(data, currentIndex, buffer, 0, size);
            currentIndex += size;
            transform.localScale = new Vector3(buffer[0], buffer[1], buffer[2]);

            return transform;
        }

        public static NetCommand BuildTransformCommand(Transform root,Transform transform)
        {
            Transform current = transform;
            string path = current.name;
            while(current.parent && current.parent != root)
            {
                current = current.parent;
                path = current.name + "/" + path;
            }

            byte[] name = StringToBytes(path);
            byte[] positionBuffer = Vector3ToBytes(transform.localPosition);
            byte[] rotationBuffer = QuaternionToBytes(transform.localRotation);
            byte[] scaleBuffer = Vector3ToBytes(transform.localScale);

            List<byte[]> buffers = new List<byte[]>{ name, positionBuffer, rotationBuffer, scaleBuffer };
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
            byte[] roughness = FloatToBytes(1f - material.GetFloat("_Smoothness"));
            byte[] roughnessTexture = StringToBytes("");
            byte[] normalMapTexture = StringToBytes("");
            byte[] emissionColor = ColorToBytes(material.GetColor("_EmissionColor"));
            byte[] emissionColorTexture = StringToBytes("");

            List<byte[]> buffers = new List<byte[]> { name, opacity, opacityMapTexture, baseColor, baseColorTexture, metallic, metallicTexture, roughness, roughnessTexture, normalMapTexture, emissionColor, emissionColorTexture };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Material);
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
            byte[] name = StringToBytes(path);

            Camera cam = cameraInfo.transform.GetComponentInChildren<Camera>();
            int sensorFit = (int)cam.gateFit;

            byte[] paramsBuffer = new byte[6 * sizeof(float) + 1 * sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(cam.focalLength), 0, paramsBuffer, 0 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(cam.nearClipPlane), 0, paramsBuffer, 1 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(cam.farClipPlane), 0, paramsBuffer, 2 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(1.8f), 0, paramsBuffer, 3 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(sensorFit), 0, paramsBuffer, 4 * sizeof(float), sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(cam.sensorSize.x), 0, paramsBuffer, 4 * sizeof(float) + sizeof(int), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(cam.sensorSize.y), 0, paramsBuffer, 5 * sizeof(float) + sizeof(int), sizeof(float));

            List<byte[]> buffers = new List<byte[]> { name, paramsBuffer };
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
            byte[] name = StringToBytes(path);

            Light light = lightInfo.transform.GetComponentInChildren<Light>();
            int shadow = light.shadows != LightShadows.None ? 1 : 0;
            Color color = light.color;

            float power = 0f;
            float spotSize = 0;
            float spotBlend = 0;

            float worldScale = root.parent.localScale.x;
            float intensity = light.intensity / (worldScale * worldScale);

            switch (light.type)
            {
                case LightType.Point:
                    power = intensity * 10f;
                    break;
                case LightType.Directional:
                    power = intensity / 1.5f;
                    break;
                case LightType.Spot:
                    power = intensity / (0.4f / 3f);
                    spotSize = light.spotAngle / 180f * 3.14f;
                    spotBlend = 1f - (light.innerSpotAngle / 100f);
                    break;
            }

            byte[] paramsBuffer = new byte[2 * sizeof(int) + 7 * sizeof(float)];
            Buffer.BlockCopy(BitConverter.GetBytes((int)light.type), 0, paramsBuffer, 0 * sizeof(int), sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(shadow), 0, paramsBuffer, 1 * sizeof(int), sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(light.color.r), 0, paramsBuffer, 2 * sizeof(int), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(light.color.g), 0, paramsBuffer, 2 * sizeof(int) + 1 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(light.color.b), 0, paramsBuffer, 2 * sizeof(int) + 2 * sizeof(float), sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(light.color.a), 0, paramsBuffer, 2 * sizeof(int) + 3 * sizeof(float), sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(power), 0, paramsBuffer, 2 * sizeof(int) + 4 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(spotSize), 0, paramsBuffer, 2 * sizeof(int) + 5 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(spotBlend), 0, paramsBuffer, 2 * sizeof(int) + 6 * sizeof(float), sizeof(float));

            List<byte[]> buffers = new List<byte[]> { name, paramsBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Light);
            return command;

        }

        public static NetCommand BuildRenameCommand(Transform root, RenameInfo rename)
        {
            byte[] srcPath = StringToBytes(GetPathName(root, rename.srcTransform));
            byte[] dstName = StringToBytes(rename.newName);

            List<byte[]> buffers = new List<byte[]> { srcPath, dstName };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Rename);
            return command;
        }

        public static NetCommand BuildDuplicateCommand(Transform root, DuplicateInfos duplicate)
        {
            byte[] srcPath = StringToBytes(GetPathName(root, duplicate.srcObject.transform));
            byte[] dstName = StringToBytes(duplicate.dstObject.name);

            Transform transform = duplicate.dstObject.transform;
            byte[] positionBuffer = Vector3ToBytes(transform.localPosition);
            byte[] rotationBuffer = QuaternionToBytes(transform.localRotation);
            byte[] scaleBuffer = Vector3ToBytes(transform.localScale);

            List<byte[]> buffers = new List<byte[]> { srcPath, dstName, positionBuffer, rotationBuffer, scaleBuffer };
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

        public static NetCommand BuildRestoreFromTrashCommand(Transform root, RestoreFromTrashInfo sendToTrash)
        {
            string path = "";
            if (sendToTrash.transform.parent != root) 
                path = GetPathName(root, sendToTrash.transform.parent);

            byte[] nameBuffer = StringToBytes(sendToTrash.transform.name);
            byte[] pathBuffer = StringToBytes(path);

            List<byte[]> buffers = new List<byte[]> { nameBuffer, pathBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.RestoreFromTrash);
            return command;
        }

        public static NetCommand BuildMeshCommand(MeshInfos meshInfos)
        {
            Mesh mesh = meshInfos.meshFilter.mesh;
            byte[] name = StringToBytes(mesh.name);

            byte[] positions = Vector3ToBytes(mesh.vertices);
            byte[] normals = Vector3ToBytes(mesh.normals);
            byte[] uvs = Vector2ToBytes(mesh.uv);

            byte[] materialIndices = new byte[sizeof(int) + mesh.subMeshCount * 2 * sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(mesh.subMeshCount), 0, materialIndices, 0, sizeof(int));
            int offset = sizeof(int);

            for (int i = 0; i <  mesh.subMeshCount; i++)
            {
                SubMeshDescriptor subMesh = mesh.GetSubMesh(i);
                int start = subMesh.indexStart / 3;
                Buffer.BlockCopy(BitConverter.GetBytes(start), 0, materialIndices, offset, sizeof(int));
                Buffer.BlockCopy(BitConverter.GetBytes(i), 0, materialIndices, offset + sizeof(int), sizeof(int));
                offset += 2 * sizeof(int);
            }

            byte[] triangles = TriangleIndicesToBytes(mesh.triangles);

            Material[] materials = meshInfos.meshRenderer.sharedMaterials;
            string[] materialNames = new string[materials.Length];
            int index = 0;
            foreach (Material material in materials)
            {
                materialNames[index++] = material.name;

            }
            byte[] materialsBuffer = StringsToBytes(materialNames);

            List<byte[]> buffers = new List<byte[]> { name, positions, normals, uvs, materialIndices, triangles, materialsBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Mesh);
            return command;
        }

        public static NetCommand BuildMeshConnectionCommand(Transform root, MeshConnectionInfos meshConnectionInfos)
        {
            Transform transform = meshConnectionInfos.meshTransform;
            Mesh mesh = transform.GetComponent<MeshFilter>().mesh;
            string path = GetPathName(root, transform);

            byte[] pathBuffer = StringToBytes(path);
            byte[] nameBuffer = StringToBytes(mesh.name);

            List<byte[]> buffers = new List<byte[]> { pathBuffer, nameBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.MeshConnection);
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

        public static void BuildCamera(Transform root, byte[] data)
        {
            int tmpIndex = 0;
            string path = GetString(data, 0, out tmpIndex);

            int currentIndex = 0;
            Transform transform = BuildPath(root, data, 0, false, out currentIndex);
            if (transform == null)
                return;

            GameObject camGameObject = null;
            string[] splittedPath = path.Split('/');
            string name = splittedPath[splittedPath.Length - 1];
            Transform camTransform = transform.Find(name);
            if (camTransform == null)
            {
                camGameObject = Utils.CreateInstance(Resources.Load("Prefabs/Camera") as GameObject, transform);
                camGameObject.name = name;
                //camGameObject.transform.GetChild(0).Rotate(0f, 180f, 0f);
                //camGameObject.transform.GetChild(0).localScale = new Vector3(-1, 1, 1);
            }
            else
            {
                camGameObject = camTransform.gameObject;
            }

            float focal = BitConverter.ToSingle(data, currentIndex);
            float near = BitConverter.ToSingle(data, currentIndex + sizeof(float));
            float far = BitConverter.ToSingle(data, currentIndex + 2 * sizeof(float));
            float aperture = BitConverter.ToSingle(data, currentIndex + 3 * sizeof(float));
            currentIndex += 4 * sizeof(float);

            Camera.GateFitMode gateFit = (Camera.GateFitMode)BitConverter.ToInt32(data, currentIndex);
            if (gateFit == Camera.GateFitMode.None)
                gateFit = Camera.GateFitMode.Horizontal;
            currentIndex += sizeof(Int32);

            float sensorWidth = BitConverter.ToSingle(data, currentIndex);
            currentIndex += sizeof(float);
            float sensorHeight = BitConverter.ToSingle(data, currentIndex);
            currentIndex += sizeof(float);

            Camera cam = camGameObject.GetComponentInChildren<Camera>();

            // Is it necessary ?
            /////////////////
            CameraController cameraController = camGameObject.GetComponent<CameraController>();
            CameraParameters cameraParameters = (CameraParameters)cameraController.GetParameters();
            cameraParameters.focal = focal;
            //cameraParameters.gateFit = gateFit;
            /////////////////

            cam.focalLength = focal;
            cam.gateFit = gateFit;

            cameraParameters.focal = focal;
            cam.focalLength = focal;
            cam.sensorSize = new Vector2(sensorWidth, sensorHeight);

            cameraController.FireValueChanged();
        }

        public static void BuildLight(Transform root, byte[] data)
        {
            int tmpIndex = 0;
            string path = GetString(data, 0, out tmpIndex);

            int currentIndex = 0;
            Transform transform = BuildPath(root, data, 0, false, out currentIndex);
            if (transform == null)
                return;

            LightType lightType = (LightType)BitConverter.ToInt32(data, currentIndex);
            currentIndex += sizeof(Int32);

            GameObject lightGameObject = null;
            string[] splittedPath = path.Split('/');
            string name = splittedPath[splittedPath.Length - 1];
            Transform lightTransform = transform.Find(name);
            if (lightTransform == null)
            {
                switch (lightType)
                {
                    case LightType.Directional:
                        lightGameObject = Utils.CreateInstance(Resources.Load("Prefabs/Sun") as GameObject, transform);
                        break;
                    case LightType.Point:
                        lightGameObject = Utils.CreateInstance(Resources.Load("Prefabs/Point") as GameObject, transform);
                        break;
                    case LightType.Spot:
                        lightGameObject = Utils.CreateInstance(Resources.Load("Prefabs/Spot") as GameObject, transform);
                        break;
                }
                //lightGameObject.transform.GetChild(0).Rotate(0f, 180f, 0f);
                lightGameObject.name = name;
            }
            else
            {
                lightGameObject = lightTransform.gameObject;
            }


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


            LightController lightController = lightGameObject.GetComponent<LightController>();
            LightParameters lightParameters = (LightParameters)lightController.GetParameters();
            lightParameters.color = lightColor;
            switch(lightType)
            {
                case LightType.Point:
                    lightParameters.intensity = power / 10f;
                    break;
                case LightType.Directional:
                    lightParameters.intensity = power * 1.5f;
                    break;
                case LightType.Spot:
                    lightParameters.intensity = power * 0.4f / 3f;
                    break;
            }

            if (lightType == LightType.Spot)
            {
                lightParameters.SetRange(1000f);
                lightParameters.SetOuterAngle(spotSize * 180f / 3.14f);
                lightParameters.SetInnerAngle((1f - spotBlend) * 100f);
            }
            lightParameters.castShadows = shadow != 0 ? true : false;
            lightController.FireValueChanged();
        }

        public static Transform ConnectMesh(Transform root, byte[] data)
        {
            int currentIndex = 0;
            Transform transform = BuildPath(root, data, 0, true, out currentIndex);
            string meshName = GetString(data, currentIndex, out currentIndex);

            GameObject gobject = transform.gameObject;
            MeshFilter meshFilter = gobject.GetComponent<MeshFilter>();
            if (meshFilter == null)
                meshFilter = gobject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = gobject.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                meshRenderer = gobject.AddComponent<MeshRenderer>();

            Mesh mesh = meshes[meshName];

            meshRenderer.sharedMaterials = meshesMaterials[meshName];

            gobject.tag = "PhysicObject";

            if(!meshInstances.ContainsKey(meshName))
                meshInstances[meshName] = new HashSet<MeshFilter>();
            meshInstances[meshName].Add(meshFilter);
            meshFilter.mesh = mesh;

            MeshCollider collider = gobject.AddComponent<MeshCollider>();

            return transform;
        }

        public static Mesh BuildMesh(Transform root, byte[] data)
        {
            int currentIndex = 0;
            string meshName = GetString(data, currentIndex, out currentIndex);

            int verticesCount = (int)BitConverter.ToUInt32(data, currentIndex);
            currentIndex += 4;
            int size = verticesCount * sizeof(float) * 3;
            Vector3[] vertices = new Vector3[verticesCount];
            float[] float3Values = new float[verticesCount * 3];
            Buffer.BlockCopy(data, currentIndex, float3Values, 0, size);
            int idx = 0;
            for(int i = 0; i < verticesCount; i++)
            {
                vertices[i].x = float3Values[idx++];
                vertices[i].y = float3Values[idx++];
                vertices[i].z = float3Values[idx++];
            }
            currentIndex += size;

            int normalsCount = (int)BitConverter.ToUInt32(data, currentIndex);
            currentIndex += 4;
            size = normalsCount * sizeof(float) * 3;
            Vector3[] normals = new Vector3[normalsCount];
            Buffer.BlockCopy(data, currentIndex, float3Values, 0, size);
            idx = 0;
            for (int i = 0; i < verticesCount; i++)
            {
                normals[i].x = float3Values[idx++];
                normals[i].y = float3Values[idx++];
                normals[i].z = float3Values[idx++];
            }
            currentIndex += size;

            UInt32 UVsCount = BitConverter.ToUInt32(data, currentIndex);
            currentIndex += 4;

            size = (int)UVsCount * sizeof(float) * 2;
            Vector2[] uvs = new Vector2[UVsCount];
            Buffer.BlockCopy(data, currentIndex, float3Values, 0, size);
            idx = 0;
            for (int i = 0; i < UVsCount; i++)
            {
                uvs[i].x = float3Values[idx++];
                uvs[i].y = float3Values[idx++];
            }
            currentIndex += size;

            int materialIndicesCount = (int)BitConverter.ToUInt32(data, currentIndex);
            currentIndex += 4;
            int[] materialIndices = new int[materialIndicesCount * 2];
            size = materialIndicesCount * 2 * sizeof(int);
            Buffer.BlockCopy(data, currentIndex, materialIndices, 0, size);
            currentIndex += size;

            int indicesCount = (int)BitConverter.ToUInt32(data, currentIndex) * 3;
            currentIndex += 4;
            int[] indices = new int[indicesCount];
            size = indicesCount * sizeof(int);
            Buffer.BlockCopy(data, currentIndex, indices, 0, size);
            currentIndex += size;


            int materialCount = (int)BitConverter.ToUInt32(data, currentIndex);
            currentIndex += 4;
            Material[] meshMaterials;
            if (materialCount == 0)
            {
                meshMaterials = new Material[1];
                meshMaterials[0] = DefaultMaterial();
                materialCount = 1;
            }
            else
            {
                meshMaterials = new Material[materialCount];
                for (int i = 0; i < materialCount; i++)
                {
                    int materialNameSize = (int)BitConverter.ToUInt32(data, currentIndex);
                    string materialName = System.Text.Encoding.UTF8.GetString(data, currentIndex + 4, materialNameSize);
                    currentIndex += materialNameSize + 4;

                    meshMaterials[i] = null;
                    if (materials.ContainsKey(materialName))
                    {
                        meshMaterials[i] = materials[materialName];
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
                mesh.triangles = indices;
            else
            {
                int remainingTringles = indicesCount / 3;
                int currentTriangleIndex = 0;
                mesh.subMeshCount = materialCount;

                int[][] subIndices = new int[materialCount][];
                int[] trianglesPerMaterialCount = new int[materialCount];
                int[] subIndicesIndices = new int[materialCount];
                for (int i = 0; i < materialCount; i++)
                {
                    trianglesPerMaterialCount[i] = 0;
                    subIndicesIndices[i] = 0;
                }

                // count
                for (int i = 0; i < materialIndicesCount; i++)
                {
                    int triangleCount = remainingTringles;
                    if (i < (materialIndicesCount - 1))
                    {
                        triangleCount = materialIndices[(i + 1) * 2] - materialIndices[i * 2];
                        remainingTringles -= triangleCount;
                    }
                    int materialIndex = materialIndices[i * 2 + 1];
                    trianglesPerMaterialCount[materialIndex] += triangleCount;
                }

                //allocate
                for(int i = 0; i < materialCount; i++)
                {
                    subIndices[i] = new int[trianglesPerMaterialCount[i] * 3];
                }

                // fill
                remainingTringles = indicesCount / 3;
                for (int i = 0; i < materialIndicesCount; i++)
                {
                    // allocate triangles
                    int triangleCount = remainingTringles;
                    if (i < (materialIndicesCount - 1))
                    {
                        triangleCount = materialIndices[(i + 1) * 2] - materialIndices[i * 2];
                        remainingTringles -= triangleCount;
                    }
                    int materialIndex = materialIndices[i * 2 + 1];
                    int dataSize = triangleCount * 3 * sizeof(int);
                    Buffer.BlockCopy(indices, currentTriangleIndex, subIndices[materialIndex], subIndicesIndices[materialIndex], dataSize);
                    subIndicesIndices[materialIndex] += dataSize;
                    currentTriangleIndex += dataSize;
                }

                // set
                for(int i = 0; i < materialCount; i++)
                {
                    mesh.SetTriangles(subIndices[i], i);
                }
            }

            mesh.RecalculateBounds();
            meshes[meshName] = mesh;
            meshesMaterials[meshName] = meshMaterials;

            if (meshInstances.ContainsKey(meshName))
            {
                HashSet<MeshFilter> filters = meshInstances[meshName];
                foreach (MeshFilter filter in filters)
                {
                    if(filter)
                        filter.mesh = mesh;
                }
            }

            return mesh;
        }
    }

    public class NetworkClient : MonoBehaviour
    {
        private static NetworkClient _instance;
        public Transform root;
        public int port = 12800;

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

        void Update()
        {
            lock (this)
            {
                if (receivedCommands.Count == 0)
                    return;

                foreach (NetCommand command in receivedCommands)
                {
                    Debug.Log("Command Id " + command.id.ToString());
                    switch (command.messageType)
                    {
                        case MessageType.Mesh:
                            NetGeometry.BuildMesh(root, command.data);
                            break;
                        case MessageType.MeshConnection:
                            NetGeometry.ConnectMesh(root, command.data);
                            break;
                        case MessageType.Transform:
                            NetGeometry.BuildTransform(root, command.data);
                            break;
                        case MessageType.Material:
                            NetGeometry.BuildMaterial(command.data);
                            break;
                        case MessageType.Camera:
                            NetGeometry.BuildCamera(root, command.data);
                            break;
                        case MessageType.Light:
                            NetGeometry.BuildLight(root, command.data);
                            break;
                        case MessageType.Delete:
                            NetGeometry.Delete(root, command.data);
                            break;
                        case MessageType.Rename:
                            NetGeometry.Rename(root, command.data);
                            break;
                        case MessageType.Duplicate:
                            NetGeometry.Duplicate(root, command.data);
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
                    }
                }
                receivedCommands.Clear();
            }
        }

        void Start()
        {
            Connect();
        }

        public void Connect()
        {
            connected = false;
            string[] args = System.Environment.GetCommandLineArgs();
            string room = "Local";
            string hostname = "localhost";
            int port = 12800;

            //hostname = "lgy-wks-054880";
            //room = "thomas.capelle";

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--room")
                {
                    room = args[i + 1];
                }

                if (args[i] == "--hostname")
                {
                    hostname = args[i + 1];
                }

                if (args[i] == "--port")
                {
                    Int32.TryParse(args[i + 1], out port);
                }

            }
            
            IPHostEntry ipHostInfo = Dns.GetHostEntry(hostname);
            if (ipHostInfo.AddressList.Length == 0)
                return;

            IPAddress ipAddress = null;
            for (int i = ipHostInfo.AddressList.Length - 1; i >= 0; i --)
            {
                IPAddress addr = ipHostInfo.AddressList[i];
                if (addr.ToString().Contains(":"))
                    continue;
                ipAddress = addr;
                break;
            }

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
            Debug.Log("Received Command Id " + commandId);
            var mtype = BitConverter.ToUInt16(header, 8 + 4);

            byte[] data = new byte[size];
            long remaining = size;
            long current = 0;
            while (remaining > 0)
            {
                int sizeRead = socket.Receive(data, (int)current, (int)remaining, SocketFlags.None);
                current += sizeRead;
                remaining -= sizeRead;
            }


            NetCommand command = new NetCommand(data, (MessageType)mtype);
            return command;
        }

        void WriteMessage(NetCommand command)
        {
            byte[] sizeBuffer = BitConverter.GetBytes((Int64)command.data.Length);
            byte[] commandId = BitConverter.GetBytes((Int32)command.id);
            byte[] typeBuffer = BitConverter.GetBytes((Int16)command.messageType);
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

        public void SendTransform(Transform transform)
        {
            NetCommand command = NetGeometry.BuildTransformCommand(root, transform);
            AddCommand(command);
        }

        public void SendMesh(MeshInfos meshInfos)
        {
            NetCommand command = NetGeometry.BuildMeshCommand(meshInfos);
            AddCommand(command);
        }

        public void SendMeshConnection(MeshConnectionInfos meshConnectionInfos)
        {
            NetCommand command = NetGeometry.BuildMeshConnectionCommand(root, meshConnectionInfos);
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
        public void SendRename(RenameInfo rename)
        {
            NetCommand command = NetGeometry.BuildRenameCommand(root, rename);
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

        public void JoinRoom(string roomName)
        {
            NetCommand command = new NetCommand(System.Text.Encoding.UTF8.GetBytes(roomName), MessageType.JoinRoom);
            AddCommand(command);
        }

        void Send(byte[] data)
        {
            lock (this)
            {
                socket.Send(data);
            }
        }

        void Run()
        {
            while(alive)
            {
                NetCommand command = ReadMessage();
                if(command != null)
                {
                    if(command.messageType > MessageType.Command)
                    {
                        lock (this)
                        {
                            receivedCommands.Add(command);
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

        public void SendEvent<T>(MessageType messageType, T data)
        {
            if(!connected) { return; }
            switch(messageType)
            {
                case MessageType.Transform:
                    SendTransform(data as Transform); break;
                case MessageType.Mesh:
                    SendMesh(data as MeshInfos); break;
                case MessageType.MeshConnection:
                    SendMeshConnection(data as MeshConnectionInfos); break;
                case MessageType.Delete:
                    SendDelete(data as DeleteInfo); break;
                case MessageType.Material:
                    SendMaterial(data as Material); break;
                case MessageType.Camera:
                    SendCamera(data as CameraInfo);
                    SendTransform((data as CameraInfo).transform);
                    break;
                case MessageType.Light:
                    SendLight(data as LightInfo); 
                    SendTransform((data as LightInfo).transform);
                    break;
                case MessageType.Rename:
                    SendRename(data as RenameInfo); break;
                case MessageType.Duplicate:
                    SendDuplicate(data as DuplicateInfos); break;
                case MessageType.SendToTrash:
                    SendToTrash(data as SendToTrashInfo); break;
                case MessageType.RestoreFromTrash:
                    RestoreFromTrash(data as RestoreFromTrashInfo); break;
            }
        }
    }
}