using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class FrameInfo
    {
        public float frame;
    }

    public class RemotePlayer : MonoBehaviour
    {
        public Dopesheet dopesheet = null;

        public void OnPlay()
        {
            NetworkClient.GetInstance().SendEvent<int>(MessageType.Play, 0);
        }

        public void OnPause()
        {
            NetworkClient.GetInstance().SendEvent<int>(MessageType.Pause, 0);
        }

        public void OnNextKey()
        {
            float keyTime = dopesheet.GetNextKeyFrame();
            dopesheet.CurrentFrame = (int)keyTime; // TMP
            NetworkClient.GetInstance().SendEvent<float>(MessageType.Frame, keyTime);
        }

        public void OnPrevKey()
        {
            float keyTime = dopesheet.GetPreviousKeyFrame();
            dopesheet.CurrentFrame = (int)keyTime; // TMP
            NetworkClient.GetInstance().SendEvent<float>(MessageType.Frame, keyTime);
        }

        public void OnNextFrame()
        {
            float keyTime = (float)Mathf.Min(dopesheet.LastFrame, dopesheet.CurrentFrame + 1);
            FrameInfo info = new FrameInfo() { frame = keyTime };
            NetworkClient.GetInstance().SendEvent<FrameInfo>(MessageType.Frame, info);
        }

        public void OnPrevFrame()
        {
            float keyTime = (float)Mathf.Max(dopesheet.FirstFrame, dopesheet.CurrentFrame - 1);
            FrameInfo info = new FrameInfo() { frame = keyTime };
            NetworkClient.GetInstance().SendEvent<FrameInfo>(MessageType.Frame, info);
        }

        public void OnFirstFrame()
        {
            float keyTime = (float)dopesheet.FirstFrame;
            FrameInfo info = new FrameInfo() { frame = keyTime };
            NetworkClient.GetInstance().SendEvent<FrameInfo>(MessageType.Frame, info);
        }

        public void OnLastFrame()
        {
            float keyTime = (float)dopesheet.LastFrame;
            FrameInfo info = new FrameInfo() { frame = keyTime };
            NetworkClient.GetInstance().SendEvent<FrameInfo>(MessageType.Frame, info);
        }

        public void OnBeginRecord()
        {

        }

        public void OnStopRecord()
        {

        }
    }
}
