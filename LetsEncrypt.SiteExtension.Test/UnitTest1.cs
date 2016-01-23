using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Cms;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace LetsEncrypt.SiteExtension.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            // this snippet can be easily used in any normal c# project as well by simply removing the .Dump() methods
            // and saving the output into a variable / file of your choice.
            // you'll need to add BouncyCastle.Crypto nuget to use this.
            // tested on 1.8.0-beta4

            AsymmetricCipherKeyPair pair;
            Pkcs10CertificationRequest csr;

            var ecMode = false;
            var values = new Dictionary<DerObjectIdentifier, string> {
    {X509Name.CN, ""}, //domain name
    {X509Name.OU, "Domain Control Validated"},
    {X509Name.O, ""}, //Organisation's Legal name
    {X509Name.L, "London"},
    {X509Name.ST, "England"},
    {X509Name.C, "GB"},
};

            var subjectAlternateNames = new GeneralName[] { };

            var extensions = new Dictionary<DerObjectIdentifier, X509Extension>()
{
    {X509Extensions.BasicConstraints, new X509Extension(true, new DerOctetString(new BasicConstraints(false)))},
    {X509Extensions.KeyUsage, new X509Extension(true, new DerOctetString(new KeyUsage(KeyUsage.DigitalSignature | KeyUsage.KeyEncipherment | KeyUsage.DataEncipherment | KeyUsage.NonRepudiation)))},
    {X509Extensions.ExtendedKeyUsage, new X509Extension(false, new DerOctetString(new ExtendedKeyUsage(KeyPurposeID.IdKPServerAuth)))},
};

            if (values[X509Name.CN].StartsWith("www.")) values[X509Name.CN] = values[X509Name.CN].Substring(4);

            if (!values[X509Name.CN].StartsWith("*.") && subjectAlternateNames.Length == 0)
                subjectAlternateNames = new GeneralName[] { new GeneralName(GeneralName.DnsName, $"www.{values[X509Name.CN]}") };

            if (subjectAlternateNames.Length > 0) extensions.Add(X509Extensions.SubjectAlternativeName, new X509Extension(false, new DerOctetString(new GeneralNames(subjectAlternateNames))));

            var subject = new X509Name(values.Keys.Reverse().ToList(), values);

            if (ecMode)
            {
                var gen = new ECKeyPairGenerator();
                var ecp = Org.BouncyCastle.Asn1.Sec.SecNamedCurves.GetByName("secp384r1");
                gen.Init(new ECKeyGenerationParameters(new ECDomainParameters(ecp.Curve, ecp.G, ecp.N, ecp.H, ecp.GetSeed()), new SecureRandom()));

                pair = gen.GenerateKeyPair();

                extensions.Add(X509Extensions.SubjectKeyIdentifier, new X509Extension(false, new DerOctetString(new SubjectKeyIdentifierStructure(pair.Public))));
                csr = new Pkcs10CertificationRequest("SHA256withECDSA", subject, pair.Public, new DerSet(new AttributePkcs(PkcsObjectIdentifiers.Pkcs9AtExtensionRequest, new DerSet(new X509Extensions(extensions)))), pair.Private);

            }
            else
            {
                var gen = new RsaKeyPairGenerator();
                gen.Init(new KeyGenerationParameters(new SecureRandom(), 2048));

                pair = gen.GenerateKeyPair();
                extensions.Add(X509Extensions.SubjectKeyIdentifier, new X509Extension(false, new DerOctetString(new SubjectKeyIdentifierStructure(pair.Public))));
                csr = new Pkcs10CertificationRequest("SHA256withRSA", subject, pair.Public, new DerSet(new AttributePkcs(PkcsObjectIdentifiers.Pkcs9AtExtensionRequest, new DerSet(new X509Extensions(extensions)))), pair.Private);
            }

            //Convert BouncyCastle csr to .PEM file.
            var csrPem = new StringBuilder();
            var csrPemWriter = new PemWriter(new StringWriter(csrPem));
            csrPemWriter.WriteObject(csr);
            csrPemWriter.Writer.Flush();

            Console.WriteLine(csrPem.ToString());

            var privateKeyPem = new StringBuilder();
            var privateKeyPemWriter = new PemWriter(new StringWriter(privateKeyPem));
            privateKeyPemWriter.WriteObject(pair.Private);
            csrPemWriter.Writer.Flush();

            //privateKeyPem.ToString().Dump("Private Key");
        }
    }
}
