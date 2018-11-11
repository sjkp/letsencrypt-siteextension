nuget restore LetsEncrypt-SiteExtension.sln

RMDIR /S /Q artifacts
md artifacts

WHERE msbuild 
IF %ERRORLEVEL% NEQ 0 SET PATH=%PATH%;"C:\Program Files (x86)\MSBuild\14.0\bin\MSBuild.exe"

MSBuild LetsEncrypt-SiteExtension\LetsEncrypt.SiteExtension.csproj /t:pipelinePreDeployCopyAllFilesToOneFolder /p:_PackageTempDir="..\artifacts";AutoParameterizationWebConfigConnectionStrings=false;Configuration=Release;SolutionDir="."
MSBuild LetsEncrypt.SiteExtension.Core\LetsEncrypt.Azure.Core.csproj /p:Configuration=Release;SolutionDir="."
MSBuild LetsEncrypt.SiteExtension.WebJob\LetsEncrypt.SiteExtension.WebJob.csproj /p:Configuration=Release;SolutionDir="."
xcopy LetsEncrypt.SiteExtension.Core\bin\Release\*.* artifacts\bin\ /sy

md artifacts\app_data\jobs\continuous\letsencrypt.siteextension.job
xcopy LetsEncrypt.SiteExtension.Core\bin\Release\*.* artifacts\app_data\jobs\continuous\letsencrypt.siteextension.job\  /sy
xcopy LetsEncrypt.SiteExtension.WebJob\bin\Release\*.* artifacts\app_data\jobs\continuous\letsencrypt.siteextension.job\ /sy

nuget pack letsencrypt.nuspec

RMDIR /S /Q artifacts\app_data\jobs
nuget pack LetsEncrypt.WebAppOnly.nuspec