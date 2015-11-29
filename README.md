FluidsynthMidiService is an Android 6.0 MIDI Device Service for Fluidsynth.

"make" should take general care.

Hack
----

Currently it expects that "/sdcard/tmp/FluidR3\_GM.sf2" on the Android
target (device or emulator) exists and tries to load it.

"make hackinstall" will try to build fluidsynth for Android and then
reruns xbuild.
