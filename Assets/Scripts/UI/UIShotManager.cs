using UnityEngine;

namespace VRtist
{
    public class UIShotManager : MonoBehaviour
    {
        public UIDynamicList shotList;
        public UILabel activeShotCountLabel;

        void Start()
        {
            ShotManager.Instance.ShotsChangedEvent.AddListener(OnShotManagerChanged);
            ShotManager.Instance.CurrentShotChangedEvent.AddListener(OnCurrentShotChanged);
            shotList.ItemClickedEvent += OnListItemClicked;
        }

        // Update UI: set current shot
        void OnCurrentShotChanged()
        {
            int itemIndex = 0;
            int currentShotIndex = ShotManager.Instance.CurrentShot;
            foreach(UIDynamicListItem item in shotList.GetItems())
            {
                ShotItem shotItem = item.Content.GetComponent<ShotItem>();
                shotItem.cameraButton.Checked = (itemIndex++ == currentShotIndex);
            }
        }

        // Update UI: rebuild shots list
        void OnShotManagerChanged()
        {
            shotList.Clear();
            ShotManager sm = ShotManager.Instance;
            int activeShotCount = 0;
            foreach(Shot shot in sm.shots)
            {
                ShotItem shotItem = ShotItem.GenerateShotItem(shot);
                shotList.AddItem(shotItem.transform);

                if(shot.enabled)
                    activeShotCount++;
            }
            shotList.CurrentIndex = sm.CurrentShot;
            activeShotCountLabel.Text = activeShotCount.ToString() + "/" + sm.shots.Count.ToString();
        }

        void OnListItemClicked(object sender, IndexedGameObjectArgs args)
        {
            ShotManager.Instance.SetCurrentShot(args.index);
        }

        // Update data: add a shot
        public void OnAddShot()
        {
            ShotManager sm = ShotManager.Instance;

            // Take the current shot to find the start frame of the new shot
            // Keep same camera as the current shot if anyone
            int start = 0;
            GameObject camera = null;
            if(sm.CurrentShot != -1)
            {
                Shot selectedShot = sm.shots[sm.CurrentShot];
                start = selectedShot.end + 1;
                camera = selectedShot.camera;
            }
            int end = start + 50;  // arbitrary duration

            // Look at all the shots to find a name for the new shot
            string name = sm.GetUniqueShotName();

            Shot shot = new Shot { name = name, camera = camera, enabled = true, end = end, start = start };
            int shotIndex = sm.CurrentShot;

            // Send network message
            ShotManagerActionInfo info = new ShotManagerActionInfo
            {
                action = ShotManagerAction.AddShot,
                cameraName = null != camera ? camera.name : "",
                shotIndex = shotIndex,
                shotName = shot.name,
                shotStart = start,
                shotEnd = end,
                shotColor = Color.blue  // TODO: find a unique color
            };
            NetworkClient.GetInstance().SendShotManagerAction(info);

            // Add the shot to ShotManager singleton
            shotIndex++;
            sm.InsertShot(shotIndex, shot);
            sm.SetCurrentShot(shotIndex);

            // Rebuild UI
            OnShotManagerChanged();
        }

        // Update data: delete shot
        public void OnDeleteShot()
        {
            ShotManager sm = ShotManager.Instance;

            // Delete the current shot
            int shotIndex = sm.CurrentShot;
            sm.RemoveShot(shotIndex);

            // Send network message
            ShotManagerActionInfo info = new ShotManagerActionInfo
            {
                action = ShotManagerAction.DeleteShot,
                shotIndex = shotIndex
            };
            NetworkClient.GetInstance().SendShotManagerAction(info);

            // Rebuild UI
            OnShotManagerChanged();
        }

        // Update data: move shot
        public void OnMoveShot(int offset)
        {
            ShotManager sm = ShotManager.Instance;


        }
    }
}
