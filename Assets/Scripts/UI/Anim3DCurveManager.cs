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
            GlobalState.Animation.onAddAnimation.AddListener(OnAnimationAdded);
            GlobalState.Animation.onRemoveAnimation.AddListener(OnAnimationRemoved);
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

        void OnAnimationAdded(GameObject gObject)
        {
            if (!Selection.IsSelected(gObject))
                return;

            UpdateCurve(gObject);
        }

        void OnAnimationRemoved(GameObject gObject)
        {
            DeleteCurve(gObject);
        }

        void ClearCurves()
        {
            foreach (GameObject curve in curves.Values)
                Destroy(curve);
            curves.Clear();
        }

        void DeleteCurve(GameObject gObject)
        {
            if (curves.ContainsKey(gObject))
            {
                Destroy(curves[gObject]);
                curves.Remove(gObject);
            }
        }

        void UpdateCurve(GameObject gObject)
        {
            DeleteCurve(gObject);
            AddCurve(gObject);
        }

        void AddCurve(GameObject gObject)
        {
            AnimationSet animationSet = GlobalState.Animation.GetObjectAnimation(gObject);
            if (null == animationSet)
                return;

            Curve positionX = animationSet.GetCurve(AnimatableProperty.PositionX);
            Curve positionY = animationSet.GetCurve(AnimatableProperty.PositionY);
            Curve positionZ = animationSet.GetCurve(AnimatableProperty.PositionZ);
            
            GameObject curve = Instantiate(curvePrefab, curvesParent);
            
            LineRenderer line = curve.GetComponent<LineRenderer>();
            int count = positionX.keys.Count;
            line.positionCount = count;
            for (int index = 0; index < count; index++)
            {
                Vector3 position = new Vector3(positionX.keys[index].value, positionY.keys[index].value, positionZ.keys[index].value);
                line.SetPosition(index, position);
            }

            curves.Add(gObject, curve);
        }
    }

}