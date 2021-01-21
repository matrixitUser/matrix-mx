namespace Matrix.Poll.Driver.Scylar
{
    enum DataType
    {
        NoData = 0,
        BitInteger8 = 1,
        BitInteger16 = 2,
        BitInteger24 = 3,
        BitInteger32 = 4,
        BitReal32 = 5,
        BitInteger48 = 6,
        BitInteger64 = 7,
        ReadoutSelection = 8,
        Bcd2 = 9,
        Bcd4 = 10,
        Bcd6 = 11,
        Bcd8 = 12,
        VariableLength = 13,
        Bcd12 = 14,
        SpecialFunction = 15
    }
    static class DataTypeExtension
    {
        public static int GetLentgh(this DataType dt)
        {
            switch (dt)
            {
                case DataType.NoData:
                    return 0;
                case DataType.BitInteger8:
                    return 1;
                case DataType.BitInteger16:
                    return 2;
                case DataType.BitInteger24:
                    return 3;
                case DataType.BitInteger32:
                    return 4;
                case DataType.BitReal32:
                    return 4;
                case DataType.BitInteger48:
                    return 6;
                case DataType.BitInteger64:
                    return 8;
                case DataType.ReadoutSelection:
                    return 0;
                case DataType.Bcd2:
                    return 2;
                case DataType.Bcd4:
                    return 4;
                case DataType.Bcd6:
                    return 6;
                case DataType.Bcd8:
                    return 8;
                case DataType.VariableLength:
                    return 0;
                case DataType.Bcd12:
                    return 12;
                case DataType.SpecialFunction:
                    return 0;
            }
            return 0;
        }
    }
}
