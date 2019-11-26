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

        public static byte[] Vector3ToBytes(Vector3 vector)
        {
            byte[] bytes = new byte[3 * sizeof(float)];

            Buffer.BlockCopy(BitConverter.GetBytes(vector.x), 0, bytes, 0, sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(vector.y), 0, bytes, sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(vector.z), 0, bytes, 2 * sizeof(float), sizeof(float));
            return bytes;
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

        public static Material DefaultMaterial()
        {
            string name = "defaultMaterial";
            if (materials.ContainsKey(name))
                return materials[name];

            Shader hdrplit = Shader.Find("HDRP/Lit");
            Material material = new Material(hdrplit);
            material.name = name;
            material.SetColor("_BaseColor", new Color(0.8f,0.8f,0.8f));
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.5f);
            materials[name] = material;

            return material;
        }

        public static void BuildMaterial(byte[] data)
        {
            int nameLength = (int)BitConverter.ToUInt32(data, 0);
            string name = System.Text.Encoding.UTF8.GetString(data, 4, nameLength);
            
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

            materials[name] = material;
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

        public static NetCommand BuildTransformCommand(Transform root,Transform transform)
        {
            Transform current = transform;
            string path = current.name;
            while(current.parent && current != root)
            {
                current = current.parent;
                path = current.name + "/" + path;
            }

            byte[] nameBuffer = System.Text.Encoding.UTF8.GetBytes(path);
            byte[] nameBufferSize = BitConverter.GetBytes(nameBuffer.Length);

            byte[] positionBuffer = Vector3ToBytes(transform.localPosition);
            byte[] rotationBuffer = QuaternionToBytes(transform.localRotation);
            byte[] scaleBuffer = Vector3ToBytes(transform.localScale);

            List<byte[]> buffers = new List<byte[]>{ nameBufferSize, nameBuffer, positionBuffer, rotationBuffer, scaleBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Transform);
            return command;
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

            int materialIndicesCount = (int)BitConverter.ToUInt32(data, currentIndex) / 2;
            currentIndex += 4;
            int[] materialIndices = new int[materialIndicesCount * 2];
            size = materialIndicesCount * 2 * sizeof(int);
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
                mesh.subMeshCount = materialIndicesCount;

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

            GameObject gobject = transform.gameObject;
            MeshFilter meshFilter = gobject.GetComponent<MeshFilter>();
            if(meshFilter == null)
                meshFilter = gobject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = gobject.GetComponent<MeshRenderer>();
            if(meshRenderer == null)
                meshRenderer = gobject.AddComponent<MeshRenderer>();
            meshFilter.mesh = mesh;

            meshRenderer.sharedMaterials = meshMaterials;

            MeshCollider collider = gobject.AddComponent<MeshCollider>();
            gobject.tag = "PhysicObject";

            return mesh;
        }
    }

    public class NetworkClient : MonoBehaviour
    {
        private static NetworkClient _instance;
        public Transform root;
        public string host = "localhost";
        public int port = 12800;

        Thread thread = null;
        bool alive = true;

        Socket socket = null;
        List<NetCommand> receivedCommands = new List<NetCommand>();
        List<NetCommand> pendingCommands = new List<NetCommand>();

        public void Awake()
        {
            _instance = this;
        }

        ~NetworkClient()
        {
            Join();
        }

        public static NetworkClient GetInstance()
        {
            return _instance;
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
            WriteMessage(command);
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

        public void SendEvent<T>(MessageType messageType, T data)
        {
            switch(messageType)
            {
                case MessageType.Transform:
                    SendTransform(data as Transform); break;
            }
        }
    }
}