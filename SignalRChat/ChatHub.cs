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





        public void SignalStartGame( string groupID)
        {
            var a = groupID;

            Group adminGroup = GroupList.FirstOrDefault(o => o.id == groupID);

            foreach (UserDetail user in adminGroup.users) {
                Groups.Add(user.ConnectionId, groupID);
                
            }


            Clients.Group(groupID).showSplash();

        }

        public void PlayerReady(string playerId, string groupID)
        {

            //var a = playerId;
            //chatHub.server.playerReady(playerId, groupID);
            //looks through all players in group, if all ready then go to next screen

            //find player in group
            GroupList.FirstOrDefault(o => o.id == groupID)
                .users.FirstOrDefault(o => o.ConnectionId == playerId).Status = PlayerStatus.Ready;

            //check that all players in group are set to status ready

            bool isReady = true;

            foreach (UserDetail user in GroupList.FirstOrDefault(o => o.id == groupID).users ) {
                if (user.Status != PlayerStatus.Ready) { isReady = false; }
            } 

            //if isReady send message to users in group.
            if (isReady) {  Clients.Group(groupID).showGameScreen();    }
            
        }

        public void UploadData(string groupId, string playerId, string presses )
        {
            //deserialise
            int keyPresses;
            int.TryParse(presses, out keyPresses);

            //add data for player
            //have we got all uploads
            var a = groupId;


        }



        public override System.Threading.Tasks.Task OnDisconnected()
        {
            var item = ConnectedUsers.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            if (item != null)
            {
                ConnectedUsers.Remove(item);
                var id = Context.ConnectionId;
                Clients.All.onUserDisconnected(id, item.UserName);

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