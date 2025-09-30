import { randomBytes, X509Certificate } from 'crypto';
import forge from 'node-forge';

export function createSelfSignedCert(commonName: string = 'localhost') {
  const pki = forge.pki;
  const keys = pki.rsa.generateKeyPair(2048);
  const cert = pki.createCertificate();
  cert.publicKey = keys.publicKey;
  cert.serialNumber = (Math.floor(Math.random() * 1e16)).toString();
  cert.validity.notBefore = new Date();
  cert.validity.notAfter = new Date();
  cert.validity.notAfter.setFullYear(cert.validity.notBefore.getFullYear() + 1);

  const attrs = [{ name: 'commonName', value: commonName }];
  cert.setSubject(attrs);
  cert.setIssuer(attrs);

  // Add SAN extension for localhost
  cert.setExtensions([
    {
      name: 'subjectAltName',
      altNames: [
        { type: 2, value: 'localhost' }, // DNS
      ]
    }
  ]);

  cert.sign(keys.privateKey);

  const certPem = pki.certificateToPem(cert);
  const x509Cert = new X509Certificate(certPem);

  return {
    key: pki.privateKeyToPem(keys.privateKey),
    cert: certPem,
    certBase64: x509Cert.raw.toString('base64')
  };
}

export function generateToken(): string {
    // 32 bytes is used to provide sufficient entropy for security (2^256 possibilities)
    const key = randomBytes(32);
    return key.toString('base64');
}
