using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PangyaLeagueLauncher;

internal static class Program
{
	private sealed class SincronoProgress : IProgress<Progresso>
	{
		private readonly Action<Progresso> _a;

		public SincronoProgress(Action<Progresso> a)
		{
			_a = a;
		}

		public void Report(Progresso p)
		{
			lock (this)
			{
				_a(p);
			}
		}
	}

	private static Mutex _unico;

	private static string Pasta => AppDomain.CurrentDomain.BaseDirectory;

	private static string MeuExe => Process.GetCurrentProcess().MainModule.FileName;

	[STAThread]
	private static int Main(string[] args)
	{
		Idioma.Carregar(Pasta);
		ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
		if (Array.IndexOf(args, "--teste") >= 0)
		{
			return Teste(Array.IndexOf(args, "--completo") >= 0);
		}
		_unico = new Mutex(initiallyOwned: true, "PangyaLeagueLauncher-" + Pasta.ToLowerInvariant().GetHashCode(), out var createdNew);
		if (!createdNew)
		{
			try
			{
				if (!_unico.WaitOne(TimeSpan.FromSeconds(5.0)))
				{
					return 0;
				}
			}
			catch (AbandonedMutexException)
			{
			}
		}
		try
		{
			try
			{
				if (File.Exists(MeuExe + ".antigo"))
				{
					File.Delete(MeuExe + ".antigo");
				}
			}
			catch
			{
			}
			ServicePointManager.DefaultConnectionLimit = 8;
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(defaultValue: false);
			Application.Run(new FrmLauncher(Config.Carregar(Pasta), Pasta, Array.IndexOf(args, "--demo") >= 0));
		}
		finally
		{
			Liberar();
		}
		return 0;
	}

	private static int Teste(bool completo)
	{
		string path = Path.Combine(Pasta, "launcher_teste.log");
		StreamWriter w = new StreamWriter(path, append: false, Encoding.UTF8)
		{
			AutoFlush = true
		};
		try
		{
			string ultimo = null;
			SincronoProgress prog = new SincronoProgress((Progresso p) =>
			{
				if (p.Texto != ultimo)
				{
					ultimo = p.Texto;
					w.WriteLine("{0:HH:mm:ss} {1,5:P0} {2}", DateTime.Now, p.Geral, p.Texto);
				}
			});
			Config config = Config.Carregar(Pasta);
			w.WriteLine("PatchUrl=" + config.PatchUrl + " pasta=" + Pasta + " completo=" + completo);
			Resultado result = new Atualizador(config, Pasta).ExecutarAsync(completo, prog, CancellationToken.None).GetAwaiter().GetResult();
			w.WriteLine("RESULTADO ok={0} atualizados={1} launcherNovo={2} semZip={3} erro={4}", result.Ok, result.Atualizados, result.LauncherNovo, result.SemZip.Count, result.Erro);
			foreach (string item in result.SemZip)
			{
				w.WriteLine("  sem zip: " + item);
			}
			return (!result.Ok) ? 2 : 0;
		}
		finally
		{
			if (w != null)
			{
				((IDisposable)w).Dispose();
			}
		}
	}

	private static void Liberar()
	{
		try
		{
			_unico?.ReleaseMutex();
		}
		catch
		{
		}
		_unico?.Dispose();
		_unico = null;
	}

	public static void TrocarLauncherEReiniciar()
	{
		try
		{
			string meuExe = MeuExe;
			string text = meuExe + ".antigo";
			if (File.Exists(text))
			{
				File.Delete(text);
			}
			File.Move(meuExe, text);
			File.Move(meuExe + ".novo", meuExe);
			Liberar();
			Process.Start(new ProcessStartInfo(meuExe)
			{
				WorkingDirectory = Pasta,
				UseShellExecute = true
			});
		}
		catch (Exception ex)
		{
			MessageBox.Show("Não foi possível trocar o launcher: " + ex.Message, "Pangya League", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
		Application.Exit();
	}
}
