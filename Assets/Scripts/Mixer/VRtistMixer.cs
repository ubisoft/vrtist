#define VRTIST

using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

namespace VRtist
{
    public class VRtistMixerImpl : MixerInterface
    {
        public override void GetNetworkData(ref string hostname, ref string room, ref int port, ref string master, ref string userName, ref Color userColor)
        {
#if !VRTIST
            Debug.Log("GetNetworkData");
#else
            hostname = GlobalState.Instance.networkSettings.host;
            room = GlobalState.Instance.networkSettings.room;
            port = GlobalState.Instance.networkSettings.port;
            master = GlobalState.Instance.networkSettings.master;
            userName = GlobalState.Instance.networkSettings.userName;
            userColor = GlobalState.Instance.networkSettings.userColor;
#endif
        }

        public override void SetNetworkData(string room, string master, string userName, Color userColor)
        {
#if !VRTIST
            Debug.Log("SetNetworkData");
#else
            GlobalState.networkUser.room = room;
            GlobalState.networkUser.masterId = master;
            GlobalState.networkUser.name = userName;
            GlobalState.networkUser.color = userColor;
#endif
        }

        public override string CreateClientNameAndColor()
        {
#if !VRTIST
            Debug.Log("SetNetworkData");
            return null;
#else
            return JsonHelper.CreateJsonClientNameAndColor(GlobalState.networkUser.name, GlobalState.networkUser.color);
#endif
        }

        public override void SetClientId(string clientId)
        {
#if !VRTIST
            Debug.Log("SetClientId " + clientId);
#else
            GlobalState.SetClientId(clientId);
#endif
        }
        public override string GetMasterId()
        {
#if !VRTIST
            Debug.Log("GetMasterId ");
            return "";
#else
            return GlobalState.networkUser.masterId;
#endif
        }
        public override void SetMasterId(string id)
        {
#if !VRTIST
            Debug.Log("SetMasterId " + id);
#else
            GlobalState.networkUser.masterId = id;
#endif
        }

        public override void OnInstanceAdded(GameObject obj)
        {
#if !VRTIST
            Debug.Log("OnInstanceAdded " + obj.name);
#else
            GlobalState.FireObjectAdded(obj);
#endif
        }
        public override void OnInstanceRemoved(GameObject obj)
        {
#if !VRTIST
            Debug.Log("OnInstanceRemoved " + obj.name);
#else
            Selection.RemoveFromSelection(obj);
            GlobalState.FireObjectRemoved(obj);
#endif
        }

        public override void OnObjectRenamed(GameObject obj)
        {
#if !VRTIST
            Debug.Log("OnObjectRenamed " + obj.name);
#else
            GlobalState.FireObjectRenamed(obj);
#endif
        }

        public override void UpdateTag(GameObject obj)
        {
#if VRTIST
            obj.tag = "PhysicObject";
#endif
        }

        public override void SetLightEnabled(GameObject obj, bool enable)
        {
#if !VRTIST
            Debug.Log("SetLightEnabled " + obj.name + " " + enable.ToString());
#else
            LightController lightController = obj.GetComponent<LightController>();
            if (lightController)
            {
                lightController.SetLightEnable(enable);
            }
#endif
        }

        public override Texture2D LoadTexture(string filePath, ImageData imageData, bool isLinear)
        {
#if !VRTIST
            Debug.Log("LoadTexture " + filePath + " " + isLinear.ToString());
            return new Texture2D(0, 0);
#else
            if (!imageData.isEmbedded)
            {
                string directory = Path.GetDirectoryName(filePath);
                string withoutExtension = Path.GetFileNameWithoutExtension(filePath);
                string ddsFile = directory + "/" + withoutExtension + ".dds";

                if (File.Exists(ddsFile))
                {
                    Texture2D t = TextureUtils.LoadTextureDXT(ddsFile, isLinear);
                    if (null != t)
                    {
                        MixerUtils.textures[filePath] = t;
                        MixerUtils.texturesFlipY.Add(filePath);
                        return t;
                    }
                }

                if (File.Exists(filePath))
                {
                    Texture2D t = TextureUtils.LoadTextureOIIO(filePath, isLinear);
                    if (null != t)
                    {
                        MixerUtils.textures[filePath] = t;
                        MixerUtils.texturesFlipY.Add(filePath);
                        return t;
                    }
                }
            }

            Texture2D texture = TextureUtils.LoadTextureFromBuffer(imageData.buffer, isLinear);
            if (null != texture)
                MixerUtils.textures[filePath] = texture;

            return texture;
#endif
        }

