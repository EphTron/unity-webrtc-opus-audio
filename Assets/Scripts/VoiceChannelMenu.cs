using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.WebRTC;
using System;
using System.Linq;

class VoiceChannelMenu: MonoBehaviour
{
#pragma warning disable 0649
    [SerializeField] private Button connectButton;
    [SerializeField] private Button voiceButton;
#pragma warning restore 0649

    public MyEncoder encoder;
    public MyDecoder decoder;

    private RTCPeerConnection pc1, pc2;
    private RTCDataChannel dataChannel, remoteDataChannel;
    private Coroutine sdpCheck;
    private byte[] buffer;
    private bool isConnected = false;
    private DelegateOnIceConnectionChange pc1OnIceConnectionChange;
    private DelegateOnIceConnectionChange pc2OnIceConnectionChange;
    private DelegateOnIceCandidate pc1OnIceCandidate;
    private DelegateOnIceCandidate pc2OnIceCandidate;
    private DelegateOnMessage onDataChannelMessage;
    private DelegateOnOpen onDataChannelOpen;
    private DelegateOnClose onDataChannelClose;
    private DelegateOnDataChannel onDataChannel;

    private RTCOfferOptions OfferOptions = new RTCOfferOptions
    {
        iceRestart = false,
        offerToReceiveAudio = true,
        offerToReceiveVideo = false
    };

    private RTCAnswerOptions AnswerOptions = new RTCAnswerOptions
    {
        iceRestart = false,
    };

    private void Awake()
    {
        WebRTC.Initialize();
        connectButton.onClick.AddListener(() => { StartCoroutine(Connect()); });
    }

    private void OnDestroy()
    {
        WebRTC.Dispose();
    }

    void OnEnable()
    {
        encoder.OnEncoded += EncodeAndSendBytes;
    }

    void OnDisable()
    {
        encoder.OnEncoded -= EncodeAndSendBytes;
    }

    private void Start()
    {
        connectButton.interactable = true;

        pc1OnIceConnectionChange = state => { OnIceConnectionChange(pc1, state); };
        pc2OnIceConnectionChange = state => { OnIceConnectionChange(pc2, state); };
        pc1OnIceCandidate = candidate => { OnIceCandidate(pc1, candidate); };
        pc2OnIceCandidate = candidate => { OnIceCandidate(pc1, candidate); };
        onDataChannelMessage = bytes => {
            Decode(bytes);
        };
        onDataChannel = channel =>
        {
            Debug.Log("Receive Channel open");
            remoteDataChannel = channel;
            remoteDataChannel.OnMessage = onDataChannelMessage;
        };
        
        onDataChannelOpen = () => { voiceButton.interactable = true; };
        onDataChannelClose = () => { voiceButton.interactable = false; };
    }

    public void ToggleVoice()
    {
        isConnected = !isConnected;
        if (isConnected)
        {
            voiceButton.gameObject.GetComponentInChildren<Text>().text = "Stop Voice";
        } else
        {
            voiceButton.gameObject.GetComponentInChildren<Text>().text = "Start Voice";
        }
    }

    void Decode(byte[] bytes)
    {
        int size = sizeof(int);
        byte[] encodedLengthBytes = bytes.Take(size).ToArray();
        byte[] encodedAudioBytes = bytes.Skip(sizeof(int)).Take(bytes.Length - sizeof(int)).ToArray();
        int length = DecodeLength(encodedLengthBytes);
        //Debug.Log("Decoder pcmLen" + length);

        decoder.Decode(encodedAudioBytes, length);
    }

