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
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nesemuto
{
    public class Cheats
    {
        public Cheats(string cheatFile)
        {
            try
            {
                LoadFromFile(cheatFile);
            }
            catch (DirectoryNotFoundException)
            {
                throw;
            }
            catch (FileNotFoundException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new InvalidDataException($"The file \"{cheatFile}\" is not a valid cheat file");
            }
        }


        void LoadFromFile(string path)
        {
            var lines = File.ReadAllLines(path).Select(i => i.Trim());
            foreach (var line in lines)
            {
                if (!line.StartsWith("SC:") && !line.StartsWith("S:") && !line.StartsWith("GG:"))
                {
                    continue;
                }

                var cheatParts = line.Split(':');
                var cheatType = cheatParts[0];
                string cheatName = null;
                const string readSubstituteCheat = "S";
                const string cheatWithCompareValue = "SC";
                const string ggCheat = "GG";
                switch (cheatType)
                {
                    case readSubstituteCheat:
                    {
                        var addr = (ushort) cheatParts[1].HexToInt();
                        byte value = (byte) cheatParts[2].HexToInt();
                        AddCheat(addr, null, value);
                        cheatName = cheatParts[3];
                        break;
                    }

                    case cheatWithCompareValue:
                    {
                        var addr = (ushort) cheatParts[1].HexToInt();
                        byte value = (byte) cheatParts[2].HexToInt();
                        byte compareValue = (byte) cheatParts[3].HexToInt();
                        AddCheat(addr, compareValue, value);
                        cheatName = cheatParts[4];
                        break;
                    }

                    case ggCheat:
                        var gg = cheatParts[1];
                        AddCheat(gg);
                        cheatName = cheatParts[2];

                        break;
                }

                if (cheatName != null)
                {
                    Console.WriteLine($"[cheat] {cheatName}");
                }
            }
        }


        void AddCheat(string gameGenieCode)
        {
            if (gameGenieCode.Length != 6 && gameGenieCode.Length != 8)
            {
                throw new ArgumentException();
            }

            gameGenieCode = gameGenieCode.ToUpper();

            var values = new byte[gameGenieCode.Length];
            for (int i = 0; i < gameGenieCode.Length; i++)
            {
                var c = gameGenieCode[i];
                values[i] = m_GameGenieLookup[c];
            }

            var address = 0x8000 + ((values[3] & 7) << 12)
                          | ((values[5] & 7) << 8) | ((values[4] & 8) << 8)
                          | ((values[2] & 7) << 4) | ((values[1] & 8) << 4)
                          | (values[4] & 7) | (values[3] & 8);


            if (gameGenieCode.Length == 6)
            {
                var value =
                    ((values[1] & 7) << 4) | ((values[0] & 8) << 4)
                                           | (values[0] & 7) | (values[5] & 8);
                AddCheat((ushort) address, null, (byte) value);
            }
            else
            {
                var value =
                    ((values[1] & 7) << 4) | ((values[0] & 8) << 4)
                                           | (values[0] & 7) | (values[7] & 8);

                var compare =
                    ((values[7] & 7) << 4) | ((values[6] & 8) << 4)
                                           | (values[6] & 7) | (values[5] & 8);
                AddCheat((ushort) address, (byte) compare, (byte) value);
            }
        }


        void AddCheat(ushort addr, byte? compareValue, byte patchValue)
        {
            if (Count == m_Cheats.Length)
            {
                throw new InvalidOperationException();
            }

            var cheat = new Cheat
            {
                Addr = addr,
                CompareValue = compareValue ?? -1,
                Value = patchValue
            };
            m_PossibleCheatNearbyAddr[addr >> 3] = true;
            m_Cheats[Count] = cheat;
            Count += 1;
        }

        public byte InterceptRead(ushort addr, byte knownValue)
        {
            if (!m_PossibleCheatNearbyAddr[addr >> 3])
            {
                // optimization: no cheat near this addr, no need to do any further checks
                return knownValue;
            }

            var cheats = m_Cheats;
            for (int i = 0; i < Count; i++)
            {
                ref var cheat = ref cheats[i];
                if (cheat.Addr == addr && (cheat.CompareValue == -1 || knownValue == cheat.CompareValue))
                {
                    return cheat.Value;
                }
            }

            return knownValue;
        }

        public int Count { get; private set; }

        struct Cheat
        {
            public ushort Addr;
            public int CompareValue;
            public byte Value;
        }

        readonly Dictionary<char, byte> m_GameGenieLookup = new Dictionary<char, byte>
        {
            {'A', 0x0},
            {'P', 0x1},
            {'Z', 0x2},
            {'L', 0x3},
            {'G', 0x4},
            {'I', 0x5},
            {'T', 0x6},
            {'Y', 0x7},
            {'E', 0x8},
            {'O', 0x9},
            {'X', 0xa},
            {'U', 0xb},
            {'K', 0xc},
            {'S', 0xd},
            {'V', 0xe},
            {'N', 0xf}
        };

        const int k_MaxCheatCount = 64;
        readonly bool[] m_PossibleCheatNearbyAddr = new bool[(0xffff >> 3) + 1];
        readonly Cheat[] m_Cheats = new Cheat[k_MaxCheatCount];
    }
}