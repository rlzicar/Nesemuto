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
using OpenTK.Input;

namespace Nesemuto
{
    public class Input
    {
        public Input()
        {
            try
            {
                LoadMappings();
            }
            catch
            {
                Console.Error.WriteLine("Error loading the config file. Using the default config.");
                m_ButtonsByKey[Key.A] = GamepadButton.B;
                m_ButtonsByKey[Key.S] = GamepadButton.A;
                m_ButtonsByKey[Key.Space] = GamepadButton.Select;
                m_ButtonsByKey[Key.Enter] = GamepadButton.Start;
                m_ButtonsByKey[Key.Right] = GamepadButton.Right;
                m_ButtonsByKey[Key.Left] = GamepadButton.Left;
                m_ButtonsByKey[Key.Up] = GamepadButton.Up;
                m_ButtonsByKey[Key.Down] = GamepadButton.Down;
            }
        }

        void LoadMappings()
        {
            var configLines = File.ReadAllLines("controls.cfg");
            foreach (var config in configLines)
            {
                if (!config.StartsWith("Button_"))
                {
                    continue;
                }

                var lineParts = config.Split(':').Select(i => i.Trim()).ToArray();
                var buttonName = lineParts[0];
                var keyName = lineParts[1];
                var button = (GamepadButton) Enum.Parse(typeof(GamepadButton),
                    buttonName.Substring("Button_".Length));
                var key = (Key) Enum.Parse(typeof(Key), keyName);
                m_ButtonsByKey[key] = button;
            }
        }

        public void HandleKeyDown(KeyboardKeyEventArgs e, Nes nes)
        {
            if (m_ButtonsByKey.TryGetValue(e.Key, out var button))
            {
                nes.SetControllerButtonPressed(button, true);
            }
        }

        public void HandleKeyUp(KeyboardKeyEventArgs e, Nes nes)
        {
            if (m_ButtonsByKey.TryGetValue(e.Key, out var button))
            {
                nes.SetControllerButtonPressed(button, false);
            }
        }

        readonly Dictionary<Key, GamepadButton> m_ButtonsByKey =
            new Dictionary<Key, GamepadButton>();
    }
}