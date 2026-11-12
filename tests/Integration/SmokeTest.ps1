param(
    [Parameter(Mandatory = $true)]
    [string]$ExecutablePath
)

$ErrorActionPreference = 'Stop'

$executable = (Resolve-Path -LiteralPath $ExecutablePath).Path
$processes = [System.Collections.Generic.List[System.Diagnostics.Process]]::new()

function Start-App {
    $process = Start-Process -FilePath $executable -PassThru
    $processes.Add($process)
    return $process
}

function Stop-App([System.Diagnostics.Process]$process) {
    $process.Refresh()
    if (-not $process.HasExited) {
        Stop-Process -Id $process.Id -Force
        if (-not $process.WaitForExit(10000)) {
            throw "HushLittlePC process $($process.Id) did not terminate."
        }
    }
}

try {
    Write-Host 'Starting the first application instance...'
    $first = Start-App
    Start-Sleep -Seconds 5
    $first.Refresh()

    if ($first.HasExited) {
        throw "The first application instance exited unexpectedly (code $($first.ExitCode))."
    }

    Write-Host 'Starting a second instance; it should exit because the single-instance mutex is held...'
    $second = Start-App

    if (-not $second.WaitForExit(10000)) {
        throw 'The second application instance remained running; single-instance enforcement failed.'
    }

    if ($second.ExitCode -ne 0) {
        throw "The second application instance exited with code $($second.ExitCode), expected 0."
    }

    $first.Refresh()
    if ($first.HasExited) {
        throw 'The first application instance exited when the second instance started.'
    }

    Write-Host 'The second instance exited and the first instance remained running.'

    Write-Host 'Stopping the first instance and verifying the mutex is released...'
    Stop-App $first

    Write-Host 'Starting the application again...'
    $third = Start-App
    Start-Sleep -Seconds 5
    $third.Refresh()

    if ($third.HasExited) {
        throw "The application failed to start after the previous instance exited (code $($third.ExitCode))."
    }

    Write-Host 'PASS: startup, single-instance enforcement, and restart smoke tests.'
}
finally {
    foreach ($process in $processes) {
        Stop-App $process
        $process.Dispose()
    }
}
