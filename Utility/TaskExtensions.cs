using System.Threading.Tasks;

namespace AdHocMAC.Utility
{
    static class TaskExtensions
    {
        public static Task IgnoreExceptions(this Task input) => input.ContinueWith(_ => { });
    }
}
