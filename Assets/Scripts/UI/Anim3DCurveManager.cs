using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{

    public class Anim3DCurveManager : MonoBehaviour
    {
        private bool displaySelectedCurves = true;
        private Dictionary<GameObject, GameObject> curves = new Dictionary<GameObject, GameObject>();
        public Transform curvesParent;
        public GameObject curvePrefab;

        // Start is called before the first frame update
        void Start()
        {
            Selection.OnSelectionChanged += OnSelectionChanged;
            //GlobalState.Animation.AddAnimationListener(OnAnimationChanged);
        }

        // Update is called once per frame
        void Update()
        {
            if (displaySelectedCurves != GlobalState.Settings.display3DCurves)
            {
                displaySelectedCurves = GlobalState.Settings.display3DCurves;
                if (displaySelectedCurves)
                    UpdateFromSelection();
                else
                    ClearCurves();
            }
        }

        void OnSelectionChanged(object sender, SelectionChangedArgs args)
        {
            if (GlobalState.Settings.display3DCurves)
                UpdateFromSelection();
        }

        void UpdateFromSelection()
        {
            ClearCurves();
            foreach (GameObject gObject in Selection.selection.Values)
            {
                AddCurve(gObject);
            }
        }

        void OnAnimationChanged(GameObject gObject, AnimationChannel channel)
        {
            if (!Selection.IsSelected(gObject))
                return;

            if (null == channel || (channel.name == "location" && channel.index == 2))
                UpdateCurve(gObject);
        }

        void ClearCurves()
        {
            foreach (GameObject curve in curves.Values)
                Destroy(curve);
            curves.Clear();
        }

        void UpdateCurve(GameObject gObject)
        {
            if (curves.ContainsKey(gObject))
            {
                Destroy(curves[gObject]);
                curves.Remove(gObject);
            }

            AddCurve(gObject);
        }

        void AddCurve(GameObject gObject)
        {
            //Dictionary<Tuple<string, int>, AnimationChannel> animations = GlobalState.Instance.GetAnimationChannels(gObject);
            //if (null == animations)
            //    return;

            //GameObject curve = Instantiate(curvePrefab, curvesParent);
            //curves[gObject] = curve;

            //AnimationChannel channelX = null;
            //AnimationChannel channelY = null;
            //AnimationChannel channelZ = null;
            //animations.TryGetValue(new Tuple<string, int>("location", 0), out channelX);
            //animations.TryGetValue(new Tuple<string, int>("location", 1), out channelY);
            //animations.TryGetValue(new Tuple<string, int>("location", 2), out channelZ);
            //if (null == channelX || null == channelY || null == channelZ)
            //    return;
            //if (channelX.keys.Count != channelY.keys.Count)
            //    return;
            //if (channelZ.keys.Count != channelY.keys.Count)
            //    return;

            //LineRenderer line = curve.GetComponent<LineRenderer>();
            //int count = channelX.keys.Count;
            //line.positionCount = count;
            //for (int index = 0; index < count; index++)
            //{
            //    Vector3 position = new Vector3(channelX.keys[index].value, channelY.keys[index].value, channelZ.keys[index].value);
            //    line.SetPosition(index, position );
            //}
        }
    }

}