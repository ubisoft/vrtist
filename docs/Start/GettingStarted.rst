.. _Getting Started:

===============
Getting Started
===============


Download project
----------------

Open the `latest release page <https://github.com/ubisoft/vrtist>`_  from the Mixer Gihub `releases page <https://github.com/ubisoft/vrtist/releases>`_. 

Install & Launch
----------------

Just unzip the release file. Then launch the VRtist.exe and put your headset.

How to build
------------

VRtist is using Unity version: 2020.2.1f1

For now, VRtist has been tested on Windows 64bit only. Build the "Main" scene in the Unity editor:

    * Platform: PC, Mac & Linux Standalone
    * Target Platform: Windows
    * Architecture: x86_64

Dependencies
^^^^^^^^^^^^

VRtist uses the following libraries as DLLs:

    * Assimp: http://www.assimp.org
    * OpenImageIO: http://www.openimageio.org

Settings & Logs
^^^^^^^^^^^^^^^

On Windows OS, VRtist writes settings, logs and saves to the %userprofile%/AppData/LocalLow/Ubisoft/VRtist/ directory (Unity.Application.persistentDataPath).

Asset Bank
----------

VRtist is ditributed with a predefined set of 3D objects. It also supports FBX files import from a specified directory (default: D:\VRtistData). This can be overriden in the advanced settings. FBX files may be exported from Blender using the following options:

    * scale: 0.01
    * Y Forward
    * Z Up
    * Apply Unit: unchecked
    * Apply Transform: checked is advised
