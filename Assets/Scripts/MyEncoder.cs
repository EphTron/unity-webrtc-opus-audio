using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System;


[RequireComponent(typeof(MyRecorder))]
public class MyEncoder : MonoBehaviour
{
    public event Action<byte[], int> OnEncoded;

    const int bitrate = 96000;
    const int frameSize = 120;
    const int outputBufferSize = frameSize * 4; // at least frameSize * sizeof(float)

    MyRecorder recorder;
    UnityOpus.Encoder encoder;
    Queue<float> pcmQueue = new Queue<float>();
    readonly float[] frameBuffer = new float[frameSize];
    public readonly byte[] outputBuffer = new byte[outputBufferSize];

    void OnEnable()
    {
        recorder = GetComponent<MyRecorder>();
        recorder.OnAudioReady += OnAudioReady;
        encoder = new UnityOpus.Encoder(
            UnityOpus.SamplingFrequency.Frequency_48000,
            UnityOpus.NumChannels.Stereo,
            UnityOpus.OpusApplication.Audio)
            {
                Bitrate = bitrate,
                Complexity = 10,
                Signal = UnityOpus.OpusSignal.Music
            };
    }

    void OnDisable()
    {
        recorder.OnAudioReady -= OnAudioReady;
        encoder.Dispose();
        encoder = null;
        pcmQueue.Clear();
    }

    void OnAudioReady(float[] data)
    {
        foreach (var sample in data)
        {
            pcmQueue.Enqueue(sample);
        }
        while (pcmQueue.Count > frameSize)
        {
            for (int i = 0; i < frameSize; i++)
            {
                frameBuffer[i] = pcmQueue.Dequeue();
            }
            var encodedLength = encoder.Encode(frameBuffer, outputBuffer);
            //Debug.Log("Endocde Len " + encodedLength);
            OnEncoded?.Invoke(outputBuffer, encodedLength);
        }
    }
}

