using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SignalRChat.Common
{

    public enum PlayerStatus
    {
        None,
        Ready
    };


    public class UserDetail
    {
        
        public string ConnectionId { get; set; }
        public string UserName { get; set; }
        public PlayerStatus Status { get; set; }
        public bool SentLatest { get; set; }
        public int KeyPresses { get; set; }

        public UserDetail(){
            this.Status = PlayerStatus.None;
            this.SentLatest = false;
        }

        public void updateKeyPresses(int keyPresses) {
            if (!this.SentLatest) {
                this.KeyPresses = keyPresses;
                this.SentLatest = true;
            }
        }


    }

   

}