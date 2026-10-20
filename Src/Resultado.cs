using System.Collections.Generic;

namespace PangyaLeagueLauncher;

public sealed class Resultado
{
	public bool Ok;

	public int Atualizados;

	public bool LauncherNovo;

	public string Erro;

	public List<string> SemZip = new List<string>();
}
