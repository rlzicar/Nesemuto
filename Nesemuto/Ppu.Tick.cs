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
using System.Runtime.CompilerServices;

namespace Nesemuto
{
    public partial class Ppu
    {
        public void Run(int tickCount)
        {
            bool showSprites = m_PpuMask.ShowSprites;
            bool showBg = m_PpuMask.ShowBackground;
            int x = m_Dot;
            int y = m_Scanline;
            for (int i = 0; i < tickCount; i++)
            {
                Tick(ref x, ref y, showSprites, showBg);
            }

            m_Scanline = y;
            m_Dot = x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Tick(ref int dot, ref int scanline, bool showSprites, bool showBg)
        {
            switch (scanline)
            {
                case -1:
                    TickPrerenderScanline(ref dot, showSprites, showBg);
                    break;
                case var line when line >= 0 && line <= 239:
                    TickScanline(ref dot, scanline, showSprites, showBg);
                    break;
                case 240:
                    // Idle
                    break;
                case var line when line >= NmiScanline && line <= 260:
                    TickVerticalBlank(dot, scanline);
                    break;
            }

            bool isVisiblePixel = scanline >= 0 && scanline < k_ScreenHeight
                                                && dot >= 0 && dot < k_ScreenWidth;
            if (isVisiblePixel)
            {
                RenderPixel(dot, scanline, showSprites, showBg);
            }

            bool shouldSignalScanline = dot == ScanlineIrqDot && scanline >= 0 && scanline <= 240;
            if (shouldSignalScanline && (showSprites || showBg))
            {
                // this is not really accurate (but it's dead simple and works fine in most games)
                // TODO: implement accurate scanline counting based on PPU A12
                m_Mapper.OnScanline();
            }

            dot += 1;

            bool isEndOfScanline = dot == ScanlineEndDot;
            if (isEndOfScanline)
            {
                dot = 0;
                scanline += 1;
                m_FineScrollX = m_NextFineScrollX;
            }

            bool isEndOfFrame = scanline == 261;
            if (isEndOfFrame)
            {
                scanline = -1;
                dot = 0;
                m_EvenFrame = !m_EvenFrame;
                m_FrameCount += 1;
                Array.Clear(m_CellSpriteCounts, 0, k_SpriteGridHeight * k_SpriteGridWidth);
                Array.Clear(m_AnySpriteOnScanline, 0, m_AnySpriteOnScanline.Length);
            }
        }
        
        void TickPrerenderScanline(ref int dot, bool showSprites, bool showBackground)
        {
            bool shouldSkipOneCycle = dot == 339 && !m_EvenFrame && showBackground;
            if (shouldSkipOneCycle)
            {
                dot += 1;
            }

            if (dot == 1)
            {
                m_PpuStatus.SpriteOverflow = false;
            }
            else if (dot == 2)
            {
                m_PpuStatus.VBlank = false;
            }

            TickScanline(ref dot, -1, showSprites, showBackground);

            bool shouldCopyVerticalBitsFromTemporary = dot >= 280 && dot <= 304 && (showSprites || showBackground);
            if (shouldCopyVerticalBitsFromTemporary)
            {
                var t = m_TemporaryVramAddress;
                m_CurrentVramAddress = (ushort) (m_CurrentVramAddress & ~0b1111101111100000);
                m_CurrentVramAddress |= (ushort) (t & 0b1111101111100000);
            }
        }


// see the NTSC frame timing diagram (https://wiki.nesdev.com/w/images/d/d1/Ntsc_timing.png)
        void TickScanline(ref int dot, int scanline, bool showSprites, bool showBackground)
        {
            bool renderingEnabled = showSprites || showBackground;

            if (dot >= 257 && dot <= 320)
            {
                m_OamAddress = 0;
            }

            if (dot == 257 && scanline < 239 && renderingEnabled)
            {
                EvaluateSprites(scanline + 1);
            }
            else if (dot == 321 && scanline < 239 && renderingEnabled)
            {
                FetchSprites(scanline + 1);
            }

            if (dot >= 1 && dot <= 256 || dot >= 321 && dot <= 336)
            {
                m_BgPatternShiftRegisterHigh <<= 1;
                m_BgPatternShiftRegisterLow <<= 1;
                const int setNametableFetchAddress = 1;
                const int loadNameTableValue = 2;
                const int setAttributeFetchAddress = 3;
                const int loadAttributeValue = 4;
                const int setLowBgTileFetchAddr = 5;
                const int loadLowBgTileValue = 6;
                const int setHighBgTileFetchAddr = 7;
                const int finishTile = 0;

                switch (dot % 8)
                {
                    case setNametableFetchAddress:
                    {
                        var v = m_CurrentVramAddress;
                        var nameTableAddr = (ushort) (0x2000 | (v & 0x0fff));
                        m_FetchAddr = nameTableAddr;
                        m_CurrentVramAddress = v;
                    }
                        break;
                    case loadNameTableValue:
                        m_NameTableValue = m_Mapper.Read(m_FetchAddr);
                        break;
                    case setAttributeFetchAddress:
                    {
                        var v = m_CurrentVramAddress;
                        var attributeAddress = (ushort) (0x23c0 | (v & 0x0c00)
                                                                | ((v >> 4) & 0x38) | ((v >> 2) & 0x07));
                        m_FetchAddr = attributeAddress;
                        m_CurrentVramAddress = v;
                    }
                        break;
                    case loadAttributeValue:
                    {
                        var attr = m_Mapper.Read(m_FetchAddr);
                        var v = m_CurrentVramAddress;
                        var coarseX = v & 0x1f;
                        var coarseY = (v & 0x3e0) >> 5;

                        var tileX = coarseX / 2;
                        var tileY = coarseY / 2;
                        bool left = (tileX & 1) == 0;
                        bool top = (tileY & 1) == 0;

                        var bgAttributes = attr;
                        byte bgAttributeBits;
                        if (top && left)
                        {
                            // top left
                            bgAttributeBits = (byte) (bgAttributes & 3);
                        }
                        else if (top)
                        {
                            // top right
                            bgAttributeBits = (byte) ((bgAttributes >> 2) & 3);
                        }
                        else if (left)
                        {
                            // bottom left
                            bgAttributeBits = (byte) ((bgAttributes >> 4) & 3);
                        }
                        else
                        {
                            // bottom right 
                            bgAttributeBits = (byte) ((bgAttributes >> 6) & 3);
                        }

                        m_AttributeValue = (byte) (bgAttributeBits << 2);
                        break;
                    }

                    case setLowBgTileFetchAddr:
                    {
                        var v = m_CurrentVramAddress;
                        var fineScrollY = (ushort) ((v >> 12) & 7);
                        var lowBgPatternAddr =
                            (ushort) (m_PpuCtrl.BackgroundTableAddress + (m_NameTableValue * 16) + fineScrollY);
                        m_FetchAddr = lowBgPatternAddr;
                        break;
                    }

                    case loadLowBgTileValue:
                        m_LowBgTilePatternByte = m_Mapper.Read(m_FetchAddr);
                        break;
                    case setHighBgTileFetchAddr:
                        var highBgPatternAddr = (ushort) (m_FetchAddr + 8);
                        m_FetchAddr = highBgPatternAddr;
                        break;
                    case finishTile:
                        m_HighBgTilePatternByte = m_Mapper.Read(m_FetchAddr);

                        m_BgAttributeShiftRegister <<= 8;
                        m_BgAttributeShiftRegister |= m_AttributeValue;

                        m_BgPatternShiftRegisterLow &= 0xff00;
                        m_BgPatternShiftRegisterLow |= (ushort) (m_LowBgTilePatternByte << m_FineScrollX);

                        m_BgPatternShiftRegisterHigh &= 0xff00;
                        m_BgPatternShiftRegisterHigh |= (ushort) (m_HighBgTilePatternByte << m_FineScrollX);

                        if (renderingEnabled)
                        {
                            m_CurrentVramAddress = IncrementCoarseX(m_CurrentVramAddress);
                        }

                        break;
                }
            }

            bool shouldIncrementCoarseY = dot == 256 && renderingEnabled;

            bool shouldCopyHorizontalBitsFromTemporary = dot == 257 && renderingEnabled;
            if (shouldIncrementCoarseY)
            {
                m_CurrentVramAddress = IncrementCoarseY(m_CurrentVramAddress);
            }

            else if (shouldCopyHorizontalBitsFromTemporary)
            {
                var v = m_CurrentVramAddress;
                var t = m_TemporaryVramAddress;
                v = (ushort) (v & ~0b0000010000011111);
                v |= (ushort) (t & 0b0000010000011111);
                m_CurrentVramAddress = v;
            }
        }
        
        void TickVerticalBlank(int dot, int scanline)
        {
            switch (scanline)
            {
                case NmiScanline when dot == 1:
                    m_PpuStatus.VBlank = true;
                    break;
                case NmiScanline when dot == 4 && (m_PpuCtrl.GenerateNmi && m_PpuStatus.VBlank):
                    m_Nmi = true;
                    break;
                case 260 when dot == 340:
                    m_PpuStatus.Sprite0Hit = false;
                    break;
            }
        }
    }
}