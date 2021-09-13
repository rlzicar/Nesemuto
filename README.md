# Nesemuto  
Nesemuto is an NES emulator written in C# (with an optional dependency on a C++ library for sound emulation).

I'm aiming to strike a balance between performance and cache-friendliness on the one hand and decent code readability and maintainability on the other.

![](https://i.imgur.com/dY88qS6m.png) ![](https://i.imgur.com/dLz9uRzm.png) ![](https://i.imgur.com/URow4P3m.png) ![](https://i.imgur.com/fx7gDLCm.png) ![](https://i.imgur.com/zvc7tvmm.png) ![](https://i.imgur.com/YBlpIr1m.png)
![](https://i.imgur.com/JviNxlbm.png) ![](https://i.imgur.com/jex5oEsm.png)
  
## Compatibility  
Only the NTSC video system is supported (USA/Japan). PAL games aren't guaranteed to work. I haven't implemented the APU IRQs, so games that rely on the APU for timing purposes won't work properly.  
  
The following mappers have been implemented:  
* NROM (000)  
* MMC1 (001)  
* UxROM (002)  
* CNROM (003)  
* MMC3 (004)  
* AxROM (007)  
* GxROM (066) 
* Mapper 225 
  
All the official CPU opcodes and some of the unofficial ones are implemented.  
  
## Building (Windows)
Open the solution in Visual Studio or Rider and build the Nesemuto project in Release mode.  
  
The Nes_Snd project, on which Nesemuto depends, is written in C++, so you'll need to have Visual C++ installed on your machine in order to build the solution. Alternatively, you can remove Nes_Snd from the build configuration and turn off the sound emulation by uncommenting  
```#define NO_APU``` in Apu.cs. That way, you'll be able to build the emulator without the C++ dependency.  
  
## Running  (Windows)
OpenAL is required for the emulator to work: https://www.openal.org/downloads/oalinst.zip

To run a game, simply pass the path to the .nes file as a command line argument:
  
```nesemuto "d:/games/mario.nes"```  

ZIP files are supported, too:

```nesemuto "d:/games/mario.zip"```  
  
You can also pass the path to a cheat file as a second argument  
  
```nesemuto "d:/games/mario.nes" "d:/games/mario.cht"```  
  
  
## Controls  
#### Default key bindings  (can be changed in ***controls.cfg***)
  
Key | NES button
--------------|-------------  
Enter | Start
Space | Select
S | A
A | B  
Up Arrow | Up
Down Arrow | Down
Left Arrow | Left
Right Arrow | Right

#### Other controls
Key | Action
------|------
Ctrl + R | reset
Ctrl + M | toggle audio
Ctrl + C | toggle cheats
F1, F2, F3 | change window size
F11 | toggle fullscreen
Ctrl + F4  | exit

## Cheat File Format  
The emulator supports raw cheats (with or without a compare value) and Game Genie cheats.
The file format looks like this:
```
// S:addr(hex):value(hex):description
S:0032:09:Player1 Infinite Lives  

// SC:addr(hex):value(hex):compareValue(hex):description
SC:dad3:2c:95:Keep Gun After Dying  

// GG:gameGenieCheat:description
GG:PEETLIAA:Start at area 2      
GG:SLAIUZ:Start with infinite lives

// Lines starting with anything other than 'S:', 'SC:' or 'GG:' are considered comments
```

## Dependencies  
Nesemuto uses OpenTK (a set of low-level C# bindings for OpenGL and OpenAL) for rendering and audio output and Shay Green's Nes_Snd_Emu library for the sound chip emulation.  
  
<https://opentk.net/>  (MIT)
  
<http://blargg.8bitalley.com/libs/audio.html#Nes_Snd_Emu>  (LGPL)
  
  
## License  
Nesemuto is free software released under the terms of the MIT license.

## References
NES programming tutorials, hardware reference, and other resources:
[http://wiki.nesdev.com/w/index.php/NES_reference_guide](http://wiki.nesdev.com/w/index.php/NES_reference_guide)
