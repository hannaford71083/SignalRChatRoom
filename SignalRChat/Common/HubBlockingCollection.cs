﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Web;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

/*
    -----------  ----------- NOTES -----------  ----------- 
 * 
 *  Class is a way to easily replace all List<> usuage (including syntax e.g. Add method) into a Thread safe collection
 *
 * ---- FUTURE IMPROVEMENTS ---
 * 1) Think about how to handle exceptions, throw exceptions up a level ???
 * 2) Is there a test to see how this works :¬s
 
 
 */


namespace SignalRChat.Common
{
    public class HubBlockingCollection<T> : BlockingCollection<T>
    {

        private object _lock = new object();

        //Add method tends to work well
        public void Add(T item, int periodInMs = 1000) { //waiting time is 1 sec by default
            //ChatHub.DebugOut("HubBlockingCollection<"+ item.GetType() +"> - Add() ");
            try
            {
                if (!this.TryAdd(item))
                {
                    ChatHub.DebugOut("HubBlockingCollection - Add() -  Add BLOCKED " + item.ToString());
                }
                else {
                   ChatHub.DebugOut("HubBlockingCollection - Add() -  Add: " + item.ToString());
                }                

            }
            catch (OperationCanceledException e)
            {
                ChatHub.DebugOut("HubBlockingCollection - Add() - Adding canceled message : " + e.Message);
            }
            
        }

        //TODO: Remove does not work well, Refactor needed
        public void Remove(T item, int periodInMs = 1000) {
            CancellationToken ct = new CancellationToken(); //TODO: deal with ct, using 'if(cancelToken.IsCancellationRequested)'
            try
            {
                if (!this.TryTake(out item, 1000, ct)) //TODO: What is a reasonible time to be waiting? Should time in MS be passed in as argument
                {
                    ChatHub.DebugOut("HubBlockingCollection - Remove() - Take Blocked " + item.ToString());
                }
                else {
                    ChatHub.DebugOut("HubBlockingCollection - Remove() - Take : "+ item.ToString());
                }
            }
            catch (OperationCanceledException)
            {
                ChatHub.DebugOut("HubBlockingCollection - Remove() -Taking canceled.");
                //break;
            }
        }

        //Same as Remove with callback
        public void RemoveAndCallback(T item, Task task  ,  int periodInMs = 1000)
        {
            CancellationToken ct = new CancellationToken();//TODO: deal with ct, using 'if(cancelToken.IsCancellationRequested)'
            try
            {
                if (!this.TryTake(out item, 1000, ct)) //TODO: What is a reasonible time to be waiting? Should time in MS be passed in as argument
                {
                    ChatHub.DebugOut("HubBlockingCollection - RemoveAndCallback() -  Take Blocked");
                    task.Wait(); //ensure task runs till finish
                }
                else
                {
                    ChatHub.DebugOut("HubBlockingCollection - RemoveAndCallback() - Take: " + item.ToString());
                }
            }
            catch (OperationCanceledException)
            {
                ChatHub.DebugOut("HubBlockingCollection - RemoveAndCallback() - Taking canceled.");
            }
        }



        //Create a method for Flushing collection availible for List but not for BlockigCollection
        public void Clear()
        {
            try {
                while (this.Count > 0)
                {
                    foreach (T item in this)
                    {
                        this.Remove(item);
                    }
                    //this.Dispose();
                }
                //this.Dispose(); //TODO: do we need to dispose of list, will probs be garbage collected
            }
            catch(Exception e){
                ChatHub.DebugOut("HubBlockingCollection -  Failed to Clear(), message : "+ e.Message); //TODO: Handle error properly
            }
        }


    }
}