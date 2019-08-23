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
    // AxROM
    public class Mapper007 : Mapper
    {
        public Mapper007(byte[] prgRom, byte[] chrRom) : base(prgRom, chrRom, Mirroring.ScreenA)
        {
            m_BankCount = PrgRom.Length / 0x8000;
        }

        protected override byte Access(ushort addr, MemoryAccessMode mode, byte value)
        {
            bool isPrgAddr = addr >= 0x8000 && addr <= 0xffff;
            bool isBankSelectAddr = mode == MemoryAccessMode.Write && isPrgAddr;
            if (isBankSelectAddr)
            {
                var bank = value & 0xf;
                m_PrgBankOffset = (bank % m_BankCount) * 0x8000 - 0x8000;
                var mirroring = (value >> 4) & 1;
                Mirroring = mirroring == 0 ? Mirroring.ScreenA : Mirroring.ScreenB;
                return 0;
            }

            if (isPrgAddr)
            {
                var finalAddr = m_PrgBankOffset + addr;
                return PrgRom[finalAddr];
            }

            if (TryAccessNameTable(addr, mode, ref value))
            {
                return value;
            }

            bool isChrAddr = addr <= 0x1fff;
            if (isChrAddr)
            {
                return Access(ChrRom, addr, mode, value);
            }

            return 0x0;
        }

        readonly int m_BankCount;
        int m_PrgBankOffset;
    }
}