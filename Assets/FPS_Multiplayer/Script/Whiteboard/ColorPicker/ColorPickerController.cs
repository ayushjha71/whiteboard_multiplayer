using Fusion;
using UnityEngine;
using UnityEngine.UI;

namespace FPS_Multiplayer.Whiteboard.ColorPicker
{
    public class ColorPickerController : NetworkBehaviour
    {
        [SerializeField]
        private float currentHue;
        [SerializeField]
        private float currentSat = 1;
        [SerializeField]
        private float currentVal = 1;
        [SerializeField]
        private RawImage hueImage;
        [SerializeField]
        private RawImage satValImage;
        [SerializeField]
        private RawImage outputImage;
        [SerializeField]
        private Slider hueSlider;


        private Texture2D mHueTexture;
        private Texture2D mSVTexture;
        private Texture2D mOutputTexture;

        public Color FinalColor
        {
            get;
            private set;
        }
        
        public override void Spawned()
        {
            CreateHueImage();
            CreateSVImage();
            CreateOutputImage();
            UpdateOutputImage();
        }

        private void CreateHueImage()
        {
            mHueTexture = new Texture2D(1, 16);
            mHueTexture.wrapMode = TextureWrapMode.Clamp;
            for (int i = 0; i < mHueTexture.height; i++)
            {
                mHueTexture.SetPixel(0, i, Color.HSVToRGB(Mathf.InverseLerp(0, mHueTexture.height, i), 1, 0.95f));
            }
            mHueTexture.Apply();
            hueImage.texture = mHueTexture;
        }
        private void CreateSVImage()
        {
            mSVTexture = new Texture2D(16, 16);
            mSVTexture.wrapMode = TextureWrapMode.Clamp;
            satValImage.texture = mSVTexture;
            UpdateSVImage();
            Debug.Log("Create SV Image Called");
        }
        private void CreateOutputImage()
        {
            mOutputTexture = new Texture2D(1, 16);
            mOutputTexture.wrapMode = TextureWrapMode.Clamp;
            outputImage.texture = mOutputTexture;
        }
        private void UpdateOutputImage()
        {
            if (mOutputTexture == null)
            {
                mOutputTexture = new Texture2D(1, 16);
                mOutputTexture.wrapMode = TextureWrapMode.Clamp;
            }
            Color currentColor = Color.HSVToRGB(currentHue, currentSat, currentVal);
            for (int i = 0; i < mOutputTexture.height; i++)
            {
                mOutputTexture.SetPixel(0, i, currentColor);
            }
            mOutputTexture.Apply();
            FinalColor = currentColor;
        }
        public void SetSV(float s, float v)
        {
            RPC_SetSV(s, v);
        }

        //[Rpc(RpcSources.InputAuthority, RpcTargets.All)]
        private void RPC_SetSV(float s, float v)
        {
            currentSat = s;
            currentVal = v;
            UpdateOutputImage();
        }
        public void UpdateSVImage()
        {
            RPC_UpdateSVImage(hueSlider.value);
        }

        private void RPC_UpdateSVImage(float hue)
        {
            currentHue = hue;
            for (int i = 0; i < mSVTexture.height; i++)
            {
                for (int j = 0; j < mSVTexture.width; j++)
                {
                    mSVTexture.SetPixel(j, i, Color.HSVToRGB(
                        currentHue,
                        Mathf.InverseLerp(0, mSVTexture.width, j),
                        Mathf.InverseLerp(0, mSVTexture.height, i)));
                }
            }
            mSVTexture.Apply();
            UpdateOutputImage();
        }
    }
}