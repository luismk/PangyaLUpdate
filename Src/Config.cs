using System.IO;

namespace PangyaLeagueLauncher;

public sealed class Config
{
	public string PatchUrl = "http://186.208.221.74:8083/pangya/season4/patch/";

	public string SiteUrl = "https://pangya.leaguesoft.com.br/";

	public string Jogo = "ProjectG.exe";

	public string Fundo = "launcher_fundo.jpg";

	public string Titulo = "Pangya League";

	public string Servidor = "America";

	public string LoginHost = "186.208.221.74";

	public int LoginPorta = 10303;

	public string NoticiasUrl = "https://pangya.leaguesoft.com.br/noticias";

	public string PatchNotesUrl = "https://pangya.leaguesoft.com.br/patchnotes";

	public string RankingUrl = "https://pangya.leaguesoft.com.br/ranking";

	public string TotaisUrl = "https://pangya.leaguesoft.com.br/api/totais";

	public static Config Carregar(string pasta)
	{
		Config config = new Config();
		string path = Path.Combine(pasta, "launcher.ini");
		if (!File.Exists(path))
		{
			return config;
		}
		string[] array = File.ReadAllLines(path);
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim();
			if (text.Length == 0 || text.StartsWith(";") || text.StartsWith("#") || text.StartsWith("["))
			{
				continue;
			}
			int num = text.IndexOf('=');
			if (num <= 0)
			{
				continue;
			}
			string text2 = text.Substring(0, num).Trim().ToLowerInvariant();
			string text3 = text.Substring(num + 1).Trim();
			switch (text2)
			{
			case "patchurl":
				config.PatchUrl = (text3.EndsWith("/") ? text3 : (text3 + "/"));
				break;
			case "siteurl":
				config.SiteUrl = text3;
				break;
			case "jogo":
				config.Jogo = text3;
				break;
			case "fundo":
				config.Fundo = text3;
				break;
			case "titulo":
				config.Titulo = text3;
				break;
			case "servidor":
				config.Servidor = text3;
				break;
			case "loginhost":
				config.LoginHost = text3;
				break;
			case "loginporta":
			{
				if (int.TryParse(text3, out var result))
				{
					config.LoginPorta = result;
				}
				break;
			}
			case "noticiasurl":
				config.NoticiasUrl = text3;
				break;
			case "patchnotesurl":
				config.PatchNotesUrl = text3;
				break;
			case "rankingurl":
				config.RankingUrl = text3;
				break;
			case "totaisurl":
				config.TotaisUrl = text3;
				break;
			}
		}
		return config;
	}
}
