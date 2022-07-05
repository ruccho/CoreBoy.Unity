using System;
using System.Collections;
using System.Collections.Generic;
using CoreBoy.controller;
using UnityEngine;

namespace CoreBoy.Unity
{
    public class CoreBoyInput : MonoBehaviour, IController
    {
        private IButtonListener buttonListener = default;

        private ButtonBinding[] bindings = new[]
        {
            new ButtonBinding(KeyCode.Z, Button.A),
            new ButtonBinding(KeyCode.X, Button.B),
            new ButtonBinding(KeyCode.Return, Button.Start),
            new ButtonBinding(KeyCode.RightShift, Button.Select),
            new ButtonBinding(KeyCode.UpArrow, Button.Up),
            new ButtonBinding(KeyCode.DownArrow, Button.Down),
            new ButtonBinding(KeyCode.LeftArrow, Button.Left),
            new ButtonBinding(KeyCode.RightArrow, Button.Right),
        };
        
        public void SetButtonListener(IButtonListener listener)
        {
            buttonListener = listener;
        }

        private void Update()
        {
            if (buttonListener == null) return;
            foreach(var binding in bindings) binding.Update(buttonListener);
        }

        class ButtonBinding
        {
            public KeyCode KeyCode { get; }
            public Button BoundButton { get; }

            private bool prevPressed = false;
            
            public ButtonBinding(KeyCode keyCode, Button button)
            {
                KeyCode = keyCode;
                BoundButton = button;
            }

            public void Update(IButtonListener listener)
            {
                bool pressed = Input.GetKey(KeyCode);

                if (pressed != prevPressed)
                {
                    if(pressed) listener.OnButtonPress(BoundButton);
                    else listener.OnButtonRelease(BoundButton);
                    prevPressed = pressed;
                }
                
            }
        }
    }
}