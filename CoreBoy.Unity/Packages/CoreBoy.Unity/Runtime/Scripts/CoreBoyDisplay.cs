using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using CoreBoy.gpu;
using CoreBoy.gui;
using UnityEngine;
using UnityEngine.UI;

namespace CoreBoy.Unity
{
    public class CoreBoyDisplay : MonoBehaviour, IDisplay
    {
        private Renderer renderer = default;
        private RawImage rawImage = default;

        private bool latestFrameApplied = true;
        private int workingPosition = 0;
        
        private readonly Color32[] latestFrame = new Color32[GameboyDisplayFrame.DisplayWidth * GameboyDisplayFrame.DisplayHeight];
        private readonly Color32[] workingFrame = new Color32[GameboyDisplayFrame.DisplayWidth * GameboyDisplayFrame.DisplayHeight];
        private Texture2D targetTexture = default;

        private readonly object updateFrameLocker = new object();

        private void OnEnable()
        {
            renderer = GetComponent<Renderer>();
            rawImage = GetComponent<RawImage>();
        }

        private void Update()
        {
            int width = GameboyDisplayFrame.DisplayWidth;
            int height = GameboyDisplayFrame.DisplayHeight;
            if (!targetTexture)
            {
                targetTexture = new Texture2D(GameboyDisplayFrame.DisplayWidth, GameboyDisplayFrame.DisplayHeight)
                {
                    filterMode = FilterMode.Point
                };

                if (renderer && renderer.material) renderer.material.mainTexture = targetTexture;
                if (rawImage) rawImage.texture = targetTexture;
            }

            lock (updateFrameLocker)
            {
                if (latestFrameApplied) return;
                if (latestFrame == null) return;
                if (latestFrame.Length != width * height)
                {
                    Debug.LogWarning("Length of the frame is incorrect.");
                    return;
                }

                targetTexture.SetPixels32(latestFrame);
                targetTexture.Apply();
                latestFrameApplied = true;
            }
        }

        public static readonly Color32[] DmgColors =
        {
            new Color32(0xe6, 0xf8, 0xda, 0xff),
            new Color32(0x99, 0xc8, 0x86, 0xff),
            new Color32(0x43, 0x79, 0x69, 0xff),
            new Color32(0x05, 0x1f, 0x2a, 0xff),
        };
        
        public void Run(CancellationToken token)
        {
        }

        public bool Enabled { get; set; }
        public event FrameProducedEventHandler OnFrameProduced;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PutDmgPixel(int color)
        {
            PushWorkingPixel(DmgColors[color]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PutColorPixel(int gbcRgb)
        {
            var r = ((gbcRgb >> 0) & 0x1f) << 3;
            var g = ((gbcRgb >> 5) & 0x1f) << 3;
            var b = ((gbcRgb >> 10) & 0x1f) << 3;
            
            PushWorkingPixel(new Color32((byte)r, (byte)g, (byte)b, 255));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PushWorkingPixel(Color32 col)
        {
            workingFrame[workingPosition++] = col;
            workingPosition %= workingFrame.Length;
        }

        public void RequestRefresh()
        {
            lock (updateFrameLocker)
            {
                Array.Copy(workingFrame, latestFrame, latestFrame.Length);
                latestFrameApplied = false;
                workingPosition = 0;
            }
        }

        public void WaitForRefresh()
        {
            lock (updateFrameLocker)
            {
                return;
            }
        }
    }
}