using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JZPay.Core
{
    public static class ErrorCode
    {
        public const int OK = 0;

        public const int ChannelError = -1000;
        public const int RecvTimeout = -1001;
        public const int SendTimeout = -1002;

        public const int UnhandleMessage = -1003;
        public const int GetClientError = -1005;
        public const int RecvUnknownData = -1006;

        public const int UnknownError = -9999;
    }
}
