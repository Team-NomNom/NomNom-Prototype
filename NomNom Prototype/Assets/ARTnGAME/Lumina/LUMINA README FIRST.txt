LUMINA: Voxel Based Global Illumination

LUMINA: Voxel based Real time Global Illumination is a new sysem for true voxel based global illumination that will locally remain 100% stable, unlike screen space based solutions that flicker and have GI dissapear when the camera moves or rotates and looking away from the light receiving objects.

The main manual is included in the same forlder as this README file and contains the latest updated manual for the system.

LUMINA has a number of demos with assets that are not directly included in the store download for making downloading easier.
The demo assets are freely avilable on the web.

To properly see demos with all objects (house items, statues, sponza atrium) please downlone the assets from the following links

Sponza HDRP CC0 (Converted to URP from https://github.com/radishface/Sponza) and Home assets collection CC0 license (Currated from https://polyhaven.com/models and https://opengameart.org)
https://drive.google.com/file/d/1IERCKONJ21e2C5qOh6ICy2herYitUCS0/view?usp=sharing

Danish Statues from Keijiro
https://github.com/keijiro/DanishStatues

For direct chat and help on any issue, please contact me via  ARTnGAME Discord Channel (https://discord.gg/X6fX6J5) 
or the ARTnGAME support E-mail (artengames@gmail.com)


LUMINA v1.2c NOTES
- In this version two Temporal AA methods have been added to reduce noise, the default enabled is the "Temporal AA Feature"
directly enabled on the sample forward renderer of LUMINA sample pipeline. The module can be fine tuned directly on the 
renderer. Make sure the jitter spead is low enough so the small camera movement needed for the temporal AA is not noticable.

LUMINA v1.4a NOTES
- In this version two new extra modes are available, one is a high contrast mode when put the Contrast variable to 20 (default is zero for normal operation)
- The second mode is the "Approximation Mode" in Advanced Settings, this mode approximates the Voxelization stage that uses Geometry Shaders with the Vertex Shader
which means is compatible with hardware that may not support Geometry Shaders. This is an experimental mode and not as accurate as the Geometry Shaders one, but
can be useful in platforms that not support Geometry Shaders or for artistic effects as the solution give different lighting distribution than the main mode.