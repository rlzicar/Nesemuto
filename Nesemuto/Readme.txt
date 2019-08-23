USAGE
-----
nesemuto pathToGameFile [pathToCheatFile]


CONTROLS
--------

Ctrl + R - reset
Ctrl + M - toggle audio
Ctrl + C - toggle cheats
F1, F2, F3 - change window size
F11 - toggle fullscreen mode
Ctrl + F4 - exit


DEFAULT KEY MAPPINGS
--------------------
    
S: A
A: B
Space: Select
Enter: Start
Left Arrow: Left
Right Arrow: Right
Up Arrow: Up
Down Arrow: Down
(can be changed in "controls.cfg")


CHEAT FILE FORMAT
-----------------

The file format is similar to the FCEUX CHT format.

S:addr(hex):value(hex):description
SC:addr(hex):value(hex):compareValue(hex):description
GG:cheat:description

"S:" denotes a raw read-substitute-style cheat
"SC:" denotes a raw cheat with a compare value
"GG:" denotes a Game Genie cheat

Lines that begin with anything other than S: or SC: or GG: are considered comments and ignored.

Example:
S:0032:09:Player1 Infinite Lives
SC:dad3:2c:95:Keep Gun After Dying
GG:PEETLIAA:Start at area 2	
GG:SLAIUZ:Start with infinite lives