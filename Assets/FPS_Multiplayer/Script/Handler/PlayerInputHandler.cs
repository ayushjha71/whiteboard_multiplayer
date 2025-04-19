using Fusion;
using UnityEngine;

namespace FPS_Multiplayer.Handler
{
    public class PlayerInputHandler : NetworkBehaviour
    {
        public Vector2 MoveInput
        {
            get;
            private set;
        }

        public Vector2 LookInput 
        { 
            get; 
            private set; 
        }

        public bool SprintInput
        {
            get;
            private set;
        }

        public bool JumpInput
        {
            get;
            private set;
        }

        public bool AnalogMovement;

        public override void FixedUpdateNetwork()
        {
            MoveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            LookInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        }

        private void Update()
        {
            if (Input.GetButtonDown("Jump"))
            {
                JumpInput = true;
            }
            if (Input.GetButtonUp("Jump"))
            {
                JumpInput = false;
            }

            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                SprintInput = true;
            }

            if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                SprintInput = false;
            }
        }
    }
}
