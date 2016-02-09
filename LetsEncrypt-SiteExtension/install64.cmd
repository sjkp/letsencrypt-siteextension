cd /D %TEMP%
IF EXIST letsencrypt.siteextension.job (
  rd /S /q letsencrypt.siteextension.job
)
mkdir letsencrypt.siteextension.job
cd letsencrypt.siteextension.job
nuget install letsencrypt.siteextension.job64 -Pre

SET JOB_FOLDER="%WEBROOT_PATH%\App_Data\jobs\continuous\letsencrypt.siteextension.job"
IF EXIST %JOB_FOLDER% (
  rd /S /q %JOB_FOLDER%
)
mkdir %JOB_FOLDER%
cd letsencrypt.siteextension.job*
xcopy content %JOB_FOLDER% /E /C