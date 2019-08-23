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
    public sealed partial class Ppu
    {
        public Ppu(Mapper mapper)
        {
            m_Mapper = mapper;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            m_Dot = 30;
            m_Scanline = 0;
        }

        public int Dot => m_Dot;
        public int Scanline => m_Scanline;
        public int FrameCount => m_FrameCount;

        public const int ScanlineIrqDot = 290;
        public const int ScanlineEndDot = 341;
        public const int NmiScanline = 241;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int GetBgColorAddr(int dot, int pixel, ushort attributeShiftRegister, int fineScrollX)
        {
            const int baseBgColorAddr = 0x3f00;
            int bgAttributeBits;
            bool tileBorderCrossed = (dot + fineScrollX) >> 3 != dot >> 3;
            if (!tileBorderCrossed)
            {
                bgAttributeBits = attributeShiftRegister >> 8;
            }
            else
            {
                bgAttributeBits = attributeShiftRegister & 0x00ff;
            }

            return pixel | bgAttributeBits | baseBgColorAddr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int GetPixelColorAddr(int pixel, int attr)
        {
            const int baseSpriteColorAddr = 0x3f10;
            return (attr & 3) << 2 | pixel | baseSpriteColorAddr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int GetBgPixel()
        {
            var bgBitLow = (m_BgPatternShiftRegisterLow & 0x8000) >> 15;
            var bgBitHigh = (m_BgPatternShiftRegisterHigh & 0x8000) >> 14;
            return bgBitLow | bgBitHigh;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte AccessPalette(int addr, MemoryAccessMode mode, byte value)
        {
            bool read = mode == MemoryAccessMode.Read;
            bool write = !read;
            if (addr >= 0x3f20)
            {
                addr = (0x3f00 + addr % 0x20);
            }
            else if (addr >= 0x3f10)
            {
                bool shouldMirror = addr == 0x3f10 || addr == 0x3f14 || addr == 0x3f18 || addr == 0x3f1c;
                if (shouldMirror)
                {
                    addr -= 0x10;
                }
            }

            bool isBackdropAddr = addr == 0x3f04 || addr == 0x3f08 || addr == 0x3f0c;
            if (read && isBackdropAddr)
            {
                addr = 0x3f00;
            }

            if (write)
            {
                m_Palettes[addr - 0x3f00] = value;
                return 0;
            }

            return (byte) m_Palettes[addr - 0x3f00];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void RenderPixel(int x, int y, bool showSprites, bool showBackground)
        {
            int spriteAttributes = 0;
            bool isSprite0 = false;
            int spritePixel = 0;
            int bgPixel = 0;

            int maxSpritesOverlappingCurrentPos;
            if (showSprites && m_AnySpriteOnScanline[y] &&
                (maxSpritesOverlappingCurrentPos = GetSpriteCountNear(x, y)) > 0)
            {
                int processedSpriteCount = 0;
                bool opaqueSpritePixelFound = false;
                var secondaryOam = m_SecondaryOam;
                var spriteOnScanlineCount = m_SecondaryOamIdx;

                for (int i = 0;
                    i < spriteOnScanlineCount && processedSpriteCount < maxSpritesOverlappingCurrentPos;
                    i++)
                {
                    ref var sprite = ref secondaryOam[i];
                    bool spriteInRange = x >= sprite.X && x <= sprite.X + 7
                                                       && sprite.X != 0xff && sprite.Y != 0xff;
                    if (!spriteInRange)
                    {
                        continue;
                    }

                    if (!opaqueSpritePixelFound)
                    {
                        var spriteBitHigh = (sprite.HighPatternByte & 0x80) >> 6;
                        var spriteBitLow = (sprite.LowPatternByte & 0x80) >> 7;
                        var bits = spriteBitLow | spriteBitHigh;

                        opaqueSpritePixelFound = bits > 0;
                        if (opaqueSpritePixelFound)
                        {
                            spriteAttributes = sprite.Attributes;
                            isSprite0 = sprite.IsSpriteZero;
                            spritePixel = (byte) bits;
                            // no break here, still need to shift the pattern bits of all the sprites in range
                        }
                    }

                    sprite.HighPatternByte <<= 1;
                    sprite.LowPatternByte <<= 1;
                    processedSpriteCount += 1;
                }
            }

            if (showBackground)
            {
                bgPixel = GetBgPixel();
            }

            bool shouldOutputSpritePixel = false;
            bool bgPixelAvailable = bgPixel > 0;

            bool spritePixelAvailable = spritePixel > 0;
            if (bgPixelAvailable && spritePixelAvailable)
            {
                var spritePriority = (spriteAttributes >> 5) & 1;
                shouldOutputSpritePixel = spritePriority == 0;

                bool isSprite0Hit = isSprite0 && x < 255 && showBackground &&
                                    (x > 7 || (m_PpuMask.ShowBackgroundInLeftmost8 &&
                                               m_PpuMask.ShowSpritesInLeftmost8));
                if (isSprite0Hit)
                {
                    m_PpuStatus.Sprite0Hit = true;
                }
            }
            else if (!bgPixelAvailable)
            {
                shouldOutputSpritePixel = spritePixelAvailable;
            }

            const int blackColorAddr = 0x3f00;

            int colorAddr = blackColorAddr;
            if (shouldOutputSpritePixel && (x >= 8 || m_PpuMask.ShowSpritesInLeftmost8))
            {
                colorAddr = GetPixelColorAddr(spritePixel, spriteAttributes);
            }
            else if (showBackground && (x >= 8 || m_PpuMask.ShowBackgroundInLeftmost8))
            {
                colorAddr = GetBgColorAddr(x, bgPixel, m_BgAttributeShiftRegister, m_FineScrollX);
            }

            var idx = y * k_ScreenWidth + x;
            m_ColorAddresses[idx] = (ushort) colorAddr;
        }

        public int[] Pixels { get; } = new int[k_ScreenHeight * k_ScreenWidth];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsPaletteAddr(ushort addr)
        {
            return addr >= 0x3f00 && addr <= 0x3fff;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FlushPixels()
        {
            var colors = m_Colors;
            var addresses = m_ColorAddresses;
            for (int i = 0; i < k_ScreenWidth * k_ScreenHeight; i++)
            {
                var addr = addresses[i];
                var c = AccessPalette(addr, MemoryAccessMode.Read, 0);
                if (c == 0xff)
                {
                    Pixels[i] = 0;
                }
                else
                {
                    Pixels[i] = colors[c];
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
// https://wiki.nesdev.com/w/index.php/PPU_scrolling
        static ushort IncrementCoarseX(ushort v)
        {
            if ((v & 0x001f) == 31)
            {
                v = (ushort) (v & ~0x001f);
                v ^= 0x0400;
            }

            else
            {
                v += 1;
            }

            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
// https://wiki.nesdev.com/w/index.php/PPU_scrolling
        static ushort IncrementCoarseY(ushort v)
        {
            if ((v & 0x7000) != 0x7000)
            {
                v += 0x1000;
            }

            else
            {
                v = (ushort) (v & ~0x7000);

                int y = (v & 0x03e0) >> 5;
                switch (y)
                {
                    case 29:
                        y = 0;
                        v ^= 0x0800;
                        break;
                    case 31:
                        y = 0;
                        break;
                    default:
                        y += 1;
                        break;
                }

                v = (ushort) ((v & ~0x03e0) | (y << 5));
            }

            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ClearSecondaryOam()
        {
            var secondaryOam = m_SecondaryOam;
            for (int i = 0; i < 8; i++)
            {
                ref var sprite = ref secondaryOam[i];
                sprite.Attributes = 0xff;
                sprite.X = 0xff;
                sprite.Y = 0xff;
                sprite.TileIndex = 0xff;
            }

            m_SecondaryOamIdx = 0;
        }

        
        void EvaluateSprites(int scanline)
        {
            ClearSecondaryOam();
            var primaryOam = m_PrimaryOam;
            var secondaryOam = m_SecondaryOam;
            var spriteHeight = m_PpuCtrl.SpriteHeight;
            int y = scanline;

            const int maxSpriteCount = 64;
            for (int n = 0; n < maxSpriteCount; n++)
            {
                var primaryOamIdx = n * 4;
                var spriteY = primaryOam[primaryOamIdx];
                var adjustedY = spriteY + 1; // sprite data is delayed by one scanline
                var deltaY = scanline - adjustedY;
                bool spriteInRange = deltaY >= 0 && deltaY < spriteHeight;
                bool isSecondaryOamFull = m_SecondaryOamIdx == 8;

                ref var sprite = ref secondaryOam[m_SecondaryOamIdx];
                // sprite Y is always set (even if not in range)
                sprite.Y = spriteY;
                sprite.IsSpriteZero = n == 0;

                if (!spriteInRange)
                {
                    continue;
                }

                if (!isSecondaryOamFull)
                {
                    sprite.TileIndex = primaryOam[++primaryOamIdx];
                    sprite.Attributes = primaryOam[++primaryOamIdx];
                    sprite.X = primaryOam[++primaryOamIdx];

                    m_SecondaryOamIdx += 1;
                    IncrementSpriteCountInCell(sprite.X, y);
                }
                else
                {
                    // This is not a 100% accurate implementation:
                    // There is a hardware bug on the real NES causing both false positives and
                    // false negatives in some edge cases.
                    // This is good enough for most games, though. 
                    m_PpuStatus.SpriteOverflow = true;
                }
            }

            m_AnySpriteOnScanline[y] = m_SecondaryOamIdx > 0;
        }

        
        void FetchSprites(int scanline)
        {
            var secondaryOam = m_SecondaryOam;
            var spriteHeight = m_PpuCtrl.SpriteHeight;

            const int maxSpritesInSecondaryOamCount = 8;
            for (int i = 0; i < maxSpritesInSecondaryOamCount; i++)
            {
                ref var sprite = ref secondaryOam[i];
                bool isLastSprite = sprite.X == 0xff && sprite.Y == 0xff && sprite.Attributes == 0xff &&
                                    sprite.TileIndex == 0xff;
                if (isLastSprite)
                {
                    break;
                }

                bool flipHorizontally = (sprite.Attributes & 0x40) == 0x40;
                bool flipVertically = (sprite.Attributes & 0x80) == 0x80;

                var adjustedY = sprite.Y + 1; // sprite data is delayed by one scanline
                var spriteLine = scanline - adjustedY;
                if (spriteLine < 0 || spriteLine >= spriteHeight)
                {
                    continue;
                }

                if (flipVertically)
                {
                    spriteLine = spriteHeight - 1 - spriteLine;
                }

                ushort patternAddr;
                bool isSprite8X8Mode = spriteHeight == 8;

                if (isSprite8X8Mode)
                {
                    var baseAddr = m_PpuCtrl.SpritePatternTableAddress8X8;
                    patternAddr = (ushort) (baseAddr + (sprite.TileIndex * 16) + spriteLine);
                }
                else
                {
                    var topTileIdx = sprite.TileIndex >> 1;
                    var bit0 = sprite.TileIndex & 1;
                    var baseAddr = bit0 * 0x1000;
                    var offset = 0;
                    bool isBottomTile = spriteLine >= 8;
                    if (isBottomTile)
                    {
                        spriteLine -= 8;
                        offset = 16;
                    }

                    patternAddr = (ushort) (baseAddr + (topTileIdx * 32) + offset + spriteLine);
                }

                var mapper = m_Mapper;
                var lowPatternByte = mapper.Read(patternAddr);
                var highPatternByte = mapper.Read((ushort) (patternAddr + 8));

                if (flipHorizontally)
                {
                    lowPatternByte = ReverseBits(lowPatternByte);
                    highPatternByte = ReverseBits(highPatternByte);
                }

                sprite.LowPatternByte = lowPatternByte;
                sprite.HighPatternByte = highPatternByte;
            }
        }

        static byte ReverseBits(int b)
        {
            b = (b & 0xf0) >> 4 | (b & 0x0f) << 4;
            b = (b & 0xcc) >> 2 | (b & 0x33) << 2;
            b = (b & 0xaa) >> 1 | (b & 0x55) << 1;
            return (byte) b;
        }

        void IncrementSpriteCountInCell(int x, int y)
        {
            var cellX = x / k_SpriteCellSize;

            var cellY = y / k_SpriteCellSize;
            if (cellX < k_SpriteGridWidth)
            {
                m_CellSpriteCounts[cellY * k_SpriteGridWidth + cellX] += 1;
            }

            cellX = (x + 7) / k_SpriteCellSize;
            if (cellX < k_SpriteGridWidth)
            {
                m_CellSpriteCounts[cellY * k_SpriteGridWidth + cellX] += 1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int GetSpriteCountNear(int x, int y)
        {
            var cellX = x / k_SpriteCellSize;
            var cellY = y / k_SpriteCellSize;
            return m_CellSpriteCounts[cellY * k_SpriteGridWidth + cellX];
        }
        
        public bool ReadAndClearNmiFlag()
        {
            var nmi = m_Nmi;
            m_Nmi = m_NmiNextInstruction;
            m_NmiNextInstruction = false;
            return nmi;
        }

        const int k_ScreenWidth = 256;
        const int k_ScreenHeight = 240;
        readonly PpuStatus m_PpuStatus = new PpuStatus();
        readonly PpuCtrl m_PpuCtrl = new PpuCtrl();
        readonly PpuMask m_PpuMask = new PpuMask();

        struct Sprite
        {
            public byte Y; // byte 0 
            public byte TileIndex; // byte 1
            public byte Attributes; // byte 2
            public byte X; // byte 3

            public bool IsSpriteZero;
            public byte LowPatternByte;
            public byte HighPatternByte;
        }

        const int k_SpriteCellSize = 8;
        const int k_SpriteGridWidth = k_ScreenWidth / k_SpriteCellSize;
        const int k_SpriteGridHeight = k_ScreenHeight / k_SpriteCellSize;
        const int k_SecondaryOamSize = 8;
        bool m_AddressLatch;
        byte m_BufferedValue;
        int m_FrameCount;
        readonly byte[] m_CellSpriteCounts = new byte[k_SpriteGridHeight * k_SpriteGridWidth];
        readonly bool[] m_AnySpriteOnScanline = new bool[k_ScreenHeight];
        int m_SecondaryOamIdx;
        int m_FineScrollX;
        int m_NextFineScrollX;
        ushort m_FetchAddr;
        bool m_NmiNextInstruction;
        bool m_Nmi;
        bool m_EvenFrame;
        byte m_AttributeValue;
        ushort m_BgAttributeShiftRegister;
        byte m_NameTableValue;
        byte m_LowBgTilePatternByte;
        byte m_HighBgTilePatternByte;
        ushort m_BgPatternShiftRegisterLow;
        ushort m_BgPatternShiftRegisterHigh;
        byte m_OamAddress;
        readonly ushort[] m_ColorAddresses = new ushort[k_ScreenHeight * k_ScreenWidth];
        readonly byte[] m_PrimaryOam = new byte[0x100];
        readonly Sprite[] m_SecondaryOam = new Sprite[k_SecondaryOamSize + 1];
        readonly Mapper m_Mapper;
        ushort m_CurrentVramAddress;
        ushort m_TemporaryVramAddress;
        readonly int[] m_Palettes = new int[0x20];
        int m_Scanline;
        int m_Dot;

        readonly int[] m_Colors =
        {
            0x606060,
            0x2080,
            0xc0,
            0x6040c0,
            0x800060,
            0xa00060,
            0xa02000,
            0x804000,
            0x604000,
            0x204000,
            0x6020,
            0x8000,
            0x4040,
            0x00,
            0x00,
            0x00,
            0xa0a0a0,
            0x60c0,
            0x40e0,
            0x8000e0,
            0xa000e0,
            0xe00080,
            0xe00000,
            0xc06000,
            0x806000,
            0x208000,
            0x8000,
            0xa060,
            0x8080,
            0x00,
            0x00,
            0x00,
            0xe0e0e0,
            0x60a0e0,
            0x8080e0,
            0xc060e0,
            0xe000e0,
            0xe060e0,
            0xe08000,
            0xe0a000,
            0xc0c000,
            0x60c000,
            0xe000,
            0x40e0c0,
            0xe0e0,
            0x00,
            0x00,
            0x00,
            0xe0e0e0,
            0xa0c0e0,
            0xc0a0e0,
            0xe0a0e0,
            0xe080e0,
            0xe0a0a0,
            0xe0c080,
            0xe0e040,
            0xe0e060,
            0xa0e040,
            0x80e060,
            0x40e0c0,
            0x80c0e0,
            0x00,
            0x00,
            0x00,
        };
    }
}