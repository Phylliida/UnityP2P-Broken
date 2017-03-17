using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityP2P;

public class P2PExample : MonoBehaviour {

    public string pubNubPublishKey;
    public string pubNubSubscribeKey;

    P2PClient client;
	// Use this for initialization
	void Start () {
        client = new P2PClient(pubNubPublishKey, pubNubSubscribeKey);
        client.OnNewPeerConnection += GotPeer;
        Debug.Log("My local ip: " + client.GetLocalIPAddress());
        Debug.Log("My local port: " + client.localPort);
        Debug.Log("My external ip: " + client.GetExternalIp());
        Debug.Log("My external port: " + client.externalPort);
    }

    private void GotPeer(P2PPeer newPeer)
    {
        Debug.Log("Got peer");
        newPeer.OnReceivedBytesFromPeer += GotBytesFromPeer;
    }

    private void GotBytesFromPeer(byte[] bytes)
    {
        Debug.Log("Got " + bytes.Length + " bytes");
        string receiveString = Encoding.UTF8.GetString(bytes);
        Debug.Log("Bytes decoded into a UTF8 string are: " + receiveString);
    }

    // Update is called once per frame
    void Update () {
        List<P2PPeer> peers = client.GetPeers();
        foreach (P2PPeer peer in peers)
        {
            peer.SendString("hi there");
        }
	}

    void OnApplicationQuit()
    {
        // DONT FORGET TO DO THIS
        client.Dispose();
    }
}
