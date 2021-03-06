using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PeerMenu : MonoBehaviour
{
    public MinimumPeer peer;

    public Button CreateOfferButton;
    public Button CreateAnswerButton;
    public Button AddOfferButton;
    public Button AddAnswerButton;
    public Button AddICEButton;

    public TMP_InputField CreateOfferInput;
    public TMP_InputField CreateAnswerInput;
    public TMP_InputField AddOfferInput;
    public TMP_InputField AddAnswerInput;
    public TMP_InputField AddICEInput;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CreateOffer()
    {
        string offer = peer.CreateOffer().sdp;
    }

    public void AddOffer()
    {
        //string offer = AddOfferInput;
    }
}
