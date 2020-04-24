using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class FrameInfo
    {
        public int frame;
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

        public void OnPlayOrPause(bool play)
        {
            if (play)
            {
                OnPlay();
            }
            else
            {
                OnPause();
            }
        }

        public void OnNextKey()
        {
            int keyTime = dopesheet.GetNextKeyFrame();
            FrameInfo info = new FrameInfo() { frame = keyTime };
            NetworkClient.GetInstance().SendEvent<FrameInfo>(MessageType.Frame, info);
        }

        public void OnPrevKey()
        {
            int keyTime = dopesheet.GetPreviousKeyFrame();
            FrameInfo info = new FrameInfo() { frame = keyTime };
            NetworkClient.GetInstance().SendEvent<FrameInfo>(MessageType.Frame, info);
        }

        public void OnNextFrame()
        {
            int keyTime = Mathf.Min(dopesheet.LastFrame, dopesheet.CurrentFrame + 1);
            FrameInfo info = new FrameInfo() { frame = keyTime };
            NetworkClient.GetInstance().SendEvent<FrameInfo>(MessageType.Frame, info);
        }

        public void OnPrevFrame()
        {
            int keyTime = Mathf.Max(dopesheet.FirstFrame, dopesheet.CurrentFrame - 1);
            FrameInfo info = new FrameInfo() { frame = keyTime };
            NetworkClient.GetInstance().SendEvent<FrameInfo>(MessageType.Frame, info);
        }

        public void OnFirstFrame()
        {
            int keyTime = dopesheet.FirstFrame;
            FrameInfo info = new FrameInfo() { frame = keyTime };
            NetworkClient.GetInstance().SendEvent<FrameInfo>(MessageType.Frame, info);
        }

        public void OnLastFrame()
        {
            int keyTime = dopesheet.LastFrame;
            FrameInfo info = new FrameInfo() { frame = keyTime };
            NetworkClient.GetInstance().SendEvent<FrameInfo>(MessageType.Frame, info);
        }

        public void OnBeginRecord()
        {

        }

        public void OnStopRecord()
        {

        }

        public void OnRecordOrStop(bool record)
        {
            if (record)
            {
                OnBeginRecord();
            }
            else
            {
                OnStopRecord();
            }
        }
    }
}
