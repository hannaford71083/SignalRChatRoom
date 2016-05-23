using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using System.Threading;

namespace SignalRChat.Common
{

    /// <summary>
    /// Class holds data that is exchanged between the Client and Server regarding the state of a particular player ;¬)
    /// </summary>


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
        private bool _sentLatestFlag;
        [JsonIgnore]
        private object _lock = new object();
        [JsonIgnore]
        public bool SentLatestFlag
        {
            set {
                lock(_lock){
                    this._sentLatestFlag = value;
                }
            }
            get {
                lock(_lock){
                    return this._sentLatestFlag;
                }
            }
        }
        
        
            
        internal void UpdateClicks(PlayerState playerState)
        {
            Interlocked.Exchange(ref this.Clicks, playerState.Clicks);
        }

        internal int GetFinishTimeMS() { 
            return Interlocked.CompareExchange( ref this.FinishTimeMS, 0, 0 ); // like Read, will only exchange 0 with 0! 
        }

        internal void resetSentLatestFlagToFalse()
        {
            this.SentLatestFlag = false;
        }

        internal bool SentLatestFlagRead()
        {
            return this.SentLatestFlag;
        }
    }

}