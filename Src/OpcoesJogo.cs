using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace PangyaLeagueLauncher;

internal static class OpcoesJogo
{
	public sealed class Dados
	{
		public bool Janela = true;

		public int Largura = 800;

		public int Altura = 600;

		public int Graficos = -1;

		public bool Som = true;

		public Dados Copia()
		{
			return (Dados)MemberwiseClone();
		}

		public bool Igual(Dados o)
		{
			if (o != null && o.Janela == Janela && o.Largura == Largura && o.Altura == Altura && o.Graficos == Graficos)
			{
				return o.Som == Som;
			}
			return false;
		}
	}

	private const string Chave = "SOFTWARE\\Ntreev\\Pangya_JP\\Option";

	public static readonly int[][] Resolucoes = new int[7][]
	{
		new int[2] { 800, 600 },
		new int[2] { 1024, 768 },
		new int[2] { 1280, 720 },
		new int[2] { 1280, 1024 },
		new int[2] { 1366, 768 },
		new int[2] { 1600, 900 },
		new int[2] { 1920, 1080 }
	};

	public static readonly string[] NomesGraficos = new string[4] { "Off", "Low", "High", "Ultra" };

	private static readonly string[] ChavesGraficos = new string[10] { "vEffectLevel", "vProjectionShadow", "vShadowmap", "vReflection", "viHdr", "viHdrStarEffect", "viDof", "viRimLight", "viAmbientControl", "viFXAA" };

	private static readonly string[][] ValoresGraficos = new string[4][]
	{
		new string[10] { "0", "0", "0", "0", "0", "0", "0", "0", "0", "0" },
		new string[10] { "1", "1", "0", "0", "0", "0", "0", "0", "0", "0" },
		new string[10] { "1", "1", "1", "1", "0", "0", "0", "1", "1", "1" },
		new string[10] { "1", "1", "1", "1", "1", "1", "1", "1", "1", "1" }
	};

	private static RegistryKey Abrir()
	{
		return RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey("SOFTWARE\\Ntreev\\Pangya_JP\\Option");
	}

	private static string Valor(RegistryKey k, string nome, string padrao)
	{
		return (k?.GetValue(nome) as string) ?? padrao;
	}

	public static Dados Ler()
	{
		Dados dados = new Dados();
		try
		{
			RegistryKey k = Abrir();
			try
			{
				dados.Janela = Valor(k, "vWindowMode", "1") != "0";
				int.TryParse(Valor(k, "vScreenWidth", "800"), out dados.Largura);
				int.TryParse(Valor(k, "vScreenHeight", "600"), out dados.Altura);
				if (dados.Largura < 640 || dados.Altura < 480)
				{
					dados.Largura = 800;
					dados.Altura = 600;
				}
				dados.Som = Valor(k, "aMssOn", "1") != "0";
				string[] atuais = ChavesGraficos.Select((string c, int i) => Valor(k, c, ValoresGraficos[1][i])).ToArray();
				dados.Graficos = Array.FindIndex(ValoresGraficos, (string[] v) => v.SequenceEqual(atuais));
			}
			finally
			{
				if (k != null)
				{
					((IDisposable)k).Dispose();
				}
			}
		}
		catch
		{
		}
		return dados;
	}

	public static string Salvar(Dados d)
	{
		string text = null;
		try
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Windows Registry Editor Version 5.00");
			sb.AppendLine();
			if (!Registro.Existe())
			{
				sb.Append(Registro.TextoReg());
			}
			sb.AppendLine("[HKEY_LOCAL_MACHINE\\SOFTWARE\\Ntreev\\Pangya_JP\\Option]");
			V("vWindowMode", d.Janela ? "1" : "0");
			V("vScreenWidth", d.Largura.ToString());
			V("vScreenHeight", d.Altura.ToString());
			V("vWideMode", ((double)d.Largura / (double)d.Altura > 1.4) ? "1" : "0");
			V("aMssOn", d.Som ? "1" : "0");
			if (d.Graficos >= 0)
			{
				V("vOverall", "-1");
				for (int i = 0; i < ChavesGraficos.Length; i++)
				{
					V(ChavesGraficos[i], ValoresGraficos[d.Graficos][i]);
				}
			}
			text = Path.Combine(Path.GetTempPath(), "pangya_league_opcoes.reg");
			File.WriteAllText(text, sb.ToString(), Encoding.Unicode);
			using (Process process = Process.Start(new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "reg.exe"), "import \"" + text + "\" /reg:32")
			{
				UseShellExecute = true,
				Verb = "runas",
				WindowStyle = ProcessWindowStyle.Hidden
			}))
			{
				if (process == null)
				{
					return Idioma.T("op_erro", "reg.exe");
				}
				process.WaitForExit(30000);
			}
			return (Ler().Igual(d) || (d.Graficos < 0 && ConfereSemGraficos(d))) ? null : Idioma.T("op_erro", "registro");
			void V(string n, string v)
			{
				sb.AppendLine("\"" + n + "\"=\"" + v + "\"");
			}
		}
		catch (Win32Exception)
		{
			return Idioma.T("op_permissao");
		}
		catch (Exception ex2)
		{
			return Idioma.T("op_erro", ex2.Message);
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

	private static bool ConfereSemGraficos(Dados d)
	{
		Dados dados = Ler();
		if (dados.Janela == d.Janela && dados.Largura == d.Largura && dados.Altura == d.Altura)
		{
			return dados.Som == d.Som;
		}
		return false;
	}
}
