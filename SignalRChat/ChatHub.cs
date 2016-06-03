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
using Newtonsoft.Json;
//using System.Threading;
using NLog;

namespace SignalRChat
{
    [HubName("chatHub")]
    public class ChatHub : Hub
    {
        #region Data Members


        static HubBlockingCollection<UserDetail>    ConnectedUsers  = new HubBlockingCollection<UserDetail>(); //If no constructor will default to ConcurrentQueue<T>
        static HubBlockingCollection<Group>         GroupList       = new HubBlockingCollection<Group>(); 
        static HubBlockingCollection<MessageDetail> CurrentMessage  = new HubBlockingCollection<MessageDetail>(); 

        static ConcurrentDictionary<string, GameGroup>  GameGroups      = new ConcurrentDictionary<string, GameGroup>();
        static ConcurrentQueue<GameGroup>           CountDownQueue      = new ConcurrentQueue<GameGroup>();
        static System.Timers.Timer                  CountdownTimerLoop  = null; //Static timer acts as Clock cycle for instances of Group countdown
        static System.Timers.Timer RePollLoop = null; //Every 40 secs Repolls clients for updated ConnectedUsers

        //Polling Data members
        public static HubBlockingCollection<string[]> PollUserList = new HubBlockingCollection<string[]>();
        private object _lock = new object();
        public static object cpLock = new object();
        private static int _currentPollingID = 0;
        public static int CurrentPollingID
        { //enusre threadsafty of value
            set{   lock (cpLock){   _currentPollingID = value;  }   }
            get{    lock (cpLock){  return _currentPollingID;   }   }
        }

        public static Logger logger = LogManager.GetCurrentClassLogger(); // for Console2Log


        #endregion










        // implements users going from screen to screen etc
        #region Lifecycle Methods

        //used for connection and reconnection
        public void Connect(string userName)
        {

            if (userName.Length > 30 ) {
                Clients.Caller.clientError("User name too long", "The name you submitted was " + userName.Length.ToString() + " , the limit is 30 charters. Please type another."  );
                return;
            }

            string htmlEncodedUserName = HttpUtility.HtmlEncode(userName);
            DateTime rightNow = new DateTime();
            rightNow = DateTime.Now;
            //Init Trace for logging
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
            Debug.WriteLine("ChatHub - Connect() at {0}", rightNow);
            var id = Context.ConnectionId;

            if (ConnectedUsers.Count(x => x.ConnectionId == id) == 0)
            {
                ConnectedUsers.Add(new UserDetail { ConnectionId = id, UserName = htmlEncodedUserName });
                Clients.Caller.onConnected(id, htmlEncodedUserName, ConnectedUsers, CurrentMessage); // send to caller
                Clients.AllExcept(id).updateUsersList(ConnectedUsers);
                this.pollProcess();
            }
            //Debug.WriteLine("---------- Current ConnectedUsers ------------");
            //foreach (UserDetail udItem in ConnectedUsers)
            //{
            //    Debug.WriteLine("UserName : {0} , ConnectionId : {1}", udItem.UserName , udItem.ConnectionId);
            //}
            //Debug.WriteLine("-----------------------------------------------");

            this.StartTimerLoop();
            this.StartRePollLoop();
        }

        public void SignalStartGame( string groupID)
        {
            //limit ammount of games taking place simultaneously
            int maxConnectionsAllowed = 100;  //max connections as obtained from testing on same machine
            if (GameGroups.Count > maxConnectionsAllowed)
            {
                Clients.Caller.clientError("Start Game Error","I'm afraid the server is busy with two many games at the moment, please try again in a minute!");
                return;
            }
            foreach (UserDetail user in GroupList.FirstOrDefault(o => o.id == groupID).users) {
                user.Status = PlayerStatus.OnSplash;
            }
            this.UpdateClientGroups();
            Clients.Group(groupID).showSplash();
        }


