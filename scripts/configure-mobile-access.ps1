# Abre portas no firewall do Windows e exibe URLs para acesso pelo celular.
# Execute como Administrador:
#   powershell -ExecutionPolicy Bypass -File scripts/configure-mobile-access.ps1

$ports = @(5173, 8080)
$rulePrefix = "SistemaHospitalar"

foreach ($port in $ports) {
    $name = "$rulePrefix-Port-$port"
    $existing = Get-NetFirewallRule -DisplayName $name -ErrorAction SilentlyContinue
    if ($existing) {
        Write-Host "Regra de firewall ja existe: $name"
    } else {
        try {
            New-NetFirewallRule -DisplayName $name -Direction Inbound -Action Allow -Protocol TCP -LocalPort $port -ErrorAction Stop | Out-Null
            Write-Host "Firewall liberado: porta TCP $port"
        } catch {
            Write-Warning "Nao foi possivel abrir a porta $port (execute como Administrador): $($_.Exception.Message)"
        }
    }
}

function Get-LanIp {
    $candidates = Get-NetIPAddress -AddressFamily IPv4 | Where-Object {
        $_.IPAddress -notlike '127.*' -and
        $_.IPAddress -notlike '169.254.*' -and
        $_.PrefixOrigin -ne 'WellKnown'
    }

    $wifi = $candidates | Where-Object { $_.IPAddress -match '^(192\.168\.|10\.)' } | Select-Object -First 1
    if ($wifi) { return $wifi.IPAddress }

    $any = $candidates | Select-Object -First 1
    if ($any) { return $any.IPAddress }

    return 'SEU_IP_LAN'
}

$ip = Get-LanIp

Write-Host ""
Write-Host "=== Acesso pelo celular (mesma rede Wi-Fi) ===" -ForegroundColor Cyan
Write-Host "Web (PEP / sistema):  http://${ip}:5173"
Write-Host "API direta (app):     http://${ip}:8080/api"
Write-Host "Swagger:              http://${ip}:8080/swagger"
Write-Host ""
Write-Host "Nao use localhost no celular. Use o IP acima." -ForegroundColor Yellow
Write-Host "Suba o sistema com: docker compose up --build -d" -ForegroundColor Yellow
