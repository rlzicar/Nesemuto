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
        static int BoolToInt(bool b)
        {
            return b ? 1 : 0;
        }

        static int GetBit7(int v)
        {
            return GetBit7((byte) v);
        }

        static int GetBit7(byte v)
        {
            return (v & 0x80) >> 7;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        byte FlagsToByte()
        {
            var v = m_FlagCarry | (m_FlagZero << 1) | (m_FlagDisableInterrupts << 2)
                    | (m_FlagDecimal << 3) | (0 << 4) | (1 << 5) | (m_FlagOverflow << 6)
                    | (m_FlagNegative << 7);
            return (byte) v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void SetNegativeZeroFlags(byte x)
        {
            m_FlagNegative = GetBit7(x);
            m_FlagZero = BoolToInt(x == 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void SetCarryOverflowFlags(byte x, byte y, int r)
        {
            m_FlagCarry = BoolToInt(r > 0xff);
            m_FlagOverflow = GetBit7(~(x ^ y) & (x ^ r));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Push(byte value)
        {
            const int stackOffset = 0x100;
            Write((ushort) (stackOffset + m_StackPointer), value);
            m_StackPointer -= 1;
        }

        byte Pop()
        {
            const int stackOffset = 0x100;
            m_StackPointer += 1;
            return Read((ushort) (stackOffset + m_StackPointer));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Branch(bool condition)
        {
            sbyte jump = (sbyte) Read(Immediate());
            if (condition)
            {
                Tick();
                // "a boundary crossing occurs when the branch destination is on a different page
                // than the instruction AFTER the branch instruction"
                var start = (ushort) (m_ProgramCounter + 1);
                var end = (ushort) (m_ProgramCounter + jump);
                int startPage = start & 0xff00;
                int endPage = end & 0xff00;
                if (startPage != endPage)
                {
                    Tick();
                }

                m_ProgramCounter = (ushort) (m_ProgramCounter + jump);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void JmpAbsolute()
        {
            var addr = Read16(Immediate16());
            m_ProgramCounter = addr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void JmpIndirect()
        {
            var operand = Read16(Immediate16());
            var lsbAddr = operand;
            // jmp ($xxFF) bug emulation
            var msbAddr = (ushort) ((operand & 0x00ff) == 0x00ff ? operand & 0xff00 : operand + 1);
            m_ProgramCounter = Read16(lsbAddr, msbAddr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Jsr()
        {
            Tick();
            var jumpLocation = Read16(Immediate16());
            var nextMinusOne = m_ProgramCounter - 1;
            var a = (byte) (nextMinusOne >> 8);
            var b = (byte) nextMinusOne;
            Push(a);
            Push(b);
            m_ProgramCounter = jumpLocation;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Axs(ushort addr)
        {
            var operand = Read(addr);
            var value = (m_X &= m_Accumulator) - operand;
            m_FlagCarry = BoolToInt((m_Accumulator & m_X) >= operand);
            m_X = (byte) value;
            SetNegativeZeroFlags(m_X);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Anc(ushort addr)
        {
            var operand = Read(addr);
            m_Accumulator &= operand;
            m_FlagNegative = GetBit7(m_Accumulator);
            SetNegativeZeroFlags(m_Accumulator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Rra(ushort addr)
        {
            var operand = Read(addr);
            var c = (byte) (m_FlagCarry << 7);
            m_FlagCarry = operand & 0x01;
            var res = (byte) ((operand >> 1) | c);
            Tick();
            Write(addr, res);
            var r = m_Accumulator + res + m_FlagCarry;
            SetCarryOverflowFlags(m_Accumulator, res, r);
            SetNegativeZeroFlags(m_Accumulator = (byte) r);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Sre(ushort addr)
        {
            var operand = Read(addr);
            m_FlagCarry = operand & 0x01;
            Tick();
            var res = (byte) (operand >> 1);
            Write(addr, res);
            SetNegativeZeroFlags(res);
            m_Accumulator ^= res;
            SetNegativeZeroFlags(m_Accumulator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Rla(ushort addr)
        {
            var operand = Read(addr);
            var c = m_FlagCarry;
            m_FlagCarry = GetBit7(operand);
            var res = (byte) ((operand << 1) | c);
            Tick();
            Write(addr, res);
            m_Accumulator &= res;
            SetNegativeZeroFlags(m_Accumulator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Slo(ushort addr)
        {
            var operand = Read(addr);
            m_FlagCarry = GetBit7(operand);
            Tick();
            var res = (byte) (operand << 1);
            Write(addr, res);
            SetNegativeZeroFlags(m_Accumulator |= res);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Asr(ushort addr)
        {
            var operand = Read(addr);
            m_Accumulator &= operand;
            m_FlagCarry = m_Accumulator & 1;
            SetNegativeZeroFlags(m_Accumulator >>= 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Isb(ushort addr)
        {
            var operand = Read(addr);
            Tick();
            var res = (byte) (operand + 1);
            Write(addr, res);
            SetNegativeZeroFlags(res);
            res ^= 0xff;
            var r = m_Accumulator + res + m_FlagCarry;
            SetCarryOverflowFlags(m_Accumulator, res, r);
            SetNegativeZeroFlags(m_Accumulator = (byte) r);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Dcp(ushort addr)
        {
            var operand = Read(addr);
            Tick();
            var res = (byte) (operand - 1);
            Write(addr, res);
            SetNegativeZeroFlags((byte) (m_Accumulator - res));
            m_FlagCarry = BoolToInt(m_Accumulator >= res);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Aax(ushort addr)
        {
            var res = (byte) (m_X & m_Accumulator);
            Store(res, addr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Lax(ushort addr)
        {
            var value = Read(addr);
            m_Accumulator = value;
            m_X = value;
            SetNegativeZeroFlags(value);
        }

        byte Transfer(byte value)
        {
            SetNegativeZeroFlags(value);
            Tick();
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Nop()
        {
            Tick();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Store(byte value, ushort addr)
        {
            Write(addr, value);
        }

        byte Load(ushort addr)
        {
            var r = Read(addr);
            SetNegativeZeroFlags(r);
            return r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Php()
        {
            Tick();
            var flags = FlagsToByte();
            const int bFlag = (1 << 4);
            flags |= bFlag; 
            Push(flags);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Pha()
        {
            Tick();
            Push(m_Accumulator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Pla()
        {
            Tick();
            Tick();
            SetNegativeZeroFlags(m_Accumulator = Pop());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Plp()
        {
            Tick();
            Tick();
            var flags = Pop();
            SetFlags(flags);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Rts()
        {
            Tick();
            Tick();
            var lsb = Pop();
            var msb = Pop();
            m_ProgramCounter = (ushort) (lsb | (msb << 8));
            m_ProgramCounter += 1;
            Tick();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Rti()
        {
            Plp();
            m_ProgramCounter = (ushort) (Pop() | (Pop() << 8));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Asl(ushort addr)
        {
            var operand = Read(addr);
            m_FlagCarry = GetBit7(operand);
            Tick();
            var res = (byte) (operand << 1);
            Write(addr, res);
            SetNegativeZeroFlags(res);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void AslAccumulator()
        {
            var operand = m_Accumulator;
            m_FlagCarry = GetBit7(operand);
            Tick();
            var res = (byte) (operand << 1);
            m_Accumulator = res;
            SetNegativeZeroFlags(res);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Lsr(ushort addr)
        {
            var operand = Read(addr);
            m_FlagCarry = operand & 0x01;
            Tick();
            var res = (byte) (operand >> 1);
            Write(addr, res);
            SetNegativeZeroFlags(res);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void LsrAccumulator()
        {
            var operand = m_Accumulator;
            m_FlagCarry = operand & 0x01;
            Tick();
            var res = (byte) (operand >> 1);
            m_Accumulator = res;
            SetNegativeZeroFlags(res);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Cmp(byte r, ushort addr)
        {
            var operand = Read(addr);
            SetNegativeZeroFlags((byte) (r - operand));
            m_FlagCarry = BoolToInt(r >= operand);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Adc(ushort addr)
        {
            var operand = Read(addr);
            var a = m_Accumulator;
            var r = a + operand + m_FlagCarry;
            SetCarryOverflowFlags(a, operand, r);
            SetNegativeZeroFlags(m_Accumulator = (byte) r);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Sbc(ushort addr)
        {
            var operand = Read(addr);
            operand ^= 0xff;
            var a = m_Accumulator;
            var r = a + operand + m_FlagCarry;
            SetCarryOverflowFlags(a, operand, r);
            SetNegativeZeroFlags(m_Accumulator = (byte) r);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Bit(ushort addr)
        {
            var operand = Read(addr);
            m_FlagZero = BoolToInt((m_Accumulator & operand) == 0);
            m_FlagNegative = GetBit7(operand);
            m_FlagOverflow = ((operand & 0x40) >> 6) & 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Rol(ushort addr)
        {
            var operand = Read(addr);
            var c = m_FlagCarry;
            m_FlagCarry = GetBit7(operand);
            var res = (byte) ((operand << 1) | c);
            Tick();
            Write(addr, res);
            SetNegativeZeroFlags(res);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void RolAccumulator()
        {
            var operand = m_Accumulator;
            var c = m_FlagCarry;
            m_FlagCarry = GetBit7(operand);
            var res = (byte) ((operand << 1) | c);
            Tick();
            m_Accumulator = res;
            SetNegativeZeroFlags(res);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Ror(ushort addr)
        {
            var operand = Read(addr);
            var c = (byte) (m_FlagCarry << 7);
            m_FlagCarry = operand & 0x01;
            var res = (byte) ((operand >> 1) | c);
            Tick();
            Write(addr, res);
            SetNegativeZeroFlags(res);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void RorAccumulator()
        {
            var operand = m_Accumulator;
            var c = (byte) (m_FlagCarry << 7);
            m_FlagCarry = operand & 0x01;
            var res = (byte) ((operand >> 1) | c);
            Tick();
            m_Accumulator = res;
            SetNegativeZeroFlags(res);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Dec(ushort addr)
        {
            var operand = Read(addr);
            Tick();
            var res = (byte) (operand - 1);
            Write(addr, res);
            SetNegativeZeroFlags(res);
        }

        byte Dec(byte v)
        {
            v -= 1;
            SetNegativeZeroFlags(v);
            Tick();
            return v;
        }

        byte Inc(byte v)
        {
            v += 1;
            SetNegativeZeroFlags(v);
            Tick();
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Inc(ushort addr)
        {
            var operand = Read(addr);
            Tick();
            var res = (byte) (operand + 1);
            Write(addr, res);
            SetNegativeZeroFlags(res);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void And(ushort addr)
        {
            var operand = Read(addr);
            SetNegativeZeroFlags(m_Accumulator &= operand);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Eor(ushort addr)
        {
            var operand = Read(addr);
            SetNegativeZeroFlags(m_Accumulator ^= operand);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Ora(ushort addr)
        {
            var operand = Read(addr);
            SetNegativeZeroFlags(m_Accumulator |= operand);
        }
    }
}