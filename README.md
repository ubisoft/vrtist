# VRtist

**Disclaimer**: This project is in alpha state and actively developed. Do not use it to edit your production assets without a backup or you might break them.

## Introduction

VRtist is a Virtual Reality tool for storytelling.

Directors and artists can setup a 3D scene being immersed into the virtual world. That will allow them to have a better understanding of the 3D scene.

## Main Features

- Set dressing: import 3D objects and compose the virtual set.
- Camera: naturally move around and find the best camera angles and depth of field.
- Animation: use the record mode or key frames to create animations.
- Lighting: use gradient sky or fine-tune the lighting using lights (directional, point and cone).
- Nonlinear editing: nonlinear editing using multi-camera footages.
- Live link: Blender and VRtist scene live link.

## Supported VR Devices

For now, VRtist has only been tested with the Oculus Rift S and Oculus Quest devices.

## How to Install & Launch

Just unzip the release file. Then launch the VRtist.exe and put your headset.

## How to Build

VRtist is using Unity version: 2020.2.1f1

For now, VRtist has been tested on Windows 64bit only. Build the "Main" scene in the Unity editor:

- Platform: PC, Mac & Linux Standalone
- Target Platform: Windows
- Architecture: x86_64

### Dependencies

VRtist uses the following libraries as DLLs:

- Assimp: http://www.assimp.org
- OpenImageIO: http://www.openimageio.org

### Settings & Logs

On Windows OS, VRtist writes settings, logs and saves to the %userprofile%/AppData/LocalLow/Ubisoft/VRtist/ directory (Unity.Application.persistentDataPath).

## Asset Bank

VRtist is ditributed with a predefined set of 3D objects.
It also supports FBX files import from a specified directory (default: D:\VRtistData). This can be overriden in the advanced settings.
FBX files may be exported from Blender using the following options:

- scale: 0.01
- Y Forward
- Z Up
- Apply Unit: unchecked
- Apply Transform: checked is advised

## Live Link with Blender

It's possible to sync a Blender scene with a VRtist one via the Mixer add-on. A typical use case is to have a working Blender scene and to explore and modify it in VR with VRtist.

### Install Blender

Download Blender 2.91 or above from blender.org (either install version or portable version).

### Install Mixer

VRtist uses Mixer to get the content of a Blender scene and to synchronize it with Unity.

see: https://github.com/ubisoft/mixer

Mixer is a standard Blender addon. Install it like any other addon.

- Go to Edit > Preferences > Add-ons
- Click the install... button and search "mixer.zip"
- Activate the add-on by checking the "Collaboration Mixer" item

### How to Launch the Live Link

Open your scene in Blender. In the 3D Viewport, press "N" to open the Mixer addon. Enter the VRtist.exe path.

Launch or join a server, create or join a room and press "Launch VRtist". Put your headset and enjoy!

A Windows firewall popup may appear during the first launch, accept it.

When done, everything that you did in VRtist is in your Blender scene. You can continue to work on it and relaunch VRtist anytime you want.

## License and copyright

The original code is Copyright (C) 2021 Ubisoft.

All code of the VRtist project is under the MIT license.

All 3D models (FBX files or Unity prefabs) are licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License. To view a copy of this license, visit http://creativecommons.org/licenses/by-nc-nd/4.0/ or send a letter to Creative Commons, PO Box 1866, Mountain View, CA 94042, USA.
