using System;
using System.Threading;
using System.Threading.Tasks;

namespace ClipShare
{
    class Locker
    {
        public static void LockedExec(object locker, Task t, int timeout)
        {
            while (!Monitor.IsEntered(locker))
            {
                if (Monitor.TryEnter(locker, timeout))
                {
                    try
                    {
                        t.RunSynchronously();
                    }
                    catch (Exception e)
                    {
                        // Fuck that
                    }
                    finally
                    {
                        Monitor.Exit(locker);
                    }
                }
                else
                {
                    // Lock timed out, wait then try again
                    Thread.Sleep(1);
                }
            }
        }
    }
}
