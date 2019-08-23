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

#include <stdio.h>
#include "stdafx.h"
#include "nes_apu/Nes_Apu.h"
#include "nes_apu/Blip_Buffer.h"

extern "C"
{

	Nes_Apu apu;
	Blip_Buffer buffer;
	int outputSize;

	typedef int(__stdcall *callback)(cpu_addr_t);
	callback readMemFunc;

	void reset() {
		apu.reset();
		buffer.clear();
	}


	int dmc_read(void *, cpu_addr_t addr) {
		return readMemFunc(addr);
	}


	__declspec(dllexport) void __stdcall ApuInit(callback readMem, int sampleRate, int clockRate, int audioOutputSize) {
		readMemFunc = readMem;
		apu.output(&buffer);
		apu.dmc_reader(dmc_read);
		buffer.sample_rate(sampleRate);
		buffer.clock_rate(clockRate);
		outputSize = audioOutputSize;

		reset();
	}

	__declspec(dllexport) void  __stdcall ApuReset() {
		reset();
	}

	__declspec(dllexport) void __stdcall ApuWrite(cpu_time_t time, cpu_addr_t addr, int value) {
		apu.write_register(time, addr, value);
	}

	__declspec(dllexport) int __stdcall ApuRead(cpu_time_t time) {
		int res = apu.read_status(time);
		return res;
	}


	__declspec(dllexport) int  __stdcall ApuRun(cpu_time_t endTime, blip_sample_t *output) {
		apu.end_frame(endTime);
		buffer.end_frame(endTime);
		return buffer.read_samples(output, outputSize);
	}
}
