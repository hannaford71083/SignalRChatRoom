using System;
using System.Linq;
using System.Timers;
using System.Web;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;

namespace SignalRChat.Common
{
    [JsonObject]
    public class GameGroup
    {

        public string id { get; private set; }
        public int countdownTime { get; private set; }
        
        public ConcurrentDictionary<string, PlayerState> PlayerStates = new ConcurrentDictionary<string, PlayerState>();


        public GameGroup(string idIn) {
            this.id = idIn;
            this.countdownTime = 5;
        }


        public void DecrementTimer(){
            this.countdownTime--; //only accessed by 1 thread no need for Thread Safety
        }


        public bool IsDownloadReady() {

            //---- MEASURE ----
            bool allready = true;
            foreach (var item in PlayerStates)
            {
                if (PlayerStates[item.Key].SentLatestFlagRead()) { allready = false; }
            }
            return allready;
            //---- MEASURE ----
        }


        public void ResetSents() {

            //---- MEASURE ----
            foreach (var item in PlayerStates)
            {
                PlayerStates[item.Key].resetSentLatestFlagToFalse(); //Threadsafe uses interlocking
                //PlayerStates[item.Key].SentLatestFlag = false;
            }
            //---- MEASURE ----
        
        }



    }
}