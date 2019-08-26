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

namespace Nesemuto
{
    public abstract class Mapper
    {
        protected Mapper(byte[] prgRom, byte[] chrRom, Mirroring mirroring)
        {
            PrgRom = prgRom;
            ChrRom = EnsureArray(chrRom, 0x2000);
            Mirroring = mirroring;
        }

        protected bool TryAccessNameTable(ushort addr, MemoryAccessMode mode, ref byte value)
        {
            if (addr < 0x2000 || addr > 0x3eff) return false;

            if (addr >= 0x3000)
            {
                addr -= 0x1000;
            }

            switch (Mirroring)
            {
                case Mirroring.Horizontal:
                {
                    bool isScreenB = addr >= 0x2400 && addr <= 0x27ff;
                    bool isScreenC = addr >= 0x2800 && addr <= 0x2bff;
                    if (isScreenB)
                    {
                        // B -> A
                        addr -= 0x400;
                    }
                    else if (isScreenC)
                    {
                        // C -> D
                        addr += 0x400;
                    }

                    break;
                }

                case Mirroring.Vertical:
                {
                    bool isScreenB = addr >= 0x2400 && addr <= 0x27ff;
                    bool isScreenC = addr >= 0x2800 && addr <= 0x2bff;

                    if (isScreenB)
                    {
                        // B -> D
                        addr += 0x800;
                    }
                    else if (isScreenC)
                    {
                        // C -> A
                        addr -= 0x800;
                    }

                    break;
                }

                case Mirroring.ScreenA:
                    addr = (ushort) (0x2000 + addr % 0x400);
                    break;
                case Mirroring.ScreenB:
                    addr = (ushort) (0x2c00 + addr % 0x400);
                    break;
            }

            value = Access(m_Nametables, addr - 0x2000, mode, value);
            return true;
        }

        public virtual void OnScanline()
        {
        }

        public bool IrqPending { get; protected set; }

        protected static byte Access(byte[] data, int addr, MemoryAccessMode mode, byte value)
        {
            if (mode == MemoryAccessMode.Read)
            {
                return data[addr];
            }

            data[addr] = value;
            return 0;
        }

        static byte[] EnsureArray(byte[] data, int size)
        {
            if (data == null || data.Length == 0)
            {
                return new byte[size];
            }

            return data;
        }

        protected abstract byte Access(ushort addr, MemoryAccessMode mode, byte value);

        public byte Read(ushort addr)
        {
            return Access(addr, MemoryAccessMode.Read, 0);
        }

        public void Write(ushort addr, byte value)
        {
            Access(addr, MemoryAccessMode.Write, value);
        }

        protected Mirroring Mirroring { set; get; }

        readonly byte[] m_Nametables = new byte[0x400 * 4];
        protected readonly byte[] PrgRom;
        protected readonly byte[] ChrRom;
    }
}