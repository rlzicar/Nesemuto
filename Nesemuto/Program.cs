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

using OpenTK;
using System;

namespace Nesemuto
{
    internal static class Program
    {
        const string k_Version = "0.1";
        
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine($"Nesemuto v{k_Version}");
                Console.WriteLine("usage: nesemuto pathToGameFile [pathToCheatFile]");
                return;
            }

            Nes nes;
            try
            {
                string gamePath = args[0];
                string cheatPath = args.Length >= 2 ? args[1] : null;
                nes = new Nes(gamePath, cheatPath);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return;
            }

            using (nes)
            {
                using (var window = new EmulatorWindow(nes, VSyncMode.On))
                {
                    const int nesFramesPerSecond = 60;
                    window.Run(0, nesFramesPerSecond);
                }
            }
        }
    }
}