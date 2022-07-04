using System;
using System.Collections;
using System.Collections.Generic;
using CoreBoy;
using CoreBoy.sound;
using UnityEngine;

namespace CoreBoy.Unity
{
    public class SoftWoundSoundOutput : MonoBehaviour, ISoundOutput
    {
        private float tick;
        private int divider;

        private int position = 0;
        private float[] stackedSamples = default;

        private int outputSampleRate = 0;

        private bool isInitialized = false;

        [SerializeField] private bool fixedSampleRate = true;

        private void Start()
        {
            lock (this)
            {
                divider = (int)(Gameboy.TicksPerSec / AudioSettings.outputSampleRate);
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
            lock (this)
            {
                if (!isInitialized) return;

                if (stackedSamples.Length - 2 <= position * 2) return;

                stackedSamples[position * 2] = left / 255f; // * 2f - 1f;
                stackedSamples[position * 2 + 1] = right / 255f; // * 2f - 1f;
                position++;
            }
        }

        private int latestRequiredSamplesGameboy = 0;
        private int latestStackedSamplesGameboy = 0;
        private void OnGUI()
        {
            lock (this)
            {
                float w = 200;
                float h = 20;
                GUI.DrawTexture(new Rect(w, 0, 5, h), Texture2D.whiteTexture);
                GUI.color = Color.red;
                GUI.DrawTexture(new Rect(0, 0, w * ((float)latestRequiredSamplesGameboy / Gameboy.TicksPerSec), h * 0.5f), Texture2D.whiteTexture);
                GUI.color = Color.green;
                GUI.DrawTexture(new Rect(0, h * 0.5f, w * ((float)latestStackedSamplesGameboy / Gameboy.TicksPerSec), h*0.5f), Texture2D.whiteTexture);
            }
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {   
            if (fixedSampleRate)
            {
                lock (this)
                {
                    if (!isInitialized) return;
                    
                    int requiredSamples = data.Length / channels;
                    int requiredSamplesGameboy = latestRequiredSamplesGameboy =
                        Mathf.FloorToInt(requiredSamples * ((float)Gameboy.TicksPerSec / outputSampleRate));
                    
                    //Debug.Log($"Required: {requiredSamples}, {requiredSamplesGameboy} (stacked), Stacked: {position}");

                    latestStackedSamplesGameboy = position;
                    
                    int remain = position - requiredSamplesGameboy;

                    if (remain < 0f) return;

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
            else
            {
                int requiredSamples = data.Length / channels;

                lock (this)
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