using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.SurveyServer.Driver.Tekon19
{
    interface IRequest
    {
        byte[] GetBytes();
    }
}
