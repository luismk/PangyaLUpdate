using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PangyaLeagueLauncher;

public sealed class Atualizador
{
	public const string Marca = "PangyaLeagueLauncher-v1";

	private readonly Config _cfg;

	private readonly string _pasta;

	private readonly string _meuExe;

	private static readonly HttpClient Http = new HttpClient
	{
		Timeout = TimeSpan.FromMinutes(30.0)
	};

	public Atualizador(Config cfg, string pasta)
	{
		_cfg = cfg;
		_pasta = pasta;
		_meuExe = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);
	}

	public async Task<Resultado> ExecutarAsync(bool completo, IProgress<Progresso> prog, CancellationToken ct)
	{
		Resultado res = new Resultado();
		try
		{
			prog.Report(new Progresso
			{
				Texto = Idioma.T("conectando"),
				Geral = 0.0,
				Arquivo = -1.0
			});
			List<Entrada> list = UpdateList.Ler(UpdateList.Decifrar(await Http.GetByteArrayAsync(_cfg.PatchUrl + "updatelist").ConfigureAwait(continueOnCapturedContext: false)));
			CacheCrc cache = new CacheCrc(_pasta);
			List<Entrada> list2 = new List<Entrada>();
			long totalVerif = list.Sum((Entrada entrada) => Math.Max(0L, entrada.Tamanho));
			long num = 0L;
			foreach (Entrada item in list)
			{
				ct.ThrowIfCancellationRequested();
				string text = Path.Combine(_pasta, item.CaminhoRelativo);
				FileInfo fi = new FileInfo(text);
				bool flag;
				int crc;
				if (!fi.Exists || fi.Length != item.Tamanho)
				{
					flag = true;
				}
				else if (!completo && cache.Tentar(item.CaminhoRelativo, fi, out crc))
				{
					flag = crc != item.Crc;
				}
				else
				{
					long base0 = num;
					string nome = item.Nome;
					int num2 = UpdateList.Fcrc(text, (long lidos) =>
					{
						if ((lidos & 0x7FFFFF) == 0L || lidos == fi.Length)
						{
							prog.Report(new Progresso
							{
								Texto = Idioma.T("verificando_arq", nome),
								Geral = Frac(base0 + lidos, totalVerif) * 0.35,
								Arquivo = Frac(lidos, fi.Length)
							});
						}
					});
					cache.Guardar(item.CaminhoRelativo, fi, num2);
					flag = num2 != item.Crc;
				}
				num += Math.Max(0L, item.Tamanho);
				if (flag)
				{
					list2.Add(item);
				}
				prog.Report(new Progresso
				{
					Texto = Idioma.T("verificando_n", list2.Count),
					Geral = Frac(num, totalVerif) * 0.35,
					Arquivo = -1.0
				});
			}
			cache.Salvar();
			long totalBaixar = list2.Sum((Entrada entrada) => (entrada.TamanhoZip <= 0) ? Math.Max(1L, entrada.Tamanho) : entrada.TamanhoZip);
			long feito = 0L;
			string tmp = Path.Combine(_pasta, "_atualizacao");
			Directory.CreateDirectory(tmp);
			foreach (Entrada e in list2)
			{
				ct.ThrowIfCancellationRequested();
				if (string.IsNullOrEmpty(e.Zip))
				{
					res.SemZip.Add(e.CaminhoRelativo);
					continue;
				}
				long base1 = feito;
				string zipLocal = Path.Combine(tmp, Path.GetFileName(e.Zip));
				if (!(await BaixarAsync(_cfg.PatchUrl + Uri.EscapeDataString(e.Zip).Replace("%2B", "+"), zipLocal, (long lidos, long total) =>
				{
					prog.Report(new Progresso
					{
						Texto = Idioma.T("baixando", e.Nome, Mb(lidos), Mb((total > 0) ? total : e.TamanhoZip)),
						Geral = 0.35 + Frac(base1 + lidos, totalBaixar) * 0.65,
						Arquivo = ((total > 0) ? Frac(lidos, total) : (-1.0))
					});
				}, ct).ConfigureAwait(continueOnCapturedContext: false)))
				{
					res.SemZip.Add(e.CaminhoRelativo);
					continue;
				}
				prog.Report(new Progresso
				{
					Texto = Idioma.T("instalando", e.Nome),
					Geral = 0.35 + Frac(base1, totalBaixar) * 0.65,
					Arquivo = -1.0
				});
				string text2 = Path.Combine(tmp, e.Nome + ".novo");
				ExtrairUm(zipLocal, e.Nome, text2);
				File.Delete(zipLocal);
				if (new FileInfo(text2).Length != e.Tamanho || UpdateList.Fcrc(text2) != e.Crc)
				{
					throw new InvalidDataException(Idioma.T("corrompido", e.Nome));
				}
				string text3 = Path.Combine(_pasta, e.CaminhoRelativo);
				if (string.Equals(e.CaminhoRelativo, _meuExe, StringComparison.OrdinalIgnoreCase))
				{
					if (ContemMarca(text2))
					{
						File.Copy(text2, text3 + ".novo", overwrite: true);
						res.LauncherNovo = true;
					}
					File.Delete(text2);
				}
				else
				{
					Directory.CreateDirectory(Path.GetDirectoryName(text3));
					if (File.Exists(text3))
					{
						File.SetAttributes(text3, FileAttributes.Normal);
					}
					File.Copy(text2, text3, overwrite: true);
					File.Delete(text2);
					cache.Guardar(e.CaminhoRelativo, new FileInfo(text3), e.Crc);
					res.Atualizados++;
				}
				feito += ((e.TamanhoZip > 0) ? e.TamanhoZip : Math.Max(1L, e.Tamanho));
			}
			cache.Salvar();
			try
			{
				Directory.Delete(tmp, recursive: true);
			}
			catch
			{
			}
			res.Ok = res.SemZip.Count == 0;
			if (!res.Ok)
			{
				res.Erro = Idioma.T("sem_baixar", string.Join(", ", res.SemZip.Take(8)) + ((res.SemZip.Count > 8) ? "…" : ""));
			}
			prog.Report(new Progresso
			{
				Texto = ((!res.Ok) ? Idioma.T("incompleta") : ((res.Atualizados > 0) ? Idioma.T("atualizados", res.Atualizados) : Idioma.T("tudo_ok"))),
				Geral = 1.0,
				Arquivo = -1.0
			});
		}
		catch (OperationCanceledException)
		{
			res.Erro = Idioma.T("cancelada");
		}
		catch (HttpRequestException ex2)
		{
			res.Erro = Idioma.T("sem_servidor", ex2.Message);
		}
		catch (Exception ex3)
		{
			res.Erro = ex3.Message;
		}
		return res;
	}

	private static async Task<bool> BaixarAsync(string url, string destino, Action<long, long> prog, CancellationToken ct)
	{
		using (HttpResponseMessage resp = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(continueOnCapturedContext: false))
		{
			if (resp.StatusCode == HttpStatusCode.NotFound || resp.StatusCode == HttpStatusCode.BadRequest)
			{
				return false;
			}
			resp.EnsureSuccessStatusCode();
			long total = resp.Content.Headers.ContentLength ?? (-1);
			long lidos = 0L;
			long ultimo = 0L;
			using Stream src = await resp.Content.ReadAsStreamAsync().ConfigureAwait(continueOnCapturedContext: false);
			using FileStream dst = new FileStream(destino, FileMode.Create, FileAccess.Write, FileShare.None, 65536);
			byte[] buf = new byte[65536];
			while (true)
			{
				int num;
				int n = (num = await src.ReadAsync(buf, 0, buf.Length, ct).ConfigureAwait(continueOnCapturedContext: false));
				if (num <= 0)
				{
					break;
				}
				await dst.WriteAsync(buf, 0, n, ct).ConfigureAwait(continueOnCapturedContext: false);
				lidos += n;
				if (lidos - ultimo >= 524288 || lidos == total)
				{
					ultimo = lidos;
					prog(lidos, total);
				}
			}
		}
		return true;
	}

	private static void ExtrairUm(string zip, string nome, string destino)
	{
		using ZipArchive zipArchive = ZipFile.OpenRead(zip);
		ZipArchiveEntry zipArchiveEntry = zipArchive.Entries.FirstOrDefault((ZipArchiveEntry x) => string.Equals(Path.GetFileName(x.FullName), nome, StringComparison.OrdinalIgnoreCase)) ?? ((zipArchive.Entries.Count == 1) ? zipArchive.Entries[0] : null);
		if (zipArchiveEntry == null)
		{
			throw new InvalidDataException(Idioma.T("zip_sem_arquivo", nome));
		}
		zipArchiveEntry.ExtractToFile(destino, overwrite: true);
	}

	private static bool ContemMarca(string arquivo)
	{
		byte[] bytes = Encoding.Unicode.GetBytes("PangyaLeagueLauncher-v1");
		byte[] array = File.ReadAllBytes(arquivo);
		for (int i = 0; i <= array.Length - bytes.Length; i++)
		{
			int j;
			for (j = 0; j < bytes.Length && array[i + j] == bytes[j]; j++)
			{
			}
			if (j == bytes.Length)
			{
				return true;
			}
		}
		return false;
	}

	private static double Frac(long a, long b)
	{
		if (b > 0)
		{
			return Math.Min(1.0, (double)a / (double)b);
		}
		return 0.0;
	}

	private static string Mb(long b)
	{
		return ((double)b / 1048576.0).ToString("0.0") + " MB";
	}
}
