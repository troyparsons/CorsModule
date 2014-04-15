function Get-IisAppHostConfigFilePath
{
	$path = "$env:systemroot\System32\inetsrv\config\applicationHost.config"

	if ( !(Test-Path $path) )
	{
		Write-Error "Did not find IIS application host config file ($path).  Is IIS installed?"
		return $null
	}

	return $path
}


function Get-IisSchemaPath
{
	$path = "$env:systemroot\System32\inetsrv\config\schema"

	if ( !(Test-Path $path) )
	{
		Write-Error "Did not find IIS schema directory ($path).  Is IIS installed?"
		return $null
	}

	return $path
}


function Get-SdkPath
{
	$regPath = 'HKLM:\SOFTWARE\Microsoft\Microsoft SDKs\Windows'
	if ( !(Test-Path -Path $regPath) )
	{
		Write-Error "Could not find installed SDK ($regPath)"
		return $null
	}

	$sdkPath = (Get-ItemProperty -Path $regPath).CurrentInstallFolder

	if ( ! $sdkPath )
	{
		Write-Error "Failed to get SDK CurrentInstallFolder"
		return $null
	}

	if ( !(Test-Path -Path $sdkPath) )
	{
		Write-Error "SDK folder does not exist: $sdkPath"
		return $null
	}

	return $sdkPath
}


function Get-GacUtilPath
{
	$sdkPath = Get-SdkPath
	if ( !$sdkPath )
	{
		Write-Error "Failed to find GacUtil"
		return $null
	}

	$gacUtil = ("$sdkPath\bin\NETFX 4.0 Tools\gacutil.exe" -replace '\\\\', '\')

	if ( !(Test-Path -Path $gacUtil) )
	{
		Write-Error "Failed to locate GacUtil: $gacUtil"
		return $null
	}

	return $gacUtil
}


function Install-CorsIisConfigSection
{
	if (Test-CorsIisConfigSection)
	{
		Write-Host "httpCors configuration section already present"
		return $true
	}
	
	$configFilePath = Get-IisAppHostConfigFilePath
	
	if ( !$configFilePath )
	{
		Write-Error "Failed to add configuration section to IIS"
		return $false
	}
	
	$xml = [xml]( Get-Content $configFilePath )
	$systemWebServer = $xml.SelectSingleNode("/configuration/configSections/sectionGroup[@name='system.webServer']")
	if ( !$systemWebServer )
	{
		Write-Error "Did not find system.webServer configuration section"
		return $false
	}
	$newNode = $xml.createElement("section")
	$newNode.setAttribute("name", "httpCors")
	$newNode.setAttribute("overrideModeDefault", "Allow")
	$newNode.setAttribute("allowDefinition", "MachineToApplication")
	$systemWebServer.AppendChild($newNode)
	$backupFilePath = "$configFilePath.$( [DateTime]::Now.ToString('yyyyMMdd_hhmmss') ).backup"
	copy $configFilePath $backupFilePath
	if ( !(Test-Path $backupFilePath) )
	{
		Write-Error "Failed to backup IIS configuration: $backupFilePath"
		return $false
	}
	Write-Host "Backup IIS application host: $backupFilePath"
	$xml.save($configFilePath)
	Write-Host "Saved IIS configuration changes"
	return $true
}


function Install-CorsIisConfig
{
	return (Install-CorsIisSchemaFile) -and (Install-CorsIisConfigSection)
}


function Install-CorsIisSchemaFile
{
	$schemaDir = Get-IisSchemaPath
	
	if ( !($schemaDir) )
	{
		Write-Error "Failed to install schema file"
		return $false
	}
	copy httpCors_schema.xml $schemaDir
	Write-Host "Installed schema file (httpCors_schema.xml)"
	return $true
}


function Install-CorsModuleAssembly
{
	$gacUtil = Get-GacUtilPath

	if ( !$gacUtil )
	{
		Write-Error "Failed to install CorsModule assembly to the GAC"
		return $false
	}

	& $gacUtil /i CorsModule.dll | Write-Host

	if ($lastExitCode)
	{
		Write-Error "Failed to install CorsModule assembly to the GAC"
		Write-Host $output
		return $false
	}
	Write-Host "Installed CorsModule assembly to GAC"
	return $true
}


function Test-CorsIisConfigSection
{
	$cfgFile = Get-IisAppHostConfigFilePath
	if ( !$cfgFile )
	{
		return $false
	}
	$xpath = '/configuration/configSections/sectionGroup[@name="system.webServer"]/section[@name="httpCors"]'
	if (Select-Xml -Path $cfgFile -XPath $xpath)
	{
		$true
	}
	else
	{
		$false
	}
}


# Perform the installation
$success = ( Install-CorsModuleAssembly ) -and ( Install-CorsIisConfig )


if ( $success )
{
	Write-Host "Installation successful"
	exit 0
}
else
{
	Write-Error "Installation failed"
	exit 1
}
