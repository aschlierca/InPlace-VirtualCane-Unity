using UnityEngine;
using System.Collections;

namespace TMPro.Examples
{
    public class CameraController : MonoBehaviour
    {
        public enum CameraModes { Follow, Isometric, Free }

        private Transform cameraTransform;
        private Transform dummyTarget;

        public Transform CameraTarget;

        public float FollowDistance = 30.0f;
        public float MaxFollowDistance = 100.0f;
        public float MinFollowDistance = 2.0f;

        public float ElevationAngle = 30.0f;
        public float MaxElevationAngle = 85.0f;
        public float MinElevationAngle = 0f;

        public float OrbitalAngle = 0f;

        public CameraModes CameraMode = CameraModes.Follow;

        public bool MovementSmoothing = true;
        public bool RotationSmoothing = false;
        private bool previousSmoothing;

        public float MovementSmoothingValue = 25f;
        public float RotationSmoothingValue = 5.0f;

        public float MoveSensitivity = 2.0f;

        private Vector3 currentVelocity = Vector3.zero;
        private Vector3 desiredPosition;
        private float mouseX;
        private float mouseY;
        private Vector3 moveVector;
        private float mouseWheel;
#if UNITY_EDITOR
        [Header("Editor IMU Simulation")]
        public bool simulateIMUInEditor = true;
        public Vector3 simulatedAccel = new Vector3(0f, 0f, -1f); // gravity down
        public Vector3 simulatedGyro = Vector3.zero; // no rotation
        public float simulationUpdateRate = 0.02f; // 50 Hz
#endif

        void Awake()
        {
            if (QualitySettings.vSyncCount > 0)
                Application.targetFrameRate = 60;
            else
                Application.targetFrameRate = -1;

            if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.Android)
                Input.simulateMouseWithTouches = false;

            cameraTransform = transform;
            previousSmoothing = MovementSmoothing;

#if UNITY_EDITOR
            if (simulateIMUInEditor)
                StartCoroutine(SimulateIMU());
#endif
        }

        void Start()
        {
            if (CameraTarget == null)
            {
                dummyTarget = new GameObject("Camera Target").transform;
                CameraTarget = dummyTarget;
            }
        }

        void LateUpdate()
        {
            GetPlayerInput();

            if (CameraTarget != null)
            {
                if (CameraMode == CameraModes.Isometric)
                    desiredPosition = CameraTarget.position + Quaternion.Euler(ElevationAngle, OrbitalAngle, 0f) * new Vector3(0, 0, -FollowDistance);
                else if (CameraMode == CameraModes.Follow)
                    desiredPosition = CameraTarget.position + CameraTarget.TransformDirection(Quaternion.Euler(ElevationAngle, OrbitalAngle, 0f) * new Vector3(0, 0, -FollowDistance));
                else
                {
                    // Free Camera implementation
                }

                if (MovementSmoothing)
                    cameraTransform.position = Vector3.SmoothDamp(cameraTransform.position, desiredPosition, ref currentVelocity, MovementSmoothingValue * Time.fixedDeltaTime);
                else
                    cameraTransform.position = desiredPosition;

                if (RotationSmoothing)
                    cameraTransform.rotation = Quaternion.Lerp(cameraTransform.rotation, Quaternion.LookRotation(CameraTarget.position - cameraTransform.position), RotationSmoothingValue * Time.deltaTime);
                else
                    cameraTransform.LookAt(CameraTarget);
            }
        }

        void GetPlayerInput()
        {
            // Your existing GetPlayerInput implementation goes here
            // Copy the entire method from your previous CameraController
            // It remains unchanged.
        }

        /// <summary>
        /// Called by MetaWearBridge for real IMU data
        /// </summary>
        /// <param name="accel"></param>
        /// <param name="gyro"></param>
        public void OnIMUData(Vector3 accel, Vector3 gyro)
        {
#if UNITY_EDITOR
            if (!simulateIMUInEditor)
                return;
#endif
            // Example: rotate camera slightly based on gyro input
            Vector3 rot = new Vector3(-gyro.y, gyro.x, 0f) * 1.0f * Time.deltaTime;
            transform.Rotate(rot, Space.World);

            // Optional: you could add tilt from accelerometer if needed
        }

#if UNITY_EDITOR
        // Simulate IMU in editor for testing without MetaWear
        private IEnumerator SimulateIMU()
        {
            while (true)
            {
                OnIMUData(simulatedAccel, simulatedGyro);
                yield return new WaitForSeconds(simulationUpdateRate);
            }
        }
#endif
    }
}
