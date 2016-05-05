using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SignalRChat.Common
{

    public enum PlayerStatus
    {
        None,
        OnSplash,
        Ready
    };


    public class UserDetail
    {
        
        public string ConnectionId { get; set; }
        public string UserName { get; set; }
        public bool SentLatest { get; set; }
        public int KeyPresses { get; set; }
        private PlayerStatus _status;
        private object _lock = new object();
        public PlayerStatus Status
        {
            get
            {
                lock (_lock)
                {
                    return this._status;
                }
            }
            set
            {
                lock(_lock){
                    this._status = value;
                }
            }
        }

        public UserDetail(){
            this.Status = PlayerStatus.None;
            this.SentLatest = false;
        }


    }

   

}