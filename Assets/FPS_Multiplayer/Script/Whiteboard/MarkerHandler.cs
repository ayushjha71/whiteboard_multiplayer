using Fusion;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FPS_Multiplayer.Whiteboard
{
    public class MarkerHandler : NetworkBehaviour
    {
        [SerializeField] private Transform markerTip;
        [SerializeField] private int penSize = 5;
        [SerializeField] private int eraserSize = 50;
        [SerializeField] private float markerDistance = 0.5f;
        [SerializeField] private Material markerMaterial;
        [SerializeField] private Material eraserMaterial;
        [SerializeField] private float minDrawDistance = 0.001f;
        [SerializeField] private float eraserThreshold = 0.05f;

        // More aggressive parameters for eraser performance
        [SerializeField] private float eraserLargeThreshold = 0.1f; // Higher threshold for large erasers

        // LOD system for eraser
        [SerializeField] private float eraserLodSpeedThreshold = 5.0f; // Speed to activate more aggressive optimization

        // Downsampling for fast movements
        [SerializeField] private int maxUpdatesPerSecondFastMode = 15;
        [SerializeField] private int maxUpdatesPerSecondNormalMode = 30;

        // Pen and eraser size limits
        [SerializeField] private int minPenSize = 5;
        [SerializeField] private int maxPenSize = 50;
        [SerializeField] private int minEraserSize = 50;
        [SerializeField] private int maxEraserSize = 100;

        private Camera mainCamera;
        private Renderer mRenderer;
        private RaycastHit mHit;
        private Vector3 lastDrawPosition;
        private Vector2 mLastHitPosition;
        private bool mIsDrawing = false;
        private bool mLastHitFrame = false;
        private bool mIsEraserMode = false;
        private WhiteboardHandler whiteBoardHandler;

        // Optimization
        private Vector3 mLastEraserPosition;
        private Vector3 mLastEraserPositionWorld;
        private Vector3 mEraserVelocity;
        private float mLastEraserUpdate = 0f;
        private float mLastVelocityCalculationTime;
        private float mEraserCurrentThreshold;
        private bool mInHighSpeedMode = false;

        private bool mSliderInitialized = false;

        private void Start()
        {
            mainCamera = Camera.main;
            mRenderer = markerTip.GetComponent<Renderer>();
            lastDrawPosition = Vector3.zero;
            mLastEraserPosition = Vector3.zero;
            mEraserCurrentThreshold = eraserThreshold;

            penSize = Mathf.Clamp(penSize, minPenSize, maxPenSize);
            eraserSize = Mathf.Clamp(eraserSize, minEraserSize, maxEraserSize);
        }

        public int GetPenSize()
        {
            return penSize;
        }

        public int GetEraserSize()
        {
            return eraserSize;
        }

        public void ToggleEraserMode(bool enabled)
        {
            if (mIsEraserMode != enabled)
            {
                mIsEraserMode = enabled;
                mRenderer.material = enabled ? eraserMaterial : markerMaterial;

                // Resetting position when switching modes
                mLastHitFrame = false;
                lastDrawPosition = Vector3.zero;
                mLastEraserPosition = Vector3.zero;
                mLastEraserPositionWorld = Vector3.zero;
                mLastEraserUpdate = 0f;
                mInHighSpeedMode = false;
                mEraserCurrentThreshold = eraserThreshold;
                whiteBoardHandler.WhiteBoardUIHandler.SizeSlider.value = enabled ? GetEraserSize() : GetPenSize();
            }
        }

        private void Update()
        {
            if (!HasStateAuthority)
                return;

            if (!mIsEraserMode && whiteBoardHandler != null)
            {
                mRenderer.material.color = whiteBoardHandler.ColorPickerController.FinalColor;
            }

            UpdateMarkerPosition();

            // Initialize slider if not already done
            if (whiteBoardHandler != null && !mSliderInitialized)
            {
                InitializeSlider();
            }

            if (mIsEraserMode)
            {
                UpdateEraserState();
                HandleEraserDrawing();
            }
            else
            {
                HandleMarkerDrawing();
            }

            if (whiteBoardHandler != null && whiteBoardHandler.IsErasing)
            {
                ToggleEraserMode(true);
            }
            else
            {
                ToggleEraserMode(false);
            }
        }

        private void InitializeSlider()
        {
            if (whiteBoardHandler != null && whiteBoardHandler.WhiteBoardUIHandler != null)
            {
                whiteBoardHandler.WhiteBoardUIHandler.SizeSlider.onValueChanged.RemoveAllListeners();
                whiteBoardHandler.WhiteBoardUIHandler.SizeSlider.onValueChanged.AddListener(UpdateSize);
                mSliderInitialized = true;
            }
        }

        private void UpdateSize(float val)
        {
            if (whiteBoardHandler == null || whiteBoardHandler.WhiteBoardUIHandler == null)
                return;

            if (whiteBoardHandler.IsErasing)
            {
                eraserSize = Mathf.Clamp(Mathf.RoundToInt(val), minEraserSize, maxEraserSize);
                whiteBoardHandler.WhiteBoardUIHandler.UpdateValue(val, ref eraserSize, true);
            }
            else
            {
                penSize = Mathf.Clamp(Mathf.RoundToInt(val), minPenSize, maxPenSize);
                whiteBoardHandler.WhiteBoardUIHandler.UpdateValue(val, ref penSize, false);
            }
        }

        // Optimization: Tracking eraser state for adaptive performance
        private void UpdateEraserState()
        {
            // Calculate velocity only if we have previous position
            if (mLastEraserPositionWorld != Vector3.zero)
            {
                float timeSinceLastCalculation = Time.time - mLastVelocityCalculationTime;
                if (timeSinceLastCalculation > 0.05f) // Recalculate velocity every 50ms
                {
                    mEraserVelocity = (transform.position - mLastEraserPositionWorld) / timeSinceLastCalculation;
                    float speed = mEraserVelocity.magnitude;

                    // Check if eraser is moving on high-speed
                    if (speed > eraserLodSpeedThreshold)
                    {
                        if (!mInHighSpeedMode)
                        {
                            mInHighSpeedMode = true;
                            mEraserCurrentThreshold = eraserLargeThreshold;
                        }
                    }
                    else if (mInHighSpeedMode)
                    {
                        mInHighSpeedMode = false;
                        mEraserCurrentThreshold = eraserThreshold;
                    }

                    mLastVelocityCalculationTime = Time.time;
                    mLastEraserPositionWorld = transform.position;
                }
            }
            else
            {
                mLastEraserPositionWorld = transform.position;
                mLastVelocityCalculationTime = Time.time;
            }
        }

        private void UpdateMarkerPosition()
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform.CompareTag("Whiteboard"))
                {
                    Vector3 markerPosition = hit.point - ray.direction * markerDistance;
                    transform.position = markerPosition;
                    transform.LookAt(hit.point);
                    transform.Rotate(90, 0, 0, Space.Self);
                }
            }
        }

        private void HandleMarkerDrawing()
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                mIsDrawing = true;
                mLastHitFrame = false;
                lastDrawPosition = Vector3.zero;
            }

            if (Input.GetMouseButtonUp(0))
            {
                mIsDrawing = false;
                mLastHitFrame = false;
                lastDrawPosition = Vector3.zero;
            }

            if (!mIsDrawing || !Physics.Raycast(markerTip.position, transform.up, out mHit, markerDistance))
                return;

            if (mHit.transform.CompareTag("Whiteboard"))
            {
                if (whiteBoardHandler == null)
                {
                    whiteBoardHandler = mHit.transform.GetComponent<WhiteboardHandler>();
                    InitializeSlider();
                }

                if (lastDrawPosition != Vector3.zero && Vector3.Distance(mHit.point, lastDrawPosition) < minDrawDistance)
                {
                    return;
                }

                Vector2 hitPosition = new Vector2(
                    mHit.textureCoord.x * whiteBoardHandler.TextureSize.x - (penSize / 2),
                    mHit.textureCoord.y * whiteBoardHandler.TextureSize.y - (penSize / 2)
                );

                if (!mLastHitFrame)
                {
                    mLastHitPosition = hitPosition;
                    mLastHitFrame = true;
                }

                whiteBoardHandler.RPC_DrawLerp(
                    (int)mLastHitPosition.x,
                    (int)mLastHitPosition.y,
                    (int)hitPosition.x,
                    (int)hitPosition.y,
                    penSize,
                    whiteBoardHandler.ColorPickerController.FinalColor
                );
                mLastHitPosition = hitPosition;
                lastDrawPosition = mHit.point;
            }
            else
            {
                whiteBoardHandler = null;
                mLastHitFrame = false;
                lastDrawPosition = Vector3.zero;
            }
        }

        private void HandleEraserDrawing()
        {
            if (Input.GetMouseButtonDown(0))
            {
                mIsDrawing = true;
                mLastHitFrame = false;
                lastDrawPosition = Vector3.zero;
                mLastEraserUpdate = 0f;
            }

            if (Input.GetMouseButtonUp(0))
            {
                mIsDrawing = false;
                mLastHitFrame = false;
                lastDrawPosition = Vector3.zero;
            }

            if (!mIsDrawing || !Physics.Raycast(markerTip.position, transform.up, out mHit, markerDistance))
                return;

            if (mHit.transform.CompareTag("Whiteboard"))
            {
                if (whiteBoardHandler == null)
                {
                    whiteBoardHandler = mHit.transform.GetComponent<WhiteboardHandler>();
                    InitializeSlider();
                }

                // Optimize update rate based on movement speed
                float minTimeBetweenUpdates = mInHighSpeedMode ?
                    1.0f / maxUpdatesPerSecondFastMode :
                    1.0f / maxUpdatesPerSecondNormalMode;

                // Dynamic time-based throttling
                if (Time.time - mLastEraserUpdate < minTimeBetweenUpdates)
                {
                    return;
                }

                // Dynamic distance-based throttling
                if (mLastEraserPosition != Vector3.zero &&
                    Vector3.Distance(mHit.point, mLastEraserPosition) < mEraserCurrentThreshold)
                {
                    return;
                }

                Vector2 hitPosition = new Vector2(
                    mHit.textureCoord.x * whiteBoardHandler.TextureSize.x - (eraserSize / 2),
                    mHit.textureCoord.y * whiteBoardHandler.TextureSize.y - (eraserSize / 2)
                );

                if (!mLastHitFrame)
                {
                    mLastHitPosition = hitPosition;
                    mLastHitFrame = true;
                }

                whiteBoardHandler.RPC_Erase(
                    (int)mLastHitPosition.x,
                    (int)mLastHitPosition.y,
                    (int)hitPosition.x,
                    (int)hitPosition.y,
                    eraserSize
                );

                mLastHitPosition = hitPosition;
                mLastEraserPosition = mHit.point;
                mLastEraserUpdate = Time.time;
            }
            else
            {
                whiteBoardHandler = null;
                mLastHitFrame = false;
                lastDrawPosition = Vector3.zero;
            }
        }
    }
}