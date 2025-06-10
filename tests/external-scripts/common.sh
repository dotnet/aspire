#!/bin/sh
SAVE=0

usage() {
    echo "Usage: $0 [-s]"
    echo "Generates a valid ASP.NET Core self-signed certificate for the local machine."
    echo "The certificate will be imported into the system's certificate store and into various other places."
    echo "  -s: Also saves the generated crtfile to the home directory"
    exit 1
}

while getopts "sh" opt
do
    case "$opt" in
        s)
            SAVE=1
            ;;
        h)
            usage
            exit 1
            ;;
        *)
            ;;
    esac
done

TMP_PATH=/var/tmp/localhost-dev-cert
if [ ! -d $TMP_PATH ]; then
    mkdir $TMP_PATH
fi

cleanup() {
    rm -R $TMP_PATH
}

KEYFILE=$TMP_PATH/dotnet-devcert.key
CRTFILE=$TMP_PATH/dotnet-devcert.crt
PFXFILE=$TMP_PATH/dotnet-devcert.pfx

NSSDB_PATHS="$HOME/.pki/nssdb \
    $HOME/snap/chromium/current/.pki/nssdb \
    $HOME/snap/postman/current/.pki/nssdb"

CONF_PATH=$TMP_PATH/localhost.conf
cat >> $CONF_PATH <<EOF
[req]
prompt                  = no
default_bits            = 2048
distinguished_name      = subject
req_extensions          = req_ext
x509_extensions         = x509_ext

[ subject ]
commonName              = localhost

[req_ext]
basicConstraints        = critical, CA:true
subjectAltName          = @alt_names

[x509_ext]
basicConstraints        = critical, CA:true
keyUsage                = critical, keyCertSign, cRLSign, digitalSignature,keyEncipherment
extendedKeyUsage        = critical, serverAuth
subjectAltName          = critical, @alt_names
1.3.6.1.4.1.311.84.1.1  = ASN1:UTF8String:ASP.NET Core HTTPS development certificate # Needed to get it imported by dotnet dev-certs

[alt_names]
DNS.1                   = localhost
EOF

configure_nssdb() {
    echo "Configuring nssdb for $1"
    certutil -d sql:"$1" -D -n dotnet-devcert
    certutil -d sql:"$1" -A -t "CP,," -n dotnet-devcert -i $CRTFILE
}

openssl req -x509 -nodes -days 365 -newkey rsa:2048 -keyout $KEYFILE -out $CRTFILE -config $CONF_PATH --passout pass:
openssl pkcs12 -export -out $PFXFILE -inkey $KEYFILE -in $CRTFILE --passout pass:

for NSSDB in $NSSDB_PATHS; do
    if [ -d "$NSSDB" ]; then
        configure_nssdb "$NSSDB"
    fi
done

if [ "$(id -u)" -ne 0 ]; then
    # shellcheck disable=SC2034 # SUDO will be used in parent scripts.
    SUDO='sudo'
fi

dotnet dev-certs https --clean --import $PFXFILE -p ""

if [ "$SAVE" = 1 ]; then
   cp $CRTFILE $HOME
   echo "Saved certificate to $HOME/$(basename $CRTFILE)"
   cp $PFXFILE $HOME
   echo "Saved certificate to $HOME/$(basename $PFXFILE)"
fi
