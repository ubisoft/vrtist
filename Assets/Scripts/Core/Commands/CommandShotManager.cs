namespace VRtist
{
    /// <summary>
    /// Command manage shot manager.
    /// </summary>
    public class CommandShotManager : ICommand
    {
        private readonly ShotManagerActionInfo oldData;
        private readonly ShotManagerActionInfo newData;
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
                        data.camera = newData.camera;
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
            SceneManager.ApplyShotManagegrAction(info);
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
            Redo();
        }
    }
}
