# UnityP2P
UDP hole punching in Unity3D in C# for peer to peer networking, using PubNub as a free signaling server.

I wrote this for use in Unity however there aren't really any Unity dependencies so this should work fine as a standalone C# library as well.

I can't promise that this code is safe, secure, reliable, works for everyone, etc. But hopefully it gets you started if you are trying to do P2P through UDP hole punching.

Note that with TCP, messages sent are guaranteed to be received (assuming the connection is maintained). They are also guaranteed to be received in the order sent. With UDP, messages sent might not be received at all, might be receved in a different order than they were sent, and the same packet might be received multiple times (so you can get duplicate packets). The only guarantee you have is that if you send a message and it gets through, it will remain intact as you originally sent it.

Someday I'll do TCP hole punching as well but it's a little more tricky so I haven't done it yet.

Feel free to submit pull requests if you'd like =).
