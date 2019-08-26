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
        void Nmi()
        {
            Tick();
            Push((byte) (m_ProgramCounter >> 8));
            Push((byte) m_ProgramCounter);
            Push(FlagsToByte());

            m_FlagDisableInterrupts = 1;
            m_ProgramCounter = Read16(0xfffa);
        }

        void Brk()
        {
            var flags = FlagsToByte();
            const int bFlag = 1 << 4;
            flags |= bFlag;
            Tick();
                                                                                           
            m_ProgramCounter += 1; // BRK padding byte

            Push((byte) (m_ProgramCounter >> 8));
            Push((byte) m_ProgramCounter);
            Push(flags);

            m_FlagDisableInterrupts = 1; 
            
            m_ProgramCounter = Read16(0xfffe);
        }

        void Irq()
        {
            Tick();
            Push((byte) (m_ProgramCounter >> 8));
            Push((byte) m_ProgramCounter);
            Push(FlagsToByte());
            m_FlagDisableInterrupts = 1;
            m_ProgramCounter = Read16(0xfffe);
        }
    }
}