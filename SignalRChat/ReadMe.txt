
**************************    READ ME    **************************

*Currently not a very good readme, is more just notes to keep track of the projec for me (Dave H).


TODO:

- (DONE) Can't deal with multiple tabs opened in one browser (disable on multiple tabs)
- upload and test on AppHarbour

   - ??? no threadsafety for refreshing list 
 7a) (disallow rudewords), also change responsiveness of chat window
 8) Ability to replay game

  9) Work on Mobile
  * Scale Canvas
  - Will be smooth (use of velocity)
  * Implement ability to do this (simple as possible, is it possible to use a library, think about possible future use)

- Ability to output errors to user (not fully implemented )
- Still a bit unpredictable seems to show 2 instances of user on first login (temp solution found)




Presentation Possible Questions/Stuff to Present :

  - Outline What Game can do, what I have been working on (test harness)
    - Problems considering Threadsafety (considering the tradeoff with performance, although this was not measured)

  - Live demo 
    - Get current App to work on AppHarbour
    * Check into Git, give it a whirl :)



  - Demonstrate the application can handle peak load
    - outline this in presentation (ability to do further )
    - Basic Game load testing and Peak load precautions
    * Work out what is the max load that can be handled with test harness and hardcode this into SignalR chat
    * We can potentially add the PerformanceCounter (see what deal is with Azure scaling) http://stackoverflow.com/questions/278071/how-to-get-the-cpu-usage-in-c and https://msdn.microsoft.com/en-us/library/system.diagnostics.performancecounter(v=vs.110).aspx?cs-save-lang=1&cs-lang=csharp#code-snippet-2
    - Testing Chat room
    * Should be a lot easier to do - task for later


  - What is required before can be put live?
    - Need to Develop a way of accessing chat rooms (on different servers) using DB will need to work on this with backend
    - Further load testing? Would like advice on this
    - Needs to be code reviewed
    * refactored, removing a lot of the Dictionaries, or have reference to the GroupList (due to keeping track of signalR Group's th )





STUFF I DONE:
  * Work out what is the max load that can be handled with test harness and hardcode this into SignalR chat
    1) What is the peak time measured, work out appropriate measure, i.e. will this suffer with connection density increase




STUFF TO IMPROVE:
*   The Groups held in lists are seperate from the groups used for SignalR groups, this needs to be possibly refactored as is overcomplicated
*   Have a ref to signal-R group in List Group objects???? (temporary solution maybe)





STUFF TO CONSIDER

Uses siganlR 2 - the latest iteration of signal-R 

    * Can I measure CPU performance ( don't do this for now, best to talk with backend folks about this )
    
    2) Bulid in mechanism to limit users joining game OR EVEN BETTER mechanism that checks server load and blocks users if load is high 
    3) What is peak loading look like on a feature, see Jacobs Google analytics (maybe he can log me in on his account)


_________________________________________________________________________________________________________________________________________

                        SIGNALR NOTES 


http://www.asp.net/signalr/overview/guide-to-the-api/handling-connection-lifetime-events

The SignalR connection lifetime events that may be raised on the client are the following:
  ConnectionSlow client event -> 
    Raised when a preset proportion of the keepalive timeout period has passed since 
    the last message or keepalive ping was received. The default keepalive timeout warning period is 2/3 of the keepalive timeout. 
    The keepalive timeout is 20 seconds, so the warning occurs at about 13 seconds. By default, the server sends keepalive pings every 10 seconds
  Reconnecting client event -> 
    Raised when (a) the transport API detects that the connection is lost, or 
    (b) the keepalive timeout period has passed since the last message or keepalive ping was received. 
  Reconnected client event -> 
    Raised when the transport connection is reestablished. The OnReconnected event handler in the Hub executes.
  Closed client event (disconnected event in JavaScript) -> 
    Raised when the disconnect timeout period expires while the SignalR client code is trying to reconnect after losing the transport connection. 
    The default disconnect timeout is 30 seconds. (This event is also raised when the connection ends because the Stop method is called.)
  ***Important*** : The sequence of events described here is not guaranteed.


When a connection is inactive, periodically the server sends a keepalive packet to the client. 
As of the date this article is being written, the default frequency is every 10 seconds. 

Transports include WebSockets, forever frame, or server-sent events.

In a browser client, the SignalR client code that maintains a SignalR connection runs in the JavaScript context of a web page. 
That's why the SignalR connection has to end when you navigate from one page to another, and that's why you have multiple 
connections with multiple connection IDs if you connect from multiple browser windows or tabs.

Settings:
KeepAlive           - This setting represents the amount of time to wait before sending a keepalive packet over an idle connection. 
                      The default value is 10 seconds. This value must not be more than 1/3 of the DisconnectTimeout value.

ConnectionTimeout   - This setting represents the amount of time to leave a transport connection open and waiting for 
                      a response before closing it and opening a new connection. The default value is 110 seconds.
                      DisconnectTimeout

DisconnectTimeout   - This setting represents the amount of time to wait after a transport connection is lost before raising the Disconnected event. 
                      The default value is 30 seconds. When you set DisconnectTimeout, KeepAlive is automatically set to 1/3 of the DisconnectTimeout value. (must be > 6 secs)

