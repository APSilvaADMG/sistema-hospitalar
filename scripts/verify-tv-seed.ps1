$ErrorActionPreference = 'Stop'
$login = Invoke-RestMethod -Uri 'http://127.0.0.1:8080/api/auth/login' -Method Post -ContentType 'application/json' -Body '{"email":"admin@hospital.local","password":"Admin123!"}'
$headers = @{ Authorization = "Bearer $($login.token)" }
$monitor = Invoke-RestMethod -Uri 'http://127.0.0.1:8080/api/tv-signage/monitor' -Headers $headers
$media = Invoke-RestMethod -Uri 'http://127.0.0.1:8080/api/tv-signage/media' -Headers $headers
$ann = Invoke-RestMethod -Uri 'http://127.0.0.1:8080/api/tv-signage/announcements' -Headers $headers
$calls = Invoke-RestMethod -Uri 'http://127.0.0.1:8080/api/tv-signage/calls?limit=20' -Headers $headers
$news = Invoke-RestMethod -Uri 'http://127.0.0.1:8080/api/tv-signage/news' -Headers $headers
$slugs = ($monitor.displays | ForEach-Object { $_.slug }) -join ', '
Write-Output "displays=$($monitor.totalDisplays) activeMedia=$($monitor.activeMedia) callsToday=$($monitor.callsToday)"
Write-Output "mediaItems=$($media.Count) announcements=$($ann.Count) recentCalls=$($calls.Count) news=$($news.Count)"
Write-Output "displaySlugs=$slugs"
$active = $calls | Where-Object { $_.isActive } | Select-Object -First 1
if ($active) { Write-Output "activeCallTicket=$($active.ticketNumber) destination=$($active.destination)" }
