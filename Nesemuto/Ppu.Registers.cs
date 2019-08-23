/*
MIT License

Copyright (c) 2019 Radek Lžičař

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System.Runtime.CompilerServices;

namespace Nesemuto
{
    public partial class Ppu
    {
        public byte ReadRegister(ushort addr)
        {
            if (addr >= 0x2000 && addr <= 0x3fff)
            {
                addr = (ushort) (0x2000 + addr % 8);
            }

            byte value = 0;
            switch (addr)
            {
                case 0x2002:
                    value = ReadPpuStatus();
                    break;
                case 0x2004:
                    value = ReadOamData();
                    break;
                case 0x2007:
                    value = ReadPpuData();
                    break;
            }

            return value;
        }

        byte ReadPpuData()
        {
            var res = m_BufferedValue;
            var v = m_CurrentVramAddress;
            var addr = (ushort) (v & 0x3fff);

            if (IsPaletteAddr(addr))
            {
                res = AccessPalette(addr, MemoryAccessMode.Read, 0);
                m_BufferedValue = m_Mapper.Read((ushort) (addr - 0x1000));
            }
            else
            {
                m_BufferedValue = m_Mapper.Read(addr);
            }

            m_CurrentVramAddress += m_PpuCtrl.VramIncrement;
            return res;
        }

        byte ReadOamData()
        {
            var res = m_PrimaryOam[m_OamAddress];
            return res;
        }

        byte ReadPpuStatus()
        {
            // $2002 read
            // w:                  = 0            
            var blank = m_PpuStatus.VBlank;
            if (blank && m_Scanline == 241 && m_Dot <= 2)
            {
                // "Reading PPUSTATUS within two cycles of the start of vertical blank will return 0 in bit 7
                // but clear the latch anyway, causing NMI to not occur that frame."
                blank = false;
            }

            var res = blank ? (byte) (1 << 7) : (byte) 0;
            res |= m_PpuStatus.Sprite0Hit ? (byte) (1 << 6) : (byte) 0;
            res |= m_PpuStatus.SpriteOverflow ? (byte) (1 << 5) : (byte) 0;
            m_PpuStatus.VBlank = false;
            m_AddressLatch = false;
            return res;
        }

        public void WriteRegister(int addr, byte value)
        {
            if (addr >= 0x2000 && addr <= 0x3fff)
            {
                addr = 0x2000 + addr % 8;
            }

            switch (addr)
            {
                case 0x2000:
                    WritePpuCtrl(value);
                    break;
                case 0x2001:
                    WritePpuMask(value);
                    break;
                case 0x2003:
                    WriteOamAddr(value);
                    break;
                case 0x2004:
                    WriteOamData(value);
                    break;
                case 0x2005:
                    WritePpuScroll(value);
                    break;
                case 0x2006:
                    WritePpuAddr(value);
                    break;
                case 0x2007:
                    WritePpuData(value);
                    break;
            }
        }

        void WritePpuData(byte value)
        {
            var addr = (ushort) (m_CurrentVramAddress & 0x3fff);
            if (IsPaletteAddr(addr))
            {
                AccessPalette(addr, MemoryAccessMode.Write, value);
            }
            else
            {
                m_Mapper.Write(addr, value);
            }

            m_CurrentVramAddress += m_PpuCtrl.VramIncrement;
        }

        void WritePpuScroll(byte value)
        {
            if (!m_AddressLatch)
            {
                // first write $2005
                // t: ....... ...HGFED = d: HGFED...
                // x:              CBA = d: .....CBA
                // w:                  = 1
                m_TemporaryVramAddress = (ushort) (m_TemporaryVramAddress & ~0b00011111);
                m_TemporaryVramAddress |= (ushort) (value >> 3);
                m_NextFineScrollX = (byte) (value & 0b00000111);
                m_AddressLatch = true;
            }
            else
            {
                // second write $2005
                // t: CBA..HG FED..... = d: HGFEDCBA
                // w:                  = 0
                var cba = (value & 0b00000111) << 12;
                var hg = (value & 0b11000000) << 2;
                var fed = (value & 0b00111000) << 2;
                m_TemporaryVramAddress = (ushort) (m_TemporaryVramAddress & ~0b111001111100000);
                m_TemporaryVramAddress |= (ushort) (cba | hg | fed);
                m_AddressLatch = false;
            }
        }

        void WritePpuAddr(byte value)
        {
            if (!m_AddressLatch)
            {
                // $2006 first write
                // t: .FEDCBA ........ = d: ..FEDCBA
                // t: X...... ........ = 0
                // w:                  = 1
                m_TemporaryVramAddress = (ushort) (m_TemporaryVramAddress & ~0b0011111100000000);
                m_TemporaryVramAddress |= (ushort) ((value & 0b00111111) << 8);
                m_TemporaryVramAddress &= 0b00111111_11111111;
                // ADDR = (ushort) (value << 8);
                m_AddressLatch = true;
            }
            else
            {
                // $2006 second write
                // t: ....... HGFEDCBA = d: HGFEDCBA
                // v                   = t
                // w:                  = 0
                m_TemporaryVramAddress = (ushort) (m_TemporaryVramAddress & ~0b11111111);
                m_TemporaryVramAddress |= value;
                m_CurrentVramAddress = m_TemporaryVramAddress;
                m_AddressLatch = false;
            }
        }

        void WriteOamData(byte value)
        {
            m_PrimaryOam[m_OamAddress] = value;
            m_OamAddress += 1;
        }

        void WriteOamAddr(byte value)
        {
            m_OamAddress = value;
        }

        void WritePpuCtrl(byte value)
        {
            // $2000 write
            // t: ...BA.. ........ = d: ......BA
            m_TemporaryVramAddress = (ushort) (m_TemporaryVramAddress & ~0b000110000000000);
            m_TemporaryVramAddress |= (ushort) ((value & 0b00000011) << 10);
            m_PpuCtrl.SpriteHeight = (value & 0x20) == 0x20 ? (byte) 16 : (byte) 8;
            bool generateNmiWasSet = m_PpuCtrl.GenerateNmi;
            m_PpuCtrl.GenerateNmi = (value & 0x80) == 0x80;
            if (!generateNmiWasSet && m_PpuCtrl.GenerateNmi && m_PpuStatus.VBlank && m_Dot != 2)
            {
                m_NmiNextInstruction = true;
            }
            else if (generateNmiWasSet && !m_PpuCtrl.GenerateNmi && m_PpuStatus.VBlank && m_Dot != 2)
            {
                m_NmiNextInstruction = false;
            }

            m_PpuCtrl.VramIncrement = (ushort) (((value >> 2) & 1) * 31 + 1);
            m_PpuCtrl.BackgroundTableAddress = (ushort) (((value >> 4) & 1) * 0x1000);
            m_PpuCtrl.SpritePatternTableAddress8X8 = (ushort) (((value >> 3) & 1) * 0x1000);
        }

        void WritePpuMask(byte value)
        {
            var mask = m_PpuMask;
            mask.ShowBackgroundInLeftmost8 = (value & 2) == 2;
            mask.ShowSpritesInLeftmost8 = (value & 4) == 4;
            mask.ShowBackground = (value & 8) == 8;
            mask.ShowSprites = (value & 16) == 16;

            // TODO
            // mask.Greyscale = (value & 1) == 1;
            // mask.EmphasizeRed = (value & 32) == 32;
            // mask.EmphasizeGreen = (value & 64) == 64;
            // mask.EmphasizeBlue = (value & 128) == 128;
        }

        class PpuMask
        {
            public bool ShowBackgroundInLeftmost8;
            public bool ShowSpritesInLeftmost8;
            public bool ShowBackground;

            public bool ShowSprites;
            // TODO
            // public bool Greyscale; 
            // public bool EmphasizeRed; 
            // public bool EmphasizeGreen; 
            //public bool EmphasizeBlue; 
        }

        class PpuCtrl
        {
            public bool GenerateNmi;
            public ushort VramIncrement;
            public byte SpriteHeight;
            public ushort SpritePatternTableAddress8X8;
            public ushort BackgroundTableAddress;
        }

        class PpuStatus
        {
            public bool SpriteOverflow;
            public bool Sprite0Hit;
            public bool VBlank;
        }
    }
}