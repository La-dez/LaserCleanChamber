using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCleanChamber.Model.Communication
{
    public class CRC8
    {
        private byte[] lookupTable = null;

        public byte InitialValue { get; set; } = 0;
        
        private byte poly = 0;
        public byte Polynome
        {
            get => poly;
            set
            {
                poly = value;
                lookupTable = calculateTable(poly);
            }
        }

        public CRC8(byte poly, byte initialValue)
        {
            Polynome = poly;
            InitialValue = initialValue;
        }

        public byte Calculate(byte[] data, int offset, int count)
        {
            byte reg = InitialValue;
            for (int i = offset; i < offset+count; i++)
            {
                reg = lookupTable[reg ^ data[i]];
            }
            return reg;
        }

        public byte Calculate(byte[] data)
        {
            return Calculate(data, 0, data.Length);
        }

        private static byte[] calculateTable(byte poly)
        {
            byte[] table = new byte[256];

            for(int i = 0; i < table.Length; i++)
            {
                table[i] = calcByte(poly, (byte)i);
            }
            return table;
        }

        private static byte calcByte(byte poly, byte b)
        {
            int reg = b;
            for (int i = 0; i < 8; i++)
            {
                reg = reg << 1;
                if ( (reg & 0b100000000) != 0)
                    reg = reg ^ poly;
            }
            reg = reg & 0xFF;
            return (byte)reg;
        }
    }
}
