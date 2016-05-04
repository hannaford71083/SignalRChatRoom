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
            this.countdownTime--;
        }


        public bool IsDownloadReady() {
            bool allready = true;
            foreach (var item in PlayerStates)
            {
                if (PlayerStates[item.Key].SentLatestFlag) { allready = false; }
            }
            return allready;
        }


        public void ResetSents() {
            foreach (var item in PlayerStates)
            {
                PlayerStates[item.Key].resetSentLatestFlagToFalse();
                //PlayerStates[item.Key].SentLatestFlag = false;
            }
        
        }



    }
}