        //looks through all players in group, if all ready then go to next screen
        public void PlayerReady(string playerId, string groupID)
        {
            //find player in group
            GroupList.FirstOrDefault(o => o.id == groupID)
                .users.FirstOrDefault(o => o.ConnectionId == playerId).Status = PlayerStatus.Ready;

            bool isReady = true;//check that all players in group are set to status ready

            foreach (UserDetail user in GroupList.FirstOrDefault(o => o.id == groupID).users ) {
                if (user.Status != PlayerStatus.Ready) { isReady = false; }
            }

            if (isReady)//if isReady send message to users in group.
            {
                this.InitGame(groupID);
            }
        }

        private void InitGame(string groupID) 
        {
            GameGroup newGroup = new GameGroup(groupID);
            bool failedAdd = false;
            foreach (UserDetail user in GroupList.FirstOrDefault(o => o.id == groupID).users) {
                PlayerState playerState = new PlayerState();
                playerState.Id = user.ConnectionId;
                if (!newGroup.PlayerStates.TryAdd(user.ConnectionId, playerState)) { failedAdd = true; } //LOW threadsafe Risk, only one client inititates creation of group
            }

            //What if not added initially, requires while loop?
            if(!GameGroups.TryAdd(groupID, newGroup)){ failedAdd = true; } //MEDIUM threadsafe Risk
            
            if(failedAdd){
                Clients.Group(groupID).clientError("Couldn't Add Player", "Failed to Add a player(s) to the game"); //failed to add player to group or group list
            }
            else{
                Clients.Group(groupID).showGameScreen();
                this.StartCountDown(GameGroups[groupID]);
            }
            
        }

        private void StartCountDown(GameGroup gg) {
            while(!CountDownQueue.Contains(gg)) {
                CountDownQueue.Enqueue(gg);
            }
        }

        public void EndGame(PlayerState playerState)
        {

            //Need to update the end time only
            GameGroups[playerState.GroupId].PlayerStates[playerState.Id] = playerState;

            // check group has finished
            Debug.WriteLine("------- Loop -------");
            bool areAllFinished = true;
            foreach (PlayerState ps in GameGroups[playerState.GroupId].PlayerStates.Values)
            {
                if (ps.GetFinishTimeMS() <= 0 && !ps.playerLeftGame)
                {
                    areAllFinished = false;
                }
                Debug.WriteLine("finsihTime : " + ps.FinishTimeMS);
            }

            if (areAllFinished)
            {
                var sortedList = GameGroups[playerState.GroupId].PlayerStates.OrderBy(kp => kp.Value.FinishTimeMS).ToList();
                Clients.Group(playerState.GroupId).clientEndGame(sortedList);
            }

        }

        public override System.Threading.Tasks.Task OnDisconnected(bool stopCalled)
        {
            Debug.WriteLine("Task OnDisconnected");
            this.pollProcess();
            return base.OnDisconnected(stopCalled);
        }

        public override System.Threading.Tasks.Task OnReconnected()
        {
            return base.OnReconnected();
        }

        private void GarbagCollect()
        {
            CurrentMessage.Clear();
            GroupList.Clear();

            //Clear Queue if reinstatiating
            while (!CountDownQueue.IsEmpty)
            {
                GameGroup gg;
                CountDownQueue.TryDequeue(out gg);
            }

            //dispose timer and set to null (needed for previous logic)
            CountdownTimerLoop.Dispose();
            CountdownTimerLoop = null;
            RePollLoop.Dispose();
            RePollLoop = null;

        }

        #endregion









        //Methods to handle user and game requests
        #region Chatroom/Game Functionality Methods


        public void SendMessageToAll(string userName, string message)
        {
            string htmlEncodedMessage = HttpUtility.HtmlEncode(message);

            // store last 100 messages in cache
            AddMessageinCache(userName, htmlEncodedMessage);
            // Broadcast message
            Clients.All.messageReceived(userName, htmlEncodedMessage);
        }

