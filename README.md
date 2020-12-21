## Git branch usages

`master` and `develop` branches are read only.  
On developing new features or fixing bugs, checkout newest `develop` branch as `<yourname>/<featurename>` and develop on it.  
After developing it, push into `origin/<yourname>/<featurename>` and create a pull request. Person in charge will merge it.

## How to build a development environment

### Development Environments

1. Install Revit 2021 (`C:\Program Files\Autodesk\Revit 2021` directory is recommended).
1. Install Visual Studio 2019.
1. Clone `develop` branch into your machine.
1. Open `ArchitectureRouting.sln` and set `ArchitectureRouting` project's debugger startup path as Revit.exe location (`C:\Program Files\Autodesk\Revit 2021\Revit.exe`).

### Language and framework versions

- **C# 9.0**  
	**Nullable reference types** is enabled.
- **.NET Framework 4.8**  
	Compatible to **Revit 2021**.

### Projects in solution

- **ArchitectureRouting.csproj**  
	Entry point of addin. Revit command classes and application classes are to be implemented in this project.  
	`*.addin` file is automatically built by `make_addin` command when `Arent3d.Revit.RevitAddinAttribute` is specified.  
- **Arent3dCommon.csproj**  
	Common utility classes and extension methods.
- **make_addin.csproj**  
	Generates `*.addin` files from assemblies with `Arent3d.Revit.RevitAddinVendorAttribute` attribute.  
	`make_addin` command surveys assemblies, collect classes with `Arent3d.Revit.RevitAddinAttribute`, and build `*.addin` file.
- **RevitAddinUtil.csproj**  
	Defines `Arent3d.Revit.RevitAddinAttribute` and `Arent3d.Revit.RevitAddinAttribute`.

## Others

### Changing addin directory

`ArchitectureRouting.csproj` copies `*.addin` into machine's `%ProgramData%\Autodesk\Revit\Addins\2021` directory. But this destination is customized by environment variables.

When oher Revit versions is on your computer, define `REVIT_VERSION` environment variable (for example `SET REVIT_VERSION=2019`).  

Also, `REVIT_ADDIN_PATH` environment variable is available. If `REVIT_ADDIN_PATH` is defined, `REVIT_ADDIN_PATH` totally overrides destination.

Here are commands that are executed after building `ArchitectureRouting.csproj`:

```
"$(SolutionDir)make_addin\$(OutDir)\make_addin" "$(TargetPath)"

if defined REVIT_ADDIN_PATH (
  copy "$(TargetDir)*.addin" "%REVIT_ADDIN_PATH%\"
) else (
  if not defined REVIT_VERSION (
    copy "$(TargetDir)*.addin" "%ProgramData%\Autodesk\Revit\Addins\2021\"
  ) else (
    copy "$(TargetDir)*.addin" "%ProgramData%\Autodesk\Revit\Addins\%REVIT_VERSION%\"
  )
)
```
