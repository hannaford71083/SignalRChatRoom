using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SignalRChat.Common
{
    public class Group
    {

        public string id { get; set; }
        private string _adminId = String.Empty;

        public List<UserDetail> users = new List<UserDetail>();

        public void addUserDetail(UserDetail userDetail ) {
            if (users.Count == 0) { //this is the group admin
                _adminId = userDetail.ConnectionId;
            }
            users.Add(userDetail);
        }

        //Posible Ammendment - add a property for admin instead of taking first item in List
        public string getAdminId()
        {
            if (_adminId == String.Empty) {
                _adminId = users.First().ConnectionId;
            }
            return this._adminId;
        }

        //removes user from group with a specific ID
        public void removeUserwithId(string id)
        {
            users.Remove(users.FirstOrDefault(o => o.ConnectionId == id));
        }




        //----- Game Methods ----- (need to rethink lifecycle of updating game events) 

        //loops group and see if SentLatest true for all
        public bool isDownloadReady()
        {
            bool check = true;
            foreach(UserDetail user in this.users)
                if (!user.SentLatest) { check = false;  }
            return check;
        }

        //reset sents
        public void resetSents()
        {

            foreach (UserDetail user in this.users)
                user.SentLatest = false;
        }

        
    }
}