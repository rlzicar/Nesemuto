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

using System.IO;
using System.IO.Compression;
using System.Linq;
using Nesemuto.Mappers;

namespace Nesemuto
{
    public class Cartridge
    {
        public Mapper Mapper { get; }
        public Mirroring Mirroring { get; }
        public int PrgSize { get; }
        public int ChrSize { get; }
        public int MapperId { get; }

        const int k_FileSignature = 0x1a53454e;

        public Cartridge(string path)
        {
            var extension = Path.GetExtension(path);
            Stream stream = File.Open(path, FileMode.Open, FileAccess.Read);

            bool isZipFile = extension?.ToLower() == ".zip";
            if (isZipFile)
            {
                var zip = new ZipArchive(stream);
                var firstNesEntryInZip = zip.Entries.FirstOrDefault(i => i.Name.ToLower().EndsWith(".nes"));
                stream = firstNesEntryInZip?.Open();
                
                if (stream == null)
                {
                    throw new FileNotFoundException("No NES file found in the archive");
                }
            }

            using (stream)
            {
                using (var reader = new BinaryReader(stream))
                {
                    int signature = reader.ReadInt32();
                    if (signature != k_FileSignature)
                    {
                        throw new InvalidDataException("Not a valid NES file");
                    }

                    PrgSize = reader.ReadByte() * 0x4000;
                    ChrSize = reader.ReadByte() * 0x2000;

                    var flags6 = reader.ReadByte();
                    var flags7 = reader.ReadByte();

                    Mirroring = (flags6 & 1) == 0 ? Mirroring.Horizontal : Mirroring.Vertical;
                    MapperId = (flags6 >> 4) | (flags7 & 0xf0);

                    const int unusedHeaderByteCount = 8;
                    var unused = reader.ReadBytes(unusedHeaderByteCount);
                    
                    var prgRom = reader.ReadBytes(PrgSize);
                    var chrRom = reader.ReadBytes(ChrSize);

                    switch (MapperId)
                    {
                        case 0:
                            Mapper = new Mapper000(prgRom, chrRom, Mirroring);
                            break;
                        case 1:
                            Mapper = new Mapper001(prgRom, chrRom, Mirroring);
                            break;
                        case 2:
                            Mapper = new Mapper002(prgRom, chrRom, Mirroring);
                            break;
                        case 3:
                            Mapper = new Mapper003(prgRom, chrRom, Mirroring);
                            break;
                        case 4:
                            Mapper = new Mapper004(prgRom, chrRom, Mirroring);
                            break;
                        case 7:
                            Mapper = new Mapper007(prgRom, chrRom);
                            break;
                        case 66:
                            Mapper = new Mapper066(prgRom, chrRom, Mirroring);
                            break;
                    }
                }
            }
        }
    }
}