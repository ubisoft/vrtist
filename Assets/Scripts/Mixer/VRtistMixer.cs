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

#define VRTIST

using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

namespace VRtist.Mixer
{
    public static class VRtistMixer
    {
        public static void GetNetworkData(ref string hostname, ref string room, ref int port, ref string master, ref string userName, ref Color userColor)
        {
            hostname = GlobalState.Instance.networkSettings.host;
            room = GlobalState.Instance.networkSettings.room;
            port = GlobalState.Instance.networkSettings.port;
            master = GlobalState.Instance.networkSettings.master;
            userName = GlobalState.Instance.networkSettings.userName;
            userColor = GlobalState.Instance.networkSettings.userColor;
        }

        public static void SetNetworkData(string room, string master, string userName, Color userColor)
        {
            GlobalState.networkUser = new MixerUser
            {
                room = room,
                masterId = master,
                name = userName,
                color = userColor
            };
        }

        public static string CreateClientNameAndColor()
        {
            return JsonHelper.CreateJsonClientNameAndColor(GlobalState.networkUser.name, GlobalState.networkUser.color);
        }

        public static void SetClientId(string clientId)
        {
            GlobalState.SetClientId(clientId);
        }

        public static string GetMasterId()
        {
            MixerUser user = (MixerUser)GlobalState.networkUser;
            if (null == user) { return ""; }
            return user.masterId;
        }

        public static void SetMasterId(string id)
        {
            MixerUser user = (MixerUser)GlobalState.networkUser;
            if (null == user) { return; }
            user.masterId = id;
        }

        public static void OnInstanceAdded(GameObject obj)
        {
            GlobalState.FireObjectAdded(obj);
        }

        public static void OnInstanceRemoved(GameObject obj)
        {
            Selection.RemoveFromSelection(obj);
            GlobalState.FireObjectRemoved(obj);
        }

        public static void OnObjectRenamed(GameObject obj)
        {
            GlobalState.FireObjectRenamed(obj);
        }

        public static void UpdateTag(GameObject obj)
        {
            obj.tag = "PhysicObject";
        }

        public static void SetLightEnabled(GameObject obj, bool enable)
        {
            LightController lightController = obj.GetComponent<LightController>();
            if (lightController)
            {
                lightController.SetLightEnable(enable);
            }
        }

        public static Texture2D LoadTexture(string filePath, ImageData imageData, bool isLinear)
        {
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
        }

        public static bool IsObjectInUse(GameObject obj)
        {
            bool recording = GlobalState.Animation.animationState == AnimationState.Recording;
            bool gripped = GlobalState.Instance.selectionGripped;
            return ((recording || gripped) && Selection.IsSelected(obj));
        }

        public static void GetCameraInfo(GameObject obj, out float focal, out float near, out float far, out bool dofEnabled, out float aperture, out Transform colimatorr)
        {
            CameraController cameraController = obj.GetComponent<CameraController>();
            focal = cameraController.focal;
            near = cameraController.near;
            far = cameraController.far;
            aperture = cameraController.aperture;
            colimatorr = cameraController.colimator;
            dofEnabled = cameraController.enableDOF;
        }

        public static void SetCameraInfo(GameObject obj, float focal, float near, float far, bool dofEnabled, float aperture, string colimatorName, Camera.GateFitMode gateFit, Vector2 sensorSize)
        {
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
        }

        public static void SetActiveCamera(GameObject cameraObject)
        {
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
        }

        public static void GetLightInfo(GameObject obj, out LightType lightType, out bool castShadows, out float power, out Color color, out float range, out float innerAngle, out float outerAngle)
        {
            LightController lightController = obj.GetComponentInChildren<LightController>();
            lightType = lightController.Type;
            castShadows = lightController.CastShadows;
            power = lightController.GetPower();
            color = lightController.Color;
            range = lightController.Range;
            innerAngle = lightController.InnerAngle;
            outerAngle = lightController.OuterAngle;
        }

        public static void SetLightInfo(GameObject obj, LightType lightType, bool castShadows, float power, Color color, float range, float innerAngle, float outerAngle)
        {
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
        }

        private static AnimatableProperty BlenderToVRtistAnimationProperty(string channelName, int channelIndex)
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
        public static void CreateAnimationKey(string objectName, string channel, int channelIndex, int frame, float value, int interpolation)
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

        public static void RemoveAnimationKey(string objectName, string channel, int channelIndex, int frame)
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

        public static void MoveAnimationKey(string objectName, string channel, int channelIndex, int frame, int newFrame)
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

        public static void ClearAnimations(GameObject obj)
        {
            GlobalState.Animation.ClearAnimations(obj);
        }

