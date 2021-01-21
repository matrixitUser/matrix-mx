namespace Matrix.SurveyServer.Driver.CE102
{
    class MappingUnit
    {
        public MappingUnit()
        {
            Type = MappingUnitType.integer;
        }
        public string Description { get; set; }
        public MappingUnitType Type { get; set; }
        public int Count { get; set; }
        public override string ToString()
        {
            return Description;
        }
    }
    enum MappingUnitType
    {
        integer,
        str,
        date,
        dateTime,
        bcd8
    }
}