    int DecodeLength(byte[] bytes)
    {
        int result = BitConverter.ToInt32(bytes, 0);
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
            Debug.Log("Send: " + data.Length + " " + length);
            buffer = EncodeLength(data, length);
            dataChannel.Send(buffer);
            //dataChannel.Send(data);
            //dataChannel.Send();
        }
    }

    IEnumerator Connect()
    {
        connectButton.interactable = false;
        //Debug.Log("GetSelectedSdpSemantics");
        var configuration = GetSelectedSdpSemantics();
        pc1 = new RTCPeerConnection(ref configuration);
        //Debug.Log("Created local peer connection object pc1");
        pc1.OnIceCandidate = pc1OnIceCandidate;
        pc1.OnIceConnectionChange = pc1OnIceConnectionChange;
        pc2 = new RTCPeerConnection(ref configuration);
        //Debug.Log("Created remote peer connection object pc2");
        pc2.OnIceCandidate = pc2OnIceCandidate;
        pc2.OnIceConnectionChange = pc2OnIceConnectionChange;
        pc2.OnDataChannel = onDataChannel;

        RTCDataChannelInit conf = new RTCDataChannelInit();
        dataChannel = pc1.CreateDataChannel("data", conf);
        dataChannel.OnOpen = onDataChannelOpen;

        Debug.Log("pc1 createOffer start");
        var op = pc1.CreateOffer(ref OfferOptions);
        yield return op;

        if (!op.IsError)
        {
            yield return StartCoroutine(OnCreateOfferSuccess(op.Desc));
        }
        else
        {
            OnCreateSessionDescriptionError(op.Error);
        }
    }


    RTCConfiguration GetSelectedSdpSemantics()
    {
        RTCConfiguration config = default;
        config.iceServers = new RTCIceServer[]
        {
            new RTCIceServer { urls = new string[] { "stun:stun.l.google.com:19302" } }
        };

        return config;
    }
    void OnIceConnectionChange(RTCPeerConnection pc, RTCIceConnectionState state)
    {
        switch (state)
        {
            case RTCIceConnectionState.New:
                Debug.Log($"{GetName(pc)} IceConnectionState: New");
                break;
            case RTCIceConnectionState.Checking:
                Debug.Log($"{GetName(pc)} IceConnectionState: Checking");
                break;
            case RTCIceConnectionState.Closed:
                Debug.Log($"{GetName(pc)} IceConnectionState: Closed");
                break;
            case RTCIceConnectionState.Completed:
                Debug.Log($"{GetName(pc)} IceConnectionState: Completed");
                break;
            case RTCIceConnectionState.Connected:
                Debug.Log($"{GetName(pc)} IceConnectionState: Connected");
                break;
            case RTCIceConnectionState.Disconnected:
                Debug.Log($"{GetName(pc)} IceConnectionState: Disconnected");
                break;
            case RTCIceConnectionState.Failed:
                Debug.Log($"{GetName(pc)} IceConnectionState: Failed");
                break;
            case RTCIceConnectionState.Max:
                Debug.Log($"{GetName(pc)} IceConnectionState: Max");
                break;
            default:
                break;
        }
    }
    void Pc1OnIceConnectinChange(RTCIceConnectionState state)
    {
        OnIceConnectionChange(pc1, state);
    }
    void Pc2OnIceConnectionChange(RTCIceConnectionState state)
    {
        OnIceConnectionChange(pc2, state);
    }

    void Pc1OnIceCandidate(RTCIceCandidate candidate)
    {
        OnIceCandidate(pc1, candidate);
    }
    void Pc2OnIceCandidate(RTCIceCandidate candidate)
    {
        OnIceCandidate(pc2, candidate);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pc"></param>
    /// <param name="streamEvent"></param>
    void OnIceCandidate(RTCPeerConnection pc, RTCIceCandidate candidate)
    {
        GetOtherPc(pc).AddIceCandidate(candidate);
            //Debug.Log($"1{GetName(pc)} ICE candidate:\n {candidate.Candidate}");
            //Debug.Log($"  2{GetName(pc)} ICE RelatedAddress:\n {candidate.RelatedAddress}");
            //Debug.Log($"  3{GetName(pc)} ICE Address:\n {candidate.Address}");
            //Debug.Log($"  4{GetName(pc)} ICE SdpMLineIndex:\n {candidate.SdpMLineIndex}");
            //Debug.Log($"  5{GetName(pc)} ICE SdpMid:\n {candidate.SdpMid}");
            //Debug.Log($"  6{GetName(pc)} ICE UserNameFragment:\n {candidate.UserNameFragment}");
            //Debug.Log($"  7{GetName(pc)} ICE candidate:\n {candidate.Type}");
            //Debug.Log($"  8{GetName(pc)} ICE candidate:\n {candidate.Candidate}");
    }

    string GetName(RTCPeerConnection pc)
    {
        return (pc == pc1) ? "pc1" : "pc2";
    }

    RTCPeerConnection GetOtherPc(RTCPeerConnection pc)
    {
        return (pc == pc1) ? pc2 : pc1;
    }

    IEnumerator OnCreateOfferSuccess(RTCSessionDescription desc)
    {
        Debug.Log("pc1 setLocalDescription start");
        var op = pc1.SetLocalDescription(ref desc);
        yield return op;

        if (!op.IsError)
        {
            OnSetLocalSuccess(pc1); //Output
        }
        else
        {
            var error = op.Error;
            OnSetSessionDescriptionError(ref error);
        }

        Debug.Log("pc2 setRemoteDescription start");
        var op2 = pc2.SetRemoteDescription(ref desc);
        yield return op2;
        if (!op2.IsError)
        {
            OnSetRemoteSuccess(pc2); //Output
        }
        else
        {
            var error = op2.Error;
            OnSetSessionDescriptionError(ref error);
        }
        Debug.Log("pc2 createAnswer start");
        // Since the 'remote' side has no media stream we need
        // to pass in the right constraints in order for it to
        // accept the incoming offer of audio and video.

        var op3 = pc2.CreateAnswer(ref AnswerOptions);
        yield return op3;
        if (!op3.IsError)
        {
            Debug.Log("Test created: " + op3.Desc.type);
            yield return OnCreateAnswerSuccess(op3.Desc);
        }
        else
        {
            OnCreateSessionDescriptionError(op3.Error);
        }
    }

    void OnSetLocalSuccess(RTCPeerConnection pc)
    {
        Debug.Log($"{GetName(pc)} SetLocalDescription complete");
    }

    void OnSetSessionDescriptionError(ref RTCError error) { }

    void OnSetRemoteSuccess(RTCPeerConnection pc)
    {
        Debug.Log($"{GetName(pc)} SetRemoteDescription complete");
    }

    IEnumerator OnCreateAnswerSuccess(RTCSessionDescription desc)
    {
        Debug.Log($"Answer from pc2:\n{desc.type}");
        Debug.Log($"Answer from pc2:\n{desc.sdp}");
        Debug.Log("pc2 setLocalDescription start");
        var op = pc2.SetLocalDescription(ref desc);
        yield return op;

        if (!op.IsError)
        {
            OnSetLocalSuccess(pc2);
        }
        else
        {
            var error = op.Error;
            OnSetSessionDescriptionError(ref error);
        }

        Debug.Log("pc1 setRemoteDescription start");

        var op2 = pc1.SetRemoteDescription(ref desc);
        yield return op2;
        if (!op2.IsError)
        {
            OnSetRemoteSuccess(pc1);
        }
        else
        {
            var error = op2.Error;
            OnSetSessionDescriptionError(ref error);
        }
    }

    IEnumerator LoopGetStats()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            if (!voiceButton.interactable)
                continue;

            var op1 = pc1.GetStats();
            var op2 = pc2.GetStats();

            yield return op1;
            yield return op2;

            Debug.Log("pc1");
            foreach (var stat in op1.Value.Stats.Values)
            {
                Debug.Log(stat.Type.ToString());
            }
            Debug.Log("pc2");
            foreach (var stat in op2.Value.Stats.Values)
            {
                Debug.Log(stat.Type.ToString());
            }
        }
    }

    void OnAddIceCandidateSuccess(RTCPeerConnection pc)
    {
        Debug.Log($"{GetName(pc)} addIceCandidate success");
    }

    void OnAddIceCandidateError(RTCPeerConnection pc, RTCError error)
    {
        Debug.Log($"{GetName(pc)} failed to add ICE Candidate: ${error}");
    }

    void OnCreateSessionDescriptionError(RTCError e)
    {

    }
}
