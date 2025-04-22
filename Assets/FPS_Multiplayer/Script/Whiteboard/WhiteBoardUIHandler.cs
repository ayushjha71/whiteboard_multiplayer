using Fusion;
using UnityEngine;
using UnityEngine.UI;

namespace FPS_Multiplayer.Whiteboard
{
    public class WhiteBoardUIHandler : NetworkBehaviour
    {
        [SerializeField]
        private Button penButton;
        [SerializeField]
        private Button eraserButton;
        [SerializeField]
        private Button colorButton;
        [SerializeField]
        private Image penSelected;
        [SerializeField]
        private Image penUnSelected;
        [SerializeField]
        private Image eraserSelected;
        [SerializeField]
        private Image eraserUnSelected;
        [SerializeField]
        private GameObject sizeSliderPanel;
        [SerializeField]
        private Slider sizeSlider;
        [SerializeField]
        private GameObject penImage;
        [SerializeField]
        private GameObject eraserImage;
        [Header("Color Picker")]
        [SerializeField]
        private GameObject colorPickerPanel;
        [SerializeField]
        private WhiteboardHandler whiteboardHandler;

        // Default sizes
        [SerializeField]
        private int defaultPenSize = 5;

        // Min and max sizes
        [SerializeField]
        private int minPenSize = 5;
        [SerializeField]
        private int maxPenSize = 50;
        [SerializeField]
        private int minEraserSize = 50;
        [SerializeField]
        private int maxEraserSize = 100;

        public GameObject ColorPickerPanel => colorPickerPanel;

        public Slider SizeSlider
        {
            get
            {
                return sizeSlider;
            }
        }
        private bool _initialized = false;

        private void OnDisable()
        {
            if (_initialized)
            {
                penButton.onClick.RemoveListener(OnClickPenButton);
                eraserButton.onClick.RemoveListener(OnClickEraserButton);
                colorButton.onClick.RemoveListener(OnClickColorButton);
                sizeSlider.onValueChanged.RemoveAllListeners();
            }
        }

        private void Start()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            if (!_initialized)
            {
                penButton.onClick.AddListener(OnClickPenButton);
                eraserButton.onClick.AddListener(OnClickEraserButton);
                colorButton.onClick.AddListener(OnClickColorButton);
                _initialized = true;
            }

            penSelected.gameObject.SetActive(true);
            penUnSelected.gameObject.SetActive(false);
            eraserSelected.gameObject.SetActive(false);
            eraserUnSelected.gameObject.SetActive(true);

            sizeSlider.minValue = minPenSize;
            sizeSlider.maxValue = maxPenSize;
            sizeSlider.value = defaultPenSize;

            penImage.SetActive(true);
            eraserImage.SetActive(false);
            if (whiteboardHandler != null)
            {
                whiteboardHandler.IsErasing = false;
            }
        }

        public void UpdateValue(float value, ref int size, bool isErasing)
        {
            size = isErasing ?
                Mathf.Clamp(Mathf.RoundToInt(value), minEraserSize, maxEraserSize) :
                Mathf.Clamp(Mathf.RoundToInt(value), minPenSize, maxPenSize);

            penImage.SetActive(!isErasing);
            eraserImage.SetActive(isErasing);
        }

        private void OnClickPenButton()
        {
            DisablePanel();
            sizeSliderPanel.SetActive(true);
            penSelected.gameObject.SetActive(true);
            penUnSelected.gameObject.SetActive(false);
            eraserSelected.gameObject.SetActive(false);
            eraserUnSelected.gameObject.SetActive(true);

            whiteboardHandler.IsErasing = false;
            sizeSlider.minValue = minPenSize;
            sizeSlider.maxValue = maxPenSize;

            penImage.SetActive(true);
            eraserImage.SetActive(false);
        }

        private void OnClickEraserButton()
        {
            DisablePanel();
            sizeSliderPanel.SetActive(true);
            penSelected.gameObject.SetActive(false);
            penUnSelected.gameObject.SetActive(true);
            eraserSelected.gameObject.SetActive(true);
            eraserUnSelected.gameObject.SetActive(false);

            whiteboardHandler.IsErasing = true;
            sizeSlider.minValue = minEraserSize;
            sizeSlider.maxValue = maxEraserSize;

            penImage.SetActive(false);
            eraserImage.SetActive(true);
        }

        private void OnClickColorButton()
        {
            colorPickerPanel.SetActive(true);
            sizeSliderPanel.SetActive(false);
        }

        private void DisablePanel()
        {
            colorPickerPanel.SetActive(false);
            sizeSliderPanel.SetActive(false);
        }
    }
}