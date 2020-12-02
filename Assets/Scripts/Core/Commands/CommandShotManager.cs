using UnityEngine;

namespace VRtist
{
    public class CommandShotManager : ICommand
    {
        private ShotManagerActionInfo oldData;
        private ShotManagerActionInfo newData;
        public CommandShotManager(ShotManagerActionInfo info)
        {
            newData = info;
            oldData = BuildInvertData();
        }

        public CommandShotManager(ShotManagerActionInfo oldInfo, ShotManagerActionInfo newInfo)
        {
            oldData = oldInfo;
            newData = newInfo;
        }

        private ShotManagerActionInfo BuildInvertData()
        {
            ShotManagerActionInfo data = new ShotManagerActionInfo();
            switch (newData.action)
            {
                case ShotManagerAction.AddShot:
                    {
                        data.action = ShotManagerAction.DeleteShot;
                        data.shotIndex = newData.shotIndex + 1;
                    }
                    break;
                case ShotManagerAction.DeleteShot:
                    {
                        data.action = ShotManagerAction.AddShot;
                        data.shotIndex = newData.shotIndex - 1;
                        data.shotName = newData.shotName;
                        data.shotStart = newData.shotStart;
                        data.shotEnd = newData.shotEnd;
                        data.shotColor = newData.shotColor;
                        data.cameraName = newData.cameraName;
                        data.shotEnabled = newData.shotEnabled;
                    }
                    break;
                case ShotManagerAction.DuplicateShot:
                    {
                        data.action = ShotManagerAction.DeleteShot;
                        data.shotIndex = newData.shotIndex;
                    }
                    break;
                case ShotManagerAction.MoveShot:
                    {
                        data.action = ShotManagerAction.MoveShot;
                        data.shotIndex = newData.shotIndex + newData.moveOffset;
                        data.moveOffset = -newData.moveOffset;
                    }
                    break;
                case ShotManagerAction.UpdateShot:
                    {
                    }
                    break;
            }
            return data;
        }

        private void Apply(ShotManagerActionInfo info)
        {
            switch (info.action)
            {
                case ShotManagerAction.AddShot:
                    {
                        GameObject cam = null;
                        if (info.cameraName.Length > 0)
                            cam = SyncData.nodes[info.cameraName].instances[0].Item1;
                        Shot shot = new Shot() { name = info.shotName, camera = cam, color = info.shotColor, start = info.shotStart, end = info.shotEnd, enabled = info.shotEnabled == 1 ? true : false };
                        ShotManager.Instance.InsertShot(info.shotIndex + 1, shot);
                    }
                    break;
                case ShotManagerAction.DeleteShot:
                    {
                        ShotManager.Instance.RemoveShot(info.shotIndex);
                    }
                    break;
                case ShotManagerAction.DuplicateShot:
                    {
                        ShotManager.Instance.DuplicateShot(info.shotIndex);
                    }
                    break;
                case ShotManagerAction.MoveShot:
                    {
                        ShotManager.Instance.SetCurrentShotIndex(info.shotIndex);
                        ShotManager.Instance.MoveShot(info.shotIndex, info.moveOffset);
                    }
                    break;
                case ShotManagerAction.UpdateShot:
                    {
                        GameObject cam = null;
                        if (info.cameraName.Length > 0)
                            cam = SyncData.nodes[info.cameraName].instances[0].Item1;

                        Shot shot = ShotManager.Instance.shots[info.shotIndex];
                        if (info.shotName.Length > 0)
                            shot.name = info.shotName;
                        if (info.cameraName.Length > 0)
                            shot.camera = SyncData.nodes[info.cameraName].instances[0].Item1;
                        if (info.shotColor.r != -1)
                            shot.color = info.shotColor;
                        if (info.shotStart != -1)
                            shot.start = info.shotStart;
                        if (info.shotEnd != -1)
                            shot.end = info.shotEnd;
                        if (info.shotEnabled != -1)
                            shot.enabled = info.shotEnabled == 1 ? true : false;
                    }
                    break;
            }
            ShotManager.Instance.FireChanged();
            MixerClient.GetInstance().SendEvent<ShotManagerActionInfo>(MessageType.ShotManagerAction, info);
        }

        public override void Undo()
        {
            Apply(oldData);
        }

        public override void Redo()
        {
            Apply(newData);
        }

        public override void Submit()
        {
            CommandManager.AddCommand(this);
        }
    }
}
