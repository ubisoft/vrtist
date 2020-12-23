using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public abstract class MixerInterface
    {
        public abstract void GetNetworkData(ref string hostname, ref string room, ref int port, ref string master, ref string userName, ref Color userColor);
        public abstract void SetNetworkData(string room, string master, string userName, Color userColor);
        public abstract string CreateClientNameAndColor();
        public abstract void SetClientId(string clientId);
        public abstract string GetMasterId();
        public abstract void SetMasterId(string id);
        public abstract void UpdateClient(string json);
        public abstract void ListAllClients(string json);
        public abstract void OnInstanceAdded(GameObject obj);
        public abstract void OnInstanceRemoved(GameObject obj);
        public abstract void OnObjectRenamed(GameObject obj);
        public abstract void UpdateTag(GameObject obj);
        public abstract void SetLightEnabled(GameObject obj, bool enable);
        public abstract Texture2D LoadTexture(string filePath, ImageData imageData, bool isLinear);
        public abstract bool IsObjectInUse(GameObject obj);
        public abstract void GetCameraInfo(GameObject obj, out float focal, out float near, out float far, out bool dofEnabled, out float aperture, out Transform colimator);
        public abstract void SetCameraInfo(GameObject obj, float focal, float near, float far, bool dofEnabled, float aperture, string colimatorName, Camera.GateFitMode gateFit, Vector2 sensorSize);
        public abstract void SetActiveCamera(GameObject cameraObject);
        public abstract void GetLightInfo(GameObject obj, out LightType lightType, out bool castShadows, out float power, out Color color, out float range, out float innerAngle, out float outerAngle);
        public abstract void SetLightInfo(GameObject obj, LightType lightType, bool castShadows, float power, Color color, float range, float innerAngle, float outerAngle);
        public abstract void CreateAnimationKey(string objectName, string channel, int channelIndex, int frame, float value, int interpolation);
        public abstract void RemoveAnimationKey(string objectName, string channel, int channelIndex, int frame);
        public abstract void MoveAnimationKey(string objectName, string channel, int channelIndex, int frame, int newFrame);
        public abstract void CreateAnimationCurve(string objectName, string channel, int channelIndex, int[] frames, float[] values, int[] interpolations);
        public abstract void ClearAnimations(GameObject obj);
        public abstract void SetSkyColors(Color topColor, Color middleColor, Color bottomColor);
        public abstract string CreateJsonPlayerInfo(ConnectedUser playerInfo);
        public abstract void SetPlaying(bool playing);
        public abstract void SetCurrentFrame(int frame);
        public abstract void SetFrameRange(int start, int end);
        public abstract void CreateStroke(float[] points, int numPoints, int lineWidth, Vector3 offset, ref GPStroke subMesh);
        public abstract void CreateFill(float[] points, int numPoints, Vector3 offset, ref GPStroke subMesh);
        public abstract void BuildGreasePencilConnection(GameObject gobject, GreasePencilData gpdata);
        public abstract void UpdateShotManager(List<Shot> shots);
        public abstract void ShotManagerInsertShot(Shot shot, int shotIndex);
        public abstract void ShotManagerDeleteShot(int shotIndex);
        public abstract void ShotManagerDuplicateShot(int shotIndex);
        public abstract void ShotManagerMoveShot(int shotIndex, int offset);
        public abstract void ShotManagerUpdateShot(int shotIndex, int start, int end, string cameraName, Color color, int enabled);
    }
}
