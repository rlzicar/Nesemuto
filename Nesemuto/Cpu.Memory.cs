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
    public partial class Cpu
    {
        void DoWrite(ushort addr, byte value)
        {
            bool isRamAddr = addr <= 0x1fff;
            bool isOamAddr = addr == 0x4014;
            bool isPaletteAddr = addr >= 0x3f00 && addr <= 0x3fff;
            bool isMapperAddr = addr >= 0x4018 && addr <= 0xffff;
            bool isPpuRegisterAddr = addr >= 0x2000 && addr <= 0x3fff;
            bool isApuAddr = (addr >= 0x4000 && addr <= 0x4013) || addr == 0x4015 || addr == 0x4017;
            bool isControllerAddr = addr == 0x4016;

            if (isRamAddr)
            {
                m_Ram[addr % 0x800] = value;
            }
            else if (isOamAddr)
            {
                OamDma(value);
            }
            else if (isPaletteAddr)
            {
                SyncPpu();
                m_Ppu.AccessPalette(addr, MemoryAccessMode.Write, value);
            }
            else if (isMapperAddr)
            {
                SyncPpu();
                m_Mapper.Write(addr, value);
            }
            else if (isPpuRegisterAddr)
            {
                SyncPpu();
                m_Ppu.WriteRegister(addr, value);
            }
            else if (isApuAddr)
            {
                m_Apu.WriteRegister(m_Cycles, addr, value);
            }
            else if (isControllerAddr)
            {
                m_ControllerStrobe = value & 1;
                if (m_ControllerStrobe == 1)
                {
                    m_ControllerShiftRegister = m_ControllerState;
                }
            }
        }

        public bool CheatsEnabled { set; get; } = true;

        byte DoRead(ushort addr)
        {
            byte value = 0;
            bool canIntercept = false;

            bool isMapperAddr = addr >= 0x4018 && addr <= 0xffff;
            bool isRamAddr = addr <= 0x1fff;
            bool isPpuRegisterAddr = addr >= 0x2000 && addr <= 0x3fff;
            bool isPaletteAddr = addr >= 0x3f00 && addr <= 0x3fff;
            bool isApuAddr = addr == 0x4015;
            bool isControllerAddr = addr == 0x4016;

            if (isMapperAddr)
            {
                value = m_Mapper.Read(addr);
                canIntercept = true;
            }
            else if (isRamAddr)
            {
                value = m_Ram[addr % 0x800];
                canIntercept = true;
            }
            else if (isPpuRegisterAddr)
            {
                SyncPpu();
                value = m_Ppu.ReadRegister(addr);
            }
            else if (isPaletteAddr)
            {
                SyncPpu();
                value = m_Ppu.AccessPalette(addr, MemoryAccessMode.Read, 0);
            }
            else if (isApuAddr)
            {
                value = m_Apu.ReadStatus(m_Cycles);
            }
            else if (isControllerAddr)
            {
                var controllerBit = m_ControllerShiftRegister & 1;
                if (m_ControllerStrobe == 0)
                {
                    m_ControllerShiftRegister >>= 1;
                }

                value = (byte) controllerBit;
            }

            if (canIntercept && m_Cheats != null && CheatsEnabled)
            {
                value = m_Cheats.InterceptRead(addr, value);
            }

            return value;
        }


        byte Read(ushort addr)
        {
            Tick();
            return DoRead(addr);
        }

        void Write(ushort addr, byte value)
        {
            Tick();
            DoWrite(addr, value);
        }

        ushort Read16(ushort lsbAddr, ushort msbAddr)
        {
            var a = Read(lsbAddr);
            var b = Read(msbAddr);
            return (ushort) (a | (b << 8));
        }

        ushort Read16(ushort addr)
        {
            return Read16(addr, ++addr);
        }


        void OamDma(byte bank)
        {
            var baseAddr = (ushort) (bank << 8);

            if ((m_TotalCycles & 1) == 1)
            {
                Tick();
            }

            Tick();
            const int oamDataSize = 64 * 4;
            for (int i = 0; i < oamDataSize; i++)
            {
                var addr = (ushort) (baseAddr + i);
                Write(0x2004, Read(addr));
            }
        }

        readonly Cheats m_Cheats;
        readonly byte[] m_Ram = new byte[0x800];
    }
}