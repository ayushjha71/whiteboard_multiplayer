using Fusion;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace FPS_Multiplayer.Whiteboard.ColorPicker
{
    public class ColorSelectionHandler : NetworkBehaviour, IDragHandler, IPointerClickHandler
    {
        [SerializeField] private Image pickerImage;
        [SerializeField] private RawImage svImage;
        [SerializeField] private ColorPickerController mColorPickerController;


        private RectTransform mPickerTransform;
        private RectTransform mSVImageTransform;

        public override void Spawned()
        {
            mPickerTransform = pickerImage.GetComponent<RectTransform>();
            mSVImageTransform = svImage.GetComponent<RectTransform>();

            // Initialize picker position (center of SVImage by default)
            Vector2 initialPosition = new Vector2(0, 0);
            mPickerTransform.localPosition = initialPosition;

            // Initialize with default values
            if (Object.HasInputAuthority && mColorPickerController != null)
            {
                mColorPickerController.SetSV(0.5f, 0.5f);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                mSVImageTransform, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                UpdatePickerPosition(localPoint);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                mSVImageTransform, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                UpdatePickerPosition(localPoint);
            }
        }

        private void UpdatePickerPosition(Vector2 localPoint)
        {
            // Clamp within SV image bounds
            float halfWidth = mSVImageTransform.rect.width * 0.5f;
            float halfHeight = mSVImageTransform.rect.height * 0.5f;
            localPoint.x = Mathf.Clamp(localPoint.x, -halfWidth, halfWidth);
            localPoint.y = Mathf.Clamp(localPoint.y, -halfHeight, halfHeight);

            // Calculate normalized position (0-1) for saturation and value
            float normX = Mathf.InverseLerp(-halfWidth, halfWidth, localPoint.x);
            float normY = Mathf.InverseLerp(-halfHeight, halfHeight, localPoint.y);

            // Send RPC to update all clients
            RPC_UpdatePickerPosition(localPoint, normX, normY);
        }

        private void RPC_UpdatePickerPosition(Vector2 pickerPosition, float normX, float normY)
        {
            // Update picker position
            mPickerTransform.localPosition = pickerPosition;

            // Update the color in the controller
            if (mColorPickerController != null)
            {
                mColorPickerController.SetSV(normX, normY);
            }
            else
            {
                Debug.LogError("ColorPickerController is not assigned!");
            }
        }
    }
}