using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MyDecoder : MonoBehaviour
{
    public event Action<float[], int> OnDecoded;

    const UnityOpus.NumChannels channels = UnityOpus.NumChannels.Mono;

    UnityOpus.Decoder decoder;
    readonly float[] pcmBuffer = new float[UnityOpus.Decoder.maximumPacketDuration * (int)channels];

    void OnEnable()
    {
        decoder = new UnityOpus.Decoder(
            UnityOpus.SamplingFrequency.Frequency_48000,
            UnityOpus.NumChannels.Mono);
    }

    void OnDisable()
    {
        decoder.Dispose();
        decoder = null;
    }

    public void Decode(byte[] data, int length)
    {
        var pcmLength = decoder.Decode(data, length, pcmBuffer);
        OnDecoded?.Invoke(pcmBuffer, pcmLength);
    }
}

