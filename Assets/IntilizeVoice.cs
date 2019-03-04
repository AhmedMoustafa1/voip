using Dissonance.Integrations.PureP2P;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntilizeVoice : MonoBehaviour {
    public PureP2PCommsNetwork p2p;
    public string sessionId;
    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.S))
        {
         //   sessionId= p2p.SessionId;
            p2p.InitializeAsServer(sessionId);
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
   //         sessionId = p2p.SessionId;

            p2p.InitializeAsClient(sessionId);
        }
    }


}
