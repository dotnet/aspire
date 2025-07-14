import { randomBytes } from 'crypto';
import forge from 'node-forge';

export function generateSelfSignedCert(commonName: string = 'localhost') {
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
        { type: 7, ip: '127.0.0.1' }    // IP
      ]
    }
  ]);

  cert.sign(keys.privateKey);

  return {
    key: pki.privateKeyToPem(keys.privateKey),
    cert: pki.certificateToPem(cert)
  };
}

export function generateToken(): string {
    const key = randomBytes(16);
    return key.toString('base64');
}