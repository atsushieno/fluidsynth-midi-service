FluidsynthMidiService is an Android 6.0 MIDI Device Service for Fluidsynth.

"make" should take general care.

Hack
----

Currently it expects that there is some \*.sf2 file in the app's OBB folder
(so that soundfonts can be located via APK expansion files).
You might be able to find the directory by running `adb shell find / -name obb`
During debugging, you could adb push some sf2 to the obb directory manually.
"make obb" would create an .obb file under `FluidsynthMidiService/bin/Release`.

"make hackinstall" will try to build fluidsynth for Android and then
reruns xbuild.


Modules
-------

FluidsynthMidiService consists of several components, and they are
submoduled under ./external directory.

- [external/android-fluidsynth](https://github.com/atsushieno/android-fluidsynth) - 
  it is to build and set up fluidsynth for Android. It subsequently submodules:
  - [external/cerbero](https://github.com/atsushieno/cerbero) - builds fluidsynth and its dependencies. It is taken from Gstreamer project.
- [external/managed-midi](https://github.com/atsushieno/managed-midi) -
  it brings a set of MIDI utility API that is used to implement several examples in the Activities.
- [external/nfluidsynth](https://github.com/atsushieno/nfluidsynth) -
  it is the cross platform binding for fluidsynth API.
- [external/mugene](https://github.com/atsushieno/mugene) -
  it is a music macro compiler that makes it easy to compose and play
  MIDI based songs by offering human-friendly instruction set.

