# BoneLib (prebuilt DLL)

Upstream release: https://github.com/yowchap/BoneLib/releases/tag/v3.2.1  
Version: `v3.2.1`  
License: GPL-3.0 (see BoneLib source)

Helper library for BONELAB MelonLoader / LemonLoader mods.
Put `BoneLib.dll` in the game `Mods` folder when running; reference it when compiling your mod.

## Game assemblies (SLZ / Il2Cpp)

BONELAB game reference DLLs (`Il2Cpp*.dll`, `Assembly-CSharp.dll` proxies, etc.) are **not** redistributed here — they are generated from your own game install:

1. Install MelonLoader / LemonLoader on BONELAB
2. Launch the game once
3. Assemblies appear under MelonLoader managed / Il2CppAssemblies for that install
4. Point `BONELAB_DIR` at your game folder (BoneLib project uses this)

Source for BoneLib: `../BoneLib/`
