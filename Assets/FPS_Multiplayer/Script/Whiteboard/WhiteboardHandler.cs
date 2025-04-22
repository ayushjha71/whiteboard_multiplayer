using Fusion;
using UnityEngine;
using Cinemachine;
using UnityEngine.UI;
using System.Collections.Generic;
using FPS_Multiplayer.Whiteboard.ColorPicker;

namespace FPS_Multiplayer.Whiteboard
{
    public class WhiteboardHandler : NetworkBehaviour
    {
        [SerializeField] private GameObject buttonPanel;
        [SerializeField] private ColorPickerController colorPickerController;
        [SerializeField] private WhiteBoardUIHandler whiteBoardUIHandler;

        [SerializeField] private Vector2 textureSize = new(2048, 2048);
        [SerializeField] private float updateInterval = 0.1f;
        // Optimization: Texture format
        [SerializeField] private bool useCompressedTexture = false;
        [SerializeField] private int mipMapBias = -1;

        [Space] public CinemachineVirtualCamera whiteboardVirtualCam;
        public Button whiteboardClose_Btn;
        public bool canDraw = false;



        private Texture2D mTexture;
        private Renderer whiteboardRenderer;
        private Color32[] mDrawBuffer;
        private bool mIsDrawing = false;
        private float mLastUpdateTime = 0f;
        private List<DrawingAction> drawingActions = new List<DrawingAction>();
        private List<EraserAction> eraserActions = new List<EraserAction>();

        // Optimization: Pre-calculate circular eraser pattern
        private Dictionary<int, Vector2Int[]> eraserPatterns = new Dictionary<int, Vector2Int[]>();

        // Optimization: Track dirty region for partial texture updates
        private bool isDirtyRegionValid = false;
        private int dirtyMinX, dirtyMinY, dirtyMaxX, dirtyMaxY;

        // Optimization: Multi-threading support
        private System.Threading.Thread updateThread;
        private bool isThreadRunning = false;
        private object threadLock = new object();

        // Optimization: Double buffer for eraser operations
        private Color32[] mTempBuffer;
        private bool useDoubleBuffering = true;

        // Optimization: Batch operations
        private const int MAX_BATCH_SIZE = 100;
        private Queue<EraserAction> pendingEraserActions = new Queue<EraserAction>();

        public bool IsErasing { get; set; }
        public Vector2 TextureSize
        {
            get { return textureSize; }
            set { textureSize = value; }
        }
        public ColorPickerController ColorPickerController
        {
            get { return colorPickerController; }
        }
        public WhiteBoardUIHandler WhiteBoardUIHandler
        {
            get { return whiteBoardUIHandler; }
        }

        private void OnEnable()
        {
            buttonPanel.SetActive(true);
            whiteBoardUIHandler.ColorPickerPanel.SetActive(false);
            if (useDoubleBuffering)
            {
                mTempBuffer = new Color32[(int)textureSize.x * (int)textureSize.y];
            }
        }

        private void OnDisable()
        {
            StopUpdateThread();
        }

        private void StopUpdateThread()
        {
            if (isThreadRunning && updateThread != null)
            {
                isThreadRunning = false;
                updateThread.Join(100); // Wait for thread to finish
                updateThread = null;
            }
        }

        public override void Spawned()
        {
            base.Spawned();
            whiteboardRenderer = GetComponent<Renderer>();

            // Create texture with optimal format for real-time updates
            TextureFormat format = useCompressedTexture ? TextureFormat.DXT1 : TextureFormat.RGBA32;
            bool mipmaps = useCompressedTexture;

            mTexture = new Texture2D((int)textureSize.x, (int)textureSize.y, format, mipmaps);
            mTexture.filterMode = FilterMode.Bilinear;

            if (mipMapBias != 0)
                mTexture.mipMapBias = mipMapBias;

            mDrawBuffer = new Color32[mTexture.width * mTexture.height];
            for (int i = 0; i < mDrawBuffer.Length; i++)
            {
                mDrawBuffer[i] = Color.white;
            }

            if (useDoubleBuffering)
            {
                System.Array.Copy(mDrawBuffer, mTempBuffer, mDrawBuffer.Length);
            }

            mTexture.SetPixels32(mDrawBuffer);
            mTexture.Apply(mipmaps);

            whiteboardRenderer.material.mainTexture = mTexture;

            if (!Object.HasStateAuthority)
            {
                foreach (var action in drawingActions)
                {
                    ApplyDraw(action);
                }

                foreach (var action in eraserActions)
                {
                    ApplyErase(action);
                }
            }

            // Optimization: Precompute eraser patterns for commonly used sizes
            PrecomputeEraserPatterns();
        }

