using System;
using System.Threading.Tasks;

namespace AdHocMAC.Utility
{
    class DuplicateRunDiscarder
    {
        private readonly Func<Task> mAction;
        private bool mIsRunning;

        public DuplicateRunDiscarder(Func<Task> Action)
        {
            mAction = Action;
        }

        // This is not thread-safe! It is only useful for single thread use.
        // That sounds counterintuitive, but in C# one thread can do multiple things
        // because of async/await freeing the thread.
        // We do not want UI button click spam to break actions that have not completed.
        public async Task Execute()
        {
            if (!mIsRunning)
            {
                mIsRunning = true;
                await mAction();
                mIsRunning = false;
            }
        }
    }
}
