using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    [System.Serializable]
    public class TrajectoryParams
    {
        /// <summary>
        /// Maximum range for aiming.
        /// </summary>
        [Tooltip("Maximum range for aiming.")]
        public float range;

        /// <summary>
        /// The MinimumElevation is relative to the AimPosition.
        /// </summary>
        [Tooltip("The MinimumElevation is relative to the AimPosition.")]
        public float minimumElevation = -100;

        /// <summary>
        /// The Gravity is used in conjunction with AimVelocity and the aim direction to simulate a projectile.
        /// </summary>
        [Tooltip("The Gravity is used in conjunction with AimVelocity and the aim direction to simulate a projectile.")]
        public float gravity = -9.8f;

        /// <summary>
        /// The AimVelocity is the initial speed of the faked projectile.
        /// </summary>
        [Tooltip("The AimVelocity is the initial speed of the faked projectile.")]
        [Range(0.001f, 50.0f)]
        public float aimVelocity = 1;

        /// <summary>
        /// The AimStep is the how much to subdivide the iteration.
        /// </summary>
        [Tooltip("The AimStep is the how much to subdivide the iteration.")]
        [Range(0.001f, 1.0f)]
        public float aimStep = 1;

    }

    public class NavigationMode_Teleport : NavigationMode
    {
        private Transform teleport = null;

        private bool isLocked = false;

        private Vector3 targetPosition = Vector3.zero;

        private const float deadZone = 0.5f;


        private bool teleporting;
        private bool teleportingRay;
        private Vector3 teleportTarget;
        private LineRenderer teleportRay;
        private Transform teleportTargetObject;
        private Transform teleportStart;
        private TrajectoryParams trajectoryParams;


        public NavigationMode_Teleport(Transform teleportObject)
        {
            teleport = teleportObject;
        }

        public override void Init(Transform rigTransform, Transform worldTransform, Transform leftHandleTransform, Transform pivotTransform, Transform cameraTransform, Transform parametersTransform)
        {
            base.Init(rigTransform, worldTransform, leftHandleTransform, pivotTransform, cameraTransform, parametersTransform);

            // Create tooltips
            Tooltips.CreateTooltip(leftHandle.Find("left_controller").gameObject, Tooltips.Anchors.Joystick, "Target/Turn");

            usedControls = UsedControls.LEFT_JOYSTICK;

            //if (arc != null)
            //{
            //    arc.gameObject.SetActive(true);
            //    arc.SetDefaultColor();
            //}
        }

        public override void DeInit()
        {
            base.DeInit();

            //if (arc != null)
            //{
            //    arc.gameObject.SetActive(false);
            //}
        }

        public override void Update()
        {
            //if (arc == null)
            //    return;

            // Teleport
            Vector2 leftJoyValue = VRInput.GetValue(VRInput.leftController, CommonUsages.primary2DAxis);
            float pressedValue = leftJoyValue.y;
            if (pressedValue > 0.5f)
            {
                teleportRay.enabled = true;
                teleportTargetObject.gameObject.SetActive(true);

                //Vector3 position;
                //Quaternion rotation;
                //GetControllerTransform(leftController, out position, out rotation);
                //leftHandle.localPosition = position;
                //leftHandle.localRotation = rotation;

                List<Vector3> points;
                RaycastHit hitInfo;
                bool hit = ComputeTrajectory(teleportStart, trajectoryParams, out points, out hitInfo);
                if (hit)
                {
                    teleportingRay = true;
                    teleportTarget = hitInfo.point;
                }
                teleportRay.positionCount = points.Count;
                teleportRay.SetPositions(points.ToArray());
                teleportRay.material.SetColor("_BaseColor", new Color(0.3f, 1f, 0.3f, 0.7f));
                teleportTargetObject.position = teleportTarget + new Vector3(0f, 0.01f, 0f);
                teleportTargetObject.rotation = Quaternion.Euler(90f, Camera.main.transform.rotation.eulerAngles.y, 0f);
                teleportTargetObject.gameObject.layer = 2;
                //teleportTool.SetActive(true);
                teleporting = true;
            }
            else if (pressedValue <= 0.5f && teleporting)
            {
                teleporting = false;

                if (teleportingRay)
                {
                    rig.position = teleportTarget;
                    teleportingRay = false;
                }
                teleportRay.material.SetColor("_BaseColor", new Color(0f, 0f, 0f, 0f));
                teleportRay.enabled = false;
                teleportTargetObject.gameObject.SetActive(false);
                //teleportTool.SetActive(false);
            }
        }

        

        public static bool ComputeTrajectory(Transform handle, TrajectoryParams tParams, out List<Vector3> points, out RaycastHit hitInfo)
        {
            points = new List<Vector3>();
            Ray startRay = new Ray();
            startRay.origin = handle.position;
            startRay.direction = handle.forward;

            var aimPosition = startRay.origin;
            var aimDirection = startRay.direction * tParams.aimVelocity;
            var rangeSquared = tParams.range * tParams.range;
            bool hit;

            do
            {
                Vector3 oldAimPosition = aimPosition;
                points.Add(aimPosition);

                var aimVector = aimDirection;
                aimVector.y = aimVector.y + tParams.gravity * 0.0111111111f * tParams.aimStep;
                aimDirection = aimVector;
                aimPosition += aimVector * tParams.aimStep;

                hit = Physics.Raycast(oldAimPosition, (aimPosition - oldAimPosition).normalized, out hitInfo, (aimPosition - oldAimPosition).magnitude);
            } while (!hit && (aimPosition.y - startRay.origin.y > tParams.minimumElevation) && ((startRay.origin - aimPosition).sqrMagnitude <= rangeSquared));

            return hit;
        }

    }
}
