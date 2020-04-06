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
        private TeleportUI teleport = null;
        private LineRenderer teleportRay = null;
        private Transform teleportTargetObject;

        private const float deadZone = 0.5f;

        private bool teleporting;
        private bool isValidLocationHit;
        private Vector3 teleportTarget = Vector3.zero;
        private Transform teleportStart;
        private TrajectoryParams trajectoryParams = null;

        public NavigationMode_Teleport(TeleportUI teleportObject, TrajectoryParams trajectoryP)
        {
            teleport = teleportObject;
            teleportRay = teleport.transform.Find("Ray").GetComponent<LineRenderer>();
            teleportTargetObject = teleport.transform.Find("Target");
            trajectoryParams = trajectoryP;
        }

        public override void Init(Transform rigTransform, Transform worldTransform, Transform leftHandleTransform, Transform pivotTransform, Transform cameraTransform, Transform parametersTransform)
        {
            base.Init(rigTransform, worldTransform, leftHandleTransform, pivotTransform, cameraTransform, parametersTransform);

            // Create tooltips
            Tooltips.CreateTooltip(leftHandle.Find("left_controller").gameObject, Tooltips.Anchors.Joystick, "Target/Turn");

            usedControls = UsedControls.LEFT_JOYSTICK;
        }

        public override void DeInit()
        {
            base.DeInit();

            if (teleport != null)
            {
                teleport.gameObject.SetActive(false);
            }
        }

        public override void Update()
        {
            if (teleport == null)
                return;

            // Teleport
            Vector2 leftJoyValue = VRInput.GetValue(VRInput.leftController, CommonUsages.primary2DAxis);
            float pressedValue = leftJoyValue.y;
            if (pressedValue > deadZone)
            {
                teleport.gameObject.SetActive(true);

                Vector3 rayStartPosition = leftHandle.TransformPoint(-0.01f, 0.0f, 0.05f);
                Vector3 rayStartDirection = leftHandle.forward;

                // TODO: once locked in teleport mode, use the leftJoyValue direction to find the rotation angle,
                // and its magnitude to know if we are releasing the joyStick to teleport.
                
                //float joyMag = leftJoyValue.magnitude;

                List<Vector3> points;
                RaycastHit hitInfo;
                bool hit = ComputeTrajectory(rayStartPosition, rayStartDirection, trajectoryParams, out points, out hitInfo);
                if (hit)
                {
                    isValidLocationHit = true;
                    teleportTarget = hitInfo.point;
                    teleport.SetActiveColor();
                }
                else
                {
                    teleport.SetImpossibleColor();
                }
                teleportRay.positionCount = points.Count;
                teleportRay.SetPositions(points.ToArray());
                teleportTargetObject.position = teleportTarget + new Vector3(0f, 0.01f, 0f);
                teleportTargetObject.rotation = Quaternion.Euler(0.0f, Camera.main.transform.rotation.eulerAngles.y, 0.0f);
                teleportTargetObject.gameObject.layer = 2; // Ignore Raycast
                teleporting = true;
            }
            else if (pressedValue <= deadZone && teleporting)
            {
                teleporting = false;

                if (isValidLocationHit)
                {
                    rig.position = teleportTarget;
                    //rig.rotation = ;
                    isValidLocationHit = false;
                }

                teleport.gameObject.SetActive(false);
            }
        }

        public static bool ComputeTrajectory(Vector3 rayStartPosition, Vector3 rayStartDirection, TrajectoryParams tParams, out List<Vector3> points, out RaycastHit hitInfo)
        {
            points = new List<Vector3>();
            Ray startRay = new Ray();
            startRay.origin = rayStartPosition;
            startRay.direction = rayStartDirection;

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
