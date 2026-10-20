using System.IO;

namespace PangyaLeagueLauncher;

public sealed class Entrada
{
	public string Nome;

	public string Pasta;

	public long Tamanho;

	public int Crc;

	public string Zip;

	public long TamanhoZip;

	public string CaminhoRelativo
	{
		get
		{
			if (!string.IsNullOrEmpty(Pasta))
			{
				return Path.Combine(Pasta.Trim('\\', '/'), Nome);
			}
			return Nome;
		}
	}
}
