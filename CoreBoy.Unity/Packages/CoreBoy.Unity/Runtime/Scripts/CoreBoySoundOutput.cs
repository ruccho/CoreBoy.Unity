using System;
using System.Collections;
using System.Collections.Generic;
using CoreBoy;
using CoreBoy.sound;
using UnityEngine;

namespace CoreBoy.Unity
{
    public class CoreBoySoundOutput : MonoBehaviour, ISoundOutput
    {
        private float tick;

        private int position = 0;
        private float[] stackedSamples = default;

        private int outputSampleRate = 0;

        private bool isInitialized = false;

        [SerializeField] private bool fixedSampleRate = true;
        [SerializeField] private bool showDebugGui = false;

        private readonly object updateSamplesLocker = new object();

        private void Start()
        {
            lock (updateSamplesLocker)
            {
                stackedSamples = new float[Gameboy.TicksPerSec * 2];
                outputSampleRate = AudioSettings.outputSampleRate;
                isInitialized = true;
            }
        }

        void ISoundOutput.Start()
        {
        }

        public void Stop()
        {
        }

        public void Play(int left, int right)
        {
            lock (updateSamplesLocker)
            {
                if (!isInitialized) return;

                if (stackedSamples.Length - 2 <= position * 2) return;

                stackedSamples[position * 2] = left / 255f;
                stackedSamples[position * 2 + 1] = right / 255f;
                position++;
            }
        }

        private int latestRequiredSamplesGameboy = 0;
        private int latestStackedSamplesGameboy = 0;
        private void OnGUI()
        {
            if (!showDebugGui) return;
            
            float w = 200;
            float h = 20;
            GUI.DrawTexture(new Rect(w, 0, 5, h), Texture2D.whiteTexture);
            GUI.color = Color.red;
            GUI.DrawTexture(new Rect(0, 0, w * ((float)latestRequiredSamplesGameboy / Gameboy.TicksPerSec), h * 0.5f), Texture2D.whiteTexture);
            GUI.color = Color.green;
            GUI.DrawTexture(new Rect(0, h * 0.5f, w * ((float)latestStackedSamplesGameboy / Gameboy.TicksPerSec), h*0.5f), Texture2D.whiteTexture);
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {   
            if (fixedSampleRate)
            {
                lock (updateSamplesLocker)
                {
                    if (!isInitialized) return;
                    
                    int requiredSamples = data.Length / channels;
                    int requiredSamplesGameboy = latestRequiredSamplesGameboy =
                        Mathf.FloorToInt(requiredSamples * ((float)Gameboy.TicksPerSec / outputSampleRate));

                    latestStackedSamplesGameboy = position;
                    
                    int remain = position - requiredSamplesGameboy;

                    if (remain < 0f)
                    {
                        
                        if (position == 0) return;
                
                        for (int i = 0; i < requiredSamples; i++)
                        {
                            int t = Mathf.FloorToInt((float)i / requiredSamples * position);

                            for (int c = 0; c < channels; c++)
                            {
                                data[i * channels + c] = stackedSamples[t * 2 + (c % 2)];
                            }
                        }

                        position = 0;
                    }
                    else
                    {
                        for (int i = 0; i < requiredSamples; i++)
                        {
                            int t = Mathf.FloorToInt((float)i / requiredSamples * requiredSamplesGameboy);

                            for (int c = 0; c < channels; c++)
                            {
                                data[i * channels + c] = stackedSamples[t * 2 + (c % 2)];
                            }
                        }

                        for (int i = 0; i < remain; i++)
                        {
                            stackedSamples[i * 2] = stackedSamples[(requiredSamplesGameboy + i) * 2];
                            stackedSamples[i * 2 + 1] = stackedSamples[(requiredSamplesGameboy + i) * 2 + 1];
                        }

                        position = remain;
                    }

                }
            }
            else
            {
                int requiredSamples = data.Length / channels;

                lock (updateSamplesLocker)
                {
                    if (position == 0) return;
                
                    for (int i = 0; i < requiredSamples; i++)
                    {
                        int t = Mathf.FloorToInt((float)i / requiredSamples * position);

                        for (int c = 0; c < channels; c++)
                        {
                            data[i * channels + c] = stackedSamples[t * 2 + (c % 2)];
                        }
                    }

                    position = 0;
                }
            }
        }
    }
}