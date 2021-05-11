using System.Collections.Generic;

using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class Gun : ToolBase
    {
        [Tooltip("Number of spawned per second")] public float fireRate = 5f;
        [Tooltip("Power")] public float power = 10f;
        public List<GameObject> prefabs = new List<GameObject>();

        private float prevTime;
        CommandGroup group;

        void Start()
        {
            prefabs.Add(Resources.Load<GameObject>("Prefabs/Primitives/PRIMITIVES/cube"));
            prefabs.Add(Resources.Load<GameObject>("Prefabs/Primitives/PRIMITIVES/cylinder"));
            prefabs.Add(Resources.Load<GameObject>("Prefabs/Primitives/PRIMITIVES/prism"));
        }

        protected override void OnDisable()
        {
            if (null != group)
            {
                group.Submit();
                group = null;
            }
        }

        protected override void DoUpdate()
        {
            VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.triggerButton,
                () =>
                {
                    group = new CommandGroup("Gun");
                },
                () =>
                {
                    group.Submit();
                    group = null;
                }
            );

            bool triggered = VRInput.GetValue(VRInput.primaryController, CommonUsages.triggerButton);
            if (triggered)
            {
                if (Time.time - prevTime > 1f / fireRate)
                {
                    int prefabIndex = UnityEngine.Random.Range(0, prefabs.Count);
                    GameObject spawned = Instantiate(prefabs[prefabIndex]);
                    spawned.AddComponent<ThrowedObject>().AddForce(transform.forward * power);
                    new CommandAddGameObject(spawned).Submit();
                    Matrix4x4 matrix = SceneManager.RightHanded.worldToLocalMatrix * mouthpiece.localToWorldMatrix;
                    Maths.DecomposeMatrix(matrix, out Vector3 t, out _, out _);
                    Vector3 scale = Vector3.one;
                    SceneManager.SetObjectMatrix(spawned, Matrix4x4.TRS(t, Quaternion.identity, scale));
                    prevTime = Time.time;
                }
            }
        }
    }
}
