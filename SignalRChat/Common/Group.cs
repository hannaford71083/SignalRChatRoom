using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;


namespace SignalRChat.Common
{
    public class Group
    {
        public string id; //{ get; set; }
        public HubBlockingCollection<UserDetail> users = new HubBlockingCollection<UserDetail>();
        private object _lock = new object();
        private object _usersLock = new object();
        private string _adminId;
        public string adminId
        {
            get{    lock(_lock){    return this._adminId;   }   }
            set{    lock(_lock){    this._adminId = value;  }   }
        }
       
        public void addUserDetail(UserDetail userDetail ) {
            users.Add(userDetail);
        }

        public string getAdminId()
        {
            if ( String.IsNullOrEmpty(this.adminId ) ){
                this.adminId = users.First().ConnectionId; 
            }
            return this.adminId;
        }

        //removes user from group with a specific ID
        public bool removeUserwithId(string id)
        {
            HubBlockingCollection<UserDetail> newUsers = new HubBlockingCollection<UserDetail>();
            bool modified = false;
            lock (this._usersLock)
            {
                foreach (UserDetail user in this.users)
                {
                    if (user.ConnectionId != id) { 
                        newUsers.Add(user);
                        //ChatHub.DebugOut("User Added id : " + user.ConnectionId);
                    }
                    else { modified = true; }
                }
                if (modified == true)
                {
                    this.users = newUsers;
                }
                return modified;
            }
            return false;
        }
        
    }
}