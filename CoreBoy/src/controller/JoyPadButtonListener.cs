using System.Collections.Concurrent;
using System.Collections.Generic;
using CoreBoy.cpu;

namespace CoreBoy.controller
{
    public class JoyPadButtonListener : IButtonListener
    {
        private readonly InterruptManager _interruptManager;
        //private readonly ConcurrentDictionary<Button, Button> _buttons;
        private readonly HashSet<Button> _buttons;

        public JoyPadButtonListener(InterruptManager interruptManager, HashSet<Button> buttons)
        {
            _interruptManager = interruptManager;
            _buttons = buttons;
        }

        public void OnButtonPress(Button button)
        {
            if (button != null)
            {
                _interruptManager.RequestInterrupt(InterruptManager.InterruptType.P1013);
                lock (_buttons)
                {
                    _buttons.Add(button);
                }
            }
        }

        public void OnButtonRelease(Button button)
        {
            if (button != null)
            {
                lock (_buttons)
                {
                    _buttons.Remove(button);
                }
            }
        }
    }
}