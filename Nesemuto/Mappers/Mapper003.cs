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
    // CNROM
    public class Mapper003 : Mapper
    {
        public Mapper003(byte[] prgRom, byte[] chrRom, Mirroring mirroring) : base(prgRom, chrRom, mirroring)
        {
            m_PrgSize = prgRom.Length;
            m_BankCount = ChrRom.Length / 0x2000;
        }

        protected override byte Access(ushort addr, MemoryAccessMode mode, byte value)
        {
            bool isPpuAddr = addr < 0x3fff;
            if (isPpuAddr)
            {
                if (TryAccessNameTable(addr, mode, ref value))
                {
                    return value;
                }

                return ChrRom[m_ChrBankOffset + addr];
            }

            bool isChrBankSelectAddr = addr >= 0x8000 && addr <= 0xffff;
            if (mode == MemoryAccessMode.Write && isChrBankSelectAddr)
            {
                var bank = value % m_BankCount;
                m_ChrBankOffset = bank * 0x2000;
                return 0;
            }

            return Mapper.Access(PrgRom, addr % m_PrgSize, mode, value);
        }

        readonly int m_BankCount;
        readonly int m_PrgSize;
        int m_ChrBankOffset;
    }
}