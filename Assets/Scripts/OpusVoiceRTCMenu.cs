using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.WebRTC;
using System.Text;
using System.Linq;

public class OpusVoiceRTCMenu : MonoBehaviour
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
    public Button TextButton;

    public TMP_InputField CreateOfferInput;
    public TMP_InputField CreateAnswerInput;
    public TMP_InputField CandidatesInput;
    public TMP_InputField AddOfferInput;
    public TMP_InputField AddAnswerInput;
    public TMP_InputField AddICEInput;
    public TMP_InputField TextInput;
    public TMP_Text statusText;
    public TMP_Text infoText;

    public MyEncoder encoder;
    public MyDecoder decoder;
    private byte[] buffer;

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

    private DelegateOnMessage onReceiveChannelMessage;
    private DelegateOnOpen onSendChannelOpen;
    private DelegateOnOpen onSendChannelClose;
    private DelegateOnOpen onReceiveChannelOpen;
    private DelegateOnClose onReceiveChannelClose;
    private DelegateOnDataChannel onReceiveDataChannel;


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

    void OnEnable()
    {
        encoder.OnEncoded += EncodeAndSendBytes;
    }

    void OnDisable()
    {
        encoder.OnEncoded -= EncodeAndSendBytes;
    }

    private void OnDestroy()
    {
        WebRTC.Dispose();
    }

    // Start is called before the first frame update
    void Start()
    {
        VoiceButton.interactable = false;
        iceList.iceList = new List<IceCandidate>();

        localPeerOnIceConnectionChange = state => { OnIceConnectionChange(state); };
        localPeerOnIceCandidate = candidate => { UpdateIceCandidates(candidate); };
        onReceiveChannelMessage = bytes => {
            Decode(bytes);
        };
        onReceiveDataChannel = channel =>
        {
            Debug.Log("Oha!" + channel.Id);
            receiveChannel = channel;
            receiveChannel.OnMessage = onReceiveChannelMessage;
            receiveChannel.OnOpen = onReceiveChannelOpen;
            receiveChannel.OnClose = onReceiveChannelClose;
        };
        
        onSendChannelOpen = () => { VoiceButton.interactable = true; Debug.Log("SendChannel Opened"); };
        onSendChannelClose = () => { VoiceButton.interactable = false; Debug.Log("SendChannel Opened"); };
        onReceiveChannelOpen = () => { infoText.text = "Receive Open"; Debug.Log("ReceiveChannel Opened"); };
        onReceiveChannelClose = () => { Debug.Log("ReceiveChannel Closed"); };

        CreateOfferButton.onClick.AddListener(() => { StartCoroutine(CreateOffer()); });
        CreateAnswerButton.onClick.AddListener(() => { StartCoroutine(CreateAnswer()); });
        AddOfferButton.onClick.AddListener(() => { StartCoroutine(AddOffer()); });
        AddAnswerButton.onClick.AddListener(() => { StartCoroutine(AddAnswer()); });
        AddICEButton.onClick.AddListener(() => { AddIceCandidateList(); });
        VoiceButton.onClick.AddListener(() => { ToggleVoice(); });
        TextButton.onClick.AddListener(() => { SendMessage(TextInput.text); });
    }

    void Decode(byte[] bytes)
    {
        Debug.Log("decoding");
        int size = sizeof(int);
        byte[] encodedLengthBytes = bytes.Take(size).ToArray();
        byte[] encodedAudioBytes = bytes.Skip(sizeof(int)).Take(bytes.Length - sizeof(int)).ToArray();
        int length = DecodeLength(encodedLengthBytes);

        decoder.Decode(encodedAudioBytes, length);
    }

    int DecodeLength(byte[] bytes)
    {
        int result = BitConverter.ToInt32(bytes, 0);
        infoText.text = Encoding.UTF8.GetString(bytes);
        return result;
    }

    byte[] EncodeLength(byte[] bytes, int length)
    {
        int[] lengthArr = new int[] { length };
        byte[] result = new byte[lengthArr.Length * sizeof(int)];
        Buffer.BlockCopy(lengthArr, 0, result, 0, result.Length);
        return AddByteToArray(bytes, result);
    }

    public byte[] AddByteToArray(byte[] bArray, byte[] newBytes)
    {
        byte[] newArray = new byte[bArray.Length + newBytes.Length];
        bArray.CopyTo(newArray, newBytes.Length);
        newBytes.CopyTo(newArray, 0);
        return newArray;
    }

    void EncodeAndSendBytes(byte[] data, int length)
    {
        if (isConnected)
        {
            Debug.Log("sending");
            buffer = EncodeLength(data, length);
            sendChannel.Send(buffer);
        }
    }
    void SendMessage(string txt) {
        Debug.Log("sending" + txt);
        sendChannel.Send(txt);
    }

    public void ToggleVoice()
    {
        isConnected = !isConnected;
        if (isConnected)
        {
            VoiceButton.GetComponentInChildren<TMP_Text>().text = "Stop Voice";
        }
        else
        {
            VoiceButton.GetComponentInChildren<TMP_Text>().text = "Start Voice";
        }
    }

    public IEnumerator CreateOffer()
    {
        var configuration = GetSelectedSdpSemantics();
        localPeer = new RTCPeerConnection(ref configuration);

        localPeer.OnIceCandidate = localPeerOnIceCandidate;
        localPeer.OnIceConnectionChange = localPeerOnIceConnectionChange;
        localPeer.OnNegotiationNeeded = localPeerOnNegotiationNeeded;

        sendChannel = localPeer.CreateDataChannel("data"+ UnityEngine.Random.Range(0,6).ToString());
        sendChannel.OnOpen = onSendChannelOpen;
        audioStream = Audio.CaptureStream();
        var senders = new List<RTCRtpSender>();
        foreach (var track in audioStream.GetTracks())
        {
            var sender = localPeer.AddTrack(track);
            senders.Add(sender);
        }


        Debug.Log("Opened channel" + sendChannel.Id);
        localPeer.OnDataChannel = onReceiveDataChannel;
        Debug.Log("Receive channel action set");

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
        Debug.Log("Opened channel" + sendChannel.Id);
        localPeer.OnDataChannel = onReceiveDataChannel;
        Debug.Log("Receive channel action set");

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

    //private void OnAudioFilterRead(float[] data, int channels)
    //{
    //    Audio.Update(data, data.Length);
    //    if (isConnected)
    //    {
    //        Debug.Log("Test");
    //    }
    //}

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
