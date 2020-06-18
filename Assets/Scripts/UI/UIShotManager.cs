using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{ 
    public class UIShotManager : MonoBehaviour
    {
        public UIDynamicList shotList;
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
            foreach(Shot shot in sm.shots)
            {
                ShotItem shotItem = ShotItem.GenerateShotItem(shot);
                shotList.AddItem(shotItem.transform);
            }
        }
    }
}