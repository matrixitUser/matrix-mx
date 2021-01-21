using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.MatrixControllers
{
    public class ActionHandlerAttribute : Attribute
    {
        public string What { get; private set; }

        public ActionHandlerAttribute(string what)
        {
            What = what;
        }
    }
}