        public override bool IsObjectInUse(GameObject obj)
        {
#if !VRTIST
            Debug.Log("IsObjectInUse " + obj.name);
            return false;
#else
            bool recording = GlobalState.Animation.animationState == AnimationState.Recording;
            bool gripped = GlobalState.Instance.selectionGripped;
            return ((recording || gripped) && Selection.IsSelected(obj));
#endif
        }

        public override void GetCameraInfo(GameObject obj, out float focal, out float near, out float far, out bool dofEnabled, out float aperture, out Transform colimatorr)
        {
#if !VRTIST
            Debug.Log("GetCameraInfo " + obj.name);
            focal = 0; near = 0; far = 0;
#else
            CameraController cameraController = obj.GetComponent<CameraController>();
            focal = cameraController.focal;
            near = cameraController.near;
            far = cameraController.far;
            aperture = cameraController.aperture;
            colimatorr = cameraController.colimator;
            dofEnabled = cameraController.enableDOF;
#endif
        }

        public override void SetCameraInfo(GameObject obj, float focal, float near, float far, bool dofEnabled, float aperture, string colimatorName, Camera.GateFitMode gateFit, Vector2 sensorSize)
        {
#if !VRTIST
            Debug.Log("SetCameraInfo " + obj.name);
#else
            bool recording = GlobalState.Animation.animationState == AnimationState.Recording;
            if (recording && Selection.IsSelected(obj))
                return;
            CameraController cameraController = obj.GetComponent<CameraController>();
            cameraController.focal = focal;
            cameraController.near = near;
            cameraController.far = far;
            cameraController.aperture = aperture;
            cameraController.colimator = colimatorName == "" ? null : SyncData.nodes[colimatorName].prefab.transform;
            cameraController.enableDOF = dofEnabled;
            cameraController.filmHeight = sensorSize.y;

            Node cameraNode = SyncData.nodes[obj.name];
            foreach (var instanceItem in cameraNode.instances)
            {
                GameObject instance = instanceItem.Item1;
                CameraController instanceCameraController = instance.GetComponent<CameraController>();
                instanceCameraController.focal = focal;
                instanceCameraController.near = near;
                instanceCameraController.far = far;
                instanceCameraController.aperture = aperture;
                instanceCameraController.colimator = colimatorName == "" ? null : SyncData.nodes[colimatorName].instances[0].Item1.transform;
                instanceCameraController.enableDOF = dofEnabled;
                instanceCameraController.filmHeight = sensorSize.y;
            }
#endif
        }

        public override void SetActiveCamera(GameObject cameraObject)
        {
#if !VRTIST
            Debug.Log("SetActiveCamera");
#else
            if (null == cameraObject)
            {
                CameraManager.Instance.ActiveCamera = null;
            }
            else
            {
                // We only have one instance of any camera in the scene                
                CameraController controller = cameraObject.GetComponent<CameraController>();
                if (null != controller) { CameraManager.Instance.ActiveCamera = controller.gameObject; }
            }
#endif
        }

        public override void GetLightInfo(GameObject obj, out LightType lightType, out bool castShadows, out float power, out Color color, out float range, out float innerAngle, out float outerAngle)
        {
#if !VRTIST
            Debug.Log("GetLightInfo " + obj.name);
            lightType = LightType.Directional;
            castShadows = false;
            power = 0; 
            color = Color.black;
            range = 1f;
            innerAngle = 0;
            outerAngle = 0;
#else
            LightController lightController = obj.GetComponentInChildren<LightController>();
            lightType = lightController.Type;
            castShadows = lightController.CastShadows;
            power = lightController.GetPower();
            color = lightController.Color;
            range = lightController.Range;
            innerAngle = lightController.InnerAngle;
            outerAngle = lightController.OuterAngle;
#endif
        }

