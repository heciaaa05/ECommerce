# Giả lập VNPay gọi IPN (có chữ ký HMAC-SHA512 hợp lệ) để test luồng "thanh toán xong -> đơn Paid" mà không cần cổng VNPay.
# HashSecret/TmnCode đọc từ user-secrets, không phải dán khoá vào đâu cả.
#
# Cách dùng (chạy từ thư mục gốc solution):
#   powershell -ExecutionPolicy Bypass -File .\tools\fake-ipn.ps1 -OrderId <orderId> -Amount 3000000
#   thêm -Fail để giả lập thanh toán thất bại; -BaseUrl để gọi qua ngrok thay vì localhost
#
# Điều kiện: đơn còn Pending (chưa quá 15 phút) và đã gọi POST /api/payments/vnpay/create/{orderId} ít nhất 1 lần.

param(
    [Parameter(Mandatory = $true)][string]$OrderId,
    [Parameter(Mandatory = $true)][long]$Amount,          # tổng tiền đơn, đơn vị VND (chưa nhân 100)
    [string]$BaseUrl = "http://localhost:5203",
    [switch]$Fail
)

$ErrorActionPreference = "Stop"
Set-Location (Split-Path $PSScriptRoot -Parent)

# --- đọc khoá từ user-secrets ---
$secrets = dotnet user-secrets list --project src\ECommerce.API
$hashSecret = ($secrets | Where-Object { $_ -match '^VnPay:HashSecret\s*=' }) -replace '^VnPay:HashSecret\s*=\s*', ''
$tmnCode    = ($secrets | Where-Object { $_ -match '^VnPay:TmnCode\s*=' })    -replace '^VnPay:TmnCode\s*=\s*', ''
if (-not $hashSecret -or -not $tmnCode) { throw "Chua co VnPay:HashSecret / VnPay:TmnCode trong user-secrets." }

# --- tham số IPN giả ---
$txnRef = ([guid]$OrderId).ToString("N")
$code   = if ($Fail) { "24" } else { "00" }

$p = [System.Collections.Generic.SortedDictionary[string,string]]::new([System.StringComparer]::Ordinal)
$p["vnp_Amount"]            = ($Amount * 100).ToString()
$p["vnp_BankCode"]          = "NCB"
$p["vnp_CardType"]          = "ATM"
$p["vnp_OrderInfo"]         = "Thanh toan don hang " + $txnRef.Substring(0, 8)
$p["vnp_PayDate"]           = (Get-Date).ToString("yyyyMMddHHmmss")
$p["vnp_ResponseCode"]      = $code
$p["vnp_TmnCode"]           = $tmnCode
$p["vnp_TransactionNo"]     = "14000000"
$p["vnp_TransactionStatus"] = $code
$p["vnp_TxnRef"]            = $txnRef

# --- ký giống hệt VnPayGateway: sort Ordinal, UrlEncode, HMAC-SHA512 (hex thường) ---
$signData = ($p.GetEnumerator() | ForEach-Object {
    [System.Net.WebUtility]::UrlEncode($_.Key) + "=" + [System.Net.WebUtility]::UrlEncode($_.Value)
}) -join "&"

$hmac = New-Object System.Security.Cryptography.HMACSHA512
$hmac.Key = [System.Text.Encoding]::UTF8.GetBytes($hashSecret)
$hash = ([System.BitConverter]::ToString($hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($signData))) -replace '-', '').ToLower()

$url = "$BaseUrl/api/payments/vnpay/ipn?$signData&vnp_SecureHash=$hash"
Write-Host "Goi IPN gia lap cho don $OrderId (ma phan hoi $code) ..." -ForegroundColor Cyan
$resp = Invoke-RestMethod -Uri $url -Method Get -Headers @{ "ngrok-skip-browser-warning" = "1" }
Write-Host ("Ket qua: RspCode={0}  Message={1}" -f $resp.RspCode, $resp.Message) -ForegroundColor Green
Write-Host "RspCode 00 = da cap nhat don; 02 = don da xac nhan truoc do; 97 = sai chu ky; 04 = sai so tien; 01 = khong thay don/chua tao payment."
