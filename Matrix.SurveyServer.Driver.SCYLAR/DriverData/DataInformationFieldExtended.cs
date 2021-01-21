namespace Matrix.SurveyServer.Driver.SCYLAR.DriverData
{
    class DataInformationFieldExtended
    {
        private DataInformationFieldExtended() { }

        public bool HasExtendedBlock { get; private set; }
        public int Unit { get; private set; }
        public int Tarrif { get; private set; }
        public int StorageNumber { get; private set; }

        public int Length { get { return 1; } }

        public static DataInformationFieldExtended Parse(byte dife)
        {
            var result = new DataInformationFieldExtended
                             {
                                 HasExtendedBlock = (dife & 0x80) == 0x80,
                                 Unit = (dife & 0x40) >> 6,
                                 Tarrif = ((dife & 30) >> 4),
                                 StorageNumber = (dife & 0x0f)
                             };


            return result;
        }

    }
}
