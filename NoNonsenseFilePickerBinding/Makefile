
ZIPFILE = NoNonsense-FilePickerBinaries.zip

all: build

build:
	xbuild

update_aar: build_aar
	cp ./NoNonsense-FilePicker/library/build/outputs/aar/library-debug.aar Jars

build_aar: NoNonsense-FilePicker
	cd NoNonsense-FilePicker/library && ../gradlew assembleDebug

NoNonsense-FilePicker:
	git clone https://github.com/spacecowboy/NoNonsense-FilePicker.git
