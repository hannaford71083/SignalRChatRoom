﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using SignalRChat.Common;
using System.Web.Script.Serialization;
using System.Timers;
using Microsoft.AspNet.SignalR.Hubs;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
//using System.Threading;


namespace SignalRChat
{
    [HubName("chatHub")]
    public class ChatHub : Hub
    {
        #region Data Members

        /*
         ---- TASKS ----
         
         Start time: 11:41  ,  duration: 1 hr, Deadline: 12:11
         2) Look at Todos
         (MAYBE)    4) Look at Lifecycle of update/broadcast of options
         
         */

        static List<UserDetail> ConnectedUsers = new List<UserDetail>(); //If no constructor will default to ConcurrentQueue<T>
        static List<MessageDetail> CurrentMessage = new List<MessageDetail>();
        static List<Group> GroupList = new List<Group>();

        // use this here :  http://www.asp.net/signalr/overview/getting-started/tutorial-high-frequency-realtime-with-signalr
        // use [JsonProperty("left")] in classes (see link)
        static ConcurrentDictionary<string, GameGroup> GameGroups = new ConcurrentDictionary<string, GameGroup>();

        //Static timer acts as Clock cycle for instances of Group countdown
        static System.Timers.Timer CountdownTimerLoop = null;
        static ConcurrentQueue<GameGroup> CountDownQueue = new ConcurrentQueue<GameGroup>();

        #endregion





        #region Methods

        public void Connect(string userName)
        {
            DateTime rightNow = new DateTime();
            rightNow = DateTime.Now;

            //Init Trace for logging
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
            Debug.WriteLine("ChatHub - Connect() at {0}", rightNow);

            var id = Context.ConnectionId;
            if (ConnectedUsers.Count(x => x.ConnectionId == id) == 0)
            {
                ConnectedUsers.Add(new UserDetail { ConnectionId = id, UserName = userName });
                Clients.Caller.onConnected(id, userName, ConnectedUsers, CurrentMessage); // send to caller
                Clients.AllExcept(id).onNewUserConnected(id, userName); // send to all except caller client
            }

            this.StartTimerLoop();
        }


        //Method is for testing harness
        public void ConnectTestUser(string userName) {
            var id = Context.ConnectionId;
            if (ConnectedUsers.Count(x => x.ConnectionId == id) == 0)
            {
                ConnectedUsers.Add(new UserDetail { ConnectionId = id, UserName = userName }); 
                //Clients.AllExcept(id).onNewUserConnected(id, userName); //Reduces initilise time for large numbers -  // send to all except caller client
            }
        }


