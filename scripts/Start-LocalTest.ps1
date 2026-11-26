param([switch]$NoBuild)
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$profileDirectory = Join-Path $repositoryRoot '.local-test'
$identityPath = Join-Path $profileDirectory 'process.json'
$projectPath = Join-Path $repositoryRoot 'src/RaceIT.ProjectManager/RaceIT.ProjectManager.csproj'
$applicationPath = Join-Path $repositoryRoot 'src/RaceIT.ProjectManager/bin/local-test/RaceIT.ProjectManager.dll'
$dotnetPath = (Get-Command dotnet -CommandType Application -ErrorAction Stop).Source
try {
    $runtime = Get-Content -LiteralPath (Join-Path $profileDirectory 'runtime.json') -Raw | ConvertFrom-Json
    $approval = Get-Content -LiteralPath (Join-Path $profileDirectory 'approval.json') -Raw | ConvertFrom-Json
    $fields = 'ProfileId','ConnectionString','ConnectorId','PersonId','TokenSha256','UiUrl','ApiUrl','PfxPath','PfxPasswordDpapiPath','PublicCertificatePath'
    foreach ($field in $fields) {
        if ([string]::IsNullOrWhiteSpace([string]$runtime.$field) -or [string]$runtime.$field -cne [string]$approval.$field) { throw 'Unapproved settings.' }
    }
    if (($runtime.AllowedFrontIds -join ',') -cne ($approval.AllowedFrontIds -join ',')) { throw 'Unapproved settings.' }
    foreach ($settings in @($runtime, $approval)) {
        if ($settings.PSObject.Properties.Name -contains 'AllowAllOwnerFronts' -and $settings.AllowAllOwnerFronts -isnot [bool]) { throw 'Invalid access mode.' }
        foreach ($flag in 'ExplicitlyEnabled','SyntheticDataOnly','SelectorEnabled','JarvisEnabled') {
            if ($settings.$flag -isnot [bool] -or -not $settings.$flag) { throw 'Unapproved flags.' }
        }
    }
    if ([bool]$runtime.AllowAllOwnerFronts -ne [bool]$approval.AllowAllOwnerFronts) { throw 'Unapproved access mode.' }
    foreach ($address in @($runtime.UiUrl, $runtime.ApiUrl)) {
        $uri = [Uri]$address
        $ip = [Net.IPAddress]::Parse($uri.Host)
        if (-not [Net.IPAddress]::IsLoopback($ip)) { throw 'Invalid binding.' }
    }
} catch { throw 'The approved private profile is missing, invalid, or its local ports are occupied. No fallback is allowed.' }
if (Test-Path -LiteralPath $identityPath) {
    $prior = Get-Content -LiteralPath $identityPath -Raw | ConvertFrom-Json
    $running = Get-Process -Id ([int]$prior.ProcessId) -ErrorAction SilentlyContinue
    if ($running -and $running.ProcessName -eq 'dotnet' -and $running.Path -ceq $prior.Executable -and $prior.Application -ceq $applicationPath -and $running.StartTime.ToUniversalTime().Ticks -eq [long]$prior.StartUtcTicks) {
        Write-Output 'The local test application is already running.'
        return
    }
}
try {
    foreach ($address in @($runtime.UiUrl, $runtime.ApiUrl)) {
        $uri = [Uri]$address
        $probe = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Parse($uri.Host), $uri.Port)
        try { $probe.Start() } finally { $probe.Stop() }
    }
} catch { throw 'The approved local ports are occupied. No fallback is allowed.' }
if (-not $NoBuild) {
    & dotnet build $projectPath -m:1 -p:OutputPath=bin/local-test/ --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'The local test build failed.' }
}
if ($PSVersionTable.PSEdition -eq 'Desktop') {
    Add-Type -AssemblyName System.Security
} else {
    Add-Type -AssemblyName System.Security.Cryptography.ProtectedData
}
$passwordBytes = [System.Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($runtime.PfxPasswordDpapiPath), $null, [System.Security.Cryptography.DataProtectionScope]::CurrentUser)
$password = [Text.Encoding]::UTF8.GetString($passwordBytes)
[Array]::Clear($passwordBytes, 0, $passwordBytes.Length)
$environmentValues = @{
    'DOTNET_ENVIRONMENT' = 'PrivateQa'; 'ASPNETCORE_ENVIRONMENT' = 'PrivateQa'
    'LocalTestProfile__Enabled' = 'true'; 'LocalTestProfile__Directory' = $profileDirectory
    'RACEIT_LOCAL_TEST_PFX_PASSWORD' = $password
    'ASPNETCORE_URLS' = $null; 'DOTNET_URLS' = $null
    'ASPNETCORE_HTTP_PORTS' = $null; 'ASPNETCORE_HTTPS_PORTS' = $null
    'DOTNET_HTTP_PORTS' = $null; 'DOTNET_HTTPS_PORTS' = $null
}
$savedEnvironment = @{}
try {
    foreach ($entry in $environmentValues.GetEnumerator()) {
        $savedEnvironment[$entry.Key] = [Environment]::GetEnvironmentVariable($entry.Key)
        [Environment]::SetEnvironmentVariable($entry.Key, $entry.Value)
    }
    $process = Start-Process -FilePath $dotnetPath -ArgumentList ('"{0}"' -f $applicationPath) -WorkingDirectory (Split-Path -Parent $projectPath) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $profileDirectory 'stdout.log') -RedirectStandardError (Join-Path $profileDirectory 'stderr.log')
    @{ ProcessId = $process.Id; StartUtcTicks = $process.StartTime.ToUniversalTime().Ticks; Executable = $dotnetPath; Application = $applicationPath } | ConvertTo-Json | Set-Content -LiteralPath $identityPath
} finally {
    foreach ($entry in $savedEnvironment.GetEnumerator()) { [Environment]::SetEnvironmentVariable($entry.Key, $entry.Value) }
    $password = $null
    $environmentValues.Clear()
}
$ready = $false
$deadline = [DateTime]::UtcNow.AddSeconds(20)
while ([DateTime]::UtcNow -lt $deadline) {
    $process.Refresh()
    if ($process.HasExited) { throw 'The local test application rejected startup; inspect the private logs.' }
    try {
        $loginUrl = ([Uri]$runtime.UiUrl).AbsoluteUri.TrimEnd('/') + '/login'
        if (([Uri]$runtime.UiUrl).Scheme -eq 'https') {
            $status = & curl.exe --silent --cacert $runtime.PublicCertificatePath --output NUL --write-out '%{http_code}' --max-time 2 $loginUrl
            if ($LASTEXITCODE -eq 0 -and $status -eq '200') { $ready = $true; break }
        } else {
            $response = Invoke-WebRequest -Uri $loginUrl -UseBasicParsing -TimeoutSec 2
            if ([int]$response.StatusCode -eq 200) { $ready = $true; break }
        }
    } catch { }
    Start-Sleep -Milliseconds 200
}
if (-not $ready) { throw 'The owned local process did not become ready in the allowed time; inspect its private logs before retrying.' }
Write-Output 'Started the local test application. Open the approved browser address and use its private test selector.'
