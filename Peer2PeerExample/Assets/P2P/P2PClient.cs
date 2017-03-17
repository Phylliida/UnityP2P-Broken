using PubNubMessaging.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace UnityP2P
{
    public class P2PClient : IDisposable
    {

        public delegate void NewPeerCallback(P2PPeer newPeer);

        public event NewPeerCallback OnNewPeerConnection;


        public P2PClient(string pubNubPubKey, string pubNubSubKey)
        {

            connectedPeers = new Dictionary<string, P2PPeer>();
            uniqueIdsPubNubSeen = new HashSet<string>();
            localIp = GetLocalIPAddress();
            externalIp = GetExternalIp();

            /*
            var discoverer = new NatDiscoverer();
            var device = discoverer.DiscoverDeviceAsync().Result;

            var ip = device.GetExternalIPAsync().Result;
            Debug.Log("The external IP Address is: " + ip);
            */

            var discoverer = new Open.Nat.NatDiscoverer();
            var device = discoverer.DiscoverDeviceAsync().Result;

            IPAddress localAddr = IPAddress.Parse(localIp);
            int workingPort = -1;
            for (int i = 0; i < ports.Length; i++)
            {
                try
                {
                    // Test tcp with  nc -vz externalip 5293 in linux
                    //tempServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    // Test udp with  nc -vz -u externalip 5293 in linux
                    Socket tempServer = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    tempServer.Bind(new IPEndPoint(localAddr, ports[i]));
                    tempServer.Close();
                    workingPort = ports[i];
                    break;
                }
                catch
                {
                }
            }


            if (workingPort == -1)
            {
                throw new Exception("Failed to connect to a port");
            }

            localPort = workingPort;
            externalPort = workingPort;

            // Mapping ports


            device.CreatePortMapAsync(new Open.Nat.Mapping(Open.Nat.Protocol.Udp, localPort, externalPort));


            Socket tempServera;
            IPAddress localAddra = IPAddress.Parse(localIp);
            try
            {
                // Test tcp with  nc -vz externalip 5293 in linux
                //tempServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                // Test udp with  nc -vz -u externalip 5293 in linux
                tempServera = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                tempServera.Bind(new IPEndPoint(localAddra, localPort));
                server = tempServera;
                if (stopped)
                {
                    server.Close();
                    server = null;
                }
            }
            catch
            {
                server = null;
            }

            if (server == null)
            {
                throw new Exception("failed: " + localPort + " " + externalPort);
            }
            udpClient = new UdpClient(localPort);


            pubnub = new Pubnub(pubNubPubKey, pubNubSubKey);
            pubnub.Subscribe<string>(
                channelName,
                OnPubNubMessage,
                OnPubNubConnect,
                OnPubNubError);
            Debug.Log("Initialized P2P Client");
        }

        public List<P2PPeer> GetPeers()
        {
            List<P2PPeer> allPeers = new List<P2PPeer>();
            lock (peerLock)
            {
                foreach (KeyValuePair<string, P2PPeer> peer in connectedPeers)
                {
                    allPeers.Add(peer.Value);
                }
            }
            return allPeers;
        }

        public Pubnub pubnub;
        string localIp;
        string externalIp;
        object peerLock = new object();

        /*
        public void Update()
        {
            lock (peerLock)
            {
                foreach (KeyValuePair<string, PeerData> peer in connectedPeers)
                {
                    Debug.Log("sending to peer: " + peer.Value.pubNubUniqueId);
                    PeerData peerData = peer.Value;
                    byte[] bytesSending = System.Text.Encoding.UTF8.GetBytes("me too thanks u is " + peerData.externalIp + "\n");
                    //udpClient.Send(bytesSending, bytesSending.Length, new IPEndPoint(IPAddress.Parse(peerData.externalIp), peerData.externalPort));
                    udpClient.Send(bytesSending, bytesSending.Length, new IPEndPoint(IPAddress.Parse(peerData.localIp), peerData.localPort));
                }
            }
        }
        */
        

        bool hasLocalIp = false;
        // From http://stackoverflow.com/questions/6803073/get-local-ip-address
        public string GetLocalIPAddress()
        {
            if (hasLocalIp)
            {
                return localIp;
            }
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    hasLocalIp = true;
                    localIp = ip.ToString();
                    return ip.ToString();
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }

        bool hasExternalIp = false;
        public string GetExternalIp()
        {
            if (hasExternalIp)
            {
                return externalIp;
            }
            for (int i = 0; i < 2; i++)
            {
                string res = GetExternalIpWithTimeout(400);
                if (res != "")
                {
                    hasExternalIp = true;
                    externalIp = res;
                    return res;
                }
            }
            throw new Exception("Failed to get IP");
        }
        private static string GetExternalIpWithTimeout(int timeoutMillis)
        {
            string[] sites = new string[] {
            "http://ipinfo.io/ip",
            "http://icanhazip.com/",
            "http://ipof.in/txt",
            "http://ifconfig.me/ip",
            "http://ipecho.net/plain"
        };
            foreach (string site in sites)
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(site);
                    request.Timeout = timeoutMillis;
                    using (var webResponse = (HttpWebResponse)request.GetResponse())
                    {
                        using (Stream responseStream = webResponse.GetResponseStream())
                        {
                            using (StreamReader responseReader = new System.IO.StreamReader(responseStream, Encoding.UTF8))
                            {
                                return responseReader.ReadToEnd().Trim();
                            }
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }

            return "";

        }


        // I looked through the list of all the ports registered and these were a few that weren't used so hopefully one of them should work.
        // You can add more here if you'd like, see https://en.wikipedia.org/wiki/List_of_TCP_and_UDP_port_numbers
        public static int[] ports = new int[]
        {
        5283,
        5284,
        5285,
        5286,
        5287,
        5288,
        5289,
        5290,
        5291,
        5292,
        5293,
        5294,
        5295,
        5296,
        5297
        };

        public int localPort;
        public int externalPort;

        Socket server;

        bool stopped;


        [Serializable]
        class PeerData
        {
            public int localPort;
            public int externalPort;
            public string localIp;
            public string externalIp;
            public string pubNubUniqueId;
            public PeerData(int localPort, int externalPort, string localIp, string externalIp, string pubNubUniqueId)
            {
                this.localPort = localPort;
                this.externalPort = externalPort;
                this.localIp = localIp;
                this.externalIp = externalIp;
                this.pubNubUniqueId = pubNubUniqueId;
            }
            public PeerData(string peerDataString)
            {
                string[] pieces = peerDataString.Split(new char[] { ' ' });
                localIp = pieces[0].Trim();
                externalIp = pieces[1].Trim();
                localPort = int.Parse(pieces[2].Trim());
                externalPort = int.Parse(pieces[3].Trim());
                pubNubUniqueId = pieces[4].Trim();

            }
            public override string ToString()
            {
                return localIp + " " + externalIp + " " + localPort + " " + externalPort + " " + pubNubUniqueId;
            }
        }

        void OnPubNubTheyGotMessage(object result)
        {

        }

        void OnPubNubMessageFailed(PubnubClientError clientError)
        {
            throw new Exception("PubNub error on publish: " + clientError.Message);
        }


        string channelName = "hithere";
        void OnPubNubConnect(string res)
        {
            pubnub.Publish<string>(channelName, (new PeerData(localPort, externalPort, localIp, externalIp, pubnub.SessionUUID)).ToString(), OnPubNubTheyGotMessage, OnPubNubMessageFailed);
        }

        void OnPubNubError(PubnubClientError clientError)
        {
            throw new Exception("PubNub error on subscribe: " + clientError.Message);
        }

        public UdpClient udpClient;

        HashSet<string> uniqueIdsPubNubSeen;
        Dictionary<string, P2PPeer> connectedPeers;

        void OnPubNubMessage(string message)
        {
            string[] splitMessage = message.Trim().Substring(1, message.Length - 2).Split(new char[] { ',' });
            string peerDataMessage = splitMessage[0].Trim().Substring(1, splitMessage[0].Trim().Length - 2);
            //string peerMessageId = splitMessage[1].Trim().Substring(1, splitMessage[1].Trim().Length - 2);
            //string room = splitMessage[2].Trim().Substring(1, splitMessage[2].Trim().Length - 2);




            PeerData peerData = new PeerData(peerDataMessage);

            // If you are on the same device then you have to do this for it to work
            if (peerData.localIp == localIp && peerData.externalIp == externalIp)
            {
                peerData.localIp = "127.0.0.1";
            }


            // From me, ignore
            if (peerData.pubNubUniqueId == pubnub.SessionUUID)
            {
                return;
            }

            if (udpClient == null)
            {
                return;
            }

            // From someone else


            // First time we have heard from them
            if (!uniqueIdsPubNubSeen.Contains(peerData.pubNubUniqueId))
            {
                uniqueIdsPubNubSeen.Add(peerData.pubNubUniqueId);
                udpClient.Send(new byte[10], 10, new IPEndPoint(IPAddress.Parse(peerData.externalIp), peerData.externalPort));
                udpClient.Send(new byte[10], 10, new IPEndPoint(IPAddress.Parse(peerData.localIp), peerData.localPort)); // This is if they are on a LAN, we will try both because why not
                pubnub.Publish<string>(channelName, (new PeerData(localPort, externalPort, localIp, externalIp, pubnub.SessionUUID)).ToString(), OnPubNubTheyGotMessage, OnPubNubMessageFailed);
            }
            // Second time we have heard from them, after then we don't care because we are connected
            else if (!connectedPeers.ContainsKey(peerData.pubNubUniqueId))
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(peerData.externalIp), peerData.externalPort);
                IPEndPoint endPointLocal = new IPEndPoint(IPAddress.Parse(peerData.localIp), peerData.localPort);
                //bool isOnLan = IsLanIP(IPAddress.Parse(peerData.externalIp)); TODO, this would be nice to test for
                bool isOnLan = true;
                P2PPeer peer = new P2PPeer(peerData.localIp, peerData.externalIp, peerData.localPort, peerData.externalPort, this, isOnLan);
                lock (peerLock)
                {
                    connectedPeers.Add(peerData.pubNubUniqueId, peer);
                }



                udpClient.Send(new byte[10], 10, endPoint);
                udpClient.Send(new byte[10], 10, endPointLocal); // This is if they are on a LAN

                pubnub.Publish<string>(channelName, (new PeerData(localPort, externalPort, localIp, externalIp, pubnub.SessionUUID)).ToString(), OnPubNubTheyGotMessage, OnPubNubMessageFailed);
                if (OnNewPeerConnection != null)
                {
                    OnNewPeerConnection(peer);
                }
            }

        }



        public void Dispose()
        {

            stopped = true;
            if (server != null)
            {
                server.Close();
                server = null;
                if (udpClient != null)
                {
                    udpClient.Close();
                    udpClient = null;
                }
            }
            //device.DeletePortMap(new Mapping(Protocol.Tcp, internalPort, externalPort));
        }
    }
}