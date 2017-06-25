Installation on Win10:
1. install "vcredist_x86.exe" from KINOVA Windows SDK
2. In Win10, turn off signing verification by executing from command line with Admin privilages:
	Right click on command line and select "run as administrator"
3. "bcdedit /set testsigning on"
4. "jaco2Install32_1.1.0.exe"
5. "bcdedit /set testsigning off"

Base position execution:
1. Open WindowsExample_AngularControl.sln in VS 2015 (x86, x64 removed as dlls are x86):
2. Build (dlls are automatically moved to build directory as they are necessary to be in same location)
3. Execute - arm moves to base position

debug directory does not have to be pushed into github...