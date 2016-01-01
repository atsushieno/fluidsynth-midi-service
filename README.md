FluidsynthMidiService is an Android 6.0 MIDI Device Service for Fluidsynth.

"make" should take general care.

Hack
----

Currently it expects that there is some \*.sf2 file in the app's OBB folder
(so that soundfonts can be located via APK expansion files).
You would be able to find the directory by running `adb shell find / -name obb`
During debugging, you could adb push some sf2 to the obb directory manually.
"make obb" would create an .obb file under `FluidsynthMidiService/bin/Release`.

"make hackinstall" will try to build fluidsynth for Android and then
reruns xbuild.
