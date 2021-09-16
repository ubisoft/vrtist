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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace VRtist
{
    [CreateAssetMenu(menuName = "VRtist/RigConfiguration", fileName = "RigConfig")]
    public class RigConfiguration : ScriptableObject
    {
        public Mesh mesh;
        public Material material;

        [System.Serializable]
        public class Joint
        {
            public string Name;
            public float stiffness;
            public bool isGoal;
            public bool showCurve;
            public Vector3 LowerAngleBound;
            public Vector3 UpperAngleBound;
        }

        public List<Joint> JointsList;


        public void GenerateGoalController(RigController rootController, Transform transform, List<Transform> path)
        {
            string boneName = transform.name;
            if (boneName.Contains("mixamorig:")) boneName = boneName.Split(':')[1];
            Joint joint = JointsList.Find(x => x.Name == boneName);
            if (null != joint)
            {
                SphereCollider collider = transform.gameObject.AddComponent<SphereCollider>();
                collider.isTrigger = true;
                RigGoalController controller = transform.gameObject.AddComponent<RigGoalController>();
                controller.SetPathToRoot(rootController, path);
                controller.stiffness = joint.stiffness;
                controller.IsGoal = joint.isGoal;
                controller.ShowCurve = joint.showCurve;
                controller.LowerAngleBound = joint.LowerAngleBound;
                controller.UpperAngleBound = joint.UpperAngleBound;

                if (joint.isGoal)
                {
                    controller.gameObject.layer = 21;
                    controller.goalCollider = collider;
                    controller.tag = "Goal";
                    MeshFilter filter = transform.gameObject.AddComponent<MeshFilter>();
                    filter.mesh = mesh;
                    MeshRenderer renderer = transform.gameObject.AddComponent<MeshRenderer>();
                    renderer.material = new Material(material);
                    controller.MeshRenderer = renderer;

                    controller.UseGoal(false);
                }
            }
            path.Add(transform);
            foreach (Transform child in transform)
            {
                GenerateGoalController(rootController, child, new List<Transform>(path));
            }
        }

    }
}
