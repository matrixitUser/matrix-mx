namespace Matrix.SurveyServer.Driver.SCYLAR.DriverData
{
    class ValueInformationField
    {
        private ValueInformationField()
        {
        }

        public bool HasExtendedBlock { get; private set; }

        public int UnitAndMultiplier { get; private set; }

        public static ValueInformationField Parse(byte vif)
        {
            var result = new ValueInformationField
                             {
                                 HasExtendedBlock = (vif & 0x80) == 0x80,
                                 UnitAndMultiplier = (vif & 0x7f),
                             };
            return result;
        }
    }
}
