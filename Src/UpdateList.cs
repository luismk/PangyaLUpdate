using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace PangyaLeagueLauncher;

public static class UpdateList
{
	private static readonly uint[] ChaveJP = new uint[4] { 34234324u, 32423423u, 45336224u, 83272673u };

	private const uint Delta = 3816266640u;

	private const uint Soma = 1640531527u;

	private static readonly Regex RxLinha = new Regex("<fileinfo ([^>]*)/>", RegexOptions.Compiled);

	private static readonly Regex RxAttr = new Regex("(\\w+)=\"([^\"]*)\"", RegexOptions.Compiled);

	private static readonly uint[] Tabela = MontarTabela();

	public static string Decifrar(byte[] dados)
	{
		if (dados.Length % 8 != 0)
		{
			throw new InvalidDataException("updatelist com tamanho invalido");
		}
		byte[] array = (byte[])dados.Clone();
		for (int i = 0; i < array.Length; i += 8)
		{
			uint num = BitConverter.ToUInt32(array, i);
			uint num2 = BitConverter.ToUInt32(array, i + 4);
			uint num3 = 3816266640u;
			for (int j = 0; j < 16; j++)
			{
				num2 -= (((num << 4) ^ (num >> 5)) + num) ^ (num3 + ChaveJP[(num3 >> 11) & 3]);
				num3 += 1640531527;
				num -= (((num2 << 4) ^ (num2 >> 5)) + num2) ^ (num3 + ChaveJP[num3 & 3]);
			}
			Array.Copy(BitConverter.GetBytes(num), 0, array, i, 4);
			Array.Copy(BitConverter.GetBytes(num2), 0, array, i + 4, 4);
		}
		int num4 = Array.IndexOf(array, (byte)0);
		return Encoding.GetEncoding(949).GetString(array, 0, (num4 < 0) ? array.Length : num4);
	}

	public static List<Entrada> Ler(string xml)
	{
		if (!xml.Contains("<updatefiles"))
		{
			throw new InvalidDataException("updatelist nao decifrou (chave ou arquivo errado)");
		}
		List<Entrada> list = new List<Entrada>();
		foreach (Match item in RxLinha.Matches(xml))
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (Match item2 in RxAttr.Matches(item.Groups[1].Value))
			{
				dictionary[item2.Groups[1].Value] = item2.Groups[2].Value;
			}
			if (dictionary.TryGetValue("fname", out var value) && value.Length != 0 && !value.Contains("..") && !Path.IsPathRooted(value))
			{
				dictionary.TryGetValue("fdir", out var value2);
				if (string.IsNullOrEmpty(value2) || (!value2.Contains("..") && !Path.IsPathRooted(value2)))
				{
					list.Add(new Entrada
					{
						Nome = value,
						Pasta = (value2 ?? ""),
						Tamanho = (long.TryParse(Valor(dictionary, "fsize"), out var result) ? result : (-1)),
						Crc = (int.TryParse(Valor(dictionary, "fcrc"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result2) ? result2 : 0),
						Zip = Valor(dictionary, "pname"),
						TamanhoZip = (long.TryParse(Valor(dictionary, "psize"), out var result3) ? result3 : 0)
					});
				}
			}
		}
		return list;
	}

	private static string Valor(Dictionary<string, string> a, string k)
	{
		if (!a.TryGetValue(k, out var value))
		{
			return "";
		}
		return value;
	}

	private static uint[] MontarTabela()
	{
		uint[] array = new uint[256];
		for (uint num = 0u; num < 256; num++)
		{
			uint num2 = num;
			for (int i = 0; i < 8; i++)
			{
				num2 = (((num2 & 1) != 0) ? ((num2 >> 1) ^ 0x4C11DB7) : (num2 >> 1));
			}
			array[num] = num2;
		}
		return array;
	}

	public static int Fcrc(string caminho, Action<long> progresso = null)
	{
		uint num = uint.MaxValue;
		byte[] array = new byte[1048576];
		long num2 = 0L;
		using (FileStream fileStream = new FileStream(caminho, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 65536))
		{
			int num3;
			while ((num3 = fileStream.Read(array, 0, array.Length)) > 0)
			{
				for (int i = 0; i < num3; i++)
				{
					num = (num >> 8) ^ Tabela[(num ^ array[i]) & 0xFF];
				}
				num2 += num3;
				progresso?.Invoke(num2);
			}
		}
		num = ~num;
		return (int)(num & 0x7FFFFFF) | -134217728;
	}
}
