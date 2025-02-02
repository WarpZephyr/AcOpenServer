# AcOpenServer
A project aimed at recreating the 5th generation Armored Core servers.  

# Progress
Currently still working on the base server code and getting past the authentication step.

# Building
First build the native libraries:  
1. Go to the native/tools folder  
2. Run native_generate_vs2022.bat  
3. Go back to the native folder  
4. Go to native/generated/vs2022 and open native.sln  
5. Build in Visual Studio 2022  

In the native folder a bin folder will be generated, managed wrapper projects point at binaries built in here.  
Finally build the AcOpenServer.sln in VS 2022.

# Credits
Massive thanks to [ds3os](https://github.com/TLeonardUK/ds3os), it's research website [timleonard.uk](https://timleonard.uk), and related authors;  
Much of the code is based on concepts presented by [ds3os](https://github.com/TLeonardUK/ds3os), and the native libraries around cryptography are mostly directly borrowed from it.