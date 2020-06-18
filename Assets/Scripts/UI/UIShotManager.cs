using System.Collections;
using System.Collections.Generic;
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
        }
        public void OnCurrentShotChanged()
        {
            int itemIndex = 0;
            int currentShotIndex = ShotManager.Instance.CurrentShot;
            foreach(UIDynamicListItem item in shotList.GetItems())
            {
                ShotItem shotItem = item.Content.GetComponent<ShotItem>();
                shotItem.cameraButton.Checked = (itemIndex++ == currentShotIndex);
            }
        }
        void OnShotManagerChanged()
        {
            shotList.Clear();
            ShotManager sm = ShotManager.Instance;
            int activeShotCount = 0;
            foreach(Shot shot in sm.shots)
            {
                ShotItem shotItem = ShotItem.GenerateShotItem(shot);
                shotList.AddItem(shotItem.transform);

                if (shot.enabled)
                    activeShotCount++;
            }
            activeShotCountLabel.Text = activeShotCount.ToString() + "/" + sm.shots.Count.ToString();
        }
    }
}