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

namespace Nesemuto.Mappers
{
    // MMC1
    public sealed class Mapper001 : Mapper
    {
        public Mapper001(byte[] prgRom, byte[] chrRom, Mirroring mirroring) : base(prgRom, chrRom, mirroring)
        {
            m_PrgBankCount = PrgRom.Length / k_PrgRomWindowSize;
            m_ChrBankCount = ChrRom.Length / k_ChrRomWindowSize;

            m_SelectedPrgBankOffset0 = -0x8000;
            m_SelectedPrgBankOffset1 = (m_PrgBankCount - 1) * k_PrgRomWindowSize - 0xc000;

            m_SelectedChrBankOffset0 = 0;
            m_SelectedChrBankOffset1 = k_ChrRomWindowSize - 0x1000;
        }

        protected override byte Access(ushort addr, MemoryAccessMode mode, byte value)
        {
            int finalAddr = addr;
            bool isPpuAddr = addr <= 0x3fff;
            if (isPpuAddr)
            {
                if (TryAccessNameTable(addr, mode, ref value))
                {
                    return value;
                }

                bool isChrBank0Addr = addr <= 0x0fff;
                bool isChrBank1Addr = addr >= 0x1000 && addr <= 0x1fff;
                if (isChrBank0Addr)
                {
                    finalAddr = m_SelectedChrBankOffset0 + addr;
                }
                else if (isChrBank1Addr)
                {
                    finalAddr = m_SelectedChrBankOffset1 + addr;
                }

                return Access(ChrRom, finalAddr, mode, value);
            }

            bool isLoadRegisterAddr = addr >= 0x8000 && addr <= 0xffff;
            if (mode == MemoryAccessMode.Write && isLoadRegisterAddr)
            {
                var dataBit = (ushort) (value & 1);
                var resetBit = (value & 0x80) >> 7;
                bool shouldReset = resetBit == 1;
                if (shouldReset)
                {
                    m_ShiftRegister = k_ShiftRegisterInitialValue;
                    m_PrgRomBankMode = PrgRomBankMode.Switch16KLow;
                }
                else
                {
                    bool shiftRegisterFull = (m_ShiftRegister & 1) == 1;

                    m_ShiftRegister >>= 1;
                    m_ShiftRegister = (byte) (m_ShiftRegister | (dataBit << 4));
                    if (shiftRegisterFull)
                    {
                        WriteInternalRegister(addr, (byte) (m_ShiftRegister & 0xff));
                        m_ShiftRegister = k_ShiftRegisterInitialValue;
                    }
                }

                return 0;
            }

            bool isPrgRamAddr = addr >= 0x6000 && addr <= 0x7fff;
            if (m_PrgRamEnabled && isPrgRamAddr)
            {
                return Access(m_PrgRam, addr - 0x6000, mode, value);
            }

            bool isPrgBank0Addr = addr >= 0x8000 && addr <= 0xbfff;
            bool isPrgBank1Addr = addr >= 0xc000 && addr <= 0xffff;
            if (isPrgBank0Addr)
            {
                finalAddr = m_SelectedPrgBankOffset0 + addr;
            }
            else if (isPrgBank1Addr)
            {
                finalAddr = m_SelectedPrgBankOffset1 + addr;
            }

            return PrgRom[finalAddr];
        }