        public override void SetLightInfo(GameObject obj, LightType lightType, bool castShadows, float power, Color color, float range, float innerAngle, float outerAngle)
        {
#if !VRTIST
            Debug.Log("SetLightInfo " + obj.name);
#else
            bool recording = GlobalState.Animation.animationState == AnimationState.Recording;
            if (recording && Selection.IsSelected(obj))
                return;
            LightController controller = obj.GetComponent<LightController>();
            controller.Type = lightType;
            controller.SetPower(power);
            controller.Color = color;
            controller.CastShadows = castShadows;
            controller.Range = range;
            controller.OuterAngle = outerAngle;
            controller.InnerAngle = innerAngle;

            Node lightNode = SyncData.nodes[obj.name];
            foreach (var instanceItem in lightNode.instances)
            {
                GameObject instance = instanceItem.Item1;
                LightController instanceController = instance.GetComponent<LightController>();
                instanceController.Type = lightType;
                instanceController.SetPower(power);
                instanceController.Color = color;
                instanceController.CastShadows = castShadows;
                instanceController.Range = range;
                instanceController.OuterAngle = outerAngle;
                instanceController.InnerAngle = innerAngle;
            }

#endif
        }

        private AnimatableProperty BlenderToVRtistAnimationProperty(string channelName, int channelIndex)
        {
            AnimatableProperty property = AnimatableProperty.Unknown;

            switch (channelName + channelIndex)
            {
                case "location0": property = AnimatableProperty.PositionX; break;
                case "location1": property = AnimatableProperty.PositionY; break;
                case "location2": property = AnimatableProperty.PositionZ; break;

                case "rotation_euler0": property = AnimatableProperty.RotationX; break;
                case "rotation_euler1": property = AnimatableProperty.RotationY; break;
                case "rotation_euler2": property = AnimatableProperty.RotationZ; break;

                case "scale0": property = AnimatableProperty.ScaleX; break;
                case "scale1": property = AnimatableProperty.ScaleY; break;
                case "scale2": property = AnimatableProperty.ScaleZ; break;

                case "lens-1": property = AnimatableProperty.CameraFocal; break;

                case "energy-1": property = AnimatableProperty.LightIntensity; break;
                case "color0": property = AnimatableProperty.ColorR; break;
                case "color1": property = AnimatableProperty.ColorG; break;
                case "color2": property = AnimatableProperty.ColorB; break;
            }
            return property;
        }
        public override void CreateAnimationKey(string objectName, string channel, int channelIndex, int frame, float value, int interpolation)
        {
            AnimatableProperty property = BlenderToVRtistAnimationProperty(channel, channelIndex);
            if (property == AnimatableProperty.Unknown)
            {
                Debug.LogError("Unknown Animation Property " + objectName + " " + channel + " " + channelIndex);
                return;
            }

            Node node = SyncData.nodes[objectName];
            // Apply to instances
            foreach (Tuple<GameObject, string> t in node.instances)
            {
                GameObject gobj = t.Item1;
                AnimationSet animationSet = GlobalState.Animation.GetOrCreateObjectAnimation(gobj);
                Curve curve = animationSet.GetCurve(property);

                if (property == AnimatableProperty.RotationX || property == AnimatableProperty.RotationY || property == AnimatableProperty.RotationZ)
                {
                    value = Mathf.Rad2Deg * value;
                }

                curve.AddKey(new AnimationKey(frame, value, (Interpolation)interpolation));
            }
        }

        public override void RemoveAnimationKey(string objectName, string channel, int channelIndex, int frame)
        {
            AnimatableProperty property = BlenderToVRtistAnimationProperty(channel, channelIndex);
            if (property == AnimatableProperty.Unknown)
            {
                Debug.LogError("Unknown Animation Property " + objectName + " " + channel + " " + channelIndex);
                return;
            }

            Node node = SyncData.nodes[objectName];
            // Apply to instances
            foreach (Tuple<GameObject, string> t in node.instances)
            {
                GameObject gobj = t.Item1;
                AnimationSet animationSet = GlobalState.Animation.GetOrCreateObjectAnimation(gobj);
                Curve curve = animationSet.GetCurve(property);
                curve.RemoveKey(frame);
            }
        }

