using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreBoy.memory
{
    public class MemoryRegisters : IAddressSpace
    {
        private readonly RegisterType[] _allowsWrite = { RegisterType.W, RegisterType.RW };
        private readonly RegisterType[] _allowsRead = { RegisterType.R, RegisterType.RW };

        private int dataOffset = 0;
        private (IRegister, int)[] data = default;

        public MemoryRegisters(int offset, int length, params IRegister[] registers)
        {
            dataOffset = offset;
            data = new (IRegister, int)[length];

            foreach (var r in registers)
            {
                int index = r.Address - offset;
                if (index < 0 || data.Length <= index)
                    throw new ArgumentOutOfRangeException(nameof(r.Address), r.Address,
                        $"Address of the register must be within the range: offset: 0x{offset:X4}, length: 0x{length:X}");

                if (data[index].Item1 != null)
                    throw new ArgumentException($"Two registers with the same address: {r.Address}");

                data[index] = (r, 0);
            }
        }

        private MemoryRegisters(MemoryRegisters original)
        {
            data = new (IRegister, int)[original.data.Length];
            Array.Copy(original.data, data, data.Length);
        }

        private (IRegister, int) this[int address]
        {
            get
            {
                int index = address - dataOffset;
                /*if (index < 0 || data.Length <= index)
                    throw new ArgumentException($"Not valid register: 0x{address:X4}");*/

                return data[index];
            }

            set
            {
                int index = address - dataOffset;
                /*if (index < 0 || data.Length <= index)
                    throw new ArgumentException($"Not valid register: 0x{address:X4}");*/

                data[index] = value;
            }
        }

        public int Get(IRegister reg)
        {
            var t = this[reg.Address];
            if (t.Item1 == null) throw new ArgumentException($"Not valid register: 0x{reg.Address:X4}");
            return t.Item2;
        }

        public void Put(IRegister reg, int value)
        {
            var t = this[reg.Address];

            if (t.Item1 == null) throw new ArgumentException("Not valid register: " + reg);

            this[reg.Address] = (t.Item1, value);
        }

        public MemoryRegisters Freeze() => new MemoryRegisters(this);

        public int PreIncrement(IRegister reg)
        {
            var t = this[reg.Address];

            if (t.Item1 == null) throw new ArgumentException("Not valid register: " + reg);

            var value = t.Item2 + 1;

            this[reg.Address] = (t.Item1, value);

            return value;
        }

        public bool Accepts(int address)
        {
            int index = address - dataOffset;
            if (index < 0 || data.Length <= index) return false;

            return data[index].Item1 != null;
        }

        public void SetByte(int address, int value)
        {
            var t = this[address];

            if (t.Item1 == null) throw new ArgumentException($"Not valid register: 0x{address:X4}");

            int index = address - dataOffset;

            var ttype = data[index].Item1.Type;
            foreach (var type in _allowsWrite)
            {
                if (type == ttype)
                {
                    this[address] = (t.Item1, value);
                    return;
                }
            }
        }

        public int GetByte(int address)
        {
            var t = this[address];

            var ttype = t.Item1.Type;
            foreach (var type in _allowsRead)
            {
                if (type == ttype) return t.Item2;
            }

            return 0xff;
        }
    }
}