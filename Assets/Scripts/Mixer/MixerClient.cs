using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using UnityEngine;

using VRtist.Serialization;

namespace VRtist
{
    // Message types
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
        _CameraAttributes,
        _LightAttributes,
        _BlenderDataRemove,
        _BlenderDataRename,
        ClearAnimations,
        CurrentCamera,
        _ShotManagerMontageMode,
        ShotManagerContent,
        _ShotManagerCurrentShot,
        ShotManagerAction,
        _BlenderDataCreate,
        _BlenderDataMedia,
        Empty,
        AddConstraint,
        RemoveConstraint,
        BlenderBank,

        Optimized_Commands = 200,
        Transform,
        Mesh,
        Material,
        AssignMaterial,
        _Frame, // deprecated
        Play,
        Pause,
        Sky,

        End_Optimized_Commands = 999,
        ClientIdWrapper = 1000
    }

    // Commands exchanged with mixer server
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


    public class MixerClient : MonoBehaviour
    {
        private static MixerClient _instance;
        public static MixerClient Instance
        {
            get { return _instance; }
        }
        public Transform root;
        public string hostname = "localhost";
        public int port = 12800;
        public string room = "Local";
        public string userName = "VRtist";
        public Color userColor = Color.white;
        public string master = "";


        Transform prefab;

        Thread thread = null;
        bool alive = true;
        bool connected = false;

        Socket socket = null;
        List<NetCommand> receivedCommands = new List<NetCommand>();
        readonly List<NetCommand> pendingCommands = new List<NetCommand>();

        public void Awake()
        {
            _instance = this;
        }


        void OnDestroy()
        {
            Join();
        }

        void Start()
        {
            if (null == SyncData.mixer)
                SyncData.mixer = new VRtistMixerImpl();

            Connect();

            GameObject prefabGameObject = new GameObject("__Prefab__");
            prefabGameObject.SetActive(false);
            prefab = prefabGameObject.transform;

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
                    if (Int32.TryParse(splitted[i], out int val) && val >= 0 && val <= 255)
                    {
                        baddr[i] = (byte)val;
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

            IPAddress addr = ipHostInfo.AddressList[ipHostInfo.AddressList.Length - 1];
            ipAddress = addr;

            return ipAddress;
        }

        public void Connect()
        {
            connected = false;
            string[] args = System.Environment.GetCommandLineArgs();
            SyncData.mixer.GetNetworkData(ref hostname, ref room, ref port, ref master, ref userName, ref userColor);

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

            SyncData.mixer.SetNetworkData(room, master, userName, userColor);

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
            //var commandId = BitConverter.ToInt32(header, 8);
            //Debug.Log("Received Command Id " + commandId);
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

            socket.Send(Converter.ConcatenateBuffers(buffers));

            //Debug.Log($"Sending command: {command.messageType}");
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
            NetCommand command = MixerUtils.BuildObjectVisibilityCommand(transform);
            AddCommand(command);
        }

        public void SendTransform(Transform transform)
        {
            NetCommand command = MixerUtils.BuildTransformCommand(transform);
            AddCommand(command);
        }

        public void SendMesh(MeshInfos meshInfos)
        {
            NetCommand command = MixerUtils.BuildMeshCommand(root, meshInfos);
            AddCommand(command);
        }

        public void SendDelete(DeleteInfo deleteInfo)
        {
            NetCommand command = MixerUtils.BuildDeleteCommand(root, deleteInfo);
            AddCommand(command);
        }

        public void SendMaterial(Material material)
        {
            NetCommand command = MixerUtils.BuildMaterialCommand(material);
            AddCommand(command);
        }

        public void SendAssignMaterial(AssignMaterialInfo info)
        {
            NetCommand command = MixerUtils.BuildAssignMaterialCommand(info);
            AddCommand(command);
        }

        public void SendEmpty(Transform transform)
        {
            NetCommand command = MixerUtils.BuildEmptyCommand(root, transform);
            AddCommand(command);
        }

        public void SendAddParentConstraint(GameObject gobject, GameObject target)
        {
            NetCommand command = MixerUtils.BuildSendAddParentConstraintCommand(gobject, target);
            AddCommand(command);
        }

        public void SendAddLookAtConstraint(GameObject gobject, GameObject target)
        {
            NetCommand command = MixerUtils.BuildSendAddLookAtConstraintCommand(gobject, target);
            AddCommand(command);
        }

        public void SendRemoveParentConstraint(GameObject gobject)
        {
            NetCommand command = MixerUtils.BuildSendRemoveParentConstraintCommand(gobject);
            AddCommand(command);
            SendTransform(gobject.transform);  // For Blender
        }

        public void SendRemoveLookAtConstraint(GameObject gobject)
        {
            NetCommand command = MixerUtils.BuildSendRemoveLookAtConstraintCommand(gobject);
            AddCommand(command);
            SendTransform(gobject.transform);  // For Blender
        }

        public void SendCamera(CameraInfo cameraInfo)
        {
            NetCommand command = MixerUtils.BuildCameraCommand(root, cameraInfo);
            AddCommand(command);
        }

        public void SendLight(LightInfo lightInfo)
        {
            NetCommand command = MixerUtils.BuildLightCommand(root, lightInfo);
            AddCommand(command);
        }
        public void SendSky(SkySettings skyInfo)
        {
            NetCommand command = MixerUtils.BuildSkyCommand(skyInfo);
            AddCommand(command);
        }
        public void SendRename(RenameInfo rename)
        {
            NetCommand command = MixerUtils.BuildRenameCommand(root, rename);
            AddCommand(command);
        }

        public void SendFrameStartEnd(FrameStartEnd range)
        {
            NetCommand command = MixerUtils.BuildSendFrameStartEndCommand(range.start, range.end);
            AddCommand(command);
        }

        public void SendAddKeyframe(SetKeyInfo data)
        {
            NetCommand command = MixerUtils.BuildSendSetKey(data);
            AddCommand(command);
        }

        public void SendAnimationCurve(CurveInfo data)
        {
            NetCommand command = MixerUtils.BuildSendAnimationCurve(data);
            AddCommand(command);
        }

        public void SendRemoveKeyframe(SetKeyInfo data)
        {
            NetCommand command = MixerUtils.BuildSendRemoveKey(data);
            AddCommand(command);
        }

        public void SendMoveKeyframe(MoveKeyInfo data)
        {
            NetCommand command = MixerUtils.BuildSendMoveKey(data);
            AddCommand(command);
        }

        public void SendQueryObjectData(string name)
        {
            NetCommand command = MixerUtils.BuildSendQueryAnimationData(name);
            AddCommand(command);
        }
        public void SendDuplicate(DuplicateInfos duplicate)
        {
            NetCommand command = MixerUtils.BuildDuplicateCommand(root, duplicate);
            AddCommand(command);
        }

        public void SendToTrash(SendToTrashInfo sendToTrash)
        {
            NetCommand command = MixerUtils.BuildSendToTrashCommand(root, sendToTrash);
            AddCommand(command);
        }

        public void RestoreFromTrash(RestoreFromTrashInfo restoreFromTrash)
        {
            NetCommand command = MixerUtils.BuildRestoreFromTrashCommand(root, restoreFromTrash);
            AddCommand(command);
        }

        public void SendAddObjectToColleciton(AddToCollectionInfo addToCollectionInfo)
        {
            string collectionName = addToCollectionInfo.collectionName;
            if (!SyncData.collectionNodes.ContainsKey(collectionName))
            {
                NetCommand addCollectionCommand = MixerUtils.BuildAddCollecitonCommand(collectionName);
                AddCommand(addCollectionCommand);
            }

            NetCommand commandAddObjectToCollection = MixerUtils.BuildAddObjectToCollecitonCommand(addToCollectionInfo);
            AddCommand(commandAddObjectToCollection);
            if (!SyncData.sceneCollections.Contains(collectionName))
            {
                NetCommand commandAddCollectionToScene = MixerUtils.BuildAddCollectionToScene(collectionName);
                AddCommand(commandAddCollectionToScene);
            }
        }

        public void SendAddObjectToScene(AddObjectToSceneInfo addObjectToScene)
        {
            NetCommand command = MixerUtils.BuildAddObjectToScene(addObjectToScene);
            AddCommand(command);
        }

        public void SendClearAnimations(ClearAnimationInfo info)
        {
            NetCommand command = MixerUtils.BuildSendClearAnimations(info);
            AddCommand(command);
        }

        public void SendShotManagerAction(ShotManagerActionInfo info)
        {
            NetCommand command = MixerUtils.BuildSendShotManagerAction(info);
            AddCommand(command);
        }

        public void SendBlenderBank(BlenderBankInfo info)
        {
            NetCommand command = MixerUtils.BuildSendBlenderBank(info);
            AddCommand(command);
        }

        public void SendPlayerTransform(ConnectedUser info)
        {
            NetCommand command = MixerUtils.BuildSendPlayerTransform(info);
            if (null != command) { AddCommand(command); }
        }

        public void JoinRoom(string roomName)
        {
            byte[] nameBuffer = Converter.StringToBytes(roomName);
            byte[] mockVersionBuffer = Converter.StringToBytes("ignored");
            byte[] versionCheckBuffer = Converter.BoolToBytes(true);
            byte[] buffer = Converter.ConcatenateBuffers(new List<byte[]> { nameBuffer, mockVersionBuffer, mockVersionBuffer, versionCheckBuffer });
            NetCommand command = new NetCommand(buffer, MessageType.JoinRoom);
            AddCommand(command);

            string json = SyncData.mixer.CreateClientNameAndColor();
            if (null == json)
                return;
            NetCommand commandClientInfo = new NetCommand(Converter.StringToBytes(json), MessageType.SetClientCustomAttribute);
            AddCommand(commandClientInfo);
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
            string masterId = Converter.GetString(command.data, ref index);

            // For debug purpose (unity in editor mode when networkSettings.master is empty)
            string currentMasterId = SyncData.mixer.GetMasterId();
            if (null == currentMasterId || currentMasterId.Length == 0)
                SyncData.mixer.SetMasterId(masterId);

            if (masterId != SyncData.mixer.GetMasterId())
                return false;

            int remainingData = command.data.Length - index;
            while (remainingData > 0)
            {
                int dataLength = Converter.GetInt(command.data, ref index);
                remainingData -= dataLength + sizeof(int);

                dataLength -= sizeof(int);
                int messageType = Converter.GetInt(command.data, ref index);
                byte[] newBuffer = new byte[dataLength];

                Buffer.BlockCopy(command.data, index, newBuffer, 0, dataLength);
                index += dataLength;

                NetCommand newCommand = new NetCommand(newBuffer, (MessageType)messageType);
                commands.Add(newCommand);
            }

            return true;
        }

        public List<NetCommand> commands = new List<NetCommand>();
        int commandProcessedCount = 0;
        private int i = 0;

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
                {
                    yield return null;
                    continue;
                }

                // don't process commands on play/record
                if (GlobalState.Animation.IsAnimating())
                {
                    yield return null;
                    continue;
                }

                DateTime before = DateTime.Now;
                bool prematuredExit = false;
                foreach (NetCommand command in commands)
                {
                    commandProcessedCount++;

                    //Debug.Log($"Receiving command: {command.messageType}");

                    try
                    {
                        switch (command.messageType)
                        {
                            case MessageType.ClientId:
                                MixerUtils.BuildClientId(command.data);
                                break;
                            case MessageType.Mesh:
                                MixerUtils.BuildMesh(command.data);
                                break;
                            case MessageType.Transform:
                                MixerUtils.BuildTransform(command.data);
                                break;
                            case MessageType.Empty:
                                MixerUtils.BuildEmpty(prefab, command.data);
                                break;
                            case MessageType.ObjectVisibility:
                                MixerUtils.BuildObjectVisibility(root, command.data);
                                break;
                            case MessageType.Material:
                                MixerUtils.BuildMaterial(command.data);
                                break;
                            case MessageType.AssignMaterial:
                                MixerUtils.BuildAssignMaterial(command.data);
                                break;
                            case MessageType.Camera:
                                MixerUtils.BuildCamera(prefab, command.data);
                                break;
                            case MessageType.Animation:
                                MixerUtils.BuildAnimation(command.data);
                                break;
                            case MessageType.AddKeyframe:
                                MixerUtils.BuildAddKeyframe(command.data);
                                break;
                            case MessageType.RemoveKeyframe:
                                MixerUtils.BuildRemoveKeyframe(command.data);
                                break;
                            case MessageType.MoveKeyframe:
                                MixerUtils.BuildMoveKeyframe(command.data);
                                break;
                            case MessageType.ClearAnimations:
                                MixerUtils.BuildClearAnimations(command.data);
                                break;
                            case MessageType.Light:
                                MixerUtils.BuildLight(prefab, command.data);
                                break;
                            case MessageType.Sky:
                                MixerUtils.BuildSky(command.data);
                                break;
                            case MessageType.AddConstraint:
                                MixerUtils.ReceiveAddConstraint(command.data);
                                break;
                            case MessageType.RemoveConstraint:
                                MixerUtils.ReceiveRemoveConstraint(command.data);
                                break;
                            case MessageType.Delete:
                                MixerUtils.Delete(prefab, command.data);
                                break;
                            case MessageType.Rename:
                                MixerUtils.Rename(prefab, command.data);
                                break;
                            case MessageType.Duplicate:
                                MixerUtils.Duplicate(prefab, command.data);
                                break;
                            case MessageType.SendToTrash:
                                MixerUtils.BuildSendToTrash(root, command.data);
                                break;
                            case MessageType.RestoreFromTrash:
                                MixerUtils.BuildRestoreFromTrash(root, command.data);
                                break;
                            case MessageType.Texture:
                                MixerUtils.BuildTexture(command.data);
                                break;
                            case MessageType.Collection:
                                MixerUtils.BuildCollection(command.data);
                                break;
                            case MessageType.CollectionRemoved:
                                MixerUtils.BuildCollectionRemoved(command.data);
                                break;
                            case MessageType.AddCollectionToCollection:
                                MixerUtils.BuildAddCollectionToCollection(prefab, command.data);
                                break;
                            case MessageType.RemoveCollectionFromCollection:
                                MixerUtils.BuildRemoveCollectionFromCollection(prefab, command.data);
                                break;
                            case MessageType.AddObjectToCollection:
                                MixerUtils.BuildAddObjectToCollection(prefab, command.data);
                                break;
                            case MessageType.RemoveObjectFromCollection:
                                MixerUtils.BuildRemoveObjectFromCollection(prefab, command.data);
                                break;
                            case MessageType.CollectionInstance:
                                MixerUtils.BuildCollectionInstance(command.data);
                                break;
                            case MessageType.AddObjectToDocument:
                                MixerUtils.BuildAddObjectToDocument(root, command.data);
                                break;
                            case MessageType.AddCollectionToScene:
                                MixerUtils.BuilAddCollectionToScene(command.data);
                                break;
                            case MessageType.SetScene:
                                MixerUtils.BuilSetScene(command.data);
                                break;
                            case MessageType.GreasePencilMaterial:
                                MixerUtils.BuildGreasePencilMaterial(command.data);
                                break;
                            case MessageType.GreasePencilMesh:
                                MixerUtils.BuildGreasePencilMesh(command.data);
                                break;
                            case MessageType.GreasePencilConnection:
                                MixerUtils.BuildGreasePencilConnection(command.data);
                                break;
                            case MessageType.GreasePencilTimeOffset:
                                MixerUtils.BuildGreasePencilTimeOffset(command.data);
                                break;
                            case MessageType.FrameStartEnd:
                                MixerUtils.BuildFrameStartEnd(command.data);
                                break;
                            case MessageType.CurrentCamera:
                                MixerUtils.BuildCurrentCamera(command.data);
                                break;
                            case MessageType.ShotManagerContent:
                                MixerUtils.BuildShotManager(command.data);
                                break;
                            case MessageType.ShotManagerAction:
                                MixerUtils.BuildShotManagerAction(command.data);
                                break;
                            case MessageType.BlenderBank:
                                MixerUtils.ReceiveBlenderBank(command.data);
                                break;

                            case MessageType.ClientUpdate:
                                MixerUtils.BuildClientAttribute(command.data);
                                break;
                            case MessageType.ListAllClients:
                                MixerUtils.BuildListAllClients(command.data);
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
                case MessageType.FrameStartEnd:
                    SendFrameStartEnd(data as FrameStartEnd); break;
                case MessageType.AddKeyframe:
                    SendAddKeyframe(data as SetKeyInfo); break;
                case MessageType.RemoveKeyframe:
                    SendRemoveKeyframe(data as SetKeyInfo); break;
                case MessageType.MoveKeyframe:
                    SendMoveKeyframe(data as MoveKeyInfo); break;
                case MessageType.ClearAnimations:
                    SendClearAnimations(data as ClearAnimationInfo); break;
                case MessageType.ShotManagerAction:
                    SendShotManagerAction(data as ShotManagerActionInfo); break;
                case MessageType.BlenderBank:
                    SendBlenderBank(data as BlenderBankInfo); break;
            }
        }
    }
}