        public override void MoveAnimationKey(string objectName, string channel, int channelIndex, int frame, int newFrame)
        {
            AnimatableProperty property = BlenderToVRtistAnimationProperty(channel, channelIndex);
            if (property == AnimatableProperty.Unknown)
            {
                Debug.LogError("Unknown Animation Property " + objectName + " " + channel + " " + channelIndex);
                return;
            }

            Node node = SyncData.nodes[objectName];
            // Apply to instances
            foreach (Tuple<GameObject, string> t in node.instances)
            {
                GameObject gobj = t.Item1;
                AnimationSet animationSet = GlobalState.Animation.GetOrCreateObjectAnimation(gobj);
                Curve curve = animationSet.GetCurve(property);
                curve.MoveKey(frame, newFrame);
            }
        }

        public override void ClearAnimations(GameObject obj)
        {
#if VRTIST
            GlobalState.Animation.ClearAnimations(obj);
#endif
        }

        public override void CreateAnimationCurve(string objectName, string channel, int channelIndex, int[] frames, float[] values, int[] interpolations)
        {
#if !VRTIST
            Debug.Log("CreateAnimationCurve " + objectName + " Channel " + channel + " channel index " + channelIndex.ToString());
#else
            AnimatableProperty property = BlenderToVRtistAnimationProperty(channel, channelIndex);

            int keyCount = frames.Length;
            List<AnimationKey> keys = new List<AnimationKey>();
            for (int i = 0; i < keyCount; i++)
            {
                float value = values[i];
                if (property == AnimatableProperty.RotationX || property == AnimatableProperty.RotationY || property == AnimatableProperty.RotationZ)
                {
                    value = Mathf.Rad2Deg * value;
                }

                keys.Add(new AnimationKey(frames[i], value, (Interpolation)interpolations[i]));
            }


            Node node = SyncData.nodes[objectName];
            // Apply to instances
            foreach (Tuple<GameObject, string> t in node.instances)
            {
                GameObject gobj = t.Item1;
                AnimationSet animationSet = GlobalState.Animation.GetOrCreateObjectAnimation(gobj);
                animationSet.SetCurve(property, keys);
            }
#endif
        }

        public override void SetSkyColors(Color topColor, Color middleColor, Color bottomColor)
        {
#if !VRTIST
            Debug.Log("SetSkyColors");
#else
            SkySettings skySettings = new SkySettings
            {
                topColor = topColor,
                middleColor = middleColor,
                bottomColor = bottomColor
            };
            GlobalState.Instance.SkySettings = skySettings;
#endif
        }

        public override string CreateJsonPlayerInfo(ConnectedUser playerInfo)
        {
#if !VRTIST
            Debug.Log("CreateJsonPlayerIngo");
            return null;
#else
            return JsonHelper.CreateJsonPlayerInfo(playerInfo);
#endif
        }

        public override void SetPlaying(bool playing)
        {
#if !VRTIST
            Debug.Log("SetPlaying " + playing.ToString());
#else
            // Deprecated playing event
#endif
        }

        public override void SetCurrentFrame(int frame)
        {
#if !VRTIST
            Debug.Log("SetCurrentFrame " + frame.ToString());
#else
            // Deprecated event
#endif
        }

        public override void SetFrameRange(int start, int end)
        {
#if !VRTIST
            Debug.Log("SetFrameRange " + start.ToString() + " " + end.ToString());
#else
            GlobalState.Animation.StartFrame = start;
            GlobalState.Animation.EndFrame = end;
#endif
        }

        public override void CreateStroke(float[] points, int numPoints, int lineWidth, Vector3 offset, ref GPStroke subMesh)
        {
#if !VRTIST
            Debug.Log("CreateStroke");
#else
            FreeDraw freeDraw = new FreeDraw();
            for (int i = 0; i < numPoints; i++)
            {
                Vector3 position = new Vector3(points[i * 5 + 0] + offset.x, points[i * 5 + 1] + offset.y, points[i * 5 + 2] + offset.z);
                float ratio = lineWidth * 0.0006f * points[i * 5 + 3];  // pressure
                freeDraw.AddRawControlPoint(position, ratio);
            }
            subMesh.vertices = freeDraw.vertices;
            subMesh.triangles = freeDraw.triangles;
#endif
        }

