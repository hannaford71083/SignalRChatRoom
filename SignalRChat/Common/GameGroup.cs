using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SignalRChat.Common
{
    public class GameGroup
    {

        public string id;
        public BlockingCollection<Dictionary<string, PlayerState>> PlayerStates = new BlockingCollection<Dictionary<string, PlayerState>>(); 

        //time stamp??



    }
}