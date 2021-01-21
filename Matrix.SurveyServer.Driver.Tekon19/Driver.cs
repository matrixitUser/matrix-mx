using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Matrix.SurveyServer.Driver.Common;

namespace Matrix.SurveyServer.Driver.Tekon19
{
    public class Driver : BaseDriver
    {
        private byte[] SendMessageToDevice(IRequest request)
        {
            byte[] response = null;
            bool success = false;
            int attemtingCount = 0;

            while (!success && attemtingCount < 5)
            {
                attemtingCount++;

                isDataReceived = false;
                receivedBuffer = null;
                RaiseDataSended(request.GetBytes());
                Wait(7000);

                if (isDataReceived)
                {
                    response = receivedBuffer;
                    success = true;
                }
            }

            return response;
        }

    }
}
