UnityCapture Virtual Camera
=============================

This folder is used to store the UnityCapture driver DLLs for Windows 10 support.

Automatic Setup:
----------------
Run "install-virtualcam.bat" (as Administrator) in the parent directory.
It will automatically download the necessary drivers from GitHub.

Manual Setup:
-------------
1. Download source from: https://github.com/schellingb/UnityCapture/archive/refs/heads/master.zip
2. Extract the zip file
3. Copy "UnityCapture-master/Install/UnityCaptureFilter64.dll" to this folder
4. Copy "UnityCapture-master/Install/UnityCaptureFilter32.dll" to this folder (optional)
5. Run "install-virtualcam.bat" again to register them.