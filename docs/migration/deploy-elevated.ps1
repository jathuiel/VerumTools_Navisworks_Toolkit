# deploy-elevated.ps1 — wrapper para executar deploy.ps1 em processo elevado com log completo.
$ErrorActionPreference = 'Continue'
Start-Transcript -Path 'C:\Users\JathuielCorrea\github\01.VerumTools\docs\migration\deploy-log.txt' -Force
$code = 0
try {
    & 'C:\Users\JathuielCorrea\github\01.VerumTools\deploy.ps1'
}
catch {
    Write-Host "ERRO: $($_.Exception.Message)"
    Write-Host $_.ScriptStackTrace
    $code = 1
}
Stop-Transcript
exit $code
