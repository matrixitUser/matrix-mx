
namespace Matrix.Poll.Driver.Scylar
{
    class DataInformationField
    {
        private DataInformationField() { }

        public int Length
        {
            get { return 1; }
        }

        public bool HasExtendedBlock { get; private set; }

        public int StorageNumberLsb { get; private set; }

        public FunctionValueType FunctionValue { get; private set; }

        public DataType DataType { get; private set; }

        public static DataInformationField Parse(int dif)
        {
            if (dif == 0x0f || dif == 0x1f) return null;
            var result = new DataInformationField
                             {
                                 HasExtendedBlock = (dif & 0x80) == 0x80,
                                 StorageNumberLsb = (dif & 0x40) << 6,
                                 FunctionValue = (FunctionValueType) ((dif & 30) >> 4),
                                 DataType = (DataType) (dif & 0x0f)
                             };


            return result;
        }
    }
}
