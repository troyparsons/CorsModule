setlocal

set appcmd=%SystemRoot%\system32\inetsrv\appcmd.exe
set poolname=Cors_SampleApp
set webroot=%~dnp1

rem Create app pool
call %appcmd% list apppool "%poolname%" && call %appcmd% delete apppool "%poolname%"
call %appcmd% add apppool /name:"%poolname%" /managedRuntimeVersion:v4.0 /managedPipelineMode:Integrated

rem Create app
call %appcmd% list app /apppool.name:"Cors_SampleApp" && call %appcmd% delete app /app.name:"default/CorsSampleApp"
call %appcmd% add app /site.name:default /path:"/CorsSampleApp" /physicalPath:"%webRoot%" /applicationPool:"%poolname%"

endlocal
