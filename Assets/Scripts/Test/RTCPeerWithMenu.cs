using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.WebRTC;

public class RTCPeerWithMenu : MonoBehaviour
{
    public Button CreateOfferButton;
    public Button CreateAnswerButton;
    public Button AddOfferButton;
    public Button AddAnswerButton;
    public Button AddICEButton;
    public Button SendButton;

    public TMP_InputField CreateOfferInput;
    public TMP_InputField CreateAnswerInput;
    public TMP_InputField CandidatesInput;
    public TMP_InputField AddOfferInput;
    public TMP_InputField AddAnswerInput;
    public TMP_InputField AddICEInput;
    public TMP_InputField SendInput;

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

    private class IceMessage
    {
        public IceMessage(string sdpType, string sdpContent)
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

    public List<string> iceCandidates = new List<string>();

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
        localPeerOnIceCandidate = candidate => { UpdateIceCandidates(candidate); };
        //localPeerOnNegotiationNeeded = () => { StartCoroutine(PeerOnNegotiationNeeded()); };
        CreateOfferButton.onClick.AddListener(() => { StartCoroutine(CreateOffer()); });
        CreateAnswerButton.onClick.AddListener(() => { StartCoroutine(CreateAnswer()); });
        AddOfferButton.onClick.AddListener(() => { StartCoroutine(AddOffer()); });
        AddAnswerButton.onClick.AddListener(() => { StartCoroutine(AddAnswer()); });
        AddAnswerButton.onClick.AddListener(() => { StartCoroutine(AddAnswer()); });
        SendButton.onClick.AddListener(() => { SendMessage(); });
    }

    public IEnumerator CreateOffer()
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
        Debug.Log("pc1 createOffer start");
        var op = localPeer.CreateOffer(ref _offerOptions);
        yield return op;

        if (!op.IsError)
        {
            yield return StartCoroutine(OnCreateOfferSuccess(op.Desc));
        }
        else
        {

            Debug.Log("ERROR: Creating offer failed " + op.Error);
        }
    }

    public IEnumerator CreateAnswer()
    {
        var op3 = localPeer.CreateAnswer(ref _answerOptions);
        yield return op3;
        if (!op3.IsError)
        {
            yield return OnCreateAnswerSuccess(op3.Desc);
        }
        else
        {
            Debug.Log("ERROR: Creating answer failed " + op3.Error.message);
        }
    }

    public IEnumerator AddOffer()
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
        Debug.Log("Test debug line");

        RTCSessionDescription desc = new RTCSessionDescription();
        desc.type = RTCSdpType.Offer;
        desc.sdp = AddOfferInput.text;
        Debug.Log("pc2 setRemoteDescription start");
        var op2 = localPeer.SetRemoteDescription(ref desc);
        yield return op2;
        
        Debug.Log("pc2 createAnswer start");
        // Since the 'remote' side has no media stream we need
        // to pass in the right constraints in order for it to
        // accept the incoming offer of audio and video.

        var op3 = localPeer.CreateAnswer(ref _answerOptions);
        yield return op3;
        if (!op3.IsError)
        {
            yield return OnCreateAnswerSuccess(op3.Desc);
        }
        else
        {
            Debug.Log("ERROR: Adding offer failed " + op3.Error.message);
        }
    }

    public IEnumerator AddAnswer()
    {
        RTCSessionDescription desc = new RTCSessionDescription();
        desc.type = RTCSdpType.Answer;
        desc.sdp = AddAnswerInput.text;
        var op2 = localPeer.SetRemoteDescription(ref desc);
        yield return op2;
        
    }
    public void AddIceCandidate(string candidate)
    {

        // TODO: create multiple candidates from string
        RTCIceCandidateInit candyInit = new RTCIceCandidateInit();
        candyInit.candidate = candidate;
        RTCIceCandidate c = new RTCIceCandidate(candyInit);
        localPeer.AddIceCandidate(c);
    }

    public void AddIceCandidate(string candidate, string sdpMid, int sdpMLineIndex)
    {
        RTCIceCandidateInit candyInit = new RTCIceCandidateInit();
        candyInit.candidate = candidate;
        candyInit.sdpMid = candidate;
        candyInit.sdpMLineIndex = sdpMLineIndex;
        RTCIceCandidate c = new RTCIceCandidate(candyInit);
        localPeer.AddIceCandidate(c);
    }

    public void SendMessage()
    {
        sendChannel.Send(SendInput.text);
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
        Debug.Log("OH OH Negotiate");
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
        CreateOfferInput.text = desc.sdp;
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

    IEnumerator OnCreateAnswerSuccess(RTCSessionDescription desc)
    {
        Debug.Log($"Answer from remote:\n{desc.sdp}");
        CreateAnswerInput.text = desc.sdp;
        Debug.Log("pc2 setLocalDescription start");
        var op = localPeer.SetLocalDescription(ref desc);
        yield return op;

        if (!op.IsError)
        {
            Debug.Log($"SetLocalDescription complete");
        }
        else
        {
            var error = op.Error;
            Debug.Log("ERROR: SetLocalDescription error - " + error.message);
        }
    }


    public void UpdateIceCandidates(RTCIceCandidate candidate)
    {
        string allCandidates = "";
        iceCandidates.Add(candidate.Candidate);
        //CandidatesInput.text = "";
        foreach (string c in iceCandidates)
        {
            allCandidates += "/" + c;
        }
        CandidatesInput.text = allCandidates;
    }

    private void OnIceCandidate(RTCIceCandidate candidate)
    {
        localPeer.AddIceCandidate(candidate);
        
        Debug.Log($"1 ICE candidate:\n {candidate.Candidate}");
        Debug.Log($"2 ICE sdpMid:\n {candidate.SdpMid}");
        Debug.Log($"3 ICE SdpMLineIndex:\n {candidate.SdpMLineIndex}");
    }

    // Update is called once per frame
    void Update()
    {

    }
}
