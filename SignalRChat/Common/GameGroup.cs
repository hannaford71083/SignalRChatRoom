using System;
using System.Linq;
using System.Timers;
using System.Web;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace SignalRChat.Common
{
    public class GameGroup
    {

        public string id { get; private set; }
        public ConcurrentDictionary<string, PlayerState> PlayerStates = new ConcurrentDictionary<string, PlayerState>();


        public int countdownTime { get; private set; }
        

        public GameGroup(string idIn) {
            this.id = idIn;
            this.countdownTime = 5;
        }

        public void DecrementTimer(){
            this.countdownTime--;
        }

    }
}