        // Optimization: Precompute circular patterns for common eraser sizes
        private void PrecomputeEraserPatterns()
        {
            // Precompute patterns for a range of eraser sizes
            for (int size = 10; size <= 50; size += 5)
            {
                int radius = size / 2;
                var pattern = new List<Vector2Int>();

                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (x * x + y * y <= radius * radius)
                        {
                            pattern.Add(new Vector2Int(x, y));
                        }
                    }
                }

                eraserPatterns[size] = pattern.ToArray();
            }
        }

        // Optimization: Get or create eraser pattern for a given size
        private Vector2Int[] GetEraserPattern(float eraserSize)
        {
            int size = Mathf.RoundToInt(eraserSize);

            // Round to nearest precomputed size
            int nearestSize = Mathf.RoundToInt(size / 5f) * 5;
            nearestSize = Mathf.Clamp(nearestSize, 10, 50);

            if (!eraserPatterns.ContainsKey(nearestSize))
            {
                PrecomputeEraserPatterns(); // Ensure patterns are created
            }

            return eraserPatterns[nearestSize];
        }

        private void Update()
        {
            ProcessPendingEraserActions();

            // Optimization: Only apply texture changes when needed and potentially use partial updates
            if (mIsDrawing && Time.time - mLastUpdateTime >= updateInterval)
            {
                ApplyTextureChanges();
                mLastUpdateTime = Time.time;
                mIsDrawing = false;
            }
        }

        // Optimization: Process pending eraser actions in batches
        private void ProcessPendingEraserActions()
        {
            if (pendingEraserActions.Count == 0)
                return;

            int count = Mathf.Min(pendingEraserActions.Count, MAX_BATCH_SIZE);
            for (int i = 0; i < count; i++)
            {
                if (pendingEraserActions.Count > 0)
                {
                    EraserAction action = pendingEraserActions.Dequeue();
                    ApplyErase(action);
                }
            }

            if (pendingEraserActions.Count > 0)
                mIsDrawing = true;
        }

        // Optimization: Apply changes to texture with potential partial updates
        private void ApplyTextureChanges()
        {
            if (isDirtyRegionValid)
            {
                // Clamp dirty region to texture bounds
                dirtyMinX = Mathf.Clamp(dirtyMinX, 0, (int)textureSize.x - 1);
                dirtyMinY = Mathf.Clamp(dirtyMinY, 0, (int)textureSize.y - 1);
                dirtyMaxX = Mathf.Clamp(dirtyMaxX, 0, (int)textureSize.x - 1);
                dirtyMaxY = Mathf.Clamp(dirtyMaxY, 0, (int)textureSize.y - 1);

                // Calculate update region dimensions
                int width = dirtyMaxX - dirtyMinX + 1;
                int height = dirtyMaxY - dirtyMinY + 1;

                // Check that dimensions are valid
                if (width > 0 && height > 0)
                {
                    // Only use partial update if the dirty region is significantly smaller than full texture
                    if (width * height < mTexture.width * mTexture.height * 0.5f)
                    {
                        Color32[] regionPixels = new Color32[width * height];
                        int index = 0;

                        for (int y = dirtyMinY; y <= dirtyMaxY; y++)
                        {
                            for (int x = dirtyMinX; x <= dirtyMaxX; x++)
                            {
                                int bufferIndex = y * mTexture.width + x;
                                if (bufferIndex >= 0 && bufferIndex < mDrawBuffer.Length)
                                {
                                    regionPixels[index++] = mDrawBuffer[bufferIndex];
                                }
                            }
                        }

                        // Safety check to ensure we're not trying to set pixels outside texture bounds
                        if (dirtyMinX + width <= mTexture.width && dirtyMinY + height <= mTexture.height)
                        {
                            mTexture.SetPixels32(dirtyMinX, dirtyMinY, width, height, regionPixels);
                            mTexture.Apply(false);
                        }
                        else
                        {
                            // Fall back to full texture update if region is invalid
                            mTexture.SetPixels32(mDrawBuffer);
                            mTexture.Apply(false);
                        }
                    }
                    else
                    {
                        mTexture.SetPixels32(mDrawBuffer);
                        mTexture.Apply(false);
                    }
                }
                else
                {
                    // Invalid dimensions, update full texture
                    mTexture.SetPixels32(mDrawBuffer);
                    mTexture.Apply(false);
                }

                isDirtyRegionValid = false;
            }
            else
            {
                mTexture.SetPixels32(mDrawBuffer);
                mTexture.Apply(false);
            }
        }

        [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true)]
        public void RPC_DrawLerp(int startX, int startY, int endX, int endY, int penSize, Color color)
        {
            DrawingAction action = new DrawingAction(startX, startY, endX, endY, penSize, color);
            drawingActions.Add(action);
            ApplyDraw(action);
        }

        [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true)]
        public void RPC_Erase(int startX, int startY, int endX, int endY, float eraserSize)
        {
            EraserAction action = new EraserAction(startX, startY, endX, endY, eraserSize);
            eraserActions.Add(action);

            // Optimization: Queue large eraser operations for batch processing
            if (eraserSize > 30)
            {
                pendingEraserActions.Enqueue(action);
                mIsDrawing = true;
            }
            else
            {
                ApplyErase(action);
            }
        }

        private void ApplyDraw(DrawingAction action)
        {
            Color32[] colors = new Color32[action.PenSize * action.PenSize];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = action.Color;

            int dx = action.EndX - action.StartX;
            int dy = action.EndY - action.StartY;

            // Calculate number of steps based on the longer distance
            int steps = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
            steps = Mathf.Max(steps, 1);

            float stepX = dx / (float)steps;
            float stepY = dy / (float)steps;

            int textureWidth = (int)textureSize.x;
            int textureHeight = (int)textureSize.y;

            // Initialize dirty region with first point
            if (!isDirtyRegionValid)
            {
                dirtyMinX = action.StartX;
                dirtyMinY = action.StartY;
                dirtyMaxX = action.StartX + action.PenSize;
                dirtyMaxY = action.StartY + action.PenSize;
                isDirtyRegionValid = true;
            }

            for (int i = 0; i <= steps; i++)
            {
                int x = Mathf.RoundToInt(action.StartX + (stepX * i));
                int y = Mathf.RoundToInt(action.StartY + (stepY * i));

                if (x < 0 || x >= textureWidth - action.PenSize ||
                    y < 0 || y >= textureHeight - action.PenSize)
                    continue;

                // Update dirty region
                dirtyMinX = Mathf.Min(dirtyMinX, x);
                dirtyMinY = Mathf.Min(dirtyMinY, y);
                dirtyMaxX = Mathf.Max(dirtyMaxX, x + action.PenSize);
                dirtyMaxY = Mathf.Max(dirtyMaxY, y + action.PenSize);

                for (int py = 0; py < action.PenSize; py++)
                {
                    for (int px = 0; px < action.PenSize; px++)
                    {
                        int bufferX = x + px;
                        int bufferY = y + py;
                        int bufferIndex = bufferY * textureWidth + bufferX;

                        if (bufferIndex >= 0 && bufferIndex < mDrawBuffer.Length)
                        {
                            mDrawBuffer[bufferIndex] = action.Color;
                        }
                    }
                }
            }

            mIsDrawing = true;
        }

        private void ApplyErase(EraserAction action)
        {
            int dx = action.EndX - action.StartX;
            int dy = action.EndY - action.StartY;

            int steps = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
            steps = Mathf.Max(steps, 1);

            float stepX = dx / (float)steps;
            float stepY = dy / (float)steps;

            int textureWidth = (int)textureSize.x;
            int textureHeight = (int)textureSize.y;

            // Optimization: Get precomputed eraser pattern
            Vector2Int[] eraserPattern = GetEraserPattern(action.EraserSize);

            // Optimization: Skip intermediate points based on eraser size for improved performance
            int stride = Mathf.Max(1, Mathf.RoundToInt(action.EraserSize / 8));

            // Initialize dirty region with first point
            if (!isDirtyRegionValid)
            {
                int radius = Mathf.RoundToInt(action.EraserSize / 2);
                dirtyMinX = action.StartX - radius;
                dirtyMinY = action.StartY - radius;
                dirtyMaxX = action.StartX + radius;
                dirtyMaxY = action.StartY + radius;
                isDirtyRegionValid = true;
            }

            // Optimization: Use double buffering if enabled
            Color32[] workBuffer = useDoubleBuffering ? mTempBuffer : mDrawBuffer;

            if (useDoubleBuffering)
            {
                System.Array.Copy(mDrawBuffer, workBuffer, mDrawBuffer.Length);
            }

            // Local reference for performance
            Vector2Int[] pattern = eraserPattern;
            int patternLength = pattern.Length;

            for (int i = 0; i <= steps; i += stride)
            {
                int centerX = Mathf.RoundToInt(action.StartX + (stepX * i));
                int centerY = Mathf.RoundToInt(action.StartY + (stepY * i));

                // Update dirty region
                int radius = Mathf.RoundToInt(action.EraserSize / 2);
                dirtyMinX = Mathf.Min(dirtyMinX, centerX - radius);
                dirtyMinY = Mathf.Min(dirtyMinY, centerY - radius);
                dirtyMaxX = Mathf.Max(dirtyMaxX, centerX + radius);
                dirtyMaxY = Mathf.Max(dirtyMaxY, centerY + radius);

                // Clamp dirty region to texture bounds
                dirtyMinX = Mathf.Max(0, dirtyMinX);
                dirtyMinY = Mathf.Max(0, dirtyMinY);
                dirtyMaxX = Mathf.Min(textureWidth - 1, dirtyMaxX);
                dirtyMaxY = Mathf.Min(textureHeight - 1, dirtyMaxY);

                // Optimization: Fast loop with minimal bounds checking
                unsafe
                {
                    fixed (Color32* bufPtr = workBuffer)
                    {
                        for (int p = 0; p < patternLength; p++)
                        {
                            int bufferX = centerX + pattern[p].x;
                            int bufferY = centerY + pattern[p].y;

                            if (bufferX >= 0 && bufferX < textureWidth &&
                                bufferY >= 0 && bufferY < textureHeight)
                            {
                                int bufferIndex = bufferY * textureWidth + bufferX;
                                bufPtr[bufferIndex] = Color.white;
                            }
                        }
                    }
                }
            }

            // Copy modified work buffer back to main buffer if using double buffering
            if (useDoubleBuffering)
            {
                System.Array.Copy(workBuffer, mDrawBuffer, mDrawBuffer.Length);
            }

            mIsDrawing = true;
        }

        private struct DrawingAction
        {
            public int StartX, StartY, EndX, EndY, PenSize;
            public Color Color;

            public DrawingAction(int startX, int startY, int endX, int endY, int penSize, Color color)
            {
                StartX = startX;
                StartY = startY;
                EndX = endX;
                EndY = endY;
                PenSize = penSize;
                Color = color;
            }
        }

        private struct EraserAction
        {
            public int StartX, StartY, EndX, EndY;
            public float EraserSize;

            public EraserAction(int startX, int startY, int endX, int endY, float eraserSize)
            {
                StartX = startX;
                StartY = startY;
                EndX = endX;
                EndY = endY;
                EraserSize = eraserSize;
            }
        }
    }
}