using System.Collections.Generic;
using System.Linq;

namespace Matrix.SurveyServer.Driver.TSRV24
{
	class Response4 : Response
    {
        protected List<double> values = new List<double>();
        public IEnumerable<double> Values
        {
            get
            {
                return values;
            }
        }

		public int RegisterCount { get; private set; }
		public List<byte> RegisterData { get; private set; }
		public Response4(byte[] data)
			: base(data)
		{
			if (data.Length > 3)
				RegisterCount = data[2];

			RegisterData = new List<byte>(RegisterCount);

			//количество регистров + 5 байт (сет. адрес, функция, длина, 2 црц)
			if (RegisterCount + 5 == data.Length && RegisterCount > 0)
			{
				RegisterData.AddRange(data.Skip(3).Take(RegisterCount));
			}
		}


        public override string ToString()
		{
			return string.Format("ответ, получено регистров: {0}", RegisterCount);
		}
	}
}
