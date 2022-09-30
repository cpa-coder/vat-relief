﻿using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace DebitExpress.VatRelief.Utils;

public sealed class CertificateManager
{
    public void Install()
    {
        try
        {
            var path = Path.Combine(Path.GetTempPath(), "vat-relief");
            Directory.CreateDirectory(path);

            //certificate file should be generated by the ci pipeline
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("DebitExpress.VatRelief.cert.cer");

            if (stream == null) return;

            var filePath = Path.Combine(path, "cert.cer");
            using var fileStream = File.Create(filePath);
            stream.Seek(0, SeekOrigin.Begin);
            stream.CopyTo(fileStream);
            fileStream.Close();

            using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);

            var cert = new X509Certificate2(filePath);
            store.Add(cert);
            store.Close();

            File.Delete(filePath);
        }
        catch (Exception)
        {
            //ignored
        }
    }
}