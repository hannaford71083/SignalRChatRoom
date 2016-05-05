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

        /// <summary>
        /// Properties are public as exchanged with Signal-R client
        /// </summary>
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

        internal int GetFinishTimeMS() { 
            return Interlocked.CompareExchange( ref this.FinishTimeMS, 0, 0 ); // like Read, will only exchange 0 with 0! 
        }

        internal void resetSentLatestFlagToFalse()
        {
            lock (this) {
                this.SentLatestFlag = false;
            }   
        }

        internal bool SentLatestFlagRead()
        {
            lock (this)
            {
                return this.SentLatestFlag;
            }  
        }
    }

}