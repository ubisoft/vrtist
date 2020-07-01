# VRtist

VR scene exploration and set dressing.

## How to install

### Install VRtist

- Copy the VRtist folder in any location you want (Here we will use the path d:/vrtist)
- Add environment variables to your system

  - VRTIST_HOST = localhost
  - VRTIST_ROOM = Local
  - VRTIST_EXE = d:/vrtist/VRtist.exe

  These environment variables are used by Mixer to launch VRtist from Blender.

Right now VRtist can't load or save any scene as a standalone software so the use of Blender & Mixer are required.

### Install Blender

Download Blender 2.83 from blender.org (either install version or portable version).

### Install Mixer

VRtist uses Mixer to get the content of a Blender scene and to synchronize it with Unity.

Mixer is a standard Blender addon. Install it like any other addon.

- Go to Edit > Preferences > Add-ons
- Click the install... button and search "mixer.zip"
- Activate the add-on by checking the "Collaboration Mixer" item

## How to launch

Open your scene in Blender. In 3D Viewport, press "N" to open the Mixer addon and press "Launch VRtist". Put your headset and enjoy!

A Windows firewall popup may appear during the first launch, accept it.

When done, everything that you did in VRtist is in your Blender scene. You can continue to work on it and relaunch VRtist anytime you want.
