using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.WebRTC;
using System.Text;

public class VoiceRTCMenu : MonoBehaviour
{
    [Serializable]
    public class IceCandidate
    {
        public IceCandidate(string candidate, string sdpMid, int? sdpMLineIndex)
        {
            this.candidate = candidate;
            this.sdpMid = sdpMid;
            this.sdpMLineIndex = sdpMLineIndex;
        }

        public string candidate = "unknown";
        public string sdpMid;
        public int? sdpMLineIndex;
    }

    [Serializable]
    public class IceList
    {
        public List<IceCandidate> iceList;
    }

    [Serializable]
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

    [Serializable]
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

    public Button CreateOfferButton;
    public Button CreateAnswerButton;
    public Button AddOfferButton;
    public Button AddAnswerButton;
    public Button AddICEButton;
    public Button VoiceButton;

    public TMP_InputField CreateOfferInput;
    public TMP_InputField CreateAnswerInput;
    public TMP_InputField CandidatesInput;
    public TMP_InputField AddOfferInput;
    public TMP_InputField AddAnswerInput;
    public TMP_InputField AddICEInput;
    public TMP_Text statusText;
    public TMP_Text infoText;

    private bool isConnected = false;
    private bool audioUpdateStarted = false;

    private RTCPeerConnection localPeer;
    private RTCDataChannel sendChannel, receiveChannel;
    private List<RTCRtpSender> localSenders;//, localReceivers;
    private List<RTCRtpSender> localReceivers;
    //private List<RTCRtpReceiver> localReceivers;
    private MediaStream audioStream;

    private DelegateOnIceConnectionChange localPeerOnIceConnectionChange;
    private DelegateOnIceCandidate localPeerOnIceCandidate;
    private DelegateOnNegotiationNeeded localPeerOnNegotiationNeeded;

    private DelegateOnMessage onDataChannelMessage;
    private DelegateOnOpen onSendChannelOpen;
    private DelegateOnOpen onSendChannelClose;
    private DelegateOnTrack OnReceiveTrack;
    private DelegateOnOpen onReceiveChannelOpen;
    private DelegateOnClose onReceiveChannelClose;
    private DelegateOnDataChannel onDataChannel;
    private StringBuilder trackInfos;


    public List<string> iceCandidates = new List<string>();
    public IceList iceList;

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
        trackInfos = new StringBuilder();
        iceList.iceList = new List<IceCandidate>();
        localSenders = new List<RTCRtpSender>();
        localReceivers = new List<RTCRtpSender>();
        //localReceivers = new List<RTCRtpReceiver>();

        localPeerOnIceConnectionChange = state => { OnIceConnectionChange(state); };
        localPeerOnIceCandidate = candidate => { UpdateIceCandidates(candidate); };
        onDataChannel = channel =>
        {
            Debug.Log("Receive Channel");
            receiveChannel = channel;
            receiveChannel.OnMessage = onDataChannelMessage;
            receiveChannel.OnOpen = onReceiveChannelOpen;
            receiveChannel.OnClose = onReceiveChannelClose;
        };

        onDataChannelMessage = bytes => { Debug.Log(System.Text.Encoding.UTF8.GetString(bytes)); };
        OnReceiveTrack = e => { OnTrack(e); };
        onSendChannelOpen = () => { VoiceButton.interactable = true; Debug.Log("SendChannel Opened"); };
        onSendChannelClose = () => { VoiceButton.interactable = false; Debug.Log("SendChannel Opened"); };
        onReceiveChannelOpen = () => { infoText.text = "Receive Open";  Debug.Log("ReceiveChannel Opened"); };
        onReceiveChannelClose = () => { Debug.Log("ReceiveChannel Closed"); };


