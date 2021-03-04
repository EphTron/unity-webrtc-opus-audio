using System.Collections.Concurrent;
using UnityEngine;
using System;


[RequireComponent(typeof(MyDecoder), typeof(AudioSource))]
public class MyPlayer : MonoBehaviour
{
    const UnityOpus.NumChannels channels = UnityOpus.NumChannels.Mono;
    const UnityOpus.SamplingFrequency frequency = UnityOpus.SamplingFrequency.Frequency_48000;
    const int audioClipLength = 1024 * 6;
    AudioSource source;
    MyDecoder decoder;
    int head = 0;
    float[] audioClipData;

    void OnEnable()
    {
        source = GetComponent<AudioSource>();
        source.clip = AudioClip.Create("Loopback", audioClipLength, (int)channels, (int)frequency, false);
        source.loop = true;
        decoder = GetComponent<MyDecoder>();
        decoder.OnDecoded += OnDecoded;
    }

    void OnDisable()
    {
        decoder.OnDecoded -= OnDecoded;
        source.Stop();
    }

    void OnDecoded(float[] pcm, int pcmLength)
    {
        if (audioClipData == null || audioClipData.Length != pcmLength)
        {
            // assume that pcmLength will not change.
            audioClipData = new float[pcmLength];
        }
        Array.Copy(pcm, audioClipData, pcmLength);
        source.clip.SetData(audioClipData, head);
        head += pcmLength;
        if (!source.isPlaying && head > audioClipLength / 2)
        {
            source.Play();
        }
        head %= audioClipLength;
    }
}
