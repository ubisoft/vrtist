﻿using System.Collections;
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
            GlobalState.Instance.AddAnimationListener(OnAnimationChanged);
        }

        // Update is called once per frame
        void Update()
        {

        }
        void OnSelectionChanged(object sender, SelectionChangedArgs args)
        {
            ClearCurves();
            foreach (GameObject gObject in Selection.selection.Values)
            {
                AddCurve(gObject);
            }
        }

        void OnAnimationChanged(GameObject gObject)
        {
            if(Selection.IsSelected(gObject))
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
            Dictionary<string, AnimationChannel> animations = GlobalState.Instance.GetAnimationChannels(gObject);
            if (null == animations)
                return;

            GameObject curve = Instantiate(curvePrefab, curvesParent);
            curves[gObject] = curve;

            AnimationChannel channelX = null;
            AnimationChannel channelY = null;
            AnimationChannel channelZ = null;
            animations.TryGetValue("location[0]", out channelX);
            animations.TryGetValue("location[1]", out channelY);
            animations.TryGetValue("location[2]", out channelZ);
            if (null == channelX || null == channelY || null == channelZ)
                return;
            if (channelX.keys.Count != channelY.keys.Count)
                return;
            if (channelZ.keys.Count != channelY.keys.Count)
                return;

            LineRenderer line = curve.GetComponent<LineRenderer>();
            int count = channelX.keys.Count;
            line.positionCount = count;
            for (int index = 0; index < count; index++)
            {
                Vector3 position = new Vector3(channelX.keys[index].value, channelY.keys[index].value, channelZ.keys[index].value);
                line.SetPosition(index, position );
            }
        }
    }

}