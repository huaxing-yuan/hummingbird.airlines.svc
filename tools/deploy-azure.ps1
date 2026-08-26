# Deploys the Hummingbird Airlines gateway to Azure App Service.
#
# Usage:
#   ./tools/deploy-azure.ps1                          # default target below
#   ./tools/deploy-azure.ps1 -VerifyOnly              # just probe the live site
#
# Example:
#   ./tools/deploy-azure.ps1 -ResourceGroup my-rg -WebAppName my-app
#   ./tools/deploy-azure.ps1 -ResourceGroup my-rg -WebAppName my-app -VerifyOnly

param(
    [Parameter(Mandatory)]
    [string]$ResourceGroup,
    [Parameter(Mandatory)]
    [string]$WebAppName,
    [switch]$VerifyOnly
)

$ErrorActionPreference = "Stop"
$baseUrl = "https://$WebAppName.azurewebsites.net"

if (-not $VerifyOnly) {
    Write-Host "== publishing =="
    dotnet publish src/Hummingbird.Airlines.Middleware -c Release -o artifacts/publish
    if ($LASTEXITCODE -ne 0) { throw "publish failed" }

    if (Test-Path artifacts/site.zip) { Remove-Item artifacts/site.zip }
    Compress-Archive -Path artifacts/publish/* -DestinationPath artifacts/site.zip

    Write-Host "== deploying zip to $WebAppName =="
    az webapp deploy -g $ResourceGroup -n $WebAppName --src-path artifacts/site.zip --type zip --timeout 600
    if ($LASTEXITCODE -ne 0) { throw "deploy failed" }
}

Write-Host "== verification =="
$checks = @(
    @{ Name = "default page";        Url = "$BaseUrl/" },
    @{ Name = "swagger";             Url = "$BaseUrl/swagger/v1/swagger.json" },
    @{ Name = "flights api";         Url = "$BaseUrl/api/v1/flights/HB900" },
    @{ Name = "protection settings"; Url = "$BaseUrl/api/v1/_protection" },
    @{ Name = "booking wsdl";        Url = "$BaseUrl/soap/booking?wsdl" },
    @{ Name = "flight wsdl";         Url = "$BaseUrl/soap/flights?wsdl" },
    @{ Name = "luggage wsdl";        Url = "$BaseUrl/soap/luggage?wsdl" }
)

$failed = 0
foreach ($check in $checks) {
    try {
        $r = Invoke-WebRequest -Uri $check.Url -TimeoutSec 120 -SkipHttpErrorCheck
        $ok = $r.StatusCode -eq 200
        if ($ok) { Write-Host "PASS $($check.Name)" -ForegroundColor Green }
        else     { Write-Host "FAIL $($check.Name): HTTP $($r.StatusCode)" -ForegroundColor Red; $failed++ }
    }
    catch {
        Write-Host "FAIL $($check.Name): $($_.Exception.Message)" -ForegroundColor Red
        $failed++
    }
}

if ($failed -eq 0) {
    Write-Host "`nAll checks passed: $baseUrl" -ForegroundColor Green
}
else {
    Write-Host "`n$failed check(s) failed. If you saw '403 Site Disabled', the F1 daily" -ForegroundColor Yellow
    Write-Host "CPU quota is exhausted; it resets at 00:00 UTC." -ForegroundColor Yellow
    exit 1
}

