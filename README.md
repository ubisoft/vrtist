# VRtist

_TODO: description_

## How to install

### Install Mixer

VRtist uses Mixer to get the content of a Blender scene and to synchronize it with Unity.

Mixer is a standard Blender addon. Install it like any other addon. _TODO: complete this section_

Right now VRtist can't load or save any scene as a standalone sofware so the use of Blender & Mixer are required.

### Install VRtist

- Copy the VRtist folder in any location you want (Here we will use the path d:/vrtist)
- Add environment variables to your system

  - VRTIST_HOST = localhost
  - VRTIST_ROOM = Local
  - VRTIST_EXE = d:/vrtist

  These environment variables are used by Mixer to launch VRtist from Blender.

## How to launch

Open your scene in Blender. Then open the Mixer addon and press "Launch VRtist". Put your headset and enjoy!

At the first launch a Windows firewall popup may appear, press "Ok".

When done, everything that you did in VRtist is in your Blender scene. You can continue to work on it and relaunch VRtist anytime you want.
