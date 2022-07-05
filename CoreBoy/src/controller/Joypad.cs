using System.Collections.Concurrent;
using System.Collections.Generic;
using CoreBoy.cpu;

namespace CoreBoy.controller
{
    public class Joypad : IAddressSpace
    {
        private readonly HashSet<Button> _buttons = new HashSet<Button>();
        private int _p1;

        public Joypad(InterruptManager interruptManager, IController controller)
        {
            controller.SetButtonListener(new JoyPadButtonListener(interruptManager, _buttons));
        }

        //0xFF00
        public bool Accepts(int address)
        {
            return address == 0xff00;
        }


        public void SetByte(int address, int value)
        {
            _p1 = value & 0b00110000;
        }

        public int GetByte(int address)
        {
            var result = _p1 | 0b11001111;

            lock (_buttons)
            {
                foreach (var b in _buttons)
                {
                    if ((b.Line & _p1) == 0)
                    {
                        result &= 0xff & ~b.Mask;
                    }
                }
            }

            return result;
        }
    }
}