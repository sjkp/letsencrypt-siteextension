nuget restore
md artifacts.job\
"C:\Program Files (x86)\MSBuild\14.0\bin\MSBuild.exe" LetsEncrypt.SiteExtension.Core\LetsEncrypt.SiteExtension.Core.csproj /p:Platform=x86;Configuration=Release;SolutionDir="."
"C:\Program Files (x86)\MSBuild\14.0\bin\MSBuild.exe" LetsEncrypt.SiteExtension.WebJob\LetsEncrypt.SiteExtension.WebJob.csproj /p:Platform=x86;Configuration=Release;SolutionDir="."
xcopy LetsEncrypt.SiteExtension.Core\bin\x86\Release\*.* artifacts.job\ /sy
xcopy LetsEncrypt.SiteExtension.WebJob\bin\x86\Release\*.* artifacts.job\ /sy
nuget pack letsencrypt.siteextension.job.nuspec
