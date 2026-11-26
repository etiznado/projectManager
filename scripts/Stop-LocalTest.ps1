param()
$ErrorActionPreference = 'Stop'
$identityPath = Join-Path (Split-Path -Parent $PSScriptRoot) '.local-test/process.json'
if (-not (Test-Path -LiteralPath $identityPath)) { Write-Output 'No local test application is recorded.'; return }
$identity = Get-Content -LiteralPath $identityPath -Raw | ConvertFrom-Json
$process = Get-Process -Id ([int]$identity.ProcessId) -ErrorAction SilentlyContinue
if ($process) {
    if ($process.ProcessName -ne 'dotnet' -or $process.Path -cne $identity.Executable -or $process.StartTime.ToUniversalTime().Ticks -ne [long]$identity.StartUtcTicks) {
        throw 'The saved process identity does not match. No unrelated process was stopped.'
    }
    $signal = [Threading.EventWaitHandle]::OpenExisting(('Local\RaceIT.ProjectManager.LocalTest.{0}' -f $process.Id))
    try { [void]$signal.Set() } finally { $signal.Dispose() }
    if (-not $process.WaitForExit(20000)) { throw 'Graceful shutdown has not finished. Inspect the exact local process.' }
}
Remove-Item -LiteralPath $identityPath
Write-Output 'Stopped the local test application; private configuration, TLS assets and existing fixture data were preserved.'
