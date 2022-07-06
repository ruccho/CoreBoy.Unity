using System;
using System.IO;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace CoreBoy.memory.cart.battery
{
    public class RawFileBattery : IBattery
    {
        private readonly FileInfo _saveFile;

        private byte[] temp = default;

        public RawFileBattery(string romName)
        {
            _saveFile = new FileInfo($"{romName}.sav");
        }

        public void LoadRam(int[] ram)
        {
            if (!_saveFile.Exists)
            {
                return;
            }

            if (temp == null || temp.Length < ram.Length) temp = new byte[ram.Length];

            using (var fs = new FileStream(_saveFile.FullName, FileMode.Open))
            {
                int headerSize = Marshal.SizeOf<Header>();
                Span<byte> headerBytes = stackalloc byte[headerSize];
                if (fs.Read(headerBytes) < headerSize) throw new InvalidDataException();

                if (!MemoryMarshal.TryRead(headerBytes, out Header header)) throw new InvalidDataException();

                if (ram.Length != header.RamLength) throw new InvalidDataException();

                long toRam = header.RamOffset - headerSize;
                if (toRam < 0) throw new InvalidDataException();

                if (fs.Seek(toRam, SeekOrigin.Current) != header.RamOffset) throw new InvalidDataException();

                if (fs.Read(temp) < header.RamLength) throw new InvalidDataException();

                for (int i = 0; i < ram.Length; i++)
                {
                    ram[i] = temp[i];
                }
            }
        }

        public void LoadRamWithClock(int[] ram, long[] clockData)
        {
            if (!_saveFile.Exists)
            {
                return;
            }

            if (temp == null || temp.Length < ram.Length) temp = new byte[ram.Length];

            using (var fs = new FileStream(_saveFile.FullName, FileMode.Open))
            {
                int headerSize = Marshal.SizeOf<Header>();
                Span<byte> headerBytes = stackalloc byte[headerSize];
                if (fs.Read(headerBytes) < headerSize) throw new InvalidDataException();

                if (!MemoryMarshal.TryRead(headerBytes, out Header header)) throw new InvalidDataException();

                if (ram.Length != header.RamLength) throw new InvalidDataException();
                if (clockData.Length * Marshal.SizeOf<long>() < header.ClockLength) throw new InvalidDataException();

                long toRam = header.RamOffset - headerSize;
                if (toRam < 0) throw new InvalidDataException();

                long ramToClock = (long)header.ClockOffset - (header.RamOffset + header.RamLength);
                if (ramToClock < 0) throw new InvalidDataException();

                if (fs.Seek(toRam, SeekOrigin.Current) != header.RamOffset) throw new InvalidDataException();

                if (fs.Read(temp.AsSpan(0, (int)header.RamLength)) < header.RamLength) throw new InvalidDataException();

                for (int i = 0; i < ram.Length; i++)
                {
                    ram[i] = temp[i];
                }

                if (fs.Seek(ramToClock, SeekOrigin.Current) != header.ClockOffset) throw new InvalidDataException();

                var clockDataSpan = MemoryMarshal.AsBytes(clockData.AsSpan(0, (int)header.ClockLength / Marshal.SizeOf<long>()));

                if (fs.Read(clockDataSpan) < header.ClockLength) throw new InvalidDataException();
            }
        }

        public void SaveRam(int[] ram)
        {
            SaveRamWithClock(ram, Array.Empty<long>());
        }

        public void SaveRamWithClock(int[] ram, long[] clockData)
        {
            if (temp == null || temp.Length < ram.Length) temp = new byte[ram.Length];

            using (var fs = new FileStream(_saveFile.FullName, FileMode.OpenOrCreate))
            {
                int headerSize = Marshal.SizeOf<Header>();
                Header header = new Header()
                {
                    HeaderSize = (uint)headerSize,
                    RamOffset = (uint)headerSize,
                    RamLength = (uint)ram.Length,
                    ClockOffset = (uint)(headerSize + ram.Length),
                    ClockLength = (uint)(clockData.Length * Marshal.SizeOf<long>())
                };
                Span<byte> headerBytes = stackalloc byte[headerSize];
                MemoryMarshal.Write(headerBytes, ref header);

                fs.Write(headerBytes);

                for (int i = 0; i < ram.Length; i++)
                {
                    temp[i] = (byte)ram[i];
                }

                fs.Write(temp.AsSpan(0, ram.Length));
                var clockDataSpan = MemoryMarshal.AsBytes(clockData.AsSpan());
                fs.Write(clockDataSpan);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Header
        {
            public uint HeaderSize { get; set; }
            public uint RamOffset { get; set; }
            public uint RamLength { get; set; }
            public uint ClockOffset { get; set; }
            public uint ClockLength { get; set; }
        }
    }
}