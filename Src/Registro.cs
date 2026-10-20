using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace PangyaLeagueLauncher;

internal static class Registro
{
	private const string Chave = "SOFTWARE\\Ntreev\\Pangya_JP";

	private static readonly string[,] Valores = new string[7, 2]
	{
		{ "Ver", "JP.R7.983.00" },
		{ "PatchNum", "419" },
		{ "IntegratedPak", "ProjectG800j+.pak" },
		{ "Launcher", "update.exe" },
		{ "Patcher", "update.exe" },
		{ "Launcher Version", "v5.01" },
		{ "Argument", "not_used" }
	};

	public static string TextoReg()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("[HKEY_LOCAL_MACHINE\\SOFTWARE\\Ntreev\\Pangya_JP]");
		for (int i = 0; i < Valores.GetLength(0); i++)
		{
			stringBuilder.AppendLine("\"" + Valores[i, 0] + "\"=\"" + Valores[i, 1] + "\"");
		}
		stringBuilder.AppendLine();
		return stringBuilder.ToString();
	}

	public static bool Existe()
	{
		try
		{
			using RegistryKey registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
			using RegistryKey registryKey2 = registryKey.OpenSubKey("SOFTWARE\\Ntreev\\Pangya_JP");
			return registryKey2 != null && !string.IsNullOrEmpty(registryKey2.GetValue("Ver") as string);
		}
		catch
		{
			return false;
		}
	}

	public static string Garantir()
	{
		if (Existe())
		{
			return null;
		}
		string text = null;
		try
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("Windows Registry Editor Version 5.00");
			stringBuilder.AppendLine();
			stringBuilder.Append(TextoReg());
			text = Path.Combine(Path.GetTempPath(), "pangya_league_registro.reg");
			File.WriteAllText(text, stringBuilder.ToString(), Encoding.Unicode);
			using (Process process = Process.Start(new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "reg.exe"), "import \"" + text + "\" /reg:32")
			{
				UseShellExecute = true,
				Verb = "runas",
				WindowStyle = ProcessWindowStyle.Hidden
			}))
			{
				if (process == null)
				{
					return Idioma.T("reg_preparar");
				}
				process.WaitForExit(30000);
			}
			return Existe() ? null : Idioma.T("reg_gravar");
		}
		catch (Win32Exception)
		{
			return Idioma.T("reg_permissao");
		}
		catch (Exception ex2)
		{
			return Idioma.T("reg_erro", ex2.Message);
		}
		finally
		{
			try
			{
				if (text != null)
				{
					File.Delete(text);
				}
			}
			catch
			{
			}
		}
	}
}
