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

        public UserDetail(){
            Status = PlayerStatus.None;        }

    }

   

}