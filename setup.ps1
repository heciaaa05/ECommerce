# Chạy từ thư mục gốc của solution:  powershell -ExecutionPolicy Bypass -File .\setup.ps1
# Việc làm: kiểm tra công cụ -> tạo lại 2 container (SQL Server, Redis) -> restore/build -> tạo migration -> cập nhật DB

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

function Test-Tool($name) {
    if (-not (Get-Command $name -ErrorAction SilentlyContinue)) {
        Write-Host "[THIEU] '$name' chua cai hoac chua nam trong PATH." -ForegroundColor Red
        return $false
    }
    Write-Host "[OK] $name" -ForegroundColor Green
    return $true
}

Write-Host "`n== 1. Kiem tra cong cu ==" -ForegroundColor Cyan
$ok = (Test-Tool "dotnet") -and (Test-Tool "docker")
if (-not $ok) { throw "Cai du .NET SDK 10 va Docker Desktop roi chay lai script." }

if (-not (Get-Command dotnet-ef -ErrorAction SilentlyContinue)) {
    Write-Host "Cai dotnet-ef..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef
}

Write-Host "`n== 2. Container SQL Server + Redis ==" -ForegroundColor Cyan
function Ensure-Container($name, $runArgs) {
    $exists = docker ps -a --filter "name=^/$name$" --format "{{.Names}}"
    if ($exists) {
        Write-Host "Container '$name' da ton tai -> start" -ForegroundColor Yellow
        docker start $name | Out-Null
    } else {
        Write-Host "Tao container '$name'" -ForegroundColor Yellow
        Invoke-Expression "docker run --name $name $runArgs" | Out-Null
    }
}

Ensure-Container "sql-ecommerce" '-e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest'
Ensure-Container "redis-ecommerce" '-p 6379:6379 -d redis:alpine'

Write-Host "Doi SQL Server khoi dong (20 giay)..." -ForegroundColor Yellow
Start-Sleep -Seconds 20

Write-Host "`n== 3. Restore + Build ==" -ForegroundColor Cyan
dotnet restore
dotnet build --no-restore

Write-Host "`n== 4. Migration ==" -ForegroundColor Cyan
if (-not (Test-Path "src\ECommerce.Infrastructure\Migrations")) {
    dotnet ef migrations add InitialCreate --project src\ECommerce.Infrastructure --startup-project src\ECommerce.API
} else {
    Write-Host "Da co thu muc Migrations -> bo qua buoc tao migration" -ForegroundColor Yellow
}
dotnet ef database update --project src\ECommerce.Infrastructure --startup-project src\ECommerce.API

Write-Host "`nXong. Chay API:  dotnet run --project src\ECommerce.API   (Swagger: http://localhost:5203/swagger)" -ForegroundColor Green