        void WriteInternalRegister(ushort addr, byte value)
        {
            bool isPrgBankSelectAddr = addr >= 0xe000 && addr <= 0xffff;
            bool isChrBank0SelectAddr = addr >= 0xa000 && addr <= 0xbfff;
            bool isChrBank1SelectAddr = addr >= 0xc000 && addr <= 0xdfff;
            bool isControlAddr = addr >= 0x8000 && addr <= 0x9fff;

            if (isPrgBankSelectAddr)
            {
                if (m_PrgRomBankMode == PrgRomBankMode.Switch32K)
                {
                    var bank = (value & 0b1110) % m_PrgBankCount;
                    m_SelectedPrgBankOffset0 = bank * k_PrgRomWindowSize;
                    m_SelectedPrgBankOffset1 = (bank + 1) * k_PrgRomWindowSize;
                }
                else
                {
                    var bank = (value & 0b1111) % m_PrgBankCount;
                    if (m_PrgRomBankMode == PrgRomBankMode.Switch16KLow)
                    {
                        m_SelectedPrgBankOffset0 = bank * k_PrgRomWindowSize; // switchable 
                        m_SelectedPrgBankOffset1 = (m_PrgBankCount - 1) * k_PrgRomWindowSize; // fixed to the last bank
                    }
                    else
                    {
                        m_SelectedPrgBankOffset0 = 0; // fixed to the first bank
                        m_SelectedPrgBankOffset1 = bank * k_PrgRomWindowSize; // switchable
                    }
                }

                m_SelectedPrgBankOffset0 -= 0x8000;
                m_SelectedPrgBankOffset1 -= 0xc000;
                m_PrgRamEnabled = ((value >> 4) & 1) == 1;
            }
            else if (isChrBank0SelectAddr)
            {
                if (m_ChrRomBankMode == ChrRomBankMode.Switch8K)
                {
                    var bank = (value & 0b11110) % m_ChrBankCount;
                    m_SelectedChrBankOffset0 = bank * k_ChrRomWindowSize;
                    m_SelectedChrBankOffset1 = (bank + 1) * k_ChrRomWindowSize;
                    m_SelectedChrBankOffset1 -= 0x1000;
                }
                else
                {
                    var bank = (value & 0b11111) % m_ChrBankCount;
                    m_SelectedChrBankOffset0 = bank * k_ChrRomWindowSize;
                }
            }
            else if (isChrBank1SelectAddr)
            {
                if (m_ChrRomBankMode == ChrRomBankMode.Switch2X4K)
                {
                    var bank = (value & 0b11111) % m_ChrBankCount;
                    m_SelectedChrBankOffset1 = bank * k_ChrRomWindowSize - 0x1000;
                }
            }
            else if (isControlAddr)
            {
                switch (value & 3)
                {
                    case 0:
                        Mirroring = Mirroring.ScreenA;
                        break;
                    case 1:
                        Mirroring = Mirroring.ScreenB;
                        break;
                    case 2:
                        Mirroring = Mirroring.Vertical;
                        break;
                    case 3:
                        Mirroring = Mirroring.Horizontal;
                        break;
                }

                var prgRomBankModeInt = (value >> 2) & 3;
                var chrRomBankModeInt = (value >> 4) & 1;
                m_ChrRomBankMode = chrRomBankModeInt == 0 ? ChrRomBankMode.Switch8K : ChrRomBankMode.Switch2X4K;
                switch (prgRomBankModeInt)
                {
                    case 0:
                    case 1:
                        m_PrgRomBankMode = PrgRomBankMode.Switch32K;
                        break;
                    case 2:
                        m_PrgRomBankMode = PrgRomBankMode.Switch16KHigh;
                        break;
                    case 3:
                        m_PrgRomBankMode = PrgRomBankMode.Switch16KLow;
                        break;
                }
            }
        }


        readonly int m_PrgBankCount;
        readonly int m_ChrBankCount;

        int m_SelectedPrgBankOffset0;
        int m_SelectedPrgBankOffset1;

        int m_SelectedChrBankOffset0;
        int m_SelectedChrBankOffset1;


        const int k_PrgRomWindowSize = 0x4000;
        const int k_ChrRomWindowSize = 0x1000;

        enum PrgRomBankMode
        {
            Switch32K,
            Switch16KHigh,
            Switch16KLow
        }

        enum ChrRomBankMode
        {
            Switch8K,
            Switch2X4K
        }

        ChrRomBankMode m_ChrRomBankMode = ChrRomBankMode.Switch8K;
        PrgRomBankMode m_PrgRomBankMode = PrgRomBankMode.Switch16KLow;


        const byte k_ShiftRegisterInitialValue = 0x10;

        byte m_ShiftRegister = k_ShiftRegisterInitialValue;

        bool m_PrgRamEnabled;

        readonly byte[] m_PrgRam = new byte[0x8000];
    }
}