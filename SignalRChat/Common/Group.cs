using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SignalRChat.Common
{
    public class Group
    {

        public string id { get; set; }

        public List<UserDetail> users = new List<UserDetail>();

        public void addUserDetail(UserDetail userDetail ) {
            users.Add(userDetail);
        }

        //Posible Ammendment - add a property for admin instead of taking first item in List
        public string getAdminId()
        {
            return users.First().ConnectionId;
        }

        //removes user from group with a specific ID
        public void removeUserwithId(string id)
        {
            users.Remove(users.FirstOrDefault(o => o.ConnectionId == id));
        }


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