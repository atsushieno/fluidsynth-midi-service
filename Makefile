# You have to set ANDROID_NDK_PATH environment variable.

SIGNING_KEY_ALIAS = googleplay
SIGNING_KEY_STORE = ~/my-google-play.keystore

all: build-native msbuild
	# cd external/android-fluidsynth && make $(AF_OPTIONS) || exit 1
	# rm -rf NFluidsynth.Android/Libs
	# cp -R external/android-fluidsynth/libs NFluidsynth.Android/Libs

prepare: prepare-fluidsynth

msbuild:
	msbuild

prepare-fluidsynth:
	cd external/fluidsynth/doc/android && make -f Makefile.android prepare || exit 1

build-native: build-fluidsynth

build-fluidsynth:
	cd external/fluidsynth/doc/android && make -f Makefile.android && make -f Makefile.android dist || exit 1
	rm -rf NFluidsynth.Android/Libs
	mkdir -p NFluidsynth.Android/Libs
	cp -R external/fluidsynth/doc/android/dist/* NFluidsynth.Android/Libs

prepare-gdbserver:
	cp $(ANDROID_NDK_PATH)/prebuilt/android-arm/gdbserver/gdbserver NFluidsynth.Android/Libs/armeabi-v7a/gdbserver.so
	cp $(ANDROID_NDK_PATH)/prebuilt/android-x86/gdbserver/gdbserver NFluidsynth.Android/Libs/x86/gdbserver.so

hackinstall: build-native
	msbuild
	msbuild FluidsynthMidiServices/FluidsynthMidiServices.csproj /t:Install $(XBUILD_ARGS)

releaseinstall:
	make XBUILD_ARGS=/p:Configuration=Release hackinstall

# I hate to specify my password in some scripts, so I manually enter that...
signrelease: 
	jarsigner -verbose -sigalg SHA1withRSA -digestalg SHA1 -keystore $(SIGNING_KEY_STORE) FluidsynthMidiServices/bin/Release/name.atsushieno.fluidsynthmidideviceservice.apk $(SIGNING_KEY_ALIAS)
	rm -f FluidsynthMidiServices/bin/Release/name.atsushieno.fluidsynthmidideviceservice-Signed.apk
	zipalign -v 4 FluidsynthMidiServices/bin/Release/name.atsushieno.fluidsynthmidideviceservice.apk FluidsynthMidiServices/bin/Release/name.atsushieno.fluidsynthmidideviceservice-Signed.apk

# obsolete
obb:
	# runnable only under Ubuntu + sf2 (fluid-soundfont-gm etc.) installed
	jobb -v -d /usr/share/sounds/sf2 -o FluidsynthMidiServices/bin/Release/sf2.obb -pn name.atsushieno.fluidsynthmidideviceservice -pv 0
