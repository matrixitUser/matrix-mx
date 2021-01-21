namespace Matrix.Poll.Driver.Scylar
{
    class ValueInformationFieldExtended
    {
        private ValueInformationFieldExtended()
        {
        }

        public bool HasExtendedBlock { get; private set; }
        public int Data { get; private set; }

        public static ValueInformationFieldExtended Parse(int dif)
        {
            var result = new ValueInformationFieldExtended
            {
                HasExtendedBlock = (dif & 0x80) == 0x80,
                Data =  dif
            };


            return result;
        }
    }
}
