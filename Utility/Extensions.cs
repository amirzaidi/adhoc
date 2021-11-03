using System.Threading.Tasks;

namespace AdHocMAC.Utility
{
    static class Extensions
    {
        public static Task IgnoreExceptions(this Task input) => input.ContinueWith(_ => { });
    }
}
