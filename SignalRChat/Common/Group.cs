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
            get{
                lock(_lock){
                    return this._adminId;
                }
            }
            set{
                lock(_lock){
                    this._adminId = value;
                }
            }
        }
       
        public void addUserDetail(UserDetail userDetail ) {
            users.Add(userDetail);
        }

        //Posible Ammendment - add a property for admin instead of taking first item in List
        public string getAdminId()
        {
            //try { 
                if ( String.IsNullOrEmpty(this.adminId ) ){
                    this.adminId = users.First().ConnectionId; // TODO: uses interlocked ??
                }
                return this.adminId;
            //}
            //catch(Exception e){
            //    Debug.WriteLine("ERROR accessing admin ID, message : "+ e.Message);
            //    return null;
            //}
        }

        //removes user from group with a specific ID
        public bool removeUserwithId(string id)
        {
            //users.Remove(users.FirstOrDefault(o => o.ConnectionId == id));
            HubBlockingCollection<UserDetail> newUsers = new HubBlockingCollection<UserDetail>();
            bool modified = false;
            lock (this._usersLock)
            {
                Debug.WriteLine("--------- Remove User -----------");
                foreach (UserDetail user in this.users)
                {
                    if (user.ConnectionId != id) { 
                        newUsers.Add(user);
                        Debug.WriteLine("User Added id : " + user.ConnectionId);
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