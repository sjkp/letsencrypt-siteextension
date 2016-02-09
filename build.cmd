nuget restore
md artifacts\bin
"C:\Program Files (x86)\MSBuild\14.0\bin\MSBuild.exe" LetsEncrypt-SiteExtension\LetsEncrypt.SiteExtension.csproj /t:pipelinePreDeployCopyAllFilesToOneFolder /p:Platform=x86 /p:_PackageTempDir="..\artifacts";AutoParameterizationWebConfigConnectionStrings=false;Configuration=Release;SolutionDir="."
"C:\Program Files (x86)\MSBuild\14.0\bin\MSBuild.exe" LetsEncrypt.SiteExtension.Core\LetsEncrypt.SiteExtension.Core.csproj /p:Platform=x86;Configuration=Release;SolutionDir="."
xcopy LetsEncrypt.SiteExtension.Core\bin\x86\Release\*.* artifacts\bin /sy
nuget pack letsencrypt.nuspec

md artifacts64\bin
"C:\Program Files (x86)\MSBuild\14.0\bin\MSBuild.exe" LetsEncrypt-SiteExtension\LetsEncrypt.SiteExtension.csproj /t:pipelinePreDeployCopyAllFilesToOneFolder /p:Platform=x64 /p:_PackageTempDir="..\artifacts64";AutoParameterizationWebConfigConnectionStrings=false;Configuration=Release;SolutionDir="."
"C:\Program Files (x86)\MSBuild\14.0\bin\MSBuild.exe" LetsEncrypt.SiteExtension.Core\LetsEncrypt.SiteExtension.Core.csproj /p:Platform=x64;Configuration=Release;SolutionDir="."
xcopy LetsEncrypt.SiteExtension.Core\bin\x64\Release\*.* artifacts64\bin /sy
cd artifacts
del install.cmd 
ren install64.cmd install.cmd 
cd..
nuget pack letsencrypt64.nuspec
