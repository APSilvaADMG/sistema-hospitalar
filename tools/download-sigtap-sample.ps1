$ftpBase = 'ftp://ftp2.datasus.gov.br/pub/sistemas/tup/downloads/'
$request = [System.Net.FtpWebRequest]::Create($ftpBase)
$request.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectory
$request.Credentials = New-Object System.Net.NetworkCredential('anonymous', 'anonymous@datasus.gov.br')
$request.UsePassive = $true
$response = $request.GetResponse()
$reader = New-Object System.IO.StreamReader($response.GetResponseStream())
$listing = $reader.ReadToEnd()
$reader.Close()
$response.Close()

$files = $listing -split "`r?`n" | Where-Object { $_ -match 'TabelaUnificada.*\.zip' } | Sort-Object -Descending
Write-Host "Found $($files.Count) ZIP files"
$files | Select-Object -First 5 | ForEach-Object { Write-Host "  $_" }

if ($files.Count -eq 0) { exit 1 }

$latest = $files[0]
Write-Host "Downloading: $latest"
$dlUrl = $ftpBase + $latest
$dlRequest = [System.Net.FtpWebRequest]::Create($dlUrl)
$dlRequest.Method = [System.Net.WebRequestMethods+Ftp]::DownloadFile
$dlRequest.Credentials = New-Object System.Net.NetworkCredential('anonymous', 'anonymous@datasus.gov.br')
$dlRequest.UseBinary = $true
$dlRequest.UsePassive = $true
$dlResponse = $dlRequest.GetResponse()
$outPath = Join-Path $PSScriptRoot '..\sample_sigtap.zip'
$fs = [System.IO.File]::Create($outPath)
$dlResponse.GetResponseStream().CopyTo($fs)
$fs.Close()
$dlResponse.Close()
$size = (Get-Item $outPath).Length
Write-Host "Downloaded to $outPath ($size bytes)"
