using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.WebRTC;

public class MediaPeerConnection : MonoBehaviour
{
    private class SdpMessage
    {
        public SdpMessage(string sdpType, string sdpContent)
        {
            type = sdpType;
            sdp = sdpContent;
        }

        public string type = "unknown";
        public string sdp;
    }

    private RTCPeerConnection localPeer;
    private RTCDataChannel sendChannel, receiveChannel;
    private List<RTCRtpSender> localSenders;
    private MediaStream audioStream;

    private DelegateOnIceConnectionChange localPeerOnIceConnectionChange;
    private DelegateOnIceCandidate localPeerOnIceCandidate;
    private DelegateOnNegotiationNeeded localPeerOnNegotiationNeeded;

    private DelegateOnMessage onDataChannelMessage;
    private DelegateOnOpen onDataChannelOpen;
    private DelegateOnClose onDataChannelClose;
    private DelegateOnDataChannel onDataChannel;

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

    private void OnDestroy()
    {
        WebRTC.Dispose();
    }

    // Start is called before the first frame update
    void Start()
    {
        localSenders = new List<RTCRtpSender>();

        localPeerOnIceConnectionChange = state => { OnIceConnectionChange(state); };
        localPeerOnIceCandidate = candidate => { OnIceCandidate(candidate); };
        localPeerOnNegotiationNeeded = () => { StartCoroutine(PeerOnNegotiationNeeded()); };
    }

    private void InitRTC()
    {
        Debug.Log("GetSelectedSdpSemantics");
        var configuration = GetSelectedSdpSemantics();
        localPeer = new RTCPeerConnection(ref configuration);
        Debug.Log("Created local peer connection object pc1");
        localPeer.OnIceCandidate = localPeerOnIceCandidate;
        localPeer.OnIceConnectionChange = localPeerOnIceConnectionChange;
        localPeer.OnNegotiationNeeded = localPeerOnNegotiationNeeded;

        sendChannel = localPeer.CreateDataChannel("audiostream");
        //audioStream = Audio.CaptureStream();

        receiveChannel = localPeer.CreateDataChannel("data");
        onDataChannel = channel =>
        {
            receiveChannel = channel;
            receiveChannel.OnMessage = onDataChannelMessage;
            receiveChannel.OnOpen = onDataChannelOpen;
            receiveChannel.OnClose = onDataChannelClose;
        };
        onDataChannelMessage = bytes => { Debug.Log(System.Text.Encoding.UTF8.GetString(bytes)); };
        onDataChannelOpen = () => { Debug.Log("ReceiveChannel Opened"); };
        onDataChannelClose = () => { Debug.Log("ReceiveChannel Closed"); };

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

    private static RTCConfiguration GetSelectedSdpSemantics()
    {
        RTCConfiguration config = default;
        config.iceServers = new[]
        {
            new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } }
            //new RTCIceServer { urls = new[] { "stun:turn.webis.de:3478" } }
        };

        return config;
    }

    void OnIceConnectionChange(RTCIceConnectionState state)
    {
        switch (state)
        {
            case RTCIceConnectionState.New:
                Debug.Log("IceConnectionState: New");
                break;
            case RTCIceConnectionState.Checking:
                Debug.Log("IceConnectionState: Checking");
                break;
            case RTCIceConnectionState.Closed:
                Debug.Log("IceConnectionState: Closed");
                break;
            case RTCIceConnectionState.Completed:
                Debug.Log("IceConnectionState: Completed");
                break;
            case RTCIceConnectionState.Connected:
                Debug.Log("IceConnectionState: Connected");
                break;
            case RTCIceConnectionState.Disconnected:
                Debug.Log("IceConnectionState: Disconnected");
                break;
            case RTCIceConnectionState.Failed:
                Debug.Log("IceConnectionState: Failed");
                break;
            case RTCIceConnectionState.Max:
                Debug.Log("IceConnectionState: Max");
                break;
            default:
                break;
        }
    }

    IEnumerator PeerOnNegotiationNeeded()
    {
        var op = localPeer.CreateOffer(ref _offerOptions);
        yield return op;

        if (!op.IsError)
        {
            yield return StartCoroutine(OnCreateOfferSuccess(op.Desc));
        }
        else
        {
            var error = op.Error;
            Debug.LogError($"Error Detail Type: {error.message}");
        }
    }

    IEnumerator OnCreateOfferSuccess(RTCSessionDescription desc)
    {
        Debug.Log($"Offer from local \n{desc.sdp}");
        Debug.Log("pc1 setLocalDescription start");
        var op = localPeer.SetLocalDescription(ref desc);
        yield return op;

        if (!op.IsError)
        {
            Debug.Log("SetLocalDescription complete");
        }
        else
        {
            var error = op.Error;
            Debug.Log("ERROR: SetLocalDescription error - " + error.message);
        }
    }

    private void OnIceCandidate(RTCIceCandidate candidate)
    {
        localPeer.AddIceCandidate(candidate);
        Debug.Log($"ICE candidate:\n {candidate.Candidate}");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
