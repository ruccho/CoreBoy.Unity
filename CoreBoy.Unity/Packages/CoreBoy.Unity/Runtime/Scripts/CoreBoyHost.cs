using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using CoreBoy.controller;
using CoreBoy.gpu;
using CoreBoy.gui;
using CoreBoy.memory.cart;
using UnityEngine;
using UnityEngine.Profiling;
using File = UnityEngine.Windows.File;
using Random = UnityEngine.Random;

namespace CoreBoy.Unity
{
    public class CoreBoyHost : MonoBehaviour
    {
        public RomLoadingModeType RomLoadingMode
        {
            get => romLoadingMode;
            set => romLoadingMode = value;
        }

        public Emulator Emulator { get; private set; }

        [SerializeField] private bool startOnAwake = true;

        [SerializeField] private RomLoadingModeType romLoadingMode = RomLoadingModeType.Filename;
        [SerializeField] private string filename = default;
        [SerializeField] private TextAsset romBytes = default;

        [SerializeField] private CoreBoySoundOutput soundOutput = default;
        [SerializeField] private CoreBoyDisplay display = default;
        [SerializeField] private CoreBoyInput input = default;

        private CancellationTokenSource cancelCurrentEmulator = default;

        private void Start()
        {
            if (startOnAwake)
            {
                StartEmulator();
            }
        }

        public void StartEmulator(string romName = null, byte[] rom = null)
        {
            if (rom == null)
            {
                (rom, romName) = LoadRom();
            }
            else if (string.IsNullOrEmpty(romName)) throw new ArgumentException();

            StopEmulator();

            cancelCurrentEmulator = new CancellationTokenSource();

            var options = new GameboyOptions();

            Emulator = new Emulator(options)
            {
                Display = display,
                SoundOutput = soundOutput,
                Controller = input,
                Rom = new Cartridge(options, romName, rom)
            };

            Emulator.BeginGameboyThread += gameboy => Profiler.BeginThreadProfiling("CoreBoy", "Gameboy");
            Emulator.FinishGameboyThread += gameboy => Profiler.EndThreadProfiling();

            Emulator.Run(cancelCurrentEmulator.Token);
        }

        public void StopEmulator()
        {
            if (Emulator != null && Emulator.Active)
            {
                Emulator.Stop(cancelCurrentEmulator);
            }
            else cancelCurrentEmulator?.Cancel();
        }


        private (byte[], string) LoadRom()
        {
            switch (romLoadingMode)
            {
                case RomLoadingModeType.Manual:
                    throw new InvalidOperationException();
                case RomLoadingModeType.Filename:
                    return (File.ReadAllBytes(filename), Path.GetFileNameWithoutExtension(filename));
                case RomLoadingModeType.StreamingAssets:
                    return (File.ReadAllBytes(Path.Combine(Application.streamingAssetsPath, filename)),
                        Path.GetFileNameWithoutExtension(filename));
                case RomLoadingModeType.TextAsset:
                    return (romBytes.bytes, romBytes.name);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnDestroy()
        {
            StopEmulator();
        }


        public enum RomLoadingModeType
        {
            Manual,
            Filename,
            StreamingAssets,
            TextAsset,
        }
    }
}