        private void AddMessageinCache(string userName, string message)
        {
            CurrentMessage.Add(new MessageDetail { UserName = userName, Message = message }); // TODO: Chat Lists/objects not threadsafe
        }

        public void SendPrivateMessage(string toUserId, string message)
        {
            string fromUserId = Context.ConnectionId;
            var toUser = ConnectedUsers.FirstOrDefault(x => x.ConnectionId == toUserId);
            var fromUser = ConnectedUsers.FirstOrDefault(x => x.ConnectionId == fromUserId);
            if (toUser != null && fromUser != null)
            {
                Clients.Client(toUserId).sendPrivateMessage(fromUserId, fromUser.UserName, message);  // send to 
                Clients.Caller.sendPrivateMessage(toUserId, fromUser.UserName, message);              // send to caller user
            }
        }


        public void AddGroup(string userId)
        {
            Debug.WriteLine("ChatHub - AddGroup() - attempt add user ID: " + userId);
            Guid groupId = Guid.NewGuid();
            Group newGroup = new Group();

            string groupIdString = groupId.ToString();
            Interlocked.Exchange(ref newGroup.id, groupIdString);//newGroup.id = groupId.ToString();
            newGroup.adminId = userId;
            UserDetail userDetail = ConnectedUsers.FirstOrDefault(o => o.ConnectionId == userId);  //find the userDetails form userId     o => o.Items != null && 
            try
            {
                newGroup.addUserDetail(userDetail);
                GroupList.Add(newGroup);
                Groups.Add(userId, newGroup.id);
            }
            catch (Exception e)
            {
                Debug.WriteLine("ChatHub - AddGroup() error : " + e.Message);
            }
            this.UpdateClientGroups();
        }


        public void UpdateClientGroups()
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            string output = jss.Serialize(GroupList);
            Clients.All.updateGroupInfo(output);
        }


        public void AddUserToGroup(string userId, string adminID)
        {
            if (GroupList.FirstOrDefault(o => o.getAdminId() == adminID).users.Count() >= 4)
            {
                Clients.Caller.clientError("Add User Error", "Group already has max memebers");
            }
            else
            {
                Debug.WriteLine("ChatHub - AddUserToGroup() - user ID: " + userId + ", admin ID: " + adminID);
                //loop through all groups 
                GroupList.FirstOrDefault(o => o.getAdminId() == adminID).addUserDetail(
                    //add user detail that = userID
                    ConnectedUsers.FirstOrDefault(o => o.ConnectionId == userId)
                    );
                Groups.Add(userId, GroupList.FirstOrDefault(o => o.getAdminId() == adminID).id); //SiganalR Group
                this.UpdateClientGroups();
            }
        }

        //Uploads from Client, finds and updates current client, then sends update to group if has latest info
        public void UploadData(PlayerState playerState)
        {
            try
            {
                GameGroups[playerState.GroupId].UpdateState(playerState, this);
            }
            catch (Exception e)
            {
                Debug.WriteLine("UploadData() error, message: " + e.Message);
            }
        }

        #endregion







        //Processes such as countdown timer and user repolling
        #region background process Methods

        /// <summary>
        /// Timer Loop for the 'Countdown' process to start race hardcoded to run every 1000ms, could be refactored to be something that could run x times every second
        ///     this could be useful in order to stagger starting times, or for the pushing of data to users.
        /// </summary>

