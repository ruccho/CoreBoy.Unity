using System;
using System.IO;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace CoreBoy.memory.cart.battery
{
    public class RawFileBattery : IBattery, IDisposable
    {
        private readonly FileInfo _saveFile;

        private byte[] temp = default;
        private readonly bool saveImmediately = false;

        private readonly object cacheLocker = new object();

        private int[] ramCache = default;
        private int ramLength = default;
        private long[] clockDataCache = default;
        private int clockDataLength = default;

        public RawFileBattery(string romName, bool saveImmediately)
        {
            _saveFile = new FileInfo($"{romName}.sav");
            this.saveImmediately = saveImmediately;
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

                if (ram.Length != header.RamSize) throw new InvalidDataException();

                long toRam = header.RamOffset - headerSize;
                if (toRam < 0) throw new InvalidDataException();

                if (fs.Seek(toRam, SeekOrigin.Current) != header.RamOffset) throw new InvalidDataException();

                if (fs.Read(temp) < header.RamSize) throw new InvalidDataException();

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

                if (ram.Length != header.RamSize) throw new InvalidDataException();
                if (clockData.Length * Marshal.SizeOf<long>() < header.ClockSize) throw new InvalidDataException();

                long toRam = header.RamOffset - headerSize;
                if (toRam < 0) throw new InvalidDataException();

                long ramToClock = (long)header.ClockOffset - (header.RamOffset + header.RamSize);
                if (ramToClock < 0) throw new InvalidDataException();

                if (fs.Seek(toRam, SeekOrigin.Current) != header.RamOffset) throw new InvalidDataException();

                if (fs.Read(temp.AsSpan(0, (int)header.RamSize)) < header.RamSize) throw new InvalidDataException();

                for (int i = 0; i < ram.Length; i++)
                {
                    ram[i] = temp[i];
                }

                if (fs.Seek(ramToClock, SeekOrigin.Current) != header.ClockOffset) throw new InvalidDataException();

                var clockDataSpan =
                    MemoryMarshal.AsBytes(clockData.AsSpan(0, (int)header.ClockSize / Marshal.SizeOf<long>()));

                if (fs.Read(clockDataSpan) < header.ClockSize) throw new InvalidDataException();
            }
        }

        public void SaveRam(int[] ram)
        {
            SaveRamWithClock(ram, Array.Empty<long>());
        }

        public void SaveRamWithClock(int[] ram, long[] clockData)
        {
            if (temp == null || temp.Length < ram.Length) temp = new byte[ram.Length];

            int headerSize = Marshal.SizeOf<Header>();
            int ramSize = ram.Length;
            int clockSize = clockData.Length * Marshal.SizeOf<long>();
            int fileSize = headerSize + ramSize + clockSize;

            if (saveImmediately)
            {
                using var fs = new FileStream(_saveFile.FullName, FileMode.OpenOrCreate);
                WriteToStream(ram, clockData, fs);
            }
            else
            {
                lock (cacheLocker)
                {
                    if (ramCache == null || ramCache.Length < ram.Length) ramCache = new int[ram.Length];
                    if (clockDataCache == null || clockDataCache.Length < clockData.Length)
                        clockDataCache = new long[clockData.Length];
                    Array.Copy(ram, ramCache, ram.Length);
                    Array.Copy(clockData, clockDataCache, clockData.Length);
                    ramLength = ram.Length;
                    clockDataLength = clockData.Length;
                }
            }
        }

        private void WriteToStream(Span<int> ram, Span<long> clockData, Stream stream)
        {
            int headerSize = Marshal.SizeOf<Header>();
            int ramSize = ram.Length;
            int clockSize = clockData.Length * Marshal.SizeOf<long>();

            Header header = new Header()
            {
                HeaderSize = (uint)headerSize,
                RamOffset = (uint)headerSize,
                RamSize = (uint)ramSize,
                ClockOffset = (uint)(headerSize + ramSize),
                ClockSize = (uint)clockSize
            };
            Span<byte> headerBytes = stackalloc byte[headerSize];
            MemoryMarshal.Write(headerBytes, ref header);

            stream.Write(headerBytes);

            for (int i = 0; i < ram.Length; i++)
            {
                temp[i] = (byte)ram[i];
            }

            stream.Write(temp.AsSpan(0, ram.Length));
            var clockDataSpan = MemoryMarshal.AsBytes(clockData);
            stream.Write(clockDataSpan);
        }

        public void Dispose()
        {
            using var fs = new FileStream(_saveFile.FullName, FileMode.OpenOrCreate);
            lock (cacheLocker)
            {
                WriteToStream(ramCache.AsSpan(0, ramLength), clockDataCache.AsSpan(0, clockDataLength), fs);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Header
        {
            public uint HeaderSize { get; set; }
            public uint RamOffset { get; set; }
            public uint RamSize { get; set; }
            public uint ClockOffset { get; set; }
            public uint ClockSize { get; set; }
        }
    }
}