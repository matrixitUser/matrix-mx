using System.Collections.Generic;
using System.Linq;

namespace Matrix.Poll.Driver.Scylar
{
    class DataInformationBlock
    {
        private DataInformationBlock() { }

        public DataInformationField Dif { get; set; }
        public IEnumerable<DataInformationFieldExtended> Dife { get; set; }

        public int Unit { get; private set; }
        public int Tarrif { get; private set; }
        public int StorageNumber { get; private set; }

        public int Length { get; private set; }

        public static DataInformationBlock Parse(byte[] data, int index)
        {
            if (data == null || index > data.Length - 1) return null;
            var dif = DataInformationField.Parse(data[index]);
            if (dif == null) return null;
            var length = 1;
            var difes = new List<DataInformationFieldExtended>();

            int unit = 0;
            int tarrif = 0;
            int storage = 0;

            if (dif.HasExtendedBlock)
            {
                while (true)
                {
                    var dife = DataInformationFieldExtended.Parse(data[index + length]);
                    if (dife == null) break;

                    difes.Add(dife);

                    length++;
                    if (!dife.HasExtendedBlock) break;
                }
            }

            if (difes.Any())
            {
                for (int i = difes.Count - 1; i >= 0; i--)
                {
                    var dife = difes[i];

                    unit = (unit << 1) + dife.Unit;
                    tarrif = (tarrif << 2) + dife.Tarrif;
                    storage = (storage << 4) + dife.StorageNumber;
                }
            }

            storage = (storage << 1) + dif.StorageNumberLsb;

            var result = new DataInformationBlock
                             {
                                 Dif = dif,
                                 Dife = difes,
                                 Unit = unit,
                                 Tarrif = tarrif,
                                 StorageNumber = storage,
                                 Length = length
                             };

            return result;
        }
    }
}