        private void StartTimerLoop()
        {
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


        private void StartRePollLoop()
        {
            //initialise only one timer loop
            if (RePollLoop == null)
            {
                Debug.WriteLine("CREATING TIMER");
                RePollLoop = new System.Timers.Timer(40000);  //40secs
                RePollLoop.Elapsed += (sender, e) => RePollLoopExecute(sender, e, this);
                RePollLoop.Enabled = true; // Enable it
            }
            else
            {
                Debug.WriteLine("TIMER ALREADY EXISTS");  
            }
        }
        
        //loops through all groups, sends update times to affected groups
        private static void CountDownLoopExecute(object sender, ElapsedEventArgs e, ChatHub ch)
        {

            if (CountDownQueue.Count > 0) {
                Debug.WriteLine("Countdown Execute - length is " + CountDownQueue.Count);
            }

            foreach (GameGroup gameGroup in CountDownQueue) { 
                gameGroup.DecrementTimer();
                ch.Clients.Group(gameGroup.id).updateCountdown(gameGroup.countdownTime); 
                if (gameGroup.countdownTime <= 0) { //if timer = 0 then dequeue this group
                    GameGroup gg;
                    CountDownQueue.TryDequeue(out gg);
                }
            }

        }


        private static void RePollLoopExecute(object sender, ElapsedEventArgs e, ChatHub ch)
        {
            ch.pollProcess();
        }


        public void PollUserUpdate(string id, string pollId){
            string[] args = { id, pollId };
            PollUserList.Add(args);
        }


        private void updatePollResultClients(int pollId)
        {
            lock (this._lock) {
                
                foreach (UserDetail ud in ConnectedUsers) //set all to false
                {
                    ud.UserPresentInPoll = false;
                }

                //mark the ConnectedUsers as connected according to latest poll 
                foreach(string[] args in PollUserList){
                    string connectionId = args[0];
                    int udPollId = 0;
                    int.TryParse(args[1], out udPollId);
                    if(udPollId == pollId){
                        try
                        {
                            ConnectedUsers.First(o => o.ConnectionId == connectionId).UserPresentInPoll = true;
                        }
                        catch (Exception e) { 
                            Debug.WriteLine("Error finding item, Error : "+ e.Message);
                        }
                    }
                }

                HubBlockingCollection<UserDetail> tempList = new HubBlockingCollection<UserDetail>();
                //remove ConnectedUsers not present in latest poll
                foreach (UserDetail ud in ConnectedUsers)
                {
                    if (ud.UserPresentInPoll == false)
                    {
                        UserDetail userToRemove = ud;
                        this.removeUserFromAllGroups(userToRemove);
                    }
                    else {
                        tempList.Add(ud);
                    }
                }

                //TODO: ****Make more threadsafe***
                ConnectedUsers = tempList;

                Clients.All.updateUsersList(ConnectedUsers); //send updated group to clients :¬D
                this.UpdateClientGroups();
            }
        }


        private void removeUserFromAllGroups(UserDetail userToRemove)
        {
            string userId = userToRemove.ConnectionId;
            string groupId = "";
            foreach(Group group in GroupList){
                if(group.users.Contains(userToRemove)){
                    group.removeUserwithId(userId);
                    try
                    {
                        GameGroups[group.id].PlayerStates[userId].playerLeftGame = true;
                        GameGroups[group.id].PlayerStates[userId].FinishTimeMS = 100000; //if user leaves sets time to 100 secs
                    }
                    catch (Exception e) {
                        Debug.WriteLine("Key Not Found Exeception : "+ e.Message);
                    }
                }
            }

        }


        private void pollProcess() {
            CurrentPollingID += 1; //CurrentPollingID used to use .... Interlocked.Add(ref CurrentPollingID, 1);

            int instancePollId = CurrentPollingID;
            Thread.Sleep(1000);
            Clients.All.pollUserCheck(CurrentPollingID);

            Thread.Sleep(2000);
            if (CurrentPollingID == instancePollId)
            {
                this.updatePollResultClients(instancePollId);
            }

            Thread.Sleep(1000);
            if (CurrentPollingID == instancePollId)
            {
                CurrentPollingID = 0;
                PollUserList.Clear();
                //is it the 'end of the session', if so then flush objects
                if (ConnectedUsers.Count == 0)
                {
                    this.GarbagCollect();
                }
            }
        
        }

        #endregion










        #region Test Harness Methods



        //Method is for testing harness
        public void ConnectTestUser(string userName)
        {
            var id = Context.ConnectionId;
            if (ConnectedUsers.Count(x => x.ConnectionId == id) == 0)
            {
                ConnectedUsers.Add(new UserDetail { ConnectionId = id, UserName = userName });
                //Clients.AllExcept(id).onNewUserConnected(id, userName); //Reduces initilise time for large numbers -  // send to all except caller client
            }
        }

        //for load test harness user genration 
        public void AssignTestUsersToGroup()
        {
            int userInGroupI = 0;
            string adminforGroupId = "";
            foreach (UserDetail user in ConnectedUsers.ToList())
            {
                //every 4 people setup new group
                if (userInGroupI == 0)
                {
                    //create user - add to general users list
                    adminforGroupId = user.ConnectionId;
                    ConnectedUsers.Add(new UserDetail { ConnectionId = adminforGroupId, UserName = user.UserName }); //user auto added to Clients.All SignalR group system


                    Guid groupId = Guid.NewGuid();
                    Group newGroup = new Group();
                    newGroup.id = groupId.ToString();
                    UserDetail userDetail = ConnectedUsers.FirstOrDefault(o => o.ConnectionId == adminforGroupId);  //find the userDetails form userId     o => o.Items != null && 

                    //this.AddUserToGroup(string userId, string adminID)

                    try
                    {
                        newGroup.addUserDetail(userDetail);
                        GroupList.Add(newGroup);
                        Groups.Add(adminforGroupId, newGroup.id);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("ChatHub - NonBlockingConsumer - error message : " + e.Message);
                    }
                }
                else
                {
                    //Linq statement taken from AddUserToGroup(string userId, string adminID) 
                    GroupList.FirstOrDefault(o => o.getAdminId() == adminforGroupId).addUserDetail(
                        ConnectedUsers.FirstOrDefault(o => o.ConnectionId == user.ConnectionId));

                    string groupId = GroupList.FirstOrDefault(o => o.getAdminId() == adminforGroupId).id;
                    Groups.Add(user.ConnectionId, groupId);

                }
                //loop through 0,1,2,3 then back 
                int beforeuserInGroupI = userInGroupI;
                userInGroupI = userInGroupI == 3 ? 0 : userInGroupI += 1;
                Clients.Client(user.ConnectionId).UploadListInfo(user.ConnectionId, GroupList.FirstOrDefault(o => o.getAdminId() == adminforGroupId).id); // send group info to client
            }

            Debug.WriteLine("----- ----- Game Groups Assignment ----- ----- ");
            foreach (Group group in GroupList)
            {
                this.addGameGroupAndUsers(group.id);
            }
            var a = GameGroups;
            Debug.WriteLine("----- ----- ----- ----- ----- ----- ");
        }


        ///for load test harness - convert add game group from GroupList info
        private void addGameGroupAndUsers(string groupID)
        {
            GameGroup newGameGroup = new GameGroup(groupID);
            bool failedAdd = false;
            foreach (UserDetail user in GroupList.FirstOrDefault(o => o.id == groupID).users)
            {
                PlayerState playerState = new PlayerState();
                playerState.Id = user.ConnectionId;
                if (!newGameGroup.PlayerStates.TryAdd(user.ConnectionId, playerState)) { failedAdd = true; } //LOW threadsafe Risk, only one client inititates creation of group
            }
            //What if not added initially, requires while loop?
            if (!GameGroups.TryAdd(groupID, newGameGroup)) { failedAdd = true; } //MEDIUM threadsafe Risk
            if (failedAdd)
            {
                //TODO: Add ClientError() callback
                Debug.WriteLine("Failed to Add Player to Group or Group to List");
            }
            else
            {
                Debug.WriteLine("Added Following Group " + newGameGroup.id + " !!!!!! ");
            }
        }


        #endregion



    }





}



// ---------------- ORIGINALLY IN CODE ---------------- 

// use this here :  http://www.asp.net/signalr/overview/getting-started/tutorial-high-frequency-realtime-with-signalr

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