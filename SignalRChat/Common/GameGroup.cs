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

        public string id;
        public ConcurrentDictionary<string, PlayerState> PlayerStates = new ConcurrentDictionary<string, PlayerState>();
        private System.Timers.Timer _countdownTimerLoop;
        private int _countdownTime;


        public GameGroup(string idIn) {
            id = idIn;
            _countdownTime = 5;
        }

        public void StartCountdown(ChatHub ch) {
            _countdownTimerLoop = new System.Timers.Timer(1000);
            _countdownTimerLoop.Elapsed += (sender, e) => DownloadCountdown(sender, e, this, ch);
            _countdownTimerLoop.Enabled = true; // Enable it
        }

        static void DownloadCountdown(object sender, ElapsedEventArgs e, GameGroup gg, ChatHub ch )
        {
            Debug.WriteLine("COuntdown time : " + gg._countdownTime.ToString() );
            ch.Clients.Group(gg.id).updateCountdown(gg._countdownTime);
            //send all to client groups 
            gg._countdownTime--; 
        }

        //time stamp??


    }
}