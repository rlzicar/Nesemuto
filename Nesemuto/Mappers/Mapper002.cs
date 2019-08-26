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
    // UxROM
    public sealed class Mapper002 : Mapper
    {
        public Mapper002(byte[] prgRom, byte[] chrRom, Mirroring mirroring) : base(prgRom, chrRom, mirroring)
        {
            m_FinalBankOffset = PrgRom.Length - 0x4000 - 0xc000;
            m_BankCount = PrgRom.Length / 0x4000;
        }

        protected override byte Access(ushort addr, MemoryAccessMode mode, byte value)
        {
            bool isCpuAddr = addr > 0x3fff;
            if (isCpuAddr)
            {
                bool isFixedPrgAddr = addr >= 0xc000 && addr <= 0xffff;
                if (mode == MemoryAccessMode.Read && isFixedPrgAddr)
                {
                    int finalAddr = m_FinalBankOffset + addr;
                    return PrgRom[finalAddr];
                }

                bool isSwitchableBankAddr = addr >= 0x8000 && addr <= 0xbfff;
                if (mode == MemoryAccessMode.Read && isSwitchableBankAddr)
                {
                    return PrgRom[m_SelectedBankOffset + addr];
                }

                bool isBankSelectRegisterAddr = addr >= 0x8000 && addr <= 0xffff;
                if (mode == MemoryAccessMode.Write && isBankSelectRegisterAddr)
                {
                    m_SelectedBankOffset = (value & 0xf) % m_BankCount * 0x4000 - 0x8000;
                    return 0;
                }
            }
            else
            {
                bool isChrRomAddr = addr <= 0x1fff;
                if (isChrRomAddr)
                {
                    return Mapper.Access(ChrRom, addr, mode, value);
                }

                if (TryAccessNameTable(addr, mode, ref value))
                {
                    return value;
                }
            }

            return 0;
        }

        readonly int m_BankCount;
        readonly int m_FinalBankOffset;
        int m_SelectedBankOffset;
    }
}