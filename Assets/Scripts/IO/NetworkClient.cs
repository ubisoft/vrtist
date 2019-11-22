using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

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

        public static void BuildMaterial(byte[] data)
        {
            int nameLength = (int)BitConverter.ToUInt32(data, 0);
            string name = System.Text.Encoding.UTF8.GetString(data, 4, nameLength);

            if (materials.ContainsKey(name))
            {
                currentMaterial = materials[name];
                return;
            }

            int currentIndex = 4 + nameLength;

            float[] buffer = new float[3];
            int size = 3 * sizeof(float);

            Buffer.BlockCopy(data, currentIndex, buffer, 0, size);
            currentIndex += size;
            Color baseColor = new Color(buffer[0], buffer[1], buffer[2]);

            float metallic = BitConverter.ToSingle(data, currentIndex);
            currentIndex += sizeof(float);
            float roughness = BitConverter.ToSingle(data, currentIndex);

            Shader hdrplit = Shader.Find("HDRP/Lit");
            Material material = new Material(hdrplit);
            material.name = name;
            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness",1f - roughness);

            materials.Add(name, material);
            currentMaterial = material;
        }

        public static Transform BuildPath(Transform root, byte[] data, int startIndex, out int bufferIndex)
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
                if(transform == null)
                {
                    transform = new GameObject(subPath).transform;                    
                    transform.parent = parent;
                }
                parent = transform;
            }
            return parent;
        }
        public static Transform BuildTransform(Transform root, byte[] data)
        {
            int currentIndex = 0;
            Transform transform = BuildPath(root, data, 0, out currentIndex);

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

        public static Mesh BuildMesh(Transform root, byte[] data)
        {
            int currentIndex = 0;
            Transform transform = BuildPath(root, data, 0, out currentIndex);

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
            int[] materialIndices = new int[materialIndicesCount];
            size = materialIndicesCount * sizeof(int);
            Buffer.BlockCopy(data, currentIndex, materialIndices, 0, size);
            currentIndex += size;

            int indicesCount = (int)BitConverter.ToUInt32(data, currentIndex);
            currentIndex += 4;
            int[] indices = new int[indicesCount];
            size = indicesCount * sizeof(int);
            Buffer.BlockCopy(data, currentIndex, indices, 0, size);
            currentIndex += size;

            int materialCount = (int)BitConverter.ToUInt32(data, currentIndex);
            currentIndex += 4;
            Material[] meshMaterials = new Material[materialCount];
            for(int i = 0; i < materialCount; i++)
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

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.triangles = indices;

            GameObject gobject = transform.gameObject;
            MeshFilter meshFilter = gobject.GetComponent<MeshFilter>();
            if(meshFilter == null)
                meshFilter = gobject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = gobject.GetComponent<MeshRenderer>();
            if(meshRenderer == null)
                meshRenderer = gobject.AddComponent<MeshRenderer>();
            meshFilter.mesh = mesh;

            //////////////////////
            meshRenderer.sharedMaterial = currentMaterial;
            //////////////////////

            return mesh;
        }
    }

    public class NetworkClient : MonoBehaviour
    {
        public Transform root;
        public string host = "localhost";
        public int port = 12800;

        Thread thread = null;
        bool alive = true;

        Socket socket = null;
        List<NetCommand> receivedCommands = new List<NetCommand>();
        List<NetCommand> pendingCommands = new List<NetCommand>();

        ~NetworkClient()
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
                        case MessageType.Transform:
                            NetGeometry.BuildTransform(root, command.data);
                            break;
                        case MessageType.Material:
                            NetGeometry.BuildMaterial(command.data);
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
            IPHostEntry ipHostInfo = Dns.GetHostEntry(host);
            IPAddress ipAddress = ipHostInfo.AddressList[1];
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
            }
            catch (SocketException se)
            {
                Console.WriteLine("SocketException : {0}", se.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception : {0}", e.ToString());
            }

            JoinRoom("toto");

            thread = new Thread(new ThreadStart(Run));
            thread.Start();
        }
        public void Join()
        {
            if (thread == null)
                return;
            alive = false;
            thread.Join();
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
            byte[] buffer = new byte[sizeof(Int64) + sizeof(Int32) + sizeof(Int16) + command.data.Length];
            Buffer.BlockCopy(sizeBuffer, 0, buffer, 0, sizeof(Int64));
            Buffer.BlockCopy(commandId, 0, buffer, sizeof(Int64), sizeof(Int32));
            Buffer.BlockCopy(typeBuffer, 0, buffer, sizeof(Int64) + sizeof(Int32), sizeof(Int16));
            Buffer.BlockCopy(command.data, 0, buffer, sizeof(Int64) + sizeof(Int32) + sizeof(Int16), command.data.Length);
            socket.Send(buffer);
        }

        void AddCommand(NetCommand command)
        {
            lock (this)
            {
                pendingCommands.Add(command);
            }
        }

        public void JoinRoom(string roomName)
        {
            NetCommand command = new NetCommand(System.Text.Encoding.UTF8.GetBytes(roomName), MessageType.JoinRoom);
            WriteMessage(command);
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
    }
}