        public static void CreateAnimationCurve(string objectName, string channel, int channelIndex, int[] frames, float[] values, int[] interpolations)
        {
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
        }

        public static void SetSkyColors(Color topColor, Color middleColor, Color bottomColor)
        {
            SkySettings skySettings = new SkySettings
            {
                topColor = topColor,
                middleColor = middleColor,
                bottomColor = bottomColor
            };
            GlobalState.Instance.SkySettings = skySettings;
        }

        public static string CreateJsonPlayerInfo(MixerUser playerInfo)
        {
            return JsonHelper.CreateJsonPlayerInfo(playerInfo);
        }

        public static void SetFrameRange(int start, int end)
        {
            GlobalState.Animation.StartFrame = start;
            GlobalState.Animation.EndFrame = end;
        }

        public static void CreateStroke(float[] points, int numPoints, int lineWidth, Vector3 offset, ref GPStroke subMesh)
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

        public static void CreateFill(float[] points, int numPoints, Vector3 offset, ref GPStroke subMesh)
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

            Triangulator.Triangulator.Triangulate(p, Triangulator.WindingOrder.CounterClockwise, out Vector2[] outputVertices, out int[] indices);

            Vector3[] positions = new Vector3[outputVertices.Length];
            for (int i = 0; i < outputVertices.Length; i++)
            {
                positions[i] = mat.MultiplyPoint(new Vector3(outputVertices[i].x, outputVertices[i].y)) + offset;
            }

            subMesh.vertices = positions;
            subMesh.triangles = indices;
        }

        public static void BuildGreasePencilConnection(GameObject gobject, GreasePencilData gpdata)
        {
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
        }

        public static void UpdateShotManager(List<Shot> shots)
        {
            ShotManager.Instance.Clear();
            foreach (Shot shot in shots)
            {
                ShotManager.Instance.AddShot(shot);
            }
            ShotManager.Instance.FireChanged();
        }

        public static void ShotManagerInsertShot(Shot shot, int shotIndex)
        {
            ShotManager.Instance.InsertShot(shotIndex, shot);
            ShotManager.Instance.FireChanged();
        }

        public static void ShotManagerDeleteShot(int shotIndex)
        {
            ShotManager.Instance.RemoveShot(shotIndex);
            ShotManager.Instance.FireChanged();
        }

        public static void ShotManagerDuplicateShot(int shotIndex)
        {
            ShotManager.Instance.DuplicateShot(shotIndex);
            ShotManager.Instance.FireChanged();
        }

        public static void ShotManagerMoveShot(int shotIndex, int offset)
        {
            ShotManager.Instance.MoveShot(shotIndex, offset);
            ShotManager.Instance.FireChanged();
        }

        public static void ShotManagerUpdateShot(int shotIndex, int start, int end, string cameraName, Color color, int enabled)
        {
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
        }

        public static void UpdateClient(string json)
        {
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
                        MixerUser newUser = new MixerUser
                        {
                            id = client.id.value
                        };
                        GlobalState.AddConnectedUser(newUser);
                    }
                }

                // Get client connected to our room
                if (!GlobalState.HasConnectedUser(client.id.value)) { return; }
                MixerUser user = (MixerUser)GlobalState.GetConnectedUser(client.id.value);

                // Retrieve the viewId (one of possible - required to send data)
                MixerUser player = (MixerUser)GlobalState.networkUser;
                if (client.viewId.IsValid && null == player.viewId)
                {
                    player.viewId = client.viewId.value;
                }

                if (client.userName.IsValid) { user.name = client.userName.value; }
                if (client.userColor.IsValid) { user.color = client.userColor.value; }

                bool changed = false;

                // Get its eye position
                if (client.eye.IsValid)
                {
                    user.position = client.eye.value;
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

        public static void ListAllClients(string json)
        {
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
                    MixerUser player = (MixerUser)GlobalState.networkUser;
                    if (client.viewId.IsValid && null == player.viewId)
                    {
                        player.viewId = client.viewId.value;
                    }

                    // Add client to the list of connected users in our room
                    if (!GlobalState.HasConnectedUser(client.id.value))
                    {
                        MixerUser newUser = new MixerUser
                        {
                            id = client.id.value
                        };
                        if (client.userName.IsValid) { newUser.name = client.userName.value; }
                        if (client.userColor.IsValid) { newUser.color = client.userColor.value; }
                        if (client.eye.IsValid) { newUser.position = client.eye.value; }
                        if (client.target.IsValid) { newUser.target = client.target.value; }
                        GlobalState.AddConnectedUser(newUser);
                    }
                }
            }
        }
    }
}
