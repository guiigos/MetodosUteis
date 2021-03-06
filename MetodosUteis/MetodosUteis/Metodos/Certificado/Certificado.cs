﻿using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;

namespace MetodosUteis
{
    /*********************************************************************************
    * 
    * Classe: Certificado
    * Descrição: Métodos para manipulação de certificados digitais
    * 
    * Guilherme Alves
    * guiigos.alves@gmail.com
    * http://guiigos.com
    * 
    *********************************************************************************/

    public class Certificado
    {
        public static string AssinarXmlPorElemento(X509Certificate2 certificado, string conteudoXML, string tag, bool assinarTagRps)
        {
            try
            {
                bool tagId = false;
                XmlDocument doc = Xml.StringToXmlDocument(conteudoXML);

                XmlNodeList listaTags = doc.GetElementsByTagName(tag);
                SignedXml signedXml;

                foreach (XmlElement infNfse in listaTags)
                {
                    string id = string.Empty;
                    if (infNfse.HasAttribute("id"))
                    {
                        id = infNfse.Attributes.GetNamedItem("id").Value;
                    }
                    else if (infNfse.HasAttribute("Id"))
                    {
                        id = infNfse.Attributes.GetNamedItem("Id").Value;
                        tagId = true;
                    }
                    else if (infNfse.HasAttribute("ID"))
                    {
                        id = infNfse.Attributes.GetNamedItem("ID").Value;
                    }
                    else if (infNfse.HasAttribute("iD"))
                    {
                        id = infNfse.Attributes.GetNamedItem("iD").Value;
                    }
                    else
                    {
                        tagId = false;

                        if (assinarTagRps)
                            continue;
                    }

                    signedXml = new SignedXml(infNfse);
                    signedXml.SigningKey = certificado.PrivateKey;

                    Reference reference = new Reference("#" + id);
                    if (!tagId) { reference.Uri = string.Empty; }

                    reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
                    reference.AddTransform(new XmlDsigC14NTransform());

                    signedXml.AddReference(reference);
                    KeyInfo keyInfo = new KeyInfo();
                    keyInfo.AddClause(new KeyInfoX509Data(certificado));
                    signedXml.KeyInfo = keyInfo;

                    signedXml.ComputeSignature();

                    XmlElement xmlSignature = doc.CreateElement("Signature", "http://www.w3.org/2000/09/xmldsig#");

                    if (tagId)
                    {
                        XmlAttribute xmlAttribute = doc.CreateAttribute("Id");
                        xmlAttribute.Value = "Ass_" + id.Replace(":", "_");
                        xmlSignature.Attributes.InsertAfter(xmlAttribute, xmlSignature.GetAttributeNode("xmlns"));
                    }

                    XmlElement xmlSignedInfo = signedXml.SignedInfo.GetXml();
                    XmlElement xmlKeyInfo = signedXml.KeyInfo.GetXml();
                    XmlElement xmlSignatureValue = doc.CreateElement("SignatureValue", xmlSignature.NamespaceURI);
                    string signBase64 = Convert.ToBase64String(signedXml.Signature.SignatureValue);

                    XmlText xmlText = doc.CreateTextNode(signBase64);
                    xmlSignatureValue.AppendChild(xmlText);

                    xmlSignature.AppendChild(doc.ImportNode(xmlSignedInfo, true));
                    xmlSignature.AppendChild(xmlSignatureValue);
                    xmlSignature.AppendChild(doc.ImportNode(xmlKeyInfo, true));

                    if (assinarTagRps) infNfse.ParentNode.ParentNode.AppendChild(xmlSignature);
                    else infNfse.ParentNode.AppendChild(xmlSignature);
                }

                string conteudoXmlAssinado = Xml.XmlDocumentToString(doc);
                return conteudoXmlAssinado;
            }
            catch (Exception ex)
            {
                throw new CustomException(MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        public static string AssinarXML(X509Certificate2 certificado, string conteudoXML)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                RSACryptoServiceProvider key = new RSACryptoServiceProvider();

                SignedXml signedDocument;
                KeyInfo keyInfo = new KeyInfo();

                doc.LoadXml(conteudoXML);
                key = (RSACryptoServiceProvider)certificado.PrivateKey;

                keyInfo.AddClause(new KeyInfoX509Data(certificado));
                signedDocument = new SignedXml(doc);
                signedDocument.SigningKey = key;
                signedDocument.KeyInfo = keyInfo;

                Reference reference = new Reference();
                reference.Uri = string.Empty;

                reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
                reference.AddTransform(new XmlDsigC14NTransform(false));

                signedDocument.AddReference(reference);
                signedDocument.ComputeSignature();

                XmlElement xmlDigitalSignature = signedDocument.GetXml();
                doc.DocumentElement.AppendChild(doc.ImportNode(xmlDigitalSignature, true));

                return doc.OuterXml;
            }
            catch (Exception ex)
            {
                throw new CustomException(MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        public static string X509CertificateToBase64(X509Certificate2 certificado)
        {
            try
            {
                byte[] rsaPublicKey = certificado.RawData;
                string base64 = Convert.ToBase64String(rsaPublicKey);

                return base64;
            }
            catch (Exception ex)
            {
                throw new CustomException(MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        public static string FileToBase64(string caminho, string senha)
        {
            try
            {
                var bytesCertificado = File.ReadAllBytes(caminho);
                var certificado = new X509Certificate2(bytesCertificado, senha, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

                var builder = new StringBuilder();
                builder.AppendLine(Convert.ToBase64String(certificado.Export(X509ContentType.Pkcs12, senha), Base64FormattingOptions.None));

                return builder.ToString();
            }
            catch (Exception ex)
            {
                throw new CustomException(MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        public static string EncryptMessage(X509Certificate2 certificado, string mensagem)
        {
            try
            {
                if (!certificado.Verify()) return string.Empty;

                RSACryptoServiceProvider rsaEncryptor = (RSACryptoServiceProvider)certificado.PrivateKey;
                byte[] cipherData = rsaEncryptor.Encrypt(Encoding.UTF8.GetBytes(mensagem), true);
                return Convert.ToBase64String(cipherData);
            }
            catch (Exception ex)
            {
                throw new CustomException(MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        public static string DescryptMessage(X509Certificate2 certificado, string mensagem)
        {
            try
            {
                if (!certificado.Verify()) return string.Empty;

                RSACryptoServiceProvider rsaEncryptor = (RSACryptoServiceProvider)certificado.PrivateKey;
                byte[] plainData = rsaEncryptor.Decrypt(Convert.FromBase64String(mensagem), true);
                return Encoding.UTF8.GetString(plainData);
            }
            catch (Exception ex)
            {
                throw new CustomException(MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        public static string RetornarThumbprint()
        {
            try
            {
                X509Certificate2 X509Cert = new X509Certificate2();
                X509Store store = new X509Store("MY", StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

                X509Certificate2Collection collection = (X509Certificate2Collection)store.Certificates;
                X509Certificate2Collection collection1 = (X509Certificate2Collection)collection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);
                X509Certificate2Collection collection2 = (X509Certificate2Collection)collection.Find(X509FindType.FindByKeyUsage, X509KeyUsageFlags.DigitalSignature, false);

                X509Certificate2Collection scollection = X509Certificate2UI.SelectFromCollection(collection2, "Certificado(s) Digital(is) disponível(is)", "Selecione o Certificado Digital para uso no aplicativo", X509SelectionFlag.SingleSelection);
                if (scollection.Count == 0) X509Cert.Reset();
                else X509Cert = scollection[0];
                store.Close();

                return X509Cert.Thumbprint;
            }
            catch (Exception ex)
            {
                throw new CustomException(MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        public static string RetornarNome()
        {
            try
            {
                X509Certificate2 X509Cert = new X509Certificate2();
                X509Store store = new X509Store("MY", StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

                X509Certificate2Collection collection = (X509Certificate2Collection)store.Certificates;
                X509Certificate2Collection collection1 = (X509Certificate2Collection)collection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);
                X509Certificate2Collection collection2 = (X509Certificate2Collection)collection.Find(X509FindType.FindByKeyUsage, X509KeyUsageFlags.DigitalSignature, false);

                X509Certificate2Collection scollection = X509Certificate2UI.SelectFromCollection(collection2, "Certificado(s) Digital(is) disponível(is)", "Selecione o Certificado Digital para uso no aplicativo", X509SelectionFlag.SingleSelection);
                if (scollection.Count == 0) X509Cert.Reset();
                else X509Cert = scollection[0];
                store.Close();

                return X509Cert.Subject;
            }
            catch (Exception ex)
            {
                throw new CustomException(MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        public static bool Vencido(X509Certificate2 certificado)
        {
            try
            {
                string subject = certificado.Subject;
                DateTime validadeInicial = certificado.NotBefore;
                DateTime validadeFinal = certificado.NotAfter;

                return DateTime.Compare(DateTime.Now, validadeFinal) > 0;
            }
            catch (Exception ex)
            {
                throw new CustomException(MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        public static X509Certificate2 SelecionarPorNome(string nome)
        {
            try
            {
                X509Certificate2 X509Cert = new X509Certificate2();
                X509Store store = new X509Store("MY", StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

                X509Certificate2Collection collection = (X509Certificate2Collection)store.Certificates;
                X509Certificate2Collection collection1 = (X509Certificate2Collection)collection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);
                X509Certificate2Collection collection2 = (X509Certificate2Collection)collection.Find(X509FindType.FindByKeyUsage, X509KeyUsageFlags.DigitalSignature, false);

                if (string.IsNullOrEmpty(nome.Trim()))
                {
                    X509Certificate2Collection scollection = X509Certificate2UI.SelectFromCollection(collection2, "Certificado(s) Digital(is) disponível(is)", "Selecione o Certificado Digital para uso no aplicativo", X509SelectionFlag.SingleSelection);
                    if (scollection.Count == 0) X509Cert.Reset();
                    else X509Cert = scollection[0];
                }
                else
                {
                    X509Certificate2Collection scollection =
                        (X509Certificate2Collection)collection2.Find(X509FindType.FindBySubjectDistinguishedName, nome, false);

                    if (scollection.Count == 0) X509Cert.Reset();
                    else X509Cert = scollection[0];
                }

                store.Close();
                return X509Cert;
            }
            catch (Exception ex)
            {
                throw new CustomException(MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        public static X509Certificate2 SelecionarPorThumbprint(string thumbprint)
        {
            try
            {
                X509Certificate2 X509Cert = new X509Certificate2();
                X509Store store = new X509Store("MY", StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

                X509Certificate2Collection collection = (X509Certificate2Collection)store.Certificates;
                X509Certificate2Collection collection1 = (X509Certificate2Collection)collection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);
                X509Certificate2Collection collection2 = (X509Certificate2Collection)collection1.Find(X509FindType.FindByKeyUsage, X509KeyUsageFlags.DigitalSignature, false);

                if (thumbprint.Equals(String.Empty))
                {
                    X509Certificate2Collection scollection = X509Certificate2UI.SelectFromCollection(collection2, "Certificado(s) Digital(is) disponível(is)", "Selecione o Certificado Digital para uso no aplicativo", X509SelectionFlag.SingleSelection);

                    if (scollection.Count == 0) X509Cert.Reset();
                    else X509Cert = scollection[0];
                }
                else
                {
                    X509Certificate2Collection scollection = (X509Certificate2Collection)collection2.Find(X509FindType.FindByThumbprint, thumbprint, false);
                    if (scollection.Count == 0) X509Cert.Reset();
                    else X509Cert = scollection[0];
                }

                store.Close();
                return X509Cert;
            }
            catch (Exception ex)
            {
                throw new CustomException(MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        public static X509Certificate2 Base64ToX509Certificate(string certificado)
        {
            try
            {
                byte[] data = Convert.FromBase64String(certificado);
                X509Certificate2 x509certificate = new X509Certificate2(data);
                return x509certificate;
            }
            catch (Exception ex)
            {
                throw new CustomException(MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, ex);
            }
        }
    }
}
