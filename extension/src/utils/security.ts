import { randomBytes, X509Certificate } from 'crypto';
import forge from 'node-forge';

interface SelfSignedCert {
    key: string;
    cert: string;
    certBase64: string;
}

export async function createSelfSignedCertAsync(commonName: string = 'localhost'): Promise<SelfSignedCert> {
  const pki = forge.pki;
  const keys = await new Promise<forge.pki.rsa.KeyPair>((resolve, reject) => {
    // 4096 bits provides enough entropy. Follows modern industry practice
    pki.rsa.generateKeyPair({ bits: 4096, workers: -1 }, (err, keypair) => {
      if (err) {
        reject(err);
      } else {
        resolve(keypair);
      }
    });
  });

  const cert = pki.createCertificate();
  cert.publicKey = keys.publicKey;
  // Generate a positive serial number as a hexadecimal string
  // X.509 requires serial numbers to be positive integers
  cert.serialNumber = randomBytes(16).toString('hex');
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
