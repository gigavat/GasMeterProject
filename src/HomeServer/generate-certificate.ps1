# PowerShell script to generate certificate for Windows

# Create https directory if it doesn't exist
if (-not (Test-Path "https")) {
  New-Item -ItemType Directory -Path "https"
}

# Check if OpenSSL is available
$opensslPath = Get-Command openssl -ErrorAction SilentlyContinue

if (-not $opensslPath) {
  Write-Host "OpenSSL is not installed or not in PATH."
  Write-Host "Please install OpenSSL or use WSL/Git Bash to run generate-certificate.sh"
  exit 1
}

# Find OpenSSL config file in common locations
$opensslConfig = $null
$possibleConfigPaths = @(
  
  "$env:ProgramFiles\OpenSSL-Win64\bin\openssl.cfg",
  "$env:ProgramFiles\OpenSSL-Win32\bin\openssl.cfg",
  "$env:ProgramFiles (x86)\OpenSSL-Win32\bin\openssl.cfg",
  "$env:ProgramFiles\Common Files\SSL\openssl.cnf",
  "$env:SystemDrive\OpenSSL-Win64\bin\openssl.cfg",
  "$env:SystemDrive\OpenSSL-Win32\bin\openssl.cfg"
)

foreach ($path in $possibleConfigPaths) {
  if (Test-Path $path) {
      $opensslConfig = $path
      break
  }
}

# Create minimal config if not found
if (-not $opensslConfig) {
  $minimalConfig = @"
[req]
distinguished_name = req_distinguished_name
[req_distinguished_name]
[v3_req]
basicConstraints = CA:FALSE
keyUsage = nonRepudiation, digitalSignature, keyEncipherment
"@
  $configPath = Join-Path $PSScriptRoot "openssl-minimal.cnf"
  $minimalConfig | Out-File -FilePath $configPath -Encoding ASCII
  $opensslConfig = $configPath
  Write-Host "Created minimal OpenSSL config at: $configPath"
}

# Generate self-signed certificate
& openssl req -x509 -newkey rsa:4096 -keyout https/aspnetapp.key -out https/aspnetapp.crt -days 365 -nodes `
-config $opensslConfig `
-subj "/C=US/ST=State/L=City/O=Organization/CN=localhost"

# Convert to PFX format for .NET
& openssl pkcs12 -export -out https/aspnetapp.pfx -inkey https/aspnetapp.key -in https/aspnetapp.crt `
-passout pass:YourCertificatePassword123!

# Create OpenSSL format certificate (PEM format)
Copy-Item https/aspnetapp.crt https/aspnetapp.pem

Write-Host "Certificate generated successfully!"
Write-Host "PFX file: https/aspnetapp.pfx"
Write-Host "PEM file (OpenSSL format): https/aspnetapp.pem"
Write-Host "Key file: https/aspnetapp.key"