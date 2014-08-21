properties {
    $base_directory = Resolve-Path ..
    $publish_directory = "$base_directory\publish-net45"
    $build_directory = "$base_directory\build"
    $src_directory = "$base_directory\src"
    $output_directory = "$base_directory\output"
    $packages_directory = "$src_directory\packages"
    $sln_file = "$src_directory\NEventStore.Persistence.GetEventStore.sln"
    $target_config = "Debug"
    $framework_version = "v4.0"
    $build_number = 0
    $assemblyInfoFilePath = "$src_directory\VersionAssemblyInfo.cs"

    $xunit_path = "$src_directory\\packages\xunit.runners.1.9.2\tools\xunit.console.clr4.exe"
    $ilMergeModule.ilMergePath = "$src_directory\packages\ilmerge.2.13.0307\ILMerge.exe"
    $nuget_dir = "$src_directory\.nuget"

    if($runPersistenceTests -eq $null) {
    	$runPersistenceTests = $false
    }
}

task default -depends Build

task Build -depends Clean, UpdateVersion, Compile, Test

task UpdateVersion {
    $version = Get-Version $assemblyInfoFilePath
    "Version: $version"
	$oldVersion = New-Object Version $version
	$newVersion = New-Object Version ($oldVersion.Major, $oldVersion.Minor, $oldVersion.Build, $buildNumber)
	Update-Version $newVersion $assemblyInfoFilePath
}

task Compile {
	exec { msbuild /nologo /verbosity:quiet $sln_file /p:Configuration=$target_config /t:Clean /p:OutDir=$output_directory}
	exec { msbuild /nologo /verbosity:quiet $sln_file /p:Configuration=$target_config /p:TargetFrameworkVersion=v4.5.1 /p:OutDir=$output_directory }
}

task Test -precondition { $runPersistenceTests } {
	"Persistence Tests"
	EnsureDirectory $output_directory
	RunXUnit -Spec '*Persistence.GetEventStore.Tests.dll'
}

task Package -depends Build, PackageNEventStore {
	move $output_directory $publish_directory
}

task PackageNEventStore -depends Clean, Compile {
	mkdir "$publish_directory\bin" | out-null
	"H1"
	Merge-Assemblies -outputFile "$publish_directory/bin/NEventStore.dll" -files @(
		"$src_directory/NEventStore/bin/$target_config/NEventStore.dll",
		"$src_directory/NEventStore/bin/$target_config/System.Reactive.Interfaces.dll",
		"$src_directory/NEventStore/bin/$target_config/System.Reactive.Core.dll",
		"$src_directory/NEventStore/bin/$target_config/System.Reactive.Linq.dll",
		"$src_directory/NEventStore/bin/$target_config/Newtonsoft.Json.dll"
	)
	"H2"
}

task Clean {
	Clean-Item $publish_directory -ea SilentlyContinue
    Clean-Item $output_directory -ea SilentlyContinue
}

task NuGetPack -depends Package {
    $versionString = Get-Version $assemblyInfoFilePath
	$version = New-Object Version $versionString
	$packageVersion = $version.Major.ToString() + "." + $version.Minor.ToString() + "." + $version.Build.ToString() + "-build" + $build_number.ToString().PadLeft(5,'0')
	gci -r -i *.nuspec "$nuget_dir" |% { .$nuget_dir\nuget.exe pack $_ -basepath $base_directory -o $publish_directory -version $packageVersion }
}

function EnsureDirectory {
	param($directory)

	if(!(test-path $directory))
	{
		mkdir $directory
	}
}
function RunXUnit {
	param([string]$Spec)
	Get-ChildItem "$output_directory\$Spec" | % {
		exec { &$xunit_path $_ }
	}
}