        //localPeerOnNegotiationNeeded = () => { StartCoroutine(PeerOnNegotiationNeeded()); };
        CreateOfferButton.onClick.AddListener(() => { StartCoroutine(CreateOffer()); });
        CreateAnswerButton.onClick.AddListener(() => { StartCoroutine(CreateAnswer()); });
        AddOfferButton.onClick.AddListener(() => { StartCoroutine(AddOffer()); });
        AddAnswerButton.onClick.AddListener(() => { StartCoroutine(AddAnswer()); });
        AddICEButton.onClick.AddListener(() => { AddIceCandidateList(); });
        VoiceButton.onClick.AddListener(() => { ToggleVoice(); });
    }
    public void ToggleVoice()
    {
        isConnected = !isConnected;
        if (isConnected)
        {
            audioStream = Audio.CaptureStream();
            AddTrack();
            VoiceButton.GetComponentInChildren<TMP_Text>().text = "Stop Voice";
        }
        else
        {
            RemoveTracks();
            VoiceButton.GetComponentInChildren<TMP_Text>().text = "Start Voice";
        }
    }

    private void AddTrack()
    {
        foreach (var track in audioStream.GetTracks())
        {
            localSenders.Add(localPeer.AddTrack(track, audioStream));
        }
        if (!audioUpdateStarted)
        {
            StartCoroutine(WebRTC.Update());
            audioUpdateStarted = true;
        }
    }

    private void RemoveTracks()
    {
        foreach (var sender in localSenders)
        {
            localPeer.RemoveTrack(sender);
        }

        localSenders.Clear();
        trackInfos.Clear();
        infoText.text = "";
    }

    private void OnTrack(RTCTrackEvent e)
    {
        //var senders = new List<RTCRtpSender>();
        //foreach (var track in audioStream.GetTracks())
        //{
        //    var sender = localPeer.AddTrack(track);
        //    senders.Add(sender);
        //}
        //localReceivers.Add(localPeer.)
        localReceivers.Add(localPeer.AddTrack(e.Track, audioStream));
        trackInfos.Append($"Receives remote track:\r\n");
        trackInfos.Append($"Track kind: {e.Track.Kind}\r\n");
        trackInfos.Append($"Track id: {e.Track.Id}\r\n");
        infoText.text = trackInfos.ToString();
    }


    public IEnumerator CreateOffer()
    {
        var configuration = GetSelectedSdpSemantics();
        localPeer = new RTCPeerConnection(ref configuration);

        localPeer.OnIceCandidate = localPeerOnIceCandidate;
        localPeer.OnIceConnectionChange = localPeerOnIceConnectionChange;
        localPeer.OnNegotiationNeeded = localPeerOnNegotiationNeeded;

        sendChannel = localPeer.CreateDataChannel("data");
        
        
        sendChannel.OnOpen = onSendChannelOpen;
        localPeer.OnDataChannel = onDataChannel;
        //AddTrack();
        localPeer.OnTrack = OnReceiveTrack;

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
        var configuration = GetSelectedSdpSemantics();
        localPeer = new RTCPeerConnection(ref configuration);

        localPeer.OnIceCandidate = localPeerOnIceCandidate;
        localPeer.OnIceConnectionChange = localPeerOnIceConnectionChange;
        localPeer.OnNegotiationNeeded = localPeerOnNegotiationNeeded;

        sendChannel = localPeer.CreateDataChannel("data");
        sendChannel.OnOpen = onSendChannelOpen;
        localPeer.OnDataChannel = onDataChannel;

        localPeer.OnTrack = OnReceiveTrack;

        // Create offer description from offer text field
        RTCSessionDescription desc = new RTCSessionDescription();
        desc.type = RTCSdpType.Offer;
        desc.sdp = AddOfferInput.text;

        var op2 = localPeer.SetRemoteDescription(ref desc);
        yield return op2;

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

    private void OnAudioFilterRead(float[] data, int channels)
    {
        Audio.Update(data, data.Length);
        if (isConnected)
        {
            Debug.Log("Test");
        }
    }

    public void AddIceCandidateList()
    {
        IceList recievedIces = JsonUtility.FromJson<IceList>(AddICEInput.text);
        Debug.Log("added ices:" + recievedIces.iceList.Count);

        foreach (IceCandidate i in recievedIces.iceList)
        {
            RTCIceCandidateInit ic = new RTCIceCandidateInit();
            ic.candidate = i.candidate;
            ic.sdpMid = i.sdpMid;
            ic.sdpMLineIndex = i.sdpMLineIndex;
            RTCIceCandidate c = new RTCIceCandidate(ic);
            localPeer.AddIceCandidate(c);
        }
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
                statusText.text = "New";
                break;
            case RTCIceConnectionState.Checking:
                Debug.Log("IceConnectionState: Checking");
                statusText.text = "Checking";
                break;
            case RTCIceConnectionState.Closed:
                Debug.Log("IceConnectionState: Closed");
                break;
            case RTCIceConnectionState.Completed:
                Debug.Log("IceConnectionState: Completed");
                statusText.text = "Completed";
                break;
            case RTCIceConnectionState.Connected:
                Debug.Log("IceConnectionState: Connected");
                statusText.text = "Connected";
                break;
            case RTCIceConnectionState.Disconnected:
                Debug.Log("IceConnectionState: Disconnected");
                statusText.text = "Disconnected";
                break;
            case RTCIceConnectionState.Failed:
                Debug.Log("IceConnectionState: Failed");
                statusText.text = "Failed";
                break;
            case RTCIceConnectionState.Max:
                Debug.Log("IceConnectionState: Max");
                break;
            default:
                break;
        }
    }

    IEnumerator OnCreateOfferSuccess(RTCSessionDescription desc)
    {

        CreateOfferInput.text = desc.sdp;

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

        CreateAnswerInput.text = desc.sdp;

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
        IceCandidate ic = new IceCandidate(candidate.Candidate, candidate.SdpMid, candidate.SdpMLineIndex);
        iceList.iceList.Add(ic);
        //CandidatesInput.text = "";
        //foreach (string c in iceCandidates)
        //{
        //    allCandidates += "/" + c;
        //}
        CandidatesInput.text = JsonUtility.ToJson(iceList);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
