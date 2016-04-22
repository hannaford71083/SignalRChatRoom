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
        public void Add(T item) {
            Console.WriteLine("Add derived method is used!!!");


            CancellationToken ct = new CancellationToken(); //TODO: Need to deal with this correctly 


            try
            {
                if (!this.TryTake(out item, 1000, ct)) //TODO: What is a reasonible time to be waiting? Should time in MS be passed in as argument
                {
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

        //Create a method for Flushing collection availible for List but not for BlockigCollection
        public void Clear()
        {
            while (this.Count > 0)
            {
                this.Dispose();
            }
        }


    }
}