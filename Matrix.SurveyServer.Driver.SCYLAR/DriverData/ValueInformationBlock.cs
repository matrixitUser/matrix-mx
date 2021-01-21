using System.Collections.Generic;

namespace Matrix.SurveyServer.Driver.SCYLAR.DriverData
{
    class ValueInformationBlock
    {
        private ValueInformationBlock() { }

        public ValueInformationField Vif { get; set; }
        public IEnumerable<ValueInformationFieldExtended> Vife { get; set; }

        public int Length { get; private set; }

        public static ValueInformationBlock Parse(byte[] data, int index)
        {
            if (data == null || index > data.Length - 1) return null;
            var vif = ValueInformationField.Parse(data[index]);
            var length = 1;
            var vifes = new List<ValueInformationFieldExtended>();
            if (vif.HasExtendedBlock)
            {
                while (true)
                {
                    var vife = ValueInformationFieldExtended.Parse(data[index + length]);
                    vifes.Add(vife);
                    length++;
                    if (!vife.HasExtendedBlock) break;
                }
            }

            var result = new ValueInformationBlock
                             {
                                 Vif = vif,
                                 Vife = vifes,
                                 Length = length
                             };

            return result;
        }
    }
}
