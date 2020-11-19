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
            GlobalState.Animation.onChangeCurve.AddListener(OnCurveChanged);
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

        void OnCurveChanged(GameObject gObject, AnimatableProperty property)
        {
            if (property != AnimatableProperty.PositionX && property != AnimatableProperty.PositionY && property != AnimatableProperty.PositionZ)
                return;

            if (!Selection.IsSelected(gObject))
                return;

            UpdateCurve(gObject);
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

            if (null == positionX || null == positionY || null == positionZ)
                return;

            if (positionX.keys.Count == 0)
                return;

            if (positionX.keys.Count != positionY.keys.Count || positionX.keys.Count != positionZ.keys.Count)
                return;

            int frameStart = Mathf.Clamp(positionX.keys[0].frame, GlobalState.Animation.StartFrame, GlobalState.Animation.EndFrame);
            int frameEnd = Mathf.Clamp(positionX.keys[positionX.keys.Count - 1].frame, GlobalState.Animation.StartFrame, GlobalState.Animation.EndFrame);

            Transform curves3DTransform = GlobalState.Instance.world.Find("Curves3D");
            Matrix4x4 matrix = curves3DTransform.worldToLocalMatrix * gObject.transform.parent.localToWorldMatrix;

            List<Vector3> positions = new List<Vector3>();
            Vector3 previousPosition = Vector3.positiveInfinity;
            for (int i = frameStart; i <= frameEnd; i++)
            {
                positionX.Evaluate(i, out float x);
                positionY.Evaluate(i, out float y);
                positionZ.Evaluate(i, out float z);
                Vector3 position = new Vector3(x, y, z);
                if (previousPosition != position)
                {
                    position = matrix.MultiplyPoint(position);

                    positions.Add(position);
                    previousPosition = position;
                }
            }

            int count = positions.Count;
            GameObject curve = Instantiate(curvePrefab, curvesParent);
            
            LineRenderer line = curve.GetComponent<LineRenderer>();
            line.positionCount = count;
            for (int index = 0; index < count; index++)
            {
                line.SetPosition(index, positions[index]);
            }

            curves.Add(gObject, curve);
        }
    }

}