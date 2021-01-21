using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.PollServer
{
    static class Codes
    {
        public const int SUCCESS = 0;
        public const int TASK_ADDED = 10;
        public const int TASK_BEGIN = 20;

        public const int TASK_CANCEL = 200;
        public const int UNKNOWN_TASK = 201;
        public const int BROKEN_DRIVER = 205;
        public const int EMPTY_TASK = 206;
        public const int NOT_NEED_WORK = 207;

        public const int NODE_LOCKED = 300;
        public const int MATRIX_NOT_CONNECTED = 301;
        public const int CANT_CALL_NO_CARRIER = 302;
        public const int CANT_CALL_BUSY = 305;
        public const int CANT_CALL_WINDOW_CLOSED = 304;
        public const int CANT_CALL_NO_AT_RESPONSE = 305;
        public const int NO_HTTP_CONNECTION = 306;
        public const int NO_SOCKET = 307;
        public const int NO_DIRECTORY = 308;
        public const int RESOURCE_BUSY = 309;
        public const int DEVICE_NO_ANSWER = 310;

        /// <summary>
        /// неизвестная ошибка
        /// </summary>
        public const int UNKNOWN = 999;

        /// <summary>
        /// отключен
        /// </summary>
        public const int DISABLE = 666;
    }
}
