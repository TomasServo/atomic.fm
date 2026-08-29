param(
    [string]$Url = "http://radio.atomic.fm:8000/atomic-radio"
)

$ErrorActionPreference = "Stop"

Write-Host "Testing $Url"

$request = [System.Net.HttpWebRequest]::Create($Url)
$request.Method = "GET"
$request.Timeout = 5000
$request.ReadWriteTimeout = 5000
$request.UserAgent = "AtomicRadioPreflight/1.0"

$response = $request.GetResponse()
try {
    Write-Host "Status: $([int]$response.StatusCode) $($response.StatusDescription)"
    Write-Host "Content-Type: $($response.ContentType)"

    $stream = $response.GetResponseStream()
    $buffer = New-Object byte[] 4096
    $bytesRead = $stream.Read($buffer, 0, $buffer.Length)

    if ($bytesRead -le 0) {
        throw "Connected, but no audio bytes were received."
    }

    Write-Host "Received $bytesRead audio bytes."
    Write-Host "Stream preflight passed."
}
finally {
    $response.Dispose()
}
