rem You can run msbuild /t:run from the command line to setup and launch the sample app

setlocal

set appcmd=%SystemRoot%\system32\inetsrv\appcmd.exe
set poolname=Cors_SampleApp
set webroot=%~dnp1

rem If dount, delete app & app pool
call %appcmd% list app /apppool.name:"Cors_SampleApp" && call %appcmd% delete app /app.name:"default/CorsSampleApp"
call %appcmd% list apppool "%poolname%" && call %appcmd% delete apppool "%poolname%"

rem Create app pool
call %appcmd% add apppool /name:"%poolname%" /managedRuntimeVersion:v4.0 /managedPipelineMode:Integrated

rem Create app
call %appcmd% add app /site.name:default /path:"/CorsSampleApp" /physicalPath:"%webRoot%" /applicationPool:"%poolname%"

endlocal
