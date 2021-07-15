﻿using System;
using System.IO;
using System.Text;
using System.Xml;
using TableCloth.Models;

namespace TableCloth
{
    static class SandboxBuilder
    {
		public static string GenerateSandboxConfiguration(string outputDirectory, SandboxConfiguration config)
		{
			if (config == null)
				throw new ArgumentNullException("config");

			if (!Directory.Exists(outputDirectory))
				Directory.CreateDirectory(outputDirectory);

			var assetsDirectory = Path.Combine(outputDirectory, "assets");
			if (!Directory.Exists(assetsDirectory))
				Directory.CreateDirectory(assetsDirectory);

			var batchFileContent = GenerateSandboxStartupScript(config);
			var batchFilePath = Path.Combine(assetsDirectory, "StartupScript.cmd");
			File.WriteAllText(batchFilePath, batchFileContent, Encoding.Default);

			var wsbFileContent = GenerateSandboxSpecDocument(config, assetsDirectory);
			var wsbFilePath = Path.Combine(outputDirectory, "InternetBankingSandbox.wsb");
			wsbFileContent.Save(wsbFilePath);

			return wsbFilePath;
		}

		public static string GenerateSandboxStartupScript(SandboxConfiguration config)
		{
			if (config == null)
				throw new ArgumentNullException("config");

			var buffer = new StringBuilder();
			var service = config.SelectedService;

			if (service != null)
            {
				foreach (var eachPackage in service.Packages)
                {
					string localFileName;
					try { localFileName = Path.GetFileName(new Uri(eachPackage.Value, UriKind.Absolute).LocalPath); }
					catch { localFileName = Guid.NewGuid().ToString("n") + ".exe"; }

					buffer.AppendLine($@"REM Run {eachPackage.Key} Setup");
					buffer.AppendLine($@"curl -L ""{eachPackage.Value}"" --output ""%temp%\{localFileName}""");
					buffer.AppendLine($@"start %temp%\{localFileName}");
					buffer.AppendLine();
				}

				buffer.AppendLine($@"start {service.HomepageUrl}");
			}

			return buffer.ToString();
		}

		public static XmlDocument GenerateSandboxSpecDocument(SandboxConfiguration config, string assetsDirectoryPath)
		{
			if (config == null)
				throw new ArgumentNullException("config");

			var doc = new XmlDocument();
			doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));
			var configurationElem = doc.CreateElement("Configuration");
			{
				var mappedFoldersElem = doc.CreateElement("MappedFolders");
				{
					var mappedFolderElem = doc.CreateElement("MappedFolder");
					{
						var hostFolderElem = doc.CreateElement("HostFolder");
						hostFolderElem.InnerText = assetsDirectoryPath;
						mappedFolderElem.AppendChild(hostFolderElem);

						var sandboxFolderElem = doc.CreateElement("SandboxFolder");
						sandboxFolderElem.InnerText = @"C:\assets";
						mappedFolderElem.AppendChild(sandboxFolderElem);

						var readOnlyElem = doc.CreateElement("ReadOnly");
						readOnlyElem.InnerText = Boolean.TrueString.ToString();
						mappedFolderElem.AppendChild(readOnlyElem);
					}
					mappedFoldersElem.AppendChild(mappedFolderElem);

					if (config.CertPair != null)
					{
						var mappedNpkiFolderElem = doc.CreateElement("MappedFolder");
						{
							var certAssetsDirectoryPath = Path.Combine(assetsDirectoryPath, "certs");
							if (!Directory.Exists(certAssetsDirectoryPath))
								Directory.CreateDirectory(certAssetsDirectoryPath);

							var destDerFilePath = Path.Combine(
								certAssetsDirectoryPath,
								Path.GetFileName(config.CertPair.DerFilePath));
							File.Copy(config.CertPair.DerFilePath, destDerFilePath, true);

							var destKeyFileName = Path.Combine(
								certAssetsDirectoryPath,
								Path.GetFileName(config.CertPair.KeyFilePath));
							File.Copy(config.CertPair.KeyFilePath, destKeyFileName, true);

							var hostFolderElem = doc.CreateElement("HostFolder");
							hostFolderElem.InnerText = certAssetsDirectoryPath;
							mappedNpkiFolderElem.AppendChild(hostFolderElem);

							var candidatePath = Path.Join("AppData", "LocalLow", "NPKI", config.CertPair.SubjectOrganization);
							if (config.CertPair.IsPersonalCert)
								candidatePath = Path.Join(candidatePath, "USER", config.CertPair.SubjectNameForNpkiApp);
							candidatePath = Path.Join(@"C:\Users\WDAGUtilityAccount", candidatePath);

							var sandboxFolderElem = doc.CreateElement("SandboxFolder");
							sandboxFolderElem.InnerText = candidatePath;

							mappedNpkiFolderElem.AppendChild(sandboxFolderElem);

							var readOnlyElem = doc.CreateElement("ReadOnly");
							readOnlyElem.InnerText = Boolean.FalseString.ToString();
							mappedNpkiFolderElem.AppendChild(readOnlyElem);
						}
						mappedFoldersElem.AppendChild(mappedNpkiFolderElem);
					}
				}
				configurationElem.AppendChild(mappedFoldersElem);

				var logonCommandElem = doc.CreateElement("LogonCommand");
				{
					var commandElem = doc.CreateElement("Command");
					commandElem.InnerText = @"C:\assets\StartupScript.cmd";
					logonCommandElem.AppendChild(commandElem);
				}
				configurationElem.AppendChild(logonCommandElem);
			}
			doc.AppendChild(configurationElem);

			return doc;
		}
	}
}
