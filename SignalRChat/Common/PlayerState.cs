using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using System.Threading;

namespace SignalRChat.Common
{
    [JsonObject]
    public class PlayerState
    {
        [JsonProperty("userId")]
        public string Id;
        [JsonProperty("groupId")]
        public string GroupId;
        [JsonProperty("distancePresses")]
        public int Clicks;
        [JsonProperty("finishTimeMs")]
        public int FinishTimeMS;
        [JsonIgnore]
        public bool SentLatestFlag;

        internal void UpdateClicks(PlayerState playerState)
        {
            
            Interlocked.Exchange(ref this.Clicks, playerState.Clicks);
            
        }

        internal void resetSentLatestFlagToFalse()
        {
            lock (this) {
                this.SentLatestFlag = false;
            }   
        }




    }

}