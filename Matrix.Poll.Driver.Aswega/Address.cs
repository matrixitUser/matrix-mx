using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SA94
{
    class Address
    {
        private Address(byte[] mBytes)
        {
            MBytes = mBytes;
        }

        public byte[] MBytes { get; private set; }
        public static Address GetFirstHourAddress()
        {
            return new Address(new byte[]{0x50, 0x00});
        }
        public static Address GetFirstDayAddress()
        {
            return new Address(new byte[]{0x67, 0x00});
        }

        public Address GetNextAddressHA(int steps)
        {
            if (steps == 0)
                return this;
            if (steps > 1024)
                return null;
            if (steps > 0)
            {
                
                while (steps > 0x3F)
                {
                    steps = steps - 0x40; //0x3F
                    MBytes[0]++;
                }
                MBytes[1] = (byte)(MBytes[1] + steps);
                if (MBytes[1] > 0x3F)
                {
                    int i = 0x40;
                    MBytes[1] = (byte)(MBytes[1] - i);
                    MBytes[0]++;
                }
                if (MBytes[0] > 0x5F)
                    {
                        byte i = 0x10;
                        MBytes[0] = (byte)(MBytes[0] - i);
                    }
            
            }
            if (steps < 0)
            {
                while (steps <-63) //-63
                {
                    MBytes[0]--;
                    steps = steps + 0x40;
                }
                MBytes[1] = (byte)(MBytes[1] + steps);
                if (MBytes[1] < 0x00)
                {
                    int i = 0x40;
                    MBytes[1] = (byte)(MBytes[1] + i);
                    MBytes[0]--;
                }
                if (MBytes[0] < 0x50)
                    {
                        byte i = 0x10;
                        MBytes[0] = (byte) (MBytes[0] + i);
                    }
            }
            return new Address(new byte[] {MBytes[0], MBytes[1]});
        }

        public Address GetNextAddressDA(int steps)
        {
            if (steps == 0)
                return this;
            if (steps > 1024)
                return null;
            if (steps > 0)
            {

                while (steps > 0x3F)
                {
                    steps = steps - 0x40; //0x3F
                    MBytes[0]++;
                }
                MBytes[1] = (byte)(MBytes[1] + steps);
                if (MBytes[1] > 0x3F)
                {
                    int i = 0x40;
                    MBytes[1] = (byte)(MBytes[1] - i);
                    MBytes[0]++;
                }
                if (MBytes[0] > 0x6F)
                {
                    byte i = 0x10;
                    MBytes[0] = (byte)(MBytes[0] - i);
                }

            }
            if (steps < 0)
            {
                while (steps < -63) //-63
                {
                    MBytes[0]--;
                    steps = steps + 0x40;
                }
                MBytes[1] = (byte)(MBytes[1] + steps);
                if (MBytes[1] < 0x00)
                {
                    int i = 0x40;
                    MBytes[1] = (byte)(MBytes[1] + i);
                    MBytes[0]--;
                }
                if (MBytes[0] < 0x60)
                {
                    byte i = 0x10;
                    MBytes[0] = (byte)(MBytes[0] + i);
                }
            }
            return new Address(new byte[] { MBytes[0], MBytes[1] });
        }




        public override bool Equals(object obj)
        {
            if (obj is Address)
                return Equals(obj as Address);
            return false;
        }

        public bool Equals(Address other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (other.MBytes.Length == MBytes.Length &&
                MBytes.Length>0)
            {
                for (int i = 0; i < MBytes.Length; i++)
                {
                    if(other.MBytes[i] != MBytes[i]) return false;
                }
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (MBytes != null ? MBytes.GetHashCode() : 0);
        }
    }
}
