using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.WebRTC;

public class MinimumPeer : MonoBehaviour
{

    private RTCPeerConnection localPeer;

    private RTCOfferOptions _offerOptions = new RTCOfferOptions
    {
        iceRestart = false,
        offerToReceiveAudio = true,
        offerToReceiveVideo = false
    };

    private RTCAnswerOptions _answerOptions = new RTCAnswerOptions
    {
        iceRestart = false,
    };

    private void Awake()
    {
        // Initialize WebRTC
        WebRTC.Initialize();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Unity.WebRTC.RTCSessionDescription CreateOffer()
    {
        var offer = localPeer.CreateOffer(ref _offerOptions).Desc;
        return offer;
    }

    public Unity.WebRTC.RTCSessionDescription CreateAnswer()
    {
        return localPeer.CreateAnswer(ref _answerOptions).Desc;
    }
}
