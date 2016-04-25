using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Web;
using System.Threading;

namespace SignalRChat.Common
{
    public class HubBlockingCollection<T> : BlockingCollection<T>
    {

        //Override Add Method TODO: Implement Thread Safe Add :D 
        public void Add(T item, int periodInMs = 1000) {
            Console.WriteLine("Add derived method is used!!!");

            try
            {
                if(!this.TryAdd(item)){  //TODO: What is a reasonible time to be waiting? Should time in MS be passed in as argument
                    Console.WriteLine(" Take Blocked");
                }
                else
                    Console.WriteLine(" Take:{0}", item.ToString());
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Taking canceled.");
                //break;
            }

            //base.Add(item);
        }



        public void Remove(T item, int periodInMs = 1000) {

            CancellationToken ct = new CancellationToken();
            //TODO: deal with ct, using 'if(cancelToken.IsCancellationRequested)'
            
            try
            {
                if (!this.TryTake(out item, 1000, ct)) //TODO: What is a reasonible time to be waiting? Should time in MS be passed in as argument
                {
                    Console.WriteLine(" Take Blocked");
                }
                else { 
                    Console.WriteLine(" Take:{0}", item.ToString());
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Taking canceled.");
                //break;
            }
        
        }


        public void RemoveClear(T item, int periodInMs = 1000)
        {

            CancellationToken ct = new CancellationToken();
            //TODO: deal with ct, using 'if(cancelToken.IsCancellationRequested)'

            try
            {
                if (!this.TryTake(out item, 1000, ct)) //TODO: What is a reasonible time to be waiting? Should time in MS be passed in as argument
                {
                    Console.WriteLine(" Take Blocked");
                }
                else
                {
                    Console.WriteLine(" Take:{0}", item.ToString());
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Taking canceled.");
                //break;
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
                Console.WriteLine("Failed to Clear(), message : {0}", e.Message); //TODO: Handle error properly
            }
        }




    }
}