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

        private bool rotating = false;
        private bool teleporting = false;
        private bool isValidLocationHit = false;
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

        public override void Init(Transform rigTransform, Transform worldTransform, Transform leftHandleTransform, Transform rightHandleTransform, Transform pivotTransform, Transform cameraTransform, Transform parametersTransform)
        {
            base.Init(rigTransform, worldTransform, leftHandleTransform, rightHandleTransform, pivotTransform, cameraTransform, parametersTransform);

            // Create tooltips
            Tooltips.CreateTooltip(leftHandle.Find("left_controller").gameObject, Tooltips.Anchors.Joystick, "Target/Turn");

            usedControls = UsedControls.LEFT_JOYSTICK;

            Transform drone = parameters.Find("Teleport");
            drone.gameObject.SetActive(true);
        }

        public override void DeInit()
        {
            Transform drone = parameters.Find("Teleport");
            drone.gameObject.SetActive(false);

            if (teleport != null)
            {
                teleport.gameObject.SetActive(false);
            }

            base.DeInit();
        }

        public override void Update()
        {
            if (teleport == null)
                return;

            // Teleport
            Vector2 leftJoyValue = VRInput.GetValue(VRInput.leftController, CommonUsages.primary2DAxis);
            float joyMag = leftJoyValue.magnitude;
            float yVal = leftJoyValue.y;
            float xVal = leftJoyValue.x;
            if (teleporting)
            {
                if (joyMag > deadZone)
                {
                    Vector3 rayStartPosition = leftHandle.Find("left_controller").Find("FrontAnchor").transform.position;
                    Vector3 rayStartDirection = leftHandle.forward;

                    List<Vector3> points;
                    RaycastHit hitInfo;
                    bool hit = ComputeTrajectory(rayStartPosition, rayStartDirection, trajectoryParams, out points, out hitInfo);
                    if (hit)
                    {
                        isValidLocationHit = true;
                        teleportTarget = hitInfo.point;
                        teleport.SetActiveColor();
                        teleportTargetObject.gameObject.SetActive(true);
                    }
                    else
                    {
                        teleport.SetImpossibleColor();
                        teleportTargetObject.gameObject.SetActive(false);
                    }

                    float cameraYAngle = Camera.main.transform.rotation.eulerAngles.y;
                    float joyAngle = 90.0f - Mathf.Rad2Deg * Mathf.Atan2(leftJoyValue.y, leftJoyValue.x);
                    teleportRay.positionCount = points.Count;
                    teleportRay.SetPositions(points.ToArray());
                    teleportTargetObject.position = teleportTarget + new Vector3(0f, 0.01f, 0f);
                    teleportTargetObject.rotation = Quaternion.Euler(0.0f, cameraYAngle + joyAngle, 0.0f);
                }
                else // GO OUT of teleport mode, and TELEPORT
                {
                    teleporting = false;

                    if (isValidLocationHit)
                    {
                        float height = rig.position.y;

                        Vector3 cameraForwardProj = new Vector3(camera.forward.x, 0.0f, camera.forward.z).normalized;
                        float YAngleDelta = Vector3.SignedAngle(cameraForwardProj, teleportTargetObject.forward, Vector3.up);
                        Quaternion deltaRotation = Quaternion.Euler(0.0f, YAngleDelta, 0.0f);
                        rig.rotation = rig.rotation * deltaRotation;
                        
                        Vector3 camera_to_rig = rig.transform.position - camera.transform.position;
                        Vector3 new_camera_to_target = new Vector3(0.0f, teleportTarget.y - camera.transform.position.y, 0.0f); // place camera above target
                        Vector3 deltaPosition = camera_to_rig - new_camera_to_target;
                        rig.position = teleportTarget;
                        
                        rig.localScale = Vector3.one;

                        if (options.lockHeight)
                        {
                            rig.position = new Vector3(rig.position.x, height, rig.position.z);
                        }

                        GlobalState.WorldScale = 1f;
                        UpdateCameraClipPlanes();

                        isValidLocationHit = false;
                    }

                    teleport.gameObject.SetActive(false);
                }
            }
            else
            {
                // GO IN teleport mode
                if (yVal > deadZone)
                {
                    teleporting = true;
                    teleport.gameObject.SetActive(true);
                    teleportTargetObject.gameObject.layer = 2; // Ignore Raycast - TODO: put in prefab.
                }
                else
                {
                    // ROTATE +/- 45 degrees using Left/Right impulses.
                    if (Mathf.Abs(xVal) > 0.8f && !rotating)
                    {
                        rig.RotateAround(camera.position, Vector3.up, Mathf.Sign(xVal) * 45f);
                        rotating = true;
                    }
                    if (Mathf.Abs(xVal) <= 0.8f && rotating)
                    {
                        rotating = false;
                    }
                }
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
            var rangeSquared = tParams.range * tParams.range / GlobalState.WorldScale;
            bool hit;
            float step = tParams.aimStep;

            int layerMask = ~(1 << 5);
            do
            {
                Vector3 oldAimPosition = aimPosition;
                points.Add(aimPosition);

                var aimVector = aimDirection;
                aimVector.y = aimVector.y + tParams.gravity * 0.0111111111f * step;
                aimDirection = aimVector;
                aimPosition += aimVector * step;
                hit = Physics.Raycast(oldAimPosition, (aimPosition - oldAimPosition).normalized, out hitInfo, (aimPosition - oldAimPosition).magnitude, layerMask);
            } while (!hit && (aimPosition.y - startRay.origin.y > tParams.minimumElevation / GlobalState.WorldScale) && ((startRay.origin - aimPosition).sqrMagnitude <= rangeSquared));

            return hit;
        }
    }
}
