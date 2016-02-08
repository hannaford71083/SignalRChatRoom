using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using SignalRChat.Common;
using System.Web.Script.Serialization;

namespace SignalRChat
{
    public class ChatHub : Hub
    {
        #region Data Members

        static List<UserDetail> ConnectedUsers = new List<UserDetail>();
        static List<MessageDetail> CurrentMessage = new List<MessageDetail>();
        static List<Group> GroupList = new List<Group>(); 

        #endregion

        #region Methods

        public void Connect(string userName)
        {
            var id = Context.ConnectionId;

            if (ConnectedUsers.Count(x => x.ConnectionId == id) == 0)
            {
                ConnectedUsers.Add(new UserDetail { ConnectionId = id, UserName = userName });

                // send to caller
                Clients.Caller.onConnected(id, userName, ConnectedUsers, CurrentMessage);

                // send to all except caller client
                Clients.AllExcept(id).onNewUserConnected(id, userName);

                this.UpdateClientGroups();

            }

        }

        public void SendMessageToAll(string userName, string message)
        {
            // store last 100 messages in cache
            AddMessageinCache(userName, message);

            // Broad cast message
            Clients.All.messageReceived(userName, message);
        }

        public void SendPrivateMessage(string toUserId, string message)
        {

            string fromUserId = Context.ConnectionId;

            var toUser = ConnectedUsers.FirstOrDefault(x => x.ConnectionId == toUserId) ;
            var fromUser = ConnectedUsers.FirstOrDefault(x => x.ConnectionId == fromUserId);

            if (toUser != null && fromUser!=null)
            {
                // send to 
                Clients.Client(toUserId).sendPrivateMessage(fromUserId, fromUser.UserName, message); 

                // send to caller user
                Clients.Caller.sendPrivateMessage(toUserId, fromUser.UserName, message); 
            }


        }


        public void AddGroup(string userId )
        {

            Guid groupId = Guid.NewGuid();
            Group newGroup = new Group();
            newGroup.id = groupId.ToString();
            UserDetail userDetail = ConnectedUsers.FirstOrDefault(o => o.ConnectionId ==  userId );  //find the userDetails form userId     o => o.Items != null && 

            try
            {
                newGroup.addUserDetail(userDetail);
                GroupList.Add(newGroup);
            }
            catch (Exception e) {
                Console.Write(e.Message); 
            }

            // send a broacast message to all user saying person with userID is in annon group
            //Clients.All.newGroupCreated(userId, groupId);

            this.UpdateClientGroups();

        }




        public void UpdateClientGroups() {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            string output = jss.Serialize(GroupList);
            Clients.All.updateGroupInfo(output);
        }



        public void AddUserToGroup(string userId, string adminID) {

            //loop through all groups 
            GroupList.FirstOrDefault(o => o.getAdminId() == adminID).addUserDetail(
                //add user detail that = userID
                ConnectedUsers.FirstOrDefault(o => o.ConnectionId == userId)
                );

            this.UpdateClientGroups();
        }





        public override System.Threading.Tasks.Task OnDisconnected()
        {
            var item = ConnectedUsers.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            if (item != null)
            {
                ConnectedUsers.Remove(item);
                var id = Context.ConnectionId;
                Clients.All.onUserDisconnected(id, item.UserName);

                //remove this user from any groups
                // TODO : if user is admin remove id and disband group
                foreach(Group group in GroupList){
                    if (group.users.FirstOrDefault(o => o.ConnectionId == id) != null ) {
                        group.removeUserwithId(id);
                    }
                }
                

                this.UpdateClientGroups();

            }

            //is it the 'end of the session', if so then flush objects
            if (ConnectedUsers.Count == 0) {
                CurrentMessage.Clear();
                GroupList.Clear();
            }

            return base.OnDisconnected();
        }

     
        #endregion

        #region private Messages

        private void AddMessageinCache(string userName, string message)
        {
            CurrentMessage.Add(new MessageDetail { UserName = userName, Message = message });

            if (CurrentMessage.Count > 100)
                CurrentMessage.RemoveAt(0);
        }

        #endregion
    }

}