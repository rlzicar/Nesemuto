namespace Nesemuto.Mappers
{
    public class Mapper225 : Mapper
    {
        public Mapper225(byte[] prgRom, byte[] chrRom) : base(prgRom, chrRom, Mirroring.Vertical)
        {
        }

        protected override byte Access(ushort addr, MemoryAccessMode mode, byte value)
        {
            bool isRegAddr = addr >= 0x8000 && addr <= 0xffff;
            if (isRegAddr && mode == MemoryAccessMode.Write)
            {
                var highBit = ((addr >> 14) & 1) << 6;
                var mirroring = (addr >> 13) & 1;
                Mirroring = mirroring == 0 ? Mirroring.Vertical : Mirroring.Horizontal;
                m_PrgMode = (addr >> 12) & 1;
                m_PrgBank = ((addr >> 6) & 0x3f) | highBit;
                m_ChrBank = (addr & 0x3f) | highBit;
                if (m_PrgMode == 0)
                {
                    m_PrgBank >>= 1;
                }

                return 0;
            }

            bool isPrgAddr = addr >= 0x8000;
            if (isPrgAddr && mode == MemoryAccessMode.Read)
            {
                switch (m_PrgMode)
                {
                    case 0:
                        return PrgRom[m_PrgBank * 0x8000 + (addr - 0x8000)];
                    case 1:
                        return addr >= 0xc000
                            ? PrgRom[m_PrgBank * 0x4000 + (addr - 0xc000)]
                            : PrgRom[m_PrgBank * 0x4000 + (addr - 0x8000)];
                }
            }

            if (TryAccessNameTable(addr, mode, ref value))
            {
                return value;
            }

            var isChrAddr = addr < 0x8000;
            if (isChrAddr && mode == MemoryAccessMode.Read)
            {
                return ChrRom[m_ChrBank * 0x2000 + addr];
            }

            return 0;
        }

        int m_PrgBank;
        int m_ChrBank;
        int m_PrgMode;
    }
}