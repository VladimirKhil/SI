SIGame projects set

.NET 6 SDK is required to compile the solutions. Visual Studio 2022 is required to publish SImulator ClickOnce application.

WiX Toolset is required to build app installers.

# Solutions

* *SImulator* - SIGame offline app (allows to host a game with a single computer and projector)
* *SIQuester* - SIGame questions editor
* *SIGame* - SIGame desktop app

# Projects

* *src/Common/AppService.Client* - allows to search for the app updates and report app usage and errors;
* *src/Common/Notions* - provides helper methods for working with strings;
* *src/Common/SI.GameServer.Client* - provides a client for SIGame server;
* *src/Common/SI.GameServer.Contract* - defines a SIGame server contract;
* *src/Common/SIEngine* - provides a low-level SIGame engine, which loads question package and generates game events;
* *src/Common/SIPackages* - provides a question package model;
* *src/Common/SIStorageService.Client* - provides a client for SIGame question storage;
* *src/Common/SIUI* - provides a SIGame table UI;
* *src/Common/Utils* - provides auxilliary classes;

* *src/SIQuester/QTxtConverter* - supports text files to question packages conversion;

* *src/SICore/SICore* - provides a high-level SIGame engine;
* *src/SICore/SICore.Connections* - provides base classes for building a SI Nodes network;
* *src/SICore/SICore.Network* - provides high-level classes for building a SI Nodes network;
* *src/SICore/SIData* - contains common SIGame models for client and server.

# Build

* `dotnet build src\SIGame\SIGame\SIGame.csproj`
* `dotnet build src\SIQuester\SIQuester\SIQuester.csproj`
* `dotnet build src\SImulator\SImulator\SImulator.csproj`

# Scripts

* *sigame-build.ps1* - builds a SIGame project in Release configuration and sets provided version number;
* *sigame-build-msi.ps1* - builds a SIGame msi for provided platform;
* *sigame-build-setup.ps1* - builds a SIGame installer;
* *sigame-publish.ps1* - publishes a SIGame and sets provided version number;

* *siquester-build.ps1* - builds a SIQuester project in Release configuration;
* *siquester-build-msi.ps1* - builds a SIQuester msi for win-x86 and win-x64;
* *siquester-publish.ps1* - publishes a SIQuester;

* *simulator-build.ps1* - builds a SImulator project in Release configuration;
* *simulator-publish.ps1* - publishes a SImulator project in Release configuration for win-x86 architecture as a compressed single file;