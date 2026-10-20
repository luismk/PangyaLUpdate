using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PangyaLeagueLauncher;

public sealed class CacheCrc
{
	private readonly string _arquivo;

	private readonly Dictionary<string, (long Tam, long Data, int Crc)> _m = new Dictionary<string, (long, long, int)>(StringComparer.OrdinalIgnoreCase);

	public CacheCrc(string pasta)
	{
		_arquivo = Path.Combine(pasta, "launcher.cache");
		try
		{
			if (!File.Exists(_arquivo))
			{
				return;
			}
			string[] array = File.ReadAllLines(_arquivo);
			for (int i = 0; i < array.Length; i++)
			{
				string[] array2 = array[i].Split('|');
				if (array2.Length == 4 && long.TryParse(array2[1], out var result) && long.TryParse(array2[2], out var result2) && int.TryParse(array2[3], out var result3))
				{
					_m[array2[0]] = (result, result2, result3);
				}
			}
		}
		catch
		{
			_m.Clear();
		}
	}

	public bool Tentar(string rel, FileInfo fi, out int crc)
	{
		crc = 0;
		if (_m.TryGetValue(rel, out (long, long, int) value) && value.Item1 == fi.Length && value.Item2 == fi.LastWriteTimeUtc.Ticks)
		{
			crc = value.Item3;
			return true;
		}
		return false;
	}

	public void Guardar(string rel, FileInfo fi, int crc)
	{
		_m[rel] = (fi.Length, fi.LastWriteTimeUtc.Ticks, crc);
	}

	public void Salvar()
	{
		try
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (KeyValuePair<string, (long, long, int)> item in _m)
			{
				stringBuilder.Append(item.Key).Append('|').Append(item.Value.Item1)
					.Append('|')
					.Append(item.Value.Item2)
					.Append('|')
					.Append(item.Value.Item3)
					.Append('\n');
			}
			File.WriteAllText(_arquivo, stringBuilder.ToString());
		}
		catch
		{
		}
	}
}
