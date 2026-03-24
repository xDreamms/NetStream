@echo off
setlocal

set "CMAKE_EXE=%LOCALAPPDATA%\Android\Sdk\cmake\3.22.1\bin\cmake.exe"
set "PATH=%LOCALAPPDATA%\Android\Sdk\cmake\3.22.1\bin;%PATH%"
set "NDK_PATH=%LOCALAPPDATA%\Android\Sdk\ndk\27.2.12479018"
set "TOOLCHAIN=%NDK_PATH%\build\cmake\android.toolchain.cmake"
set "SRC=C:\Users\Cumhur\Desktop\NetStream\LibTorrentSharp2"
set "LT=C:\Users\Cumhur\Desktop\libtorrent\libtorrent"
set "BOOST=C:\Libraries\boost_1_87_0"
set "OUT=C:\Users\Cumhur\Desktop\NetStream\New NetStream\NetStream\NetStream\NetStream.Android\lib"

echo === Building arm64-v8a ===
if not exist "%SRC%\build\android\arm64-v8a" mkdir "%SRC%\build\android\arm64-v8a"
"%CMAKE_EXE%" -S "%SRC%" -B "%SRC%\build\android\arm64-v8a" -DCMAKE_TOOLCHAIN_FILE="%TOOLCHAIN%" -DANDROID_ABI=arm64-v8a -DANDROID_PLATFORM=android-21 -DANDROID_STL=c++_shared -DCMAKE_BUILD_TYPE=Release -DLIBTORRENT_ROOT="%LT%" -DBOOST_ROOT="%BOOST%" -G "Ninja"
if errorlevel 1 (echo FAILED configure arm64-v8a & exit /b 1)
"%CMAKE_EXE%" --build "%SRC%\build\android\arm64-v8a" --config Release -j %NUMBER_OF_PROCESSORS%
if errorlevel 1 (echo FAILED build arm64-v8a & exit /b 1)
if not exist "%OUT%\arm64-v8a" mkdir "%OUT%\arm64-v8a"
copy /Y "%SRC%\build\android\arm64-v8a\libLibTorrentSharp2.so" "%OUT%\arm64-v8a\libLibTorrentSharp2.so"
echo arm64-v8a OK

echo === Building armeabi-v7a ===
if not exist "%SRC%\build\android\armeabi-v7a" mkdir "%SRC%\build\android\armeabi-v7a"
"%CMAKE_EXE%" -S "%SRC%" -B "%SRC%\build\android\armeabi-v7a" -DCMAKE_TOOLCHAIN_FILE="%TOOLCHAIN%" -DANDROID_ABI=armeabi-v7a -DANDROID_PLATFORM=android-21 -DANDROID_STL=c++_shared -DCMAKE_BUILD_TYPE=Release -DLIBTORRENT_ROOT="%LT%" -DBOOST_ROOT="%BOOST%" -G "Ninja"
if errorlevel 1 (echo FAILED configure armeabi-v7a & exit /b 1)
"%CMAKE_EXE%" --build "%SRC%\build\android\armeabi-v7a" --config Release -j %NUMBER_OF_PROCESSORS%
if errorlevel 1 (echo FAILED build armeabi-v7a & exit /b 1)
if not exist "%OUT%\armeabi-v7a" mkdir "%OUT%\armeabi-v7a"
copy /Y "%SRC%\build\android\armeabi-v7a\libLibTorrentSharp2.so" "%OUT%\armeabi-v7a\libLibTorrentSharp2.so"
echo armeabi-v7a OK

echo === Building x86_64 ===
if not exist "%SRC%\build\android\x86_64" mkdir "%SRC%\build\android\x86_64"
"%CMAKE_EXE%" -S "%SRC%" -B "%SRC%\build\android\x86_64" -DCMAKE_TOOLCHAIN_FILE="%TOOLCHAIN%" -DANDROID_ABI=x86_64 -DANDROID_PLATFORM=android-21 -DANDROID_STL=c++_shared -DCMAKE_BUILD_TYPE=Release -DLIBTORRENT_ROOT="%LT%" -DBOOST_ROOT="%BOOST%" -G "Ninja"
if errorlevel 1 (echo FAILED configure x86_64 & exit /b 1)
"%CMAKE_EXE%" --build "%SRC%\build\android\x86_64" --config Release -j %NUMBER_OF_PROCESSORS%
if errorlevel 1 (echo FAILED build x86_64 & exit /b 1)
if not exist "%OUT%\x86_64" mkdir "%OUT%\x86_64"
copy /Y "%SRC%\build\android\x86_64\libLibTorrentSharp2.so" "%OUT%\x86_64\libLibTorrentSharp2.so"
echo x86_64 OK

echo.
echo ============================================================
echo ALL ABIs BUILT SUCCESSFULLY!
echo ============================================================
