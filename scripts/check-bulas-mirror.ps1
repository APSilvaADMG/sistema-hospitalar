# Verifica progresso do espelho HTTrack em C:\Meus Sites\BulasdeAaZ
param(
    [string]$MirrorDir = "C:\Meus Sites\BulasdeAaZ",
    [switch]$Watch,
    [int]$IntervalSeconds = 30
)

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$argsList = @("--mirror-dir", $MirrorDir)
if ($Watch) {
    $argsList += @("--watch", "--interval", $IntervalSeconds)
}

python "$root\check-offline-cr-mirror.py" @argsList
