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
    public partial class Cpu
    {
        enum BoundaryCrossAction
        {
            None,
            AddTick
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ushort ZeroPage()
        {
            return Read(Immediate());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ushort Absolute()
        {
            return Read16(Immediate16());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ushort Immediate()
        {
            return m_ProgramCounter++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ushort Immediate16()
        {
            var pc = m_ProgramCounter;
            m_ProgramCounter += 2;
            return pc;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool DifferentPages(ushort addr, int targetAddr)
        {
            return (addr & 0xff00) != (targetAddr & 0xff00);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ushort AbsoluteIndexedX(BoundaryCrossAction action = BoundaryCrossAction.AddTick)
        {
            var a = Absolute();
            if (action == BoundaryCrossAction.AddTick && DifferentPages(a, a + m_X))
            {
                Tick();
            }

            return (ushort) (a + m_X);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ushort AbsoluteIndexedY(BoundaryCrossAction action = BoundaryCrossAction.AddTick)
        {
            var a = Absolute();
            if (action == BoundaryCrossAction.AddTick && DifferentPages(a, a + m_Y))
            {
                Tick();
            }

            return (ushort) (a + m_Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ushort ZeroPageIndexedX()
        {
            Tick();
            return (ushort) ((ZeroPage() + m_X) % 0x100);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ushort ZeroPageIndexedY()
        {
            Tick();
            return (ushort) ((ZeroPage() + m_Y) % 0x100);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ushort IndexedIndirect()
        {
            var a = ZeroPageIndexedX();
            return Read16(a, (ushort) ((a + 1) % 0x100));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ushort IndirectIndexed(BoundaryCrossAction action = BoundaryCrossAction.AddTick)
        {
            var a = ZeroPage();
            var addr = (ushort) (Read16(a, (ushort) ((a + 1) % 0x100)) + m_Y);
            if (action == BoundaryCrossAction.AddTick && DifferentPages((ushort) (addr - m_Y), addr))
            {
                Tick();
            }

            return addr;
        }
    }
}