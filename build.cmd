nuget restore

md artifacts64
"C:\Program Files (x86)\MSBuild\14.0\bin\MSBuild.exe" LetsEncrypt-SiteExtension\LetsEncrypt.SiteExtension.csproj /t:pipelinePreDeployCopyAllFilesToOneFolder /p:Platform=x64 /p:_PackageTempDir="..\artifacts64";AutoParameterizationWebConfigConnectionStrings=false;Configuration=Release;SolutionDir="."
"C:\Program Files (x86)\MSBuild\14.0\bin\MSBuild.exe" LetsEncrypt.SiteExtension.Core\LetsEncrypt.Azure.Core.csproj /p:Platform=x64;Configuration=Release;SolutionDir="."
"C:\Program Files (x86)\MSBuild\14.0\bin\MSBuild.exe" LetsEncrypt.SiteExtension.WebJob\LetsEncrypt.SiteExtension.WebJob.csproj /p:Platform=x64;Configuration=Release;SolutionDir="."
xcopy LetsEncrypt.SiteExtension.Core\bin\x64\Release\*.* artifacts64\bin /sy

md artifacts64\app_data\jobs\continuous\letsencrypt.siteextension.job
xcopy LetsEncrypt.SiteExtension.Core\bin\x64\Release\*.* artifacts64\app_data\jobs\continuous\letsencrypt.siteextension.job  /sy
xcopy LetsEncrypt.SiteExtension.WebJob\bin\x64\Release\*.* artifacts64\app_data\jobs\continuous\letsencrypt.siteextension.job /sy

nuget pack letsencrypt64.nuspec

md artifacts
"C:\Program Files (x86)\MSBuild\14.0\bin\MSBuild.exe" LetsEncrypt-SiteExtension\LetsEncrypt.SiteExtension.csproj /t:pipelinePreDeployCopyAllFilesToOneFolder /p:Platform=x86 /p:_PackageTempDir="..\artifacts";AutoParameterizationWebConfigConnectionStrings=false;Configuration=Release;SolutionDir="."
"C:\Program Files (x86)\MSBuild\14.0\bin\MSBuild.exe" LetsEncrypt.SiteExtension.Core\LetsEncrypt.Azure.Core.csproj /p:Platform=x86;Configuration=Release;SolutionDir="."
"C:\Program Files (x86)\MSBuild\14.0\bin\MSBuild.exe" LetsEncrypt.SiteExtension.WebJob\LetsEncrypt.SiteExtension.WebJob.csproj /p:Platform=x86;Configuration=Release;SolutionDir="."
xcopy LetsEncrypt.SiteExtension.Core\bin\x86\Release\*.* artifacts\bin /sy

md artifacts\app_data\jobs\continuous\letsencrypt.siteextension.job
xcopy LetsEncrypt.SiteExtension.Core\bin\x64\Release\*.* artifacts\app_data\jobs\continuous\letsencrypt.siteextension.job  /sy
xcopy LetsEncrypt.SiteExtension.WebJob\bin\x64\Release\*.* artifacts\app_data\jobs\continuous\letsencrypt.siteextension.job /sy

nuget pack letsencrypt.nuspec