        public override void CreateFill(float[] points, int numPoints, Vector3 offset, ref GPStroke subMesh)
        {
#if !VRTIST
            Debug.Log("CreateStroke");
#else
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

            Triangulator.Triangulator.Triangulate(p, Triangulator.WindingOrder.CounterClockwise, out Vector2[] outputVertices, out int[] indices);

            Vector3[] positions = new Vector3[outputVertices.Length];
            for (int i = 0; i < outputVertices.Length; i++)
            {
                positions[i] = mat.MultiplyPoint(new Vector3(outputVertices[i].x, outputVertices[i].y)) + offset;
            }

            subMesh.vertices = positions;
            subMesh.triangles = indices;
#endif
        }

        public override void BuildGreasePencilConnection(GameObject gobject, GreasePencilData gpdata)
        {
#if !VRTIST
            Debug.Log("BuildGreasePencilConnection");
#else
            GreasePencilBuilder greasePencilBuilder = gobject.GetComponent<GreasePencilBuilder>();
            if (null == greasePencilBuilder)
                greasePencilBuilder = gobject.AddComponent<GreasePencilBuilder>();

            GreasePencil greasePencil = gobject.GetComponent<GreasePencil>();
            if (null == greasePencil)
                greasePencil = gobject.AddComponent<GreasePencil>();

            greasePencil.data = gpdata;

            Node prefab = SyncData.nodes[gobject.name];
            foreach (var item in prefab.instances)
            {
                GreasePencil greasePencilInstance = item.Item1.GetComponent<GreasePencil>();
                greasePencilInstance.data = gpdata;
                greasePencilInstance.ForceUpdate();
            }

            gobject.tag = "PhysicObject";
#endif
        }

        public override void UpdateShotManager(List<Shot> shots)
        {
#if !VRTIST
            Debug.Log("UpdateShotManager");
#else
            ShotManager.Instance.Clear();
            foreach (Shot shot in shots)
            {
                ShotManager.Instance.AddShot(shot);
            }
            ShotManager.Instance.FireChanged();
#endif
        }
        public override void ShotManagerInsertShot(Shot shot, int shotIndex)
        {
#if !VRTIST
            Debug.Log("ShotManagerInsertShot");
#else
            ShotManager.Instance.InsertShot(shotIndex, shot);
            ShotManager.Instance.FireChanged();
#endif
        }
        public override void ShotManagerDeleteShot(int shotIndex)
        {
#if !VRTIST
            Debug.Log("ShotManagerDeleteShot");
#else
            ShotManager.Instance.RemoveShot(shotIndex);
            ShotManager.Instance.FireChanged();
#endif
        }
        public override void ShotManagerDuplicateShot(int shotIndex)
        {
#if !VRTIST
            Debug.Log("ShotManagerDuplicateShot");
#else
            ShotManager.Instance.DuplicateShot(shotIndex);
            ShotManager.Instance.FireChanged();
#endif
        }
        public override void ShotManagerMoveShot(int shotIndex, int offset)
        {
#if !VRTIST
            Debug.Log("ShotManagerDuplicateShot");
#else
            ShotManager.Instance.MoveShot(shotIndex, offset);
            ShotManager.Instance.FireChanged();
#endif
        }

        public override void ShotManagerUpdateShot(int shotIndex, int start, int end, string cameraName, Color color, int enabled)
        {
#if !VRTIST
            Debug.Log("ShotManagerUpdateShot");
#else
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
                shot.enabled = enabled == 1;
            }

            ShotManager.Instance.UpdateShot(shotIndex, shot);
            ShotManager.Instance.FireChanged();
#endif
        }

        public override void UpdateClient(string json)
        {
#if !VRTIST
            Debug.Log("UpdateClient");
#else
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
                        ConnectedUser newUser = new ConnectedUser
                        {
                            id = client.id.value
                        };
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
#endif
        }
        public override void ListAllClients(string json)
        {
#if !VRTIST
            Debug.Log("ListAllClients");
#else
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
                        ConnectedUser newUser = new ConnectedUser
                        {
                            id = client.id.value
                        };
                        if (client.userName.IsValid) { newUser.name = client.userName.value; }
                        if (client.userColor.IsValid) { newUser.color = client.userColor.value; }
                        if (client.eye.IsValid) { newUser.eye = client.eye.value; }
                        if (client.target.IsValid) { newUser.target = client.target.value; }
                        GlobalState.AddConnectedUser(newUser);
                    }
                }
            }
#endif

        }
    }
}
