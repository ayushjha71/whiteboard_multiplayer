using UnityEngine;

namespace FPS_Multiplayer.Handler
{
    public class FPSCameraController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform playerTransform; // Player body (for horizontal rotation)
        [SerializeField] private Transform cameraTransform; // Camera (for vertical rotation)
        [SerializeField] private PlayerInputHandler playerInputHandler;

        [Header("Settings")]
        [SerializeField] private float lookSensitivity = 2f;
        [SerializeField] private float minVerticalAngle = -90f; // Can't look further down than this
        [SerializeField] private float maxVerticalAngle = 90f;  // Can't look further up than this
        [SerializeField] private bool invertY = false;

        private float verticalRotation = 0f;
        private bool isCursorLocked = true;

        private void Start()
        {
            LockCursor(true);
            InitializeCamera();
        }

        private void InitializeCamera()
        {
            if (cameraTransform == null)
            {
                cameraTransform = GetComponent<Camera>().transform;
                if (cameraTransform == null)
                {
                    Debug.LogError("No camera found as child of FPSCameraController!");
                }
            }

            // Initialize rotation to current values
            verticalRotation = cameraTransform.localEulerAngles.x;
        }

        private void Update()
        {
            if (playerInputHandler == null) return;

            HandleCursorToggle();

            if (isCursorLocked)
            {
                HandleCameraRotation();
            }
        }

        private void HandleCursorToggle()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                LockCursor(!isCursorLocked);
            }
        }

        private void LockCursor(bool shouldLock)
        {
            isCursorLocked = shouldLock;
            Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !shouldLock;
        }

        private void HandleCameraRotation()
        {
            // Get input
            Vector2 lookInput = playerInputHandler.LookInput;

            // Horizontal rotation (turns the player body)
            float horizontalRotation = lookInput.x * lookSensitivity;
            playerTransform.Rotate(Vector3.up * horizontalRotation);

            // Vertical rotation (only tilts the camera)
            float verticalInput = invertY ? -lookInput.y : lookInput.y;
            verticalRotation += verticalInput * lookSensitivity;
            verticalRotation = Mathf.Clamp(verticalRotation, minVerticalAngle, maxVerticalAngle);

            // Apply vertical rotation to camera
            cameraTransform.localEulerAngles = Vector3.right * verticalRotation;
        }
    }
}