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
    // NROM
    public sealed class Mapper000 : Mapper
    {
        public Mapper000(byte[] prgRom, byte[] chrRom, Mirroring mirroring) : base(prgRom, chrRom, mirroring)
        {
            m_PrgSize = prgRom.Length;
        }

        protected override byte Access(ushort addr, MemoryAccessMode mode, byte value)
        {
            if (TryAccessNameTable(addr, mode, ref value))
            {
                return value;
            }

            bool isChrAddr = addr <= 0x1fff;
            if (isChrAddr)
            {
                return Mapper.Access(ChrRom, addr, mode, value);
            }
            
            return PrgRom[addr % m_PrgSize];
        }

        readonly int m_PrgSize;
    }
}