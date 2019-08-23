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
    public sealed partial class Cpu
    {
        public Cpu(Mapper mapper, Ppu ppu, Apu apu, Cheats cheats = null)
        {
            m_Mapper = mapper;
            m_Ppu = ppu;
            m_Apu = apu;

            if (cheats?.Count > 0)
            {
                m_Cheats = cheats;
            }
        }

        public void SetGamepadButtonPressed(GamepadButton button, bool pressed)
        {
            if (pressed)
            {
                m_ControllerState |= (byte) button;
            }
            else
            {
                m_ControllerState &= (byte) ~button;
            }
        }

        public void PowerOn(ushort? entryPoint = null)
        {
            m_Accumulator = 0;
            m_X = 0;
            m_Y = 0;
            m_StackPointer = 0xfd;
            SetFlags(0x34);
            Reset(entryPoint);
        }

        public void Reset(ushort? entryPoint = null)
        {
            m_StackPointer -= 3;
            m_FlagDisableInterrupts = 1;
            m_ProgramCounter = entryPoint ?? Read16(0xfffc);
            m_Cycles = 0;
            m_TotalCycles = 0;
        }

        void RunUntil(int scanline, int dot)
        {
            var ppu = m_Ppu;
            while (ppu.Scanline <= scanline && ppu.Dot + m_PendingPpuCycles <= dot)
            {
                Step();
            }
        }
        
        public void EmulateFrame()
        {
            var ppu = m_Ppu;
            var mapper = m_Mapper;
            bool nmiProcessed = false;

            while (m_Cycles < k_CyclesPerFrame)
            {
                // NMI scanline
                while (ppu.Scanline == Ppu.NmiScanline && !nmiProcessed)
                {
                    Step();
                    SyncPpu(); // immediate catch-up after each CPU step until an NMI occurs

                    if (ppu.ReadAndClearNmiFlag())
                    {
                        nmiProcessed = true;
                        Nmi();
                    }
                }
                
                // run the CPU until a mapper IRQ might occur
                RunUntil(ppu.Scanline, Ppu.ScanlineIrqDot);
                SyncPpu();

                if (mapper.IrqPending && m_FlagDisableInterrupts == 0)
                {
                    Irq();
                }

                // run the CPU until the end of the current scanline
                RunUntil(ppu.Scanline, Ppu.ScanlineEndDot);
                SyncPpu();
            }

            m_Apu.Run(m_Cycles);
            m_Cycles -= k_CyclesPerFrame;
        }

        void Tick()
        {
            m_Cycles += 1;
            m_TotalCycles += 1;
            const int ppuCyclesPerCpuCycle = 3;
            m_PendingPpuCycles += ppuCyclesPerCpuCycle;
        }

        void SetFlags(byte flags)
        {
            m_FlagCarry = (flags >> 0) & 1;
            m_FlagZero = (flags >> 1) & 1;
            m_FlagDisableInterrupts = (flags >> 2) & 1;
            m_FlagDecimal = (flags >> 3) & 1;
            m_FlagOverflow = (flags >> 6) & 1;
            m_FlagNegative = (flags >> 7) & 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void SyncPpu()
        {
            if (m_PendingPpuCycles == 0)
            {
                return;
            }

            m_Ppu.Run(m_PendingPpuCycles);
            m_PendingPpuCycles = 0;
        }

        const int k_CyclesPerFrame = 29781;

        readonly Mapper m_Mapper;
        readonly Ppu m_Ppu;
        readonly Apu m_Apu;

        int m_PendingPpuCycles;
        int m_Cycles;
        long m_TotalCycles;

        int m_ControllerStrobe;
        byte m_ControllerShiftRegister;
        byte m_ControllerState;

        // flags
        int m_FlagDisableInterrupts;
        int m_FlagDecimal;
        int m_FlagNegative;
        int m_FlagZero;
        int m_FlagCarry;
        int m_FlagOverflow;

        // registers
        ushort m_ProgramCounter;
        byte m_StackPointer;
        byte m_X;
        byte m_Y;
        byte m_Accumulator;
    }
}