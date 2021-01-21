namespace Matrix.SurveyServer.Driver.SCYLAR.DriverData
{
    class DataRecordHeader
    {
        private DataRecordHeader()
        {
        }

        public DataInformationBlock Dib { get; private set; }
        public ValueInformationBlock Vib { get; private set; }

        public int Length { get; private set; }

        public static DataRecordHeader Parse(byte[] data, int index)
        {
            if (data == null) return null;

            
            var dib = DataInformationBlock.Parse(data, index);
            if (dib == null) return null;
            var vib = ValueInformationBlock.Parse(data, index + dib.Length);

            var result = new DataRecordHeader
                             {
                                 Dib = dib,
                                 Vib = vib,
                                 Length = dib.Length + vib.Length
                             };
            return result;
        }
    }
}
