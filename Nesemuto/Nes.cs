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
    public sealed class Nes : IDisposable
    {
        public Nes(string path, string cheatPath, ushort? entryPoint = null)
        {
            var cartridge = new Cartridge(path);

            Console.WriteLine($"Mapper: {cartridge.MapperId}");
            Console.WriteLine($"PRG size: 0x{cartridge.PrgSize.ToHex()}");
            Console.WriteLine($"CHR size: 0x{cartridge.ChrSize.ToHex()}");
            Console.WriteLine($"Mirroring: {cartridge.Mirroring}");
            Console.WriteLine();

            if (cartridge.Mapper == null)
            {
                throw new InvalidOperationException("Unsupported mapper");
            }

            var mapper = cartridge.Mapper;
            var cheats = string.IsNullOrEmpty(cheatPath) ? null : new Cheats(cheatPath);

            m_Ppu = new Ppu(mapper);
            m_Apu = new Apu(mapper);
            m_Cpu = new Cpu(mapper, m_Ppu, m_Apu, cheats);

            m_Cpu.PowerOn(entryPoint);
        }

        public bool AudioEnabled
        {
            set => m_Apu.Enabled = value;
            get => m_Apu.Enabled;
        }

        public bool CheatsEnabled
        {
            set => m_Cpu.CheatsEnabled = value;
            get => m_Cpu.CheatsEnabled;
        }

        public void EmulateFrame()
        {
            m_Cpu.EmulateFrame();
            m_Ppu.FlushPixels();
        }

        public void Reset()
        {
            m_Cpu.Reset();
            m_Ppu.Reset();
            m_Apu.Reset();
        }

        public void SetControllerButtonPressed(GamepadButton button, bool isPressed)
        {
            m_Cpu.SetGamepadButtonPressed(button, isPressed);
        }

        public void Dispose()
        {
            m_Apu.Dispose();
        }

        public int[] Pixels => m_Ppu.Pixels;

        readonly Cpu m_Cpu;
        readonly Ppu m_Ppu;
        readonly Apu m_Apu;
    }
}