        //for load test harness user genration 
        public void AssignTestUsersToGroup() {
            int userInGroupI = 0;
            string adminforGroupId = "";
            foreach (UserDetail user in ConnectedUsers.ToList())
            {
                //every 4 people setup new group
                if (userInGroupI == 0)
                {
                    adminforGroupId = user.ConnectionId;
                    ConnectedUsers.Add(new UserDetail { ConnectionId = adminforGroupId, UserName = user.UserName });
                    Guid groupId = Guid.NewGuid();
                    Group newGroup = new Group();
                    newGroup.id = groupId.ToString();
                    UserDetail userDetail = ConnectedUsers.FirstOrDefault(o => o.ConnectionId == adminforGroupId);  //find the userDetails form userId     o => o.Items != null && 
                    try
                    {
                        newGroup.addUserDetail(userDetail);
                        GroupList.Add(newGroup);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("ChatHub - NonBlockingConsumer - error message : "+ e.Message);
                    }
                }
                else
                {
                    //Linq statement taken from AddUserToGroup(string userId, string adminID) 
                    GroupList.FirstOrDefault(o => o.getAdminId() == adminforGroupId).addUserDetail(
                        ConnectedUsers.FirstOrDefault(o => o.ConnectionId == user.ConnectionId));
                }

                //loop through 0,1,2,3 then back 
                if (userInGroupI == 3)
                {
                    userInGroupI = 0;
                }
                else
                {
                    userInGroupI += 1;
                }

                Clients.Client(user.ConnectionId).UploadListInfo(user.ConnectionId, GroupList.FirstOrDefault(o => o.getAdminId() == adminforGroupId).id);
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
            var toUser        = ConnectedUsers.FirstOrDefault(x => x.ConnectionId == toUserId) ;
            var fromUser      = ConnectedUsers.FirstOrDefault(x => x.ConnectionId == fromUserId);

            if (toUser != null && fromUser!=null)
            {
                Clients.Client(toUserId).sendPrivateMessage(fromUserId, fromUser.UserName, message);  // send to 
                Clients.Caller.sendPrivateMessage(toUserId, fromUser.UserName, message);              // send to caller user
            }
        }



        public void AddGroup(string userId )
        {

            Debug.WriteLine("ChatHub - AddGroup() - attempt add user ID: " + userId );  
            Guid groupId = Guid.NewGuid();
            Group newGroup = new Group();
            newGroup.id = groupId.ToString();
            newGroup.adminId = userId;
            UserDetail userDetail = ConnectedUsers.FirstOrDefault(o => o.ConnectionId ==  userId );  //find the userDetails form userId     o => o.Items != null && 
            try
            {
                newGroup.addUserDetail(userDetail);
                GroupList.Add(newGroup);
            }
            catch (Exception e) {
                Debug.WriteLine("ChatHub - AddGroup() error : "+ e.Message);  
            }
            this.UpdateClientGroups();
        }


        public void UpdateClientGroups() {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            string output = jss.Serialize(GroupList);
            Clients.All.updateGroupInfo(output);
        }


        public void AddUserToGroup(string userId, string adminID) {
            Debug.WriteLine("ChatHub - AddUserToGroup() - user ID: " + userId + ", admin ID: " + adminID);  
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
            if (isReady) {
                this.InitGame(groupID);
            }            
        }

        
        private void InitGame(string groupID) {
            //loop through players group
            GameGroup newGroup = new GameGroup(groupID);
            foreach (UserDetail user in GroupList.FirstOrDefault(o => o.id == groupID).users) {
                PlayerState playerState = new PlayerState();
                playerState.id = user.ConnectionId;
                newGroup.PlayerStates.TryAdd(user.ConnectionId, playerState);
            }
            GameGroups.TryAdd(groupID, newGroup);
            Clients.Group(groupID).showGameScreen();
            this.StartCountDown(GameGroups[groupID]);
        }

        private void StartCountDown(GameGroup gg) {
            while(!CountDownQueue.Contains(gg)) {
                CountDownQueue.Enqueue(gg);            
            }
        }


        /// <summary>
        /// Timer Loop for the 'Countdown' process to start race hardcoded to run every 1000ms, could be refactored to be something that could run x times every second
        ///     this could be useful in order to stagger starting times, or for the pushing of data to users.
        /// </summary>
 
        private void StartTimerLoop(){
            //initialise only one timer loop, like a Singleton pattern :¬D
            if (CountdownTimerLoop == null)
            {
                Debug.WriteLine("CREATING TIMER");
                CountdownTimerLoop = new System.Timers.Timer(1000);
                CountdownTimerLoop.Elapsed += (sender, e) => CountDownLoopExecute(sender, e, this);
                CountdownTimerLoop.Enabled = true; // Enable it
            }
            else
            {
                Debug.WriteLine("TIMER ALREADY EXISTS");  
            }
        }

        //loops through all groups, sends update times to affected groups
        private static void CountDownLoopExecute(object sender, ElapsedEventArgs e, ChatHub ch)
        {
            Debug.WriteLine("Countdown Execute - length is " + CountDownQueue.Count);
            foreach (GameGroup gameGroup in CountDownQueue) { 
                gameGroup.DecrementTimer();
                ch.Clients.Group(gameGroup.id).updateCountdown(gameGroup.countdownTime); 
                if (gameGroup.countdownTime <= 0) { //if timer = 0 then dequeue this group
                    GameGroup gg;
                    CountDownQueue.TryDequeue(out gg);
                }
            }
        }




        public void UploadData(string groupId, string playerId, string presses )
        {
            //deserialise
            int keyPresses;
            int.TryParse(presses, out keyPresses);
            //add data for player
            //have we got all uploads
            var a = groupId;

            //TODO - try/catch put here

            //find group, find player   //TODO : Make users group thread safe
            GroupList.FirstOrDefault(o => o.id == groupId)
                .users.FirstOrDefault(o => o.ConnectionId == playerId)
                .updateKeyPresses(keyPresses);

            if (GroupList.FirstOrDefault(o => o.id == groupId).isDownloadReady())
            {
                //update position
                JavaScriptSerializer jss = new JavaScriptSerializer();
                string output = jss.Serialize(GroupList.FirstOrDefault(o => o.id == groupId));
                Clients.Group(groupId).updateGame(output);

                GroupList.FirstOrDefault(o => o.id == groupId).resetSents();
            }
            
        }


        public void EndGame(string groupId, string finishObject){
            Clients.Group(groupId).stopGameInterupt(finishObject);
        }
       



        public override System.Threading.Tasks.Task OnDisconnected(bool stopCalled) 
        {
            var item = ConnectedUsers.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            if (item != null)
            {
                while (ConnectedUsers.Contains(item)) { 
                    var id = Context.ConnectionId;
                    ConnectedUsers.Remove(item);
                    Clients.All.onUserDisconnected(id, item.UserName);

                    foreach (Group group in GroupList)
                    {
                        if (group.users.FirstOrDefault(o => o.ConnectionId == id) != null)
                        {
                            group.removeUserwithId(id); //TODO: REFACTOR LATER - NOT THREAD SAFE
                        }
                    }
                }
                UpdateClientGroups();
                // << REMOVED CONTENT SEE BOTTOM >>
            }
            //is it the 'end of the session', if so then flush objects
            if (ConnectedUsers.Count == 0) {
                this.GarbagCollect();
            }
            return base.OnDisconnected(stopCalled); 
        }


        private void GarbagCollect() {
            //CurrentMessage.Dispose();
            CurrentMessage.Clear();
            GroupList.Clear();
            
            //Clear Queue if reinstatiating
            while (!CountDownQueue.IsEmpty)
            {
                GameGroup gg;
                CountDownQueue.TryDequeue(out gg);
            }

            //stop/dispose timer
            //CountdownTimerLoop.Stop();
            CountdownTimerLoop.Dispose();
            CountdownTimerLoop = null;
        }



        //<< STATIC FUNCITONS WHERE HERE >>
     
        #endregion

        #region private Messages

        private void AddMessageinCache(string userName, string message)
        {
            CurrentMessage.Add(new MessageDetail { UserName = userName, Message = message });

            //TODO : ADD limit back
            //if (CurrentMessage.Count > 100)
            //    CurrentMessage.RemoveAt(0);
        }

        #endregion
    }

}


// ---------------- ORIGINALLY IN CODE ---------------- 


// ------- AS FOUND IN public override System.Threading.Tasks.Task OnDisconnected(bool stopCalled) ------- 

//(CLASS VARIABLE) delegate void removeGroup(ChatHub ch, UserDetail item, string id);

//CheckAndRemoveGroup(this, item, id);
//Action<ChatHub, UserDetail, string> removeGroup = CheckAndRemoveGroup;
//object[] objects = new object[] {  this, item, id };
//Task task = new Task(removeGroup);
//Action removeGroup<string, int, int > = 
//switch to this.Remove(item);
//Task< UserDetail> tasky = new Task<UserDetail>(  x => Console.Write(""+ x.ConnectionId ); , 1000   );
//Task afterSuccessfulRemove = new Task(delegate { CheckAndRemoveGroup(this, item, id); });
//ConnectedUsers.RemoveAndCallback(item, afterSuccessfulRemove);
//NonBlockingConsumer<UserDetail>(ConnectedUsers, new CancellationToken(), item );
//Have this.Remove(item, delegate) override with a delegate that contains stuff below
//this.Clients.All.onUserDisconnected(id, item.UserName);

//foreach(Group group in GroupList){
//    if (group.users.FirstOrDefault(o => o.ConnectionId == id) != null ) {
//        group.removeUserwithId(id); //REFACTOR LATER - NOT THREAD SAFE
//    }
//}

//this.UpdateClientGroups();

// ------------ END --------------- OnDisconnected(bool stopCalled) ------------ END ---------------



////id Clients item this
//public static void CheckAndRemoveGroup( SignalRChat.ChatHub ch, SignalRChat.Common.UserDetail item, string id ) {
//    Debug.WriteLine("ChatHub - CheckAndRemoveGroup(xxxx)");
//    ch.Clients.All.onUserDisconnected(id, item.UserName);
//    foreach (Group group in GroupList)
//    {
//        if (group.users.FirstOrDefault(o => o.ConnectionId == id) != null)
//        {
//            group.removeUserwithId(id); //REFACTOR LATER - NOT THREAD SAFE
//        }
//    }
//    ch.UpdateClientGroups();
//}

////TODO: Unhandled Error, is CancellationToken needed as parameter to be passed in (more research required), also access modifier required?
//static void NonBlockingConsumer<T>( BlockingCollection<T> bc, CancellationToken ct, T item)
//{
//    try
//    {
//        if (!bc.TryTake(out item, 1000, ct)) //TODO: What is a reasonible time to be waiting? Should time in MS be passed in as argument
//        {
//            Debug.WriteLine("ChatHub - NonBlockingConsumer - Take Blocked");
//        }
//        else {
//            Debug.WriteLine("ChatHub - NonBlockingConsumer - Take:  " + item.ToString());
//        }

//    }
//    catch (OperationCanceledException) {
//        Debug.WriteLine("ChatHub - NonBlockingConsumer -Taking canceled.");
//        //break;
//    }
//}




//Trace.AutoFlush = true;
//Trace.Indent();
//Trace.WriteLine("Entering Main");
//Console.WriteLine("Hello World.");
//Trace.WriteLine("Exiting Main"); 
//Trace.Unindent();




//// IsCompleted == (IsAddingCompleted && Count == 0)
//while (!bc.IsCompleted)
//{
//    //int nextItem = 0;
//    try
//    {
//        //if (!bc.TryTake(out nextItem, 0, ct))
//        if (!bc.TryTake(out item, 0, ct))
//        {
//            Console.WriteLine(" Take Blocked");
//        }
//        else
//            Console.WriteLine(" Take:{0}", item.ToString());
//    }

//    catch (OperationCanceledException)
//    {
//        Console.WriteLine("Taking canceled.");
//        break;
//    }

//    // Slow down consumer just a little to cause
//    // collection to fill up faster, and lead to "AddBlocked"
//    // Thread.SpinWait(500000);
//}

//Console.WriteLine("\r\nNo more items to take.");