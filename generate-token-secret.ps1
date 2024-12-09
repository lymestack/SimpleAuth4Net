# This script generates a secure TOKEN_SECRET in Base64 format

Write-Host "Generating a secure TOKEN_SECRET..."

# Generate a 64-byte random value and encode it to Base64
$bytes = New-Object byte[] 64
(New-Object System.Security.Cryptography.RNGCryptoServiceProvider).GetBytes($bytes)
$secret = [Convert]::ToBase64String($bytes)

Write-Host "TOKEN_SECRET: $secret"
Write-Host "Copy the above TOKEN_SECRET and use it in your application."
