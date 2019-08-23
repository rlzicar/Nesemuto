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

using System;

namespace Nesemuto
{
    public partial class Cpu
    {
        void Step()
        {
            var opcode = Read(m_ProgramCounter++);

            switch (opcode)
            {
                default:
                    throw new NotImplementedException("unimplemented opcode " + opcode.ToHex());
                case 0x00:
                    Brk();
                    break;

                case 0x4b:
                    Asr(Immediate());
                    break;

                case 0xd4:
                case 0x14:
                case 0x34:
                case 0x54:
                case 0xf4:
                case 0x74:
                    Tick();
                    Tick();
                    Tick();
                    m_ProgramCounter += 1;
                    break;

                case 0x04:
                case 0x44:
                case 0x64:
                    Tick();
                    Tick();
                    m_ProgramCounter += 1;
                    break;

                case 0xc2:
                case 0xe2:
                case 0x89:
                case 0x82:
                case 0x80:
                    Tick();
                    m_ProgramCounter += 1;
                    break;

                case 0x1c:
                case 0x3c:
                case 0x5c:
                case 0x7c:
                case 0xdc:
                case 0xfc:
                    Tick();
                    AbsoluteIndexedX();
                    break;
                case 0x0c:
                    Tick();
                    Tick();
                    Tick();
                    m_ProgramCounter += 2;
                    break;
                case 0x1a:
                case 0x3a:
                case 0x5a:
                case 0x7a:
                case 0xda:
                case 0xfa:
                    Tick();
                    break;
                case 0x0b:
                case 0x2b:
                    Anc(Immediate());
                    break;
                case 0xcb:
                    Axs(Immediate());
                    break;
                case 0xa3:
                    Lax(IndexedIndirect());
                    break;
                case 0xa7:
                    Lax(ZeroPage());
                    break;
                case 0xb7:
                    Lax(ZeroPageIndexedY());
                    break;
                case 0xaf:
                    Lax(Absolute());
                    break;
                case 0xbf:
                    Lax(AbsoluteIndexedY());
                    break;
                case 0xb3:
                    Lax(IndirectIndexed());
                    break;
                case 0x87:
                    Aax(ZeroPage());
                    break;
                case 0x97:
                    Aax(ZeroPageIndexedY());
                    break;
                case 0x83:
                    Aax(IndexedIndirect());
                    break;
                case 0x8f:
                    Aax(Absolute());
                    break;
                case 0x01:
                    Ora(IndexedIndirect());
                    break;
                case 0xc7:
                    Dcp(ZeroPage());
                    break;
                case 0xd7:
                    Dcp(ZeroPageIndexedX());
                    break;
                case 0xcf:
                    Dcp(Absolute());
                    break;
                case 0xdf:
                    Dcp(AbsoluteIndexedX());
                    break;
                case 0xdb:
                    Dcp(AbsoluteIndexedY());
                    break;
                case 0xc3:
                    Dcp(IndexedIndirect());
                    break;
                case 0xd3:
                    Dcp(IndirectIndexed());
                    break;
                case 0x67:
                    Rra(ZeroPage());
                    break;
                case 0x77:
                    Rra(ZeroPageIndexedX());
                    break;
                case 0x6f:
                    Rra(Absolute());
                    break;
                case 0x7f:
                    Rra(AbsoluteIndexedX());
                    break;
                case 0x7b:
                    Rra(AbsoluteIndexedY());
                    break;
                case 0x63:
                    Rra(IndexedIndirect());
                    break;
                case 0x73:
                    Rra(IndirectIndexed());
                    break;
                case 0xe7:
                    Isb(ZeroPage());
                    break;
                case 0xf7:
                    Isb(ZeroPageIndexedX());
                    break;
                case 0xef:
                    Isb(Absolute());
                    break;
                case 0xff:
                    Isb(AbsoluteIndexedX());
                    break;
                case 0xfb:
                    Isb(AbsoluteIndexedY());
                    break;
                case 0xe3:
                    Isb(IndexedIndirect());
                    break;
                case 0xf3:
                    Isb(IndirectIndexed());
                    break;
                case 0x27:
                    Rla(ZeroPage());
                    break;
                case 0x37:
                    Rla(ZeroPageIndexedX());
                    break;
                case 0x2f:
                    Rla(Absolute());
                    break;
                case 0x3f:
                    Tick();
                    Rla(AbsoluteIndexedX());
                    break;
                case 0x3b:
                    Tick();
                    Rla(AbsoluteIndexedY());
                    break;
                case 0x23:
                    Rla(IndexedIndirect());
                    break;
                case 0x33:
                    Tick();
                    Rla(IndirectIndexed());
                    break;
                case 0x47:
                    Sre(ZeroPage());
                    break;
                case 0x57:
                    Sre(ZeroPageIndexedX());
                    break;
                case 0x4f:
                    Sre(Absolute());
                    break;
                case 0x5f:
                    Tick();
                    Sre(AbsoluteIndexedX(BoundaryCrossAction.None));
                    break;
                case 0x5b:
                    Sre(AbsoluteIndexedY());
                    break;
                case 0x43:
                    Sre(IndexedIndirect());
                    break;
                case 0x53:
                    Sre(IndirectIndexed());
                    break;
                case 0x07:
                    Slo(ZeroPage());
                    break;
                case 0x17:
                    Slo(ZeroPageIndexedX());
                    break;
                case 0x0f:
                    Slo(Absolute());
                    break;
                case 0x1f:
                    Tick();
                    Slo(AbsoluteIndexedX());
                    break;
                case 0x1b:
                    Tick();
                    Slo(AbsoluteIndexedY());
                    break;
                case 0x03:
                    Slo(IndexedIndirect());
                    break;
                case 0x13:
                    Tick();
                    Slo(IndirectIndexed());
                    break;


                case 0x05:
                    Ora(ZeroPage());
                    break;
                case 0x06:
                    Asl(ZeroPage());
                    break;
                case 0x08:
                    Php();
                    break;
                case 0x09:
                    Ora(Immediate());
                    break;
                case 0x0a:
                    AslAccumulator();
                    break;
                case 0x0d:
                    Ora(Absolute());
                    break;
                case 0x0e:
                    Asl(Absolute());
                    break;
                case 0x10:
                    Branch(m_FlagNegative == 0);
                    break;
                case 0x11:
                    Ora(IndirectIndexed());
                    break;
                case 0x15:
                    Ora(ZeroPageIndexedX());
                    break;
                case 0x16:
                    Asl(ZeroPageIndexedX());
                    break;
                case 0x18:
                    m_FlagCarry = 0;
                    Tick();
                    break;
                case 0x19:
                    Ora(AbsoluteIndexedY());
                    break;
                case 0x1d:
                    Ora(AbsoluteIndexedX());
                    break;
                case 0x1e:
                    Tick();
                    Asl(AbsoluteIndexedX(BoundaryCrossAction.None));
                    break;
                case 0x20:
                    Jsr();
                    break;
                case 0x21:
                    And(IndexedIndirect());
                    break;
                case 0x24:
                    Bit(ZeroPage());
                    break;
                case 0x25:
                    And(ZeroPage());
                    break;
                case 0x26:
                    Rol(ZeroPage());
                    break;
                case 0x28:
                    Plp();
                    break;
                case 0x29:
                    And(Immediate());
                    break;
                case 0x2a:
                    RolAccumulator();
                    break;
                case 0x2c:
                    Bit(Absolute());
                    break;
                case 0x2d:
                    And(Absolute());
                    break;
                case 0x2e:
                    Rol(Absolute());
                    break;
                case 0x30:
                    Branch(m_FlagNegative == 1);
                    break;
                case 0x31:
                    And(IndirectIndexed());
                    break;
                case 0x35:
                    And(ZeroPageIndexedX());
                    break;
                case 0x36:
                    Rol(ZeroPageIndexedX());
                    break;
                case 0x38:
                    m_FlagCarry = 1;
                    Tick();
                    break;
                case 0x39:
                    And(AbsoluteIndexedY());
                    break;
                case 0x3d:
                    And(AbsoluteIndexedX());
                    break;
                case 0x3e:
                    Tick();
                    Rol(AbsoluteIndexedX(BoundaryCrossAction.None));
                    break;
                case 0x40:
                    Rti();
                    break;
                case 0x41:
                    Eor(IndexedIndirect());
                    break;
                case 0x45:
                    Eor(ZeroPage());
                    break;
                case 0x46:
                    Lsr(ZeroPage());
                    break;
                case 0x48:
                    Pha();
                    break;
                case 0x49:
                    Eor(Immediate());
                    break;
                case 0x4a:
                    LsrAccumulator();
                    break;
                case 0x4c:
                    JmpAbsolute();
                    break;
                case 0x4d:
                    Eor(Absolute());
                    break;
                case 0x4e:
                    Lsr(Absolute());
                    break;
                case 0x50:
                    Branch(m_FlagOverflow == 0);
                    break;
                case 0x51:
                    Eor(IndirectIndexed());
                    break;
                case 0x55:
                    Eor(ZeroPageIndexedX());
                    break;
                case 0x56:
                    Lsr(ZeroPageIndexedX());
                    break;
                case 0x58:
                    m_FlagDisableInterrupts = 0;
                    Tick();
                    break;
                case 0x59:
                    Eor(AbsoluteIndexedY());
                    break;
                case 0x5d:
                    Eor(AbsoluteIndexedX());
                    break;
                case 0x5e:
                    Tick();
                    Lsr(AbsoluteIndexedX(BoundaryCrossAction.None));
                    break;
                case 0x60:
                    Rts();
                    break;
                case 0x61:
                    Adc(IndexedIndirect());
                    break;
                case 0x65:
                    Adc(ZeroPage());
                    break;
                case 0x66:
                    Ror(ZeroPage());
                    break;
                case 0x68:
                    Pla();
                    break;
                case 0x69:
                    Adc(Immediate());
                    break;
                case 0x6a:
                    RorAccumulator();
                    break;
                case 0x6c:
                    JmpIndirect();
                    break;
                case 0x6d:
                    Adc(Absolute());
                    break;
                case 0x6e:
                    Ror(Absolute());
                    break;
                case 0x70:
                    Branch(m_FlagOverflow == 1);
                    break;
                case 0x71:
                    Adc(IndirectIndexed());
                    break;
                case 0x75:
                    Adc(ZeroPageIndexedX());
                    break;
                case 0x76:
                    Ror(ZeroPageIndexedX());
                    break;
                case 0x78:
                    m_FlagDisableInterrupts = 1;
                    Tick();
                    break;
                case 0x79:
                    Adc(AbsoluteIndexedY());
                    break;
                case 0x7d:
                    Adc(AbsoluteIndexedX());
                    break;
                case 0x7e:
                    Tick();
                    Ror(AbsoluteIndexedX(BoundaryCrossAction.None));
                    break;
                case 0x81:
                    Store(m_Accumulator, IndexedIndirect());
                    break;
                case 0x84:
                    Store(m_Y, ZeroPage());
                    break;
                case 0x85:
                    Store(m_Accumulator, ZeroPage());
                    break;
                case 0x86:
                    Store(m_X, ZeroPage());
                    break;
                case 0x88:
                    m_Y = Dec(m_Y);
                    break;
                case 0x8a:
                    m_Accumulator = Transfer(m_X);
                    break;
                case 0x8c:
                    Store(m_Y, Absolute());
                    break;
                case 0x8d:
                    Store(m_Accumulator, Absolute());
                    break;
                case 0x8e:
                    Store(m_X, Absolute());
                    break;
                case 0x90:
                    Branch(m_FlagCarry == 0);
                    break;
                case 0x91:
                    Tick();
                    Store(m_Accumulator, IndirectIndexed(BoundaryCrossAction.None));
                    break;
                case 0x94:
                    Store(m_Y, ZeroPageIndexedX());
                    break;
                case 0x95:
                    Store(m_Accumulator, ZeroPageIndexedX());
                    break;
                case 0x96:
                    Store(m_X, ZeroPageIndexedY());
                    break;
                case 0x98:
                    m_Accumulator = Transfer(m_Y);
                    break;
                case 0x99:
                    Tick();
                    Store(m_Accumulator, AbsoluteIndexedY(BoundaryCrossAction.None));
                    break;
                case 0x9a:
                    m_StackPointer = m_X; // no flags affected so can't use Transfer()
                    Tick();
                    break;
                case 0x9d:
                    Tick();
                    Store(m_Accumulator, AbsoluteIndexedX(BoundaryCrossAction.None));
                    break;
                case 0xa0:
                    m_Y = Load(Immediate());
                    break;
                case 0xa1:
                    m_Accumulator = Load(IndexedIndirect());
                    break;
                case 0xa2:
                    m_X = Load(Immediate());
                    break;
                case 0xa4:
                    m_Y = Load(ZeroPage());
                    break;
                case 0xa5:
                    m_Accumulator = Load(ZeroPage());
                    break;
                case 0xa6:
                    m_X = Load(ZeroPage());
                    break;
                case 0xa8:
                    m_Y = Transfer(m_Accumulator);
                    break;
                case 0xa9:
                    m_Accumulator = Load(Immediate());
                    break;
                case 0xaa:
                    m_X = Transfer(m_Accumulator);
                    break;
                case 0xac:
                    m_Y = Load(Absolute());
                    break;
                case 0xad:
                    m_Accumulator = Load(Absolute());
                    break;
                case 0xae:
                    m_X = Load(Absolute());
                    break;
                case 0xb0:
                    Branch(m_FlagCarry == 1);
                    break;
                case 0xb1:
                    m_Accumulator = Load(IndirectIndexed());
                    break;
                case 0xb4:
                    m_Y = Load(ZeroPageIndexedX());
                    break;
                case 0xb5:
                    m_Accumulator = Load(ZeroPageIndexedX());
                    break;
                case 0xb6:
                    m_X = Load(ZeroPageIndexedY());
                    break;
                case 0xb8:
                    m_FlagOverflow = 0;
                    Tick();
                    break;
                case 0xb9:
                    m_Accumulator = Load(AbsoluteIndexedY());
                    break;
                case 0xba:
                    m_X = Transfer(m_StackPointer);
                    break;
                case 0xbc:
                    m_Y = Load(AbsoluteIndexedX());
                    break;
                case 0xbd:
                    m_Accumulator = Load(AbsoluteIndexedX());
                    break;
                case 0xbe:
                    m_X = Load(AbsoluteIndexedY());
                    break;
                case 0xc0:
                    Cmp(m_Y, Immediate());
                    break;
                case 0xc1:
                    Cmp(m_Accumulator, IndexedIndirect());
                    break;
                case 0xc4:
                    Cmp(m_Y, ZeroPage());
                    break;
                case 0xc5:
                    Cmp(m_Accumulator, ZeroPage());
                    break;
                case 0xc6:
                    Dec(ZeroPage());
                    break;
                case 0xc8:
                    m_Y = Inc(m_Y);
                    break;
                case 0xc9:
                    Cmp(m_Accumulator, Immediate());
                    break;
                case 0xca:
                    m_X = Dec(m_X);
                    break;
                case 0xcc:
                    Cmp(m_Y, Absolute());
                    break;
                case 0xcd:
                    Cmp(m_Accumulator, Absolute());
                    break;
                case 0xce:
                    Dec(Absolute());
                    break;
                case 0xd0:
                    Branch(m_FlagZero == 0);
                    break;
                case 0xd1:
                    Cmp(m_Accumulator, IndirectIndexed());
                    break;
                case 0xd5:
                    Cmp(m_Accumulator, ZeroPageIndexedX());
                    break;
                case 0xd6:
                    Dec(ZeroPageIndexedX());
                    break;
                case 0xd8:
                    m_FlagDecimal = 0;
                    Tick();
                    break;
                case 0xd9:
                    Cmp(m_Accumulator, AbsoluteIndexedY());
                    break;
                case 0xdd:
                    Cmp(m_Accumulator, AbsoluteIndexedX());
                    break;
                case 0xde:
                    Tick();
                    Dec(AbsoluteIndexedX(BoundaryCrossAction.None));
                    break;
                case 0xe0:
                    Cmp(m_X, Immediate());
                    break;
                case 0xe1:
                    Sbc(IndexedIndirect());
                    break;
                case 0xe4:
                    Cmp(m_X, ZeroPage());
                    break;
                case 0xe5:
                    Sbc(ZeroPage());
                    break;
                case 0xe6:
                    Inc(ZeroPage());
                    break;
                case 0xe8:
                    m_X = Inc(m_X);
                    break;
                case 0xeb:
                case 0xe9:
                    Sbc(Immediate());
                    break;
                case 0xea:
                    Nop();
                    break;
                case 0xec:
                    Cmp(m_X, Absolute());
                    break;
                case 0xed:
                    Sbc(Absolute());
                    break;
                case 0xee:
                    Inc(Absolute());
                    break;
                case 0xf0:
                    Branch(m_FlagZero == 1);
                    break;
                case 0xf1:
                    Sbc(IndirectIndexed());
                    break;
                case 0xf5:
                    Sbc(ZeroPageIndexedX());
                    break;
                case 0xf6:
                    Inc(ZeroPageIndexedX());
                    break;
                case 0xf8:
                    m_FlagDecimal = 1;
                    Tick();
                    break;
                case 0xf9:
                    Sbc(AbsoluteIndexedY());
                    break;
                case 0xfd:
                    Sbc(AbsoluteIndexedX());
                    break;
                case 0xfe:
                    Tick();
                    Inc(AbsoluteIndexedX(BoundaryCrossAction.None));
                    break;
            }
        }
    }
}