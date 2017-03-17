﻿using System;
using UnityTest;
using UnityEngine;
using Pathfinding.Serialization.JsonFx;
using PubNubMessaging.Core;
using System.Collections;
using System.Collections.Generic;

namespace PubNubMessaging.Tests
{
    public class TestSubscribePubSubV2PresenceResponse: MonoBehaviour
    {
        public bool SslOn = false;
        public bool CipherOn = false;
        public bool AsObject = false;
        public bool BothString = false;
        Pubnub pubnub;
        public IEnumerator Start ()
        {

            #if PUBNUB_PS_V2_RESPONSE
            yield return StartCoroutine(DoTestSubscribePSV2(this.name));
            UnityEngine.Debug.Log (string.Format("{0}: After StartCoroutine", this.name));
            yield return new WaitForSeconds (CommonIntergrationTests.WaitTimeBetweenCalls);
            #else
            yield return null;
            UnityEngine.Debug.Log (string.Format("{0}: Ignoring test", this.name));
            IntegrationTest.Pass();
            #endif
        }

        public IEnumerator DoTestSubscribePSV2 ( string testName)
        {
            pubnub = new Pubnub (CommonIntergrationTests.PublishKey,
                CommonIntergrationTests.SubscribeKey);

            System.Random r = new System.Random ();
            string ch = "UnityIntegrationTest_CH." + r.Next (100);
            UnityEngine.Debug.Log (string.Format ("{0} {1}: Start coroutine ", DateTime.Now.ToString (), testName));
            string uuid = "UnityIntegrationTest_UUID";
            pubnub.ChangeUUID(uuid);

            /*Subscribe CG
            ⁃   Publish to CH
            ⁃   Read Message on CG*/

            bool bSubMessage2 = false;
            bool bSubConnect = false;
            string pubMessage = "TestMessagePSV2";
            if(AsObject){
                pubnub.Presence<object>(ch, "", (object returnMessage)=>{
                    UnityEngine.Debug.Log (string.Format ("{0}: {1} Subscribe {2}", DateTime.Now.ToString (), testName, returnMessage));
                    PNPresenceEventResult pnMessageResult = returnMessage as PNPresenceEventResult;

                    UnityEngine.Debug.Log (string.Format ("DisplayReturnMessageSubscribeObject: {0}", pnMessageResult.Event));
                    UnityEngine.Debug.Log (string.Format ("DisplayReturnMessageSubscribeObject: {0}", pnMessageResult.Channel));
                    UnityEngine.Debug.Log (string.Format ("DisplayReturnMessageSubscribeObject: {0}", pnMessageResult.Subscription));
                    UnityEngine.Debug.Log (string.Format ("DisplayReturnMessageSubscribeObject: {0}", pnMessageResult.Occupancy));
                    UnityEngine.Debug.Log (string.Format ("DisplayReturnMessageSubscribeObject: {0}", pnMessageResult.Timetoken));
                    UnityEngine.Debug.Log (string.Format ("DisplayReturnMessageSubscribeObject: {0}", pnMessageResult.IssuingClientId));
                    //UnityEngine.Debug.Log (string.Format ("DisplayReturnMessageSubscribeObject: {0}", pnMessageResult.UserMetadata.ToString()));

                    if(pnMessageResult.Event.ToString().Contains("join") 
                        && pnMessageResult.Channel.Contains(ch)
                        && pnMessageResult.IssuingClientId.Equals(pubnub.SessionUUID)
                    ){
                        bSubMessage2 = true;
                    }
                }, (object retConnect)=>{
                    UnityEngine.Debug.Log (string.Format ("{0}: {1} Subscribe Connected {2}", DateTime.Now.ToString (), testName, retConnect));
                    bSubConnect = true;
                }, this.DisplayErrorMessage); 

            }else{
                pubnub.Presence<string>(ch, "", (string returnMessage)=>{
                    UnityEngine.Debug.Log (string.Format ("{0}: {1} Subscribe {2}", DateTime.Now.ToString (), testName, returnMessage));
                    object obj = pubnub.JsonPluggableLibrary.DeserializeToObject(returnMessage);
                    Dictionary<string, object> dict = obj as Dictionary<string, object>;
                    UnityEngine.Debug.Log (string.Format ("DisplayReturnMessageSubscribeString Object: result:{0}\nobj:{1}\nCount:{2}\n", returnMessage,
                        obj.ToString(), dict.Count));

                    bool pairmatch1 = false;
                    bool pairmatch2 = false;
                    foreach(var pair in dict){
                        UnityEngine.Debug.Log (string.Format ("DisplayReturnMessageSubscribeString pair.Key: {0}, pair.Value:{1}", 
                            pair.Key, pair.Value));
                        if(pair.Key.Equals("Event") && (pair.Value.Equals("join"))){
                            pairmatch1 = true;
                        }
                        if(pair.Key.Equals("Channel") && (pair.Value.Equals(ch))){
                            pairmatch2 = true;
                        }
                    }
                    if(pairmatch1 && pairmatch2){
                        bSubMessage2 = true;
                    }
                }, (string retConnect)=>{
                    UnityEngine.Debug.Log (string.Format ("{0}: {1} Subscribe Connected {2}", DateTime.Now.ToString (), testName, retConnect));
                    bSubConnect = true;
                }, this.DisplayErrorMessage); 

            }
            yield return new WaitForSeconds (CommonIntergrationTests.WaitTimeBetweenCallsLow); 
            if(AsObject){
                pubnub.Subscribe<object>(ch, "", (object pub)=>{
                    
                },(object pub)=>{}, (object pub)=>{}, this.DisplayErrorMessage);
            } else {
                pubnub.Subscribe<string>(ch, "", (string pub)=>{
                    
                },
                    (string pub)=>{}, 
 
                    (string pub)=>{}, 
                    this.DisplayErrorMessage);
            }
            yield return new WaitForSeconds (CommonIntergrationTests.WaitTimeBetweenCallsLow); 

            /*⁃   Unsub from CG*/

            bool bUnsub = true;

            yield return new WaitForSeconds (CommonIntergrationTests.WaitTimeBetweenCallsLow);

            string strLog2 = string.Format ("{0}: {1} After wait2   {2}", 
                DateTime.Now.ToString (), 
                testName, 
                bSubMessage2
            );
            UnityEngine.Debug.Log (strLog2);

            if(
                bSubMessage2

            ){
                IntegrationTest.Pass();
            }            
            pubnub.EndPendingRequests ();
            pubnub.CleanUp();
        }

        public void DisplayErrorMessage (PubnubClientError result)
        {
            //DeliveryStatus = true;
            UnityEngine.Debug.Log ("DisplayErrorMessage:" + result.ToString ());
        }

        public void DisplayReturnMessageDummy (object result)
        {
            //deliveryStatus = true;
            //Response = result;
            UnityEngine.Debug.Log ("DisplayReturnMessageDummy:" + result.ToString ());
        }

    }


}

