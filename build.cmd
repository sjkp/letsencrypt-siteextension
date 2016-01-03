nuget restore
md artifacts\bin
"C:\Program Files (x86)\MSBuild\14.0\bin\MSBuild.exe" LetsEncrypt-SiteExtension\LetsEncrypt.SiteExtension.csproj /t:pipelinePreDeployCopyAllFilesToOneFolder /p:_PackageTempDir="..\artifacts";AutoParameterizationWebConfigConnectionStrings=false;Configuration=Release;SolutionDir="."
nuget pack
