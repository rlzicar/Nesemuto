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
    // MMC3
    public class Mapper004 : Mapper
    {
        public Mapper004(byte[] prgRom, byte[] chrRom, Mirroring mirroring) : base(prgRom, chrRom, mirroring)
        {
            m_PrgBankCount = PrgRom.Length / k_PrgBankSize;
            m_ChrBankCount = ChrRom.Length / k_ChrBankSize;
            m_PrgRam = new byte[0x2000];
            UpdateChrOffsets();
            UpdatePrgOffsets();
        }

        protected override byte Access(ushort addr, MemoryAccessMode mode, byte value)
        {
            bool isPpuAddr = addr <= 0x3fff;
            if (isPpuAddr)
            {
                if (TryAccessNameTable(addr, mode, ref value))
                {
                    return value;
                }

                bool isChrAddr = addr <= 0x1fff;
                if (isChrAddr)
                {
                    var ppuBank = addr / k_ChrBankSize;
                    var finalAddr = m_ChrBankOffsets[ppuBank] + addr;
                    return Access(ChrRom, finalAddr, mode, value);
                }
            }
            else
            {
                bool write = mode == MemoryAccessMode.Write;
                bool read = !write;
                if (write)
                {
                    bool isEvenAddr = (addr & 1) == 0;
                    bool isOddAddr = !isEvenAddr;
                    bool isBankSelectAddr = isEvenAddr && (addr >= 0x8000 && addr <= 0x9ffe);
                    if (isBankSelectAddr)
                    {
                        m_RegisterToUpdate = value & 7;
                        m_PrgBankMode = value & 0x40;
                        m_ChrBankMode = value & 0x80;
                        return 0;
                    }

                    bool isBankDataAddr = isOddAddr && (addr >= 0x8001 && addr <= 0x9fff);
                    if (isBankDataAddr)
                    {
                        if (m_RegisterToUpdate == 6 || m_RegisterToUpdate == 7)
                        {
                            value = (byte) ((value & 0x3f) % m_PrgBankCount);
                        }
                        else
                        {
                            value = (byte) (value % m_ChrBankCount);
                        }

                        m_Registers[m_RegisterToUpdate] = value;
                        UpdateChrOffsets();
                        UpdatePrgOffsets();
                        return 0;
                    }

                    bool isMirroringSelectAddr = isEvenAddr && (addr >= 0xa000 && addr <= 0xbffe);
                    if (isMirroringSelectAddr)
                    {
                        Mirroring = (value & 1) == 0 ? Mirroring.Vertical : Mirroring.Horizontal;
                        return 0;
                    }

                    bool isIrqLatchAddr = isEvenAddr && (addr >= 0xc000 && addr <= 0xdffe);
                    if (isIrqLatchAddr)
                    {
                        m_ScanlineCounterLatch = value;
                        return 0;
                    }

                    bool isIrqReloadAddr = isOddAddr && (addr >= 0xc001 && addr <= 0xdfff);
                    if (isIrqReloadAddr)
                    {
                        m_ScanlineCounter = 0; // force reload 
                        return 0;
                    }

                    bool isIrqDisableAddr = isEvenAddr && (addr >= 0xe000 && addr <= 0xfffe);
                    if (isIrqDisableAddr)
                    {
                        m_IrqEnabled = false;
                        IrqPending = false;
                        return 0;
                    }

                    bool isIrqEnableAddr = isOddAddr && (addr >= 0xe001 && addr <= 0xffff);
                    if (isIrqEnableAddr)
                    {
                        m_IrqEnabled = true;
                        return 0;
                    }
                }

                bool isPrgAddr = addr >= 0x8000 && addr <= 0xffff;
                if (read && isPrgAddr)
                {
                    var cpuBank = ((addr - 0x8000) / k_PrgBankSize);
                    var offset = m_PrgBankOffsets[cpuBank];
                    var finalAddr = (offset + addr);
                    return PrgRom[finalAddr];
                }

                bool isPrgRamAddr = addr >= 0x6000 && addr <= 0x7fff;
                if (isPrgRamAddr)
                {
                    return Access(m_PrgRam, addr - 0x6000, mode, value);
                }
            }

            return 0;
        }

        public override void OnScanline()
        {
            if (m_ScanlineCounter == 0)
            {
                m_ScanlineCounter = m_ScanlineCounterLatch;
            }
            else if (m_ScanlineCounter > 0)
            {
                m_ScanlineCounter -= 1;
            }

            if (m_ScanlineCounter == 0 && m_IrqEnabled)
            {
                IrqPending = true;
            }
        }

        void UpdateChrOffsets()
        {
            var chrOffsets = m_ChrBankOffsets;
            if (m_ChrBankMode == 0x80)
            {
                chrOffsets[0] = GetChrStartOffset(m_Registers[2], 0);
                chrOffsets[1] = GetChrStartOffset(m_Registers[3], 1);
                chrOffsets[2] = GetChrStartOffset(m_Registers[4], 2);
                chrOffsets[3] = GetChrStartOffset(m_Registers[5], 3);
                chrOffsets[4] = GetChrStartOffset(m_Registers[0] & ~1, 4);
                chrOffsets[5] = GetChrStartOffset(m_Registers[0] & ~1, 5) + k_ChrBankSize;
                chrOffsets[6] = GetChrStartOffset(m_Registers[1] & ~1, 6);
                chrOffsets[7] = GetChrStartOffset(m_Registers[1] & ~1, 7) + k_ChrBankSize;
            }
            else
            {
                chrOffsets[0] = GetChrStartOffset(m_Registers[0] & ~1, 0);
                chrOffsets[1] = GetChrStartOffset(m_Registers[0] & ~1, 1) + k_ChrBankSize;
                chrOffsets[2] = GetChrStartOffset(m_Registers[1] & ~1, 2);
                chrOffsets[3] = GetChrStartOffset(m_Registers[1] & ~1, 3) + k_ChrBankSize;
                chrOffsets[4] = GetChrStartOffset(m_Registers[2], 4);
                chrOffsets[5] = GetChrStartOffset(m_Registers[3], 5);
                chrOffsets[6] = GetChrStartOffset(m_Registers[4], 6);
                chrOffsets[7] = GetChrStartOffset(m_Registers[5], 7);
            }
        }

        void UpdatePrgOffsets()
        {
            var prgOffsets = m_PrgBankOffsets;
            if (m_PrgBankMode == 0x40)
            {
                prgOffsets[0] = GetPrgStartOffset(m_PrgBankCount - 2, 0);
                prgOffsets[1] = GetPrgStartOffset(m_Registers[7], 1);
                prgOffsets[2] = GetPrgStartOffset(m_Registers[6], 2);
                prgOffsets[3] = GetPrgStartOffset(m_PrgBankCount - 1, 3);
            }
            else
            {
                prgOffsets[0] = GetPrgStartOffset(m_Registers[6], 0);
                prgOffsets[1] = GetPrgStartOffset(m_Registers[7], 1);
                prgOffsets[2] = GetPrgStartOffset(m_PrgBankCount - 2, 2);
                prgOffsets[3] = GetPrgStartOffset(m_PrgBankCount - 1, 3);
            }
        }

        static int GetPrgStartOffset(int bank, int cpuBank)
        {
            var offset = (bank * k_PrgBankSize) - (cpuBank * k_PrgBankSize + 0x8000);
            return offset;
        }

        static int GetChrStartOffset(int bank, int ppuBank)
        {
            return bank * k_ChrBankSize - (ppuBank * k_ChrBankSize);
        }


        int m_ScanlineCounter;
        int m_ScanlineCounterLatch;
        readonly byte[] m_PrgRam;
        const int k_PrgBankSize = 0x2000;
        const int k_ChrBankSize = 0x400;
        readonly byte[] m_Registers = new byte[8];
        readonly int[] m_PrgBankOffsets = new int[4];
        readonly int[] m_ChrBankOffsets = new int[8];
        readonly int m_PrgBankCount;
        readonly int m_ChrBankCount;
        bool m_IrqEnabled;
        int m_PrgBankMode;
        int m_ChrBankMode;
        int m_RegisterToUpdate;
    }
}