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

using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using System;
using System.Runtime.InteropServices;

namespace Nesemuto
{
    public class Apu
    {
        public Apu(Mapper mapper)
        {
            if (s_Instance != null)
            {
                // can't create more than one instance of this class due to the usage of
                // the unmanaged APU library
                throw new InvalidOperationException();
            }

            m_Mapper = mapper;
            s_Instance = this;

            // this is necessary in order to extend the delegate's lifetime until it's certain that
            // no more calls from the unmanaged APU library code will occur
            s_ReadMemFunc = DmcRead;

            ApuInit(s_ReadMemFunc, k_SampleRate, k_NtscClockRate, k_OutputSize);

            m_AudioContext = new AudioContext();
            m_BufferIds = AL.GenBuffers(k_NumBuffers);
            m_SourceId = AL.GenSource();
            foreach (var bufferId in m_BufferIds)
            {
                AL.BufferData(bufferId, ALFormat.Mono16, m_Samples, 2, k_SampleRate);
            }

            AL.SourceQueueBuffers(m_SourceId, m_BufferIds.Length, m_BufferIds);
            AL.SourceUnqueueBuffers(m_SourceId, m_BufferIds.Length, m_BufferIds);
            AL.SourcePlay(m_SourceId);
        }

        public void Reset()
        {
            ApuReset();
        }

        int DmcRead(int addr)
        {
            return m_Mapper.Read((ushort) addr);
        }

        public byte ReadStatus(int time)
        {
            return (byte) ApuRead(time);
        }

        public void WriteRegister(int time, int addr, byte value)
        {
            ApuWrite(time, addr, value);
        }

        void TryQueueData(byte[] samples, int count)
        {
            AL.GetSource(m_SourceId, ALGetSourcei.BuffersProcessed, out var availableBufferCount);
            if (availableBufferCount == 0)
            {
                return;
            }

            AL.SourceUnqueueBuffers(m_SourceId, 1, s_UnqueuedBufferIds);
            AL.BufferData(s_UnqueuedBufferIds[0], ALFormat.Mono16, samples, count * sizeof(short), k_SampleRate);
            AL.SourceQueueBuffers(m_SourceId, 1, s_UnqueuedBufferIds);
        }

        public bool Enabled { set; get; } = true;

        public void Run(int endTime)
        {
            int pendingSampleCount = ApuRun(endTime, m_Samples);
            if (!Enabled)
            {
                return;
            }

            // GC.Collect(); uncomment to test the delegate called from the unmanaged code doesn't get GC'ed

            if (pendingSampleCount > 0)
            {
                TryQueueData(m_Samples, pendingSampleCount);
            }

            bool audioPlaying = AL.GetSourceState(m_SourceId) == ALSourceState.Playing;
            if (!audioPlaying)
            {
                AL.SourcePlay(m_SourceId);
            }
        }


        public void Dispose()
        {
            AL.SourceStop(m_SourceId);
            AL.DeleteSource(m_SourceId);
            AL.DeleteBuffers(m_BufferIds);
            m_AudioContext.Dispose();
            s_Instance = null;
        }

        delegate int ReadMemoryFunc(int addr);

#if NO_APU
        void ApuInit(ReadMemoryFunc func, int sampleRate, int clockRate, int outputSize)
        {
        }

        int ApuRun(int endTime, byte[] output)
        {
            return 0;
        }

        void ApuWrite(int time, int addr, int value)
        {
        }

        int ApuRead(int time)
        {
            return 0;
        }

        void ApuReset()
        {
        }
#else
        [DllImport(k_DllName)]
        static extern void ApuInit(ReadMemoryFunc func, int sampleRate, int clockRate, int outputSize);

        [DllImport(k_DllName)]
        static extern int ApuRun(int endTime, byte[] output);

        [DllImport(k_DllName)]
        static extern void ApuWrite(int time, int addr, int value);

        [DllImport(k_DllName)]
        static extern int ApuRead(int time);

        [DllImport(k_DllName)]
        static extern void ApuReset();

        const string k_DllName = "Nes_Snd.dll";
#endif

        const int k_SampleRate = 44100;
        const int k_OutputSize = k_SampleRate / 60 * sizeof(short);
        const int k_NumBuffers = 4;
        const int k_NtscClockRate = 1789773;
        
        static ReadMemoryFunc s_ReadMemFunc;
        static Apu s_Instance;

        static readonly int[] s_UnqueuedBufferIds = new int[1];
        readonly byte[] m_Samples = new byte[k_OutputSize * sizeof(short)];
        readonly int m_SourceId;
        readonly AudioContext m_AudioContext;
        readonly int[] m_BufferIds;
        readonly Mapper m_Mapper;
    }
}