# set ANDROID_NDK_PATH environment variable.

all:
	cd external/android-fluidsynth && make $(AF_OPTIONS) || exit 1
	rm -rf NFluidsynth.Android/Libs
	cp -R external/android-fluidsynth/libs NFluidsynth.Android/Libs

prepare:
	cd external/android-fluidsynth && make $(AF_OPTIONS) prepare || exit 1

hackinstall:
	cd external/android-fluidsynth && make $(AF_OPTIONS) || exit 1
	rm -rf NFluidsynth.Android/Libs
	cp -R external/android-fluidsynth/libs NFluidsynth.Android/Libs
	cp $(ANDROID_NDK_PATH)/prebuilt/android-arm/gdbserver/gdbserver NFluidsynth.Android/Libs/armeabi-v7a/gdbserver.so
	cp $(ANDROID_NDK_PATH)/prebuilt/android-x86/gdbserver/gdbserver NFluidsynth.Android/Libs/x86/gdbserver.so
	xbuild
	xbuild FluidsynthMidiServices/FluidsynthMidiServices.csproj /t:Install $(XBUILD_ARGS)
