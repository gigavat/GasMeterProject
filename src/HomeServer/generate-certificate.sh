#!/bin/bash

# Create https directory if it doesn't exist
mkdir -p https

# Generate self-signed certificate
openssl req -x509 -newkey rsa:4096 -keyout https/aspnetapp.key -out https/aspnetapp.crt -days 365 -nodes \
  -subj "/C=US/ST=State/L=City/O=Organization/CN=localhost"

# Convert to PFX format for .NET
openssl pkcs12 -export -out https/aspnetapp.pfx -inkey https/aspnetapp.key -in https/aspnetapp.crt \
  -passout pass:YourCertificatePassword123!

# Create OpenSSL format certificate (PEM format)
cp https/aspnetapp.crt https/aspnetapp.pem

echo "Certificate generated successfully!"
echo "PFX file: https/aspnetapp.pfx"
echo "PEM file (OpenSSL format): https/aspnetapp.pem"
echo "Key file: https/aspnetapp.key"

