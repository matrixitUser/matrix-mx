using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.MatrixControllers
{
    /// <summary>
    /// обработчик команды, единица протокола
    /// </summary>
    public interface IActionState
    {
        Task<bool> Start(IEnumerable<dynamic> path, dynamic details);
        void AcceptFrame(MatrixRequest request);
        MatrixSession Session { get; set; }
        bool CanChange();
    }
}
