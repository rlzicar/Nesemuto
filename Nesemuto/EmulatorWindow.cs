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
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;

namespace Nesemuto
{
    public class EmulatorWindow : GameWindow
    {
        public EmulatorWindow(Nes nes, VSyncMode vSyncMode) :
            base(k_NesScreenWidth * k_DefaultScreenSizeMultiplier,
                k_NesScreenHeight * k_DefaultScreenSizeMultiplier,
                GraphicsMode.Default, k_Title)
        {
            VSync = vSyncMode;
            m_Nes = nes;
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);
            m_Input.HandleKeyDown(e, m_Nes);

            var key = e.Key;
            switch (key)
            {
                case Key.C when e.Control:
                    m_Nes.CheatsEnabled = !m_Nes.CheatsEnabled;
                    Console.WriteLine($"Cheats {(m_Nes.CheatsEnabled ? "enabled" : "disabled")}");
                    break;
                case Key.R when e.Control:
                    m_Nes.Reset();
                    break;
                case Key.M when e.Control:
                    m_Nes.AudioEnabled = !m_Nes.AudioEnabled;
                    Console.WriteLine($"Audio {(m_Nes.AudioEnabled ? "enabled" : "disabled")}");
                    break;
                case Key.F1:
                case Key.F2:
                case Key.F3:
                    ChangeWindowSize(m_SizeMultipliers[key - Key.F1]);
                    break;
                case Key.F11:
                    ToggleFullscreen();
                    break;
                case Key.F4 when e.Alt:
                {
                    Close();
                    break;
                }
            }
        }

        void ToggleFullscreen()
        {
            WindowState =
                WindowState == WindowState.Fullscreen ? WindowState.Normal : WindowState.Fullscreen;
        }

        void ChangeWindowSize(float nesScreenSizeMultiplier)
        {
            m_VertexCoordX = 1;
            WindowState = WindowState.Normal;
            var oldWidth = Width;
            var oldHeight = Height;
            Width = (int) (k_NesScreenWidth * nesScreenSizeMultiplier);
            Height = (int) (k_NesScreenHeight * nesScreenSizeMultiplier);
            X += (oldWidth - Width) / 2;
            Y += (oldHeight - Height) / 2;
        }


        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            base.OnKeyUp(e);
            m_Input.HandleKeyUp(e, m_Nes);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            m_TextureId = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, m_TextureId);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                256, 256, 0, PixelFormat.Bgra,
                PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int) TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int) TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,
                (int) TextureWrapMode.Clamp);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,
                (int) TextureWrapMode.Clamp);
            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Texture2D);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            const float screenRatio = k_NesScreenWidth / (float) k_NesScreenHeight;
            if (Width >= Height)
            {
                float viewportWidth = Height * screenRatio;
                m_VertexCoordX = viewportWidth / Width;
                m_VertexCoordY = 1f;
            }
            else
            {
                float viewportHeight = Width / screenRatio;
                m_VertexCoordY = viewportHeight / Height;
                m_VertexCoordX = 1f;
            }

            GL.Viewport(0, 0, Width, Height);

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(-1.0, 1.0, -1.0, 1.0, 0.0, 4.0);
        }


        protected override void OnRenderFrame(FrameEventArgs e)
        {
            m_Nes.EmulateFrame();
            Context.MakeCurrent(WindowInfo);

            GL.Clear(ClearBufferMask.DepthBufferBit);
            GL.BindTexture(TextureTarget.Texture2D, m_TextureId);
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0,
                256, 240, PixelFormat.Bgra, PixelType.UnsignedByte,
                m_Nes.Pixels);

            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0, 0);
            GL.Vertex2(-m_VertexCoordX, m_VertexCoordY);

            GL.TexCoord2(1, 0);
            GL.Vertex2(m_VertexCoordX, m_VertexCoordY);

            const float texCoordY = 240 / 256f;
            GL.TexCoord2(1, texCoordY);
            GL.Vertex2(m_VertexCoordX, -m_VertexCoordY);

            GL.TexCoord2(0, texCoordY);
            GL.Vertex2(-m_VertexCoordX, -m_VertexCoordY);
            GL.End();
            Context.SwapBuffers();
        }


        public override void Dispose()
        {
            base.Dispose();
            if (GraphicsContext.CurrentContext != null)
            {
                GL.DeleteTexture(m_TextureId);
            }
        }

        const int k_NesScreenWidth = 256;
        const int k_NesScreenHeight = 240;
        const int k_DefaultScreenSizeMultiplier = 2;
        const string k_Title = "Nesemuto";

        readonly float[] m_SizeMultipliers = {1, 2, 3};
        readonly Input m_Input = new Input();
        float m_VertexCoordX;
        float m_VertexCoordY;
        readonly Nes m_Nes;
        int m_TextureId;
    }
}