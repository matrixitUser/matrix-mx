namespace Matrix.SurveyServer.Driver.CE102
{
    class ValueUnit
    {
        public MappingUnit MappingUnit { get; set; }
        public object Value { get; set; }
        public override string ToString()
        {
            return string.Format("{0}:{1}", MappingUnit, Value);
        }
    }
}
