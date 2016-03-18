FluidsynthMidiService is an Android 6.0 MIDI Device Service for Fluidsynth.

FluidsynthMidiService is based on Xamarin.Android.

Trying it out
----

It is still at very early development stage, but the core fluidsynth OpenSLES support is working. The sample app around it, on the other hand, is super lame so far.

In case you want to try, here is a demo version of it. https://deploygate.com/distributions/2fc9721e74ab2b5434b675def671f7840097b580

(DeployGate is a service where we Android devs can easily publish alpha versions without slow Google Play reviews and approvals.)

Build
----

The repo consists of a couple of modules and it is complicated, but in general "make" should take care. The modules are explained later.

Hack
----

Currently it expects that there is some \*.sf2 file under /data/local/tmp/name.atsushieno.fluidsynthmidideviceservice.
"make obb" would create an .obb file under `FluidsynthMidiService/bin/Release`.

"make hackinstall" will try to build fluidsynth for Android and then
reruns xbuild.


Modules
-------

FluidsynthMidiService consists of several components, and they are
submoduled under ./external directory.

- [external/android-fluidsynth](https://github.com/atsushieno/android-fluidsynth) - 
  it is to build and set up fluidsynth for Android. It subsequently submodules:
  - [external/cerbero](https://github.com/atsushieno/cerbero) - builds fluidsynth and its dependencies. It is forked from Gstreamer project.
    - This fork adds build script for fluidsynth, and it actually pulls from [my fork of fluidsynth](https://github.com/atsushieno/fluidsynth) that adds Android/OpenSLES support. (Note that the origin does not support any audio output for Android.)
- [external/managed-midi](https://github.com/atsushieno/managed-midi) -
  it brings a set of MIDI utility API that is used to implement several examples in the Activities.
- [external/nfluidsynth](https://github.com/atsushieno/nfluidsynth) -
  it is the cross platform binding for fluidsynth API.
- [external/mugene](https://github.com/atsushieno/mugene) -
  it is a music macro compiler that makes it easy to compose and play
  MIDI based songs by offering human-friendly instruction set.


