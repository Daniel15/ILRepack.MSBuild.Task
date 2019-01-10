﻿ILRepack.MSBuild.Task
=====================

MSBuild task for [ILRepack](https://github.com/gluck/il-repack) which is an open-source alternative to ILMerge.

Install via NuGet
=================
	Install-Package ILRepack.MSBuild.Task

Supported build tools
=====================
* MSBuild

Usage - MSBuild
============
```
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>netcoreapp2.2</TargetFramework>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="ILRepack.MSBuild.Task" />
    </ItemGroup>

    <Target Name="ILRepackThisAssembly" AfterTargets="Build">

        <ItemGroup>
            <InputAssemblies Include="$(OutputPath)\ExampleAssemblyToMerge1.dll" />
            <InputAssemblies Include="$(OutputPath)\ExampleAssemblyToMerge2.dll" />
            <InputAssemblies Include="$(OutputPath)\ExampleAssemblyToMerge3.dll" />
        </ItemGroup>

        <ItemGroup>
            <!-- Must use fully qualified assembly name -->
            <DoNotInternalizeAssemblies Include="ExampleAssemblyToMerge3" />
        </ItemGroup>

        <ILRepack
            Parallel="true"
            Internalize="true"
            InternalizeExclude="@(DoNotInternalizeAssemblies)"
            InputAssemblies="@(InputAssemblies)"
            TargetKind="Dll"
            OutputFile="$(OutputPath)\$(AssemblyName).dll" />

    </Target>

</Project>
```

Task options
=======================

<table border="0" cellpadding="3" cellspacing="0" width="90%" id="tasksTable">
    <tr>
        <th align="left" width="190">
            Option
        </th>
        <th align="left">
            Description
        </th>
    </tr>
	<tr>
        <td>
           KeyFile  
        </td>
        <td>
            Specifies a keyfile to sign the output assembly
        </td>
    </tr>
	<tr>
        <td>
           LogFile  
        </td>
        <td>
           Specifies a logfile to output log information
        </td>
    </tr>
	<tr>
        <td>
           Union  
        </td>
        <td>
           Merges types with identical names into one
        </td>
    </tr>
	<tr>
        <td>
            DebugInfo
        </td>
        <td>
            Enable/disable symbol file generation
        </td>
    </tr>
	<tr>
        <td>
            AttributeFile 
        </td>
        <td>
            Take assembly attributes from the given assembly file
        </td>
    </tr>
	<tr>
        <td>
            CopyAttributes 
        </td>
        <td>
            Copy assembly attributes
        </td>
    </tr>
	<tr>
        <td>
            AllowMultiple 
        </td>
        <td>
            Allows multiple attributes (if type allows)
        </td>
    </tr>
	<tr>
        <td>
            TargetKind 
        </td>
        <td>
            Target assembly kind (Exe|Dll|WinExe|SameAsPrimaryAssembly)
        </td>
    </tr>
	<tr>
        <td>
            TargetPlatformVersion 
        </td>
        <td>
            Target platform (v1, v1.1, v2, v4 supported)
        </td>
    </tr>
	<tr>
        <td>
            XmlDocumentation 
        </td>
        <td>
            Merge assembly xml documentation
        </td>
    </tr>
	<tr>
        <td>
            LibraryPath 
        </td>
        <td>
            List of paths to use as "include directories" when attempting to merge assemblies
        </td>
    </tr>
	<tr>
        <td>
            Internalize 
        </td>
        <td>
            Set all types but the ones from the first assembly 'internal'
        </td>
    </tr>
	<tr>
        <td>
            InternalizeExclude 
        </td>
        <td>
            Assemblies that will not be internalized.
        </td>
    </tr>
	<tr>
        <td>
            OutputFile 
        </td>
        <td>
            Output name for merged assembly
        </td>
    </tr>
	<tr>
        <td>
            InputAssemblies 
        </td>
        <td>
            List of assemblies that will be merged
        </td>
    </tr>
	<tr>
        <td>
            DelaySign 
        </td>
        <td>
            Set the keyfile, but don't sign the assembly
        </td>
    </tr>
	<tr>
        <td>
            AllowDuplicateResources 
        </td>
        <td>
            Allows to duplicate resources in output assembly 
        </td>
    </tr>
	<tr>
        <td>
            ZeroPeKind 
        </td>
        <td>
            Allows assemblies with Zero PeKind (but obviously only IL will get merged)
        </td>
    </tr>
	<tr>
        <td>
            Parallel 
        </td>
        <td>
            Use as many CPUs as possible to merge the assemblies
        </td>
    </tr>
	<tr>
        <td>
            Verbose 
        </td>
        <td>
            Additional debug information during merge that will be outputted to LogFile
        </td>
    </tr>
	<tr>
        <td>
            PrimaryAssemblyFile 
        </td>
        <td>
            Used in conjunction with Internalize to specify the main assembly filename
        </td>
    </tr>
	<tr>
        <td>
            Wildcards 
        </td>
        <td>
            Allows (and resolves) file wildcards (e.g. `*`.dll) in input assemblies
        </td>
    </tr>
</table>

License
=======
Checkout the [License](https://github.com/peters/ILRepack.MSBuild.Task/blob/master/LICENSE.md)
