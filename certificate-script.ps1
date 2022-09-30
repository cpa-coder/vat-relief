<#
.Synopsis
   Script to Create Self Signed Certificate use for code signing.
.How to use: 
   Run the script in PowerShell ISE in Admin mode.
.Note:
   Run the following command "Set-ExecutionPolicy RemoteSigned" to ensure execution from PowerShell ISE.
#>

Param(
   [string]$FriendlyName = "DebitExpress Business Solutions",
   [string]$DnsName = "DebitExpress Business Solutions",
   [string]$Subject = "CN=DebitExpress Business Solutions",
   
   [Parameter(Mandatory = $true)]
   [Alias("y")]
   [System.Object]$YearsValid,

   [Parameter(Mandatory = $true)]
   [Alias("p")]
   [string]$Pass,

   [Parameter(Mandatory = $true)]
   [Alias("n")]
   [string]$CertName,

   [Parameter(Mandatory = $true)]
   [Alias("o")]
   [string]$CertFolder,

   [SecureString]$SecurePassword
)

$SecurePassword = ConvertTo-SecureString $Pass -AsPlainText -Force

# Get current date
$ValidFrom = [DateTime]::Now

$ValidUntill = [DateTime]::Now.AddYears($YearsValid)

# Create certificate folder if not exists
if (!(Test-Path $CertFolder)) {
   New-Item -Path $CertFolder -Force -ItemType Directory
}

$CerFilePath = $CertFolder + "\\" + $CertName + ".cer"
$PfxFilePath = $CertFolder + "\\" + $CertName + ".pfx"

# Create Certificate
$Cert = New-SelfSignedCertificate -FriendlyName $FriendlyName -DnsName $DnsName -Type CodeSigning -Subject $Subject -NotBefore $ValidFrom -NotAfter $ValidUntill -KeyExportPolicy Exportable -KeyAlgorithm RSA -KeyLength 2048 -HashAlgorithm "SHA256" -Provider "Microsoft Strong Cryptographic Provider" -CertStoreLocation "Cert:\LocalMachine\My"

#Generate .pfx
Export-PfxCertificate -Cert $Cert -FilePath $PfxFilePath -Password $SecurePassword

#Export to .cer format
Export-Certificate -Type CERT -Cert $Cert -FilePath $CerFilePath

Write-Host Task Completed ! -ForegroundColor Green
