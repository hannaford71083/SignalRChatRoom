using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace SignalRChat.Common
{
    public class Group
    {
        public string id { get; set; }
        public string adminId = String.Empty;
        public HubBlockingCollection<UserDetail> users = new HubBlockingCollection<UserDetail>();

        public void addUserDetail(UserDetail userDetail ) {
            users.Add(userDetail);
        }

        //Posible Ammendment - add a property for admin instead of taking first item in List
        public string getAdminId()
        {
            lock (this) { 
                if (this.adminId == String.Empty) {
                    this.adminId = users.First().ConnectionId; // TODO: uses interlocked ??
                }
                return this.adminId;
            }
        }

        //removes user from group with a specific ID
        public void removeUserwithId(string id)
        {
            users.Remove(users.FirstOrDefault(o => o.ConnectionId == id));
        }
        
    }
}