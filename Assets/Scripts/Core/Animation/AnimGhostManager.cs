/* MIT License
 *
 * Université de Rennes 1 / Invictus Project
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */


using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class AnimGhostManager : MonoBehaviour
    {

        public class Node
        {
            public GameObject Target;
            public AnimationSet ObjectAnimation;
            public List<Node> Childrens;
            public GameObject Sphere;
            public GameObject Link;
            public int Frame;

            private Vector3 worldPosition;

            public void ClearNode()
            {
                Childrens.ForEach(x => x.ClearNode());
                Childrens.Clear();
                Destroy(Sphere);
                Destroy(Link);
            }

            public Node(GameObject targetObject, int frame, Transform parentNode, Matrix4x4 parentMatrix, float scale = 1f)
            {
                Target = targetObject;
                Frame = frame;
                ObjectAnimation = GlobalState.Animation.GetObjectAnimation(Target);
                Childrens = new List<Node>();
                if (null == ObjectAnimation) return;
                Matrix4x4 objectMatrix = parentMatrix * ObjectAnimation.GetTRSMatrix(frame);
                Maths.DecomposeMatrix(objectMatrix, out worldPosition, out Quaternion objectRotation, out Vector3 objectScale);

                if (Target.TryGetComponent<RigGoalController>(out RigGoalController controller) && controller.IsGoal)
                {
                    Sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    controller.CheckAnimations();
                }
                else
                    Sphere = new GameObject();

                Sphere.layer = 5;
                Sphere.transform.parent = parentNode;
                Sphere.transform.SetPositionAndRotation(worldPosition, objectRotation);
                Sphere.transform.localScale = Vector3.one * scale;
                Sphere.name = targetObject.name + "-" + frame;

                if (null != controller && !controller.name.Contains("Hips"))
                {
                    Link = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    Link.transform.parent = parentNode;
                    Link.transform.localPosition = Sphere.transform.localPosition / 2f;
                    Link.transform.up = Sphere.transform.position - parentNode.position;
                    Link.transform.localScale = new Vector3(scale, Sphere.transform.localPosition.magnitude / 2f, scale);
                    Link.layer = 5;
                }

                if (targetObject.transform.childCount > 3) scale = 0.5f;
                else scale = 1f;
                foreach (Transform child in targetObject.transform)
                {
                    Childrens.Add(new Node(child.gameObject, frame, Sphere.transform, objectMatrix, scale));
                }
            }

            /// <summary>
            /// For the hover skeleton, we don't want to recreate it every time, instead we try to reused the allready created objects
            /// </summary>
            public void RetargetNode(GameObject targetObject, int frame, Matrix4x4 parentMatrix, float scale)
            {
                Target = targetObject;
                Frame = frame;
                ObjectAnimation = GlobalState.Animation.GetObjectAnimation(Target);
                if (null == ObjectAnimation) return;
                Matrix4x4 objectMatrix = parentMatrix * ObjectAnimation.GetTRSMatrix(frame);
                Maths.DecomposeMatrix(objectMatrix, out worldPosition, out Quaternion objectRotation, out Vector3 objectScale);

                Sphere.transform.SetPositionAndRotation(worldPosition, objectRotation);
                Sphere.name = targetObject.name + "-" + frame;
                Sphere.transform.localScale = Vector3.one * scale;

                if (null != Link)
                {
                    Link.transform.localPosition = Sphere.transform.localPosition / 2f;
                    Link.transform.up = Sphere.transform.position - Sphere.transform.parent.position;
                    Link.transform.localScale = new Vector3(scale, Sphere.transform.localPosition.magnitude / 2f, scale);
                }
                if (targetObject.transform.childCount > 3) scale = 0.5f;
                else scale = 1;

                //If the rigs are different, we still need to recreate the skeleton
                if (Childrens.Count == targetObject.transform.childCount)
                {
                    for (int i = 0; i < Childrens.Count; i++)
                    {
                        Childrens[i].RetargetNode(targetObject.transform.GetChild(i).gameObject, frame, objectMatrix, scale);
                    }
                }
                else
                {
                    Childrens.ForEach(x => ClearNode());
                    Childrens.Clear();
                    foreach (Transform child in targetObject.transform)
                    {
                        Childrens.Add(new Node(child.gameObject, frame, Sphere.transform, objectMatrix, scale));
                    }
                }
            }

            public void UpdateNode(Matrix4x4 parentMatrix)
            {
                if (null == ObjectAnimation) return;
                Matrix4x4 objectMatrix = parentMatrix * ObjectAnimation.GetTRSMatrix(Frame);
                Maths.DecomposeMatrix(objectMatrix, out Vector3 objectPosition, out Quaternion objectRotation, out Vector3 objectScale);
                if (null != Sphere)
                {
                    Sphere.transform.SetPositionAndRotation(objectPosition, objectRotation);
                }
                Childrens.ForEach(x => x.UpdateNode(objectMatrix));
            }

            public void SetOffset(Vector3 offset)
            {
                Sphere.transform.position = worldPosition + offset;
            }


            public void ShowNode(bool state)
            {
                Sphere.SetActive(state);
                if (null != Link) Link.SetActive(state);
            }
        }

        public Dictionary<RigController, Dictionary<int, Node>> ghostDictionary;
        public Transform GhostParent;
        private bool isAnimationTool;

        private bool showSkeleton;

        private float currentOffset;

        private Node HoverGhost;


        public void Start()
        {
            ghostDictionary = new Dictionary<RigController, Dictionary<int, Node>>();

            ToolsUIManager.Instance.OnToolChangedEvent += OnToolChanged;
            Selection.onSelectionChanged.AddListener(OnSelectionChanged);
            GlobalState.Animation.onFrameEvent.AddListener(UpdateOffset);
            GlobalState.Animation.onChangeCurve.AddListener(OnCurveChanged);
            GlobalState.Animation.onRemoveAnimation.AddListener(OnAnimationRemoved);

            showSkeleton = GlobalState.Settings.DisplaySkeletons;
            currentOffset = GlobalState.Settings.CurveForwardOffset;
        }

        public void Update()
        {
            if (showSkeleton != GlobalState.Settings.DisplaySkeletons)
            {
                showSkeleton = GlobalState.Settings.DisplaySkeletons;
                if (showSkeleton)
                {
                    UpdateFromSelection();
                }
                else
                {
                    ClearGhosts();
                }
            }

            if (currentOffset != GlobalState.Settings.CurveForwardOffset)
            {
                currentOffset = GlobalState.Settings.CurveForwardOffset;
                UpdateOffset(GlobalState.Animation.CurrentFrame);
            }
        }

        private void OnToolChanged(object sender, ToolChangedArgs args)
        {
            bool switchToAnim = args.toolName == "Animation";
            if (switchToAnim && !isAnimationTool) UpdateFromSelection();
            if (!switchToAnim && isAnimationTool) ClearGhosts();
            isAnimationTool = switchToAnim;
        }

        void OnSelectionChanged(HashSet<GameObject> previousSelectedObjects, HashSet<GameObject> selectedObjects)
        {
            UpdateFromSelection();
        }

        void UpdateFromSelection()
        {
            if (ToolsManager.CurrentToolName() != "Animation" || !showSkeleton) return;
            ClearGhosts();
            foreach (GameObject gObject in Selection.SelectedObjects)
            {
                if (gObject.TryGetComponent<RigController>(out RigController controller))
                {
                    CreateGhost(controller);
                }
            }
        }

        public void OnCurveChanged(GameObject gObject, AnimatableProperty property)
        {
            if (ToolsManager.CurrentToolName() != "Animation" || !showSkeleton) return;
            if (gObject.TryGetComponent<RigController>(out RigController controller))
            {
                if (ghostDictionary.TryGetValue(controller, out Dictionary<int, Node> nodeDictionary))
                {
                    foreach (KeyValuePair<int, Node> node in nodeDictionary)
                    {
                        node.Value.ClearNode();
                    }
                }
                CreateGhost(controller);
            }
        }

        public void OnAnimationRemoved(GameObject gobject)
        {
            if (gobject.TryGetComponent<RigController>(out RigController controller))
            {
                if (ghostDictionary.TryGetValue(controller, out Dictionary<int, Node> nodeDictionary))
                {
                    foreach (KeyValuePair<int, Node> node in nodeDictionary)
                    {
                        node.Value.ClearNode();
                    }
                    ghostDictionary.Remove(controller);
                }
            }
        }

        private void CreateGhost(RigController controller)
        {
            AnimationSet HipsAnim = GlobalState.Animation.GetObjectAnimation(controller.RootObject.gameObject);
            if (null == HipsAnim) return;

            AnimationSet rootAnim = GlobalState.Animation.GetObjectAnimation(controller.gameObject);
            ghostDictionary[controller] = new Dictionary<int, Node>();
            Curve rotX = HipsAnim.GetCurve(AnimatableProperty.RotationX);
            rotX.keys.ForEach(keyframe =>
            {
                Matrix4x4 rootMatrix = rootAnim != null ? rootAnim.GetTRSMatrix(keyframe.frame) : Matrix4x4.TRS(controller.transform.localPosition, controller.transform.localRotation, controller.transform.localScale);
                ghostDictionary[controller].Add(keyframe.frame,
                    new Node(controller.RootObject.gameObject, keyframe.frame, GhostParent, controller.transform.parent.localToWorldMatrix * rootMatrix, controller.transform.localScale.magnitude * 5));
            });
            UpdateOffset(GlobalState.Animation.CurrentFrame);
        }

        public void UpdateOffset(int currentFrame)
        {
            foreach (KeyValuePair<RigController, Dictionary<int, Node>> ghost in ghostDictionary)
            {
                Vector3 forwardVector = (ghost.Key.transform.forward * ghost.Key.transform.localScale.x) * currentOffset;
                foreach (KeyValuePair<int, Node> node in ghost.Value)
                {
                    int offsetSize = currentFrame - node.Key;
                    node.Value.SetOffset(forwardVector * offsetSize);
                }
            }
        }

        private void ClearGhosts()
        {
            foreach (KeyValuePair<RigController, Dictionary<int, Node>> ghost in ghostDictionary)
            {
                foreach (KeyValuePair<int, Node> node in ghost.Value)
                {
                    node.Value.ClearNode();
                }
            }
            ghostDictionary.Clear();
        }

        public void CreateHoverGhost(RigController controller, int frame)
        {
            AnimationSet rootAnim = GlobalState.Animation.GetObjectAnimation(controller.gameObject);
            Matrix4x4 rootMatrix = rootAnim != null ? rootAnim.GetTRSMatrix(frame) : Matrix4x4.TRS(controller.transform.localPosition, controller.transform.localRotation, controller.transform.localScale); ;
            if (null == HoverGhost)
            {
                HoverGhost = new Node(controller.RootObject.gameObject, frame, GhostParent, controller.transform.parent.localToWorldMatrix * rootMatrix, controller.transform.localScale.magnitude * 5);
            }
            else
            {
                HoverGhost.RetargetNode(controller.RootObject.gameObject, frame, controller.transform.parent.localToWorldMatrix * rootMatrix, controller.transform.localScale.magnitude * 5);
                HoverGhost.ShowNode(true);
            }
            Vector3 forwardVector = (controller.transform.forward * controller.transform.localScale.x) * currentOffset;
            int offsetSize = GlobalState.Animation.CurrentFrame - frame;
            HoverGhost.SetOffset(forwardVector * offsetSize);
        }

        public void HideGhost()
        {
            if (null == HoverGhost) return;
            HoverGhost.ShowNode(false);
        }
    }

}