# Uno.VersionChecker

This is a tool to extract the version of _dotnet assemblies_ used on a Uno.UI application. Should also work with almost any application built on _Uno Bootstrapper_.

## Usage

Start the executable using the URI of your Uno application.

``` shell
> Uno.VersionChecker https://nuget.info/
```

You should see the result as

```
Checking website at address nuget.info
Identifiying the Uno.UI application
Application found, configuration at url https://nuget.info/package_f70b285907b7e95c22b73b365da9ce59bf07069f/uno-config.js
Starting assembly is PackageExplorer
120 assemblies found. Reading metadata...
Name                                               Version                                                                                                                File Version     Framework
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
AuthenticodeExaminer                               1.0.0                                                                                                                  1.0.0.0          .NETStandard,Version=v2.0
ColorCode.Core                                     2.0.8+gd42a883502                                                                                                      2.0.8.37         .NETStandard,Version=v2.0
ColorCode.UWP                                      2.0.8+gd42a883502                                                                                                      2.0.8.37         .NETStandard,Version=v2.0
CommonServiceLocator                               2.0.5                                                                                                                  2.0.5.0          .NETCoreApp,Version=v3.0
[... many lines ...]
Uno                                                3.8.0-dev.457+Branch.master.Sha.5c6bd5fcd9ad846448db6c0f6029d5a331630c46.5c6bd5fcd9ad846448db6c0f6029d5a331630c46      255.255.255.255  .NETStandard,Version=v2.0
Uno.Core                                           2.4.0-dev.2+Branch.master.Sha.f3652f8713d699a7f7452550173f5c2ef3fd2080.f3652f8713d699a7f7452550173f5c2ef3fd2080        1.0.0.0          .NETCoreApp,Version=v5.0
Uno.Diagnostics.Eventing                           1.0.4+Branch.release-stable-1.0.Sha.1ca00890632a6524587ce0092f613861192c7567.1ca00890632a6524587ce0092f613861192c7567  255.255.255.255  .NETStandard,Version=v2.0
Uno.Foundation                                     3.8.0-dev.457+Branch.master.Sha.5c6bd5fcd9ad846448db6c0f6029d5a331630c46.5c6bd5fcd9ad846448db6c0f6029d5a331630c46      255.255.255.255  .NETStandard,Version=v2.0
Uno.Foundation.Runtime.WebAssembly                 3.8.0-dev.457+Branch.master.Sha.5c6bd5fcd9ad846448db6c0f6029d5a331630c46.5c6bd5fcd9ad846448db6c0f6029d5a331630c46      255.255.255.255  .NETStandard,Version=v2.0
Uno.UI                                             3.8.0-dev.457+Branch.master.Sha.5c6bd5fcd9ad846448db6c0f6029d5a331630c46.5c6bd5fcd9ad846448db6c0f6029d5a331630c46      255.255.255.255  .NETStandard,Version=v2.0
Uno.UI.FluentTheme                                 3.8.0-dev.457+Branch.master.Sha.5c6bd5fcd9ad846448db6c0f6029d5a331630c46.5c6bd5fcd9ad846448db6c0f6029d5a331630c46      255.255.255.255  .NETStandard,Version=v2.0
Uno.UI.Runtime.WebAssembly                         3.8.0-dev.457+Branch.master.Sha.5c6bd5fcd9ad846448db6c0f6029d5a331630c46.5c6bd5fcd9ad846448db6c0f6029d5a331630c46      255.255.255.255  .NETStandard,Version=v2.0
Uno.UI.Toolkit                                     3.8.0-dev.457+Branch.master.Sha.5c6bd5fcd9ad846448db6c0f6029d5a331630c46.5c6bd5fcd9ad846448db6c0f6029d5a331630c46      255.255.255.255  .NETStandard,Version=v2.0
Uno.Wasm.TimezoneData                              3.0.0-dev.87+Branch.main.Sha.9b385b45c6a8fca078496fb104747d3eba2814d8.9b385b45c6a8fca078496fb104747d3eba2814d8         1.0.0.0          .NETStandard,Version=v2.0
Uno.Xaml                                           3.8.0-dev.457+Branch.master.Sha.5c6bd5fcd9ad846448db6c0f6029d5a331630c46.5c6bd5fcd9ad846448db6c0f6029d5a331630c46      255.255.255.255  .NETStandard,Version=v2.0

Uno.UI version is 3.8.0-dev.457+Branch.master.Sha.5c6bd5fcd9ad846448db6c0f6029d5a331630c46.5c6bd5fcd9ad846448db6c0f6029d5a331630c46
```

