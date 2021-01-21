using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.Poll.Driver.SA94
{
    public struct Version
    {
        public VersionHardware verHw;
        public VersionSoftware verSw;
        public int subVer;
        public string verText;
        public bool isExtended;
        public bool isM;
        public bool hasReadNextAddrStatCmd;
        public const int BlockSize = 128;
        public int GetRecordSize()
        {
            return isExtended ? 64 : 32;
        }
        public int GetRecordsInBlock()
        {
            return BlockSize / GetRecordSize();
        }
        public int GetSegmentSize()
        {
            return isExtended ? (64 * 1024) : (16 * 1024);
        }
        public int GetBlocksInSegment()
        {
            return GetSegmentSize() / BlockSize;
        }
        public int GetRecordsInSegment()
        {
            return GetSegmentSize() / GetRecordSize();
        }
        public string GetVerHwText()
        {
            switch(verHw)
            {
                case VersionHardware.SA94_1:
                    return "SA-94/1";
                case VersionHardware.SA94_2:
                    return "SA-94/2";
                case VersionHardware.SA94_2M:
                    return "SA-94/2M";
            }
            return "SA-94/???";
        }
    }

    public enum VersionHardware
    {
        SA94_1,
        SA94_2,
        SA94_2M
    }

    public enum VersionSoftware
    {
        ver100,
        verM100,
        ver101,
        verM101,
        ver200,
        verM200,
        ver201,
        verMTE1,
        ver300,
        verM300,
        ver301,
        verM301
    }

}
