using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace PangyaLeagueLauncher;

internal static class Ingresso
{
	private static readonly uint[] A = new uint[4] { 104951167u, 355124318u, 108056999u, 3477209727u };

	private static readonly uint[] B = new uint[4] { 2683492345u, 669120991u, 338576817u, 3470604463u };

	private static readonly int[] R = new int[4] { 23, 22, 14, 10 };

	public static string Argumento()
	{
		return "PLL=" + Gerar();
	}

	private static uint Rotr(uint v, int n)
	{
		return (v >> n) | (v << 32 - n);
	}

	private static void Xtea(uint[] k, ref uint v0, ref uint v1)
	{
		uint num = 0u;
		for (int i = 0; i < 32; i++)
		{
			v0 += (((v1 << 4) ^ (v1 >> 5)) + v1) ^ (num + k[num & 3]);
			num += 2654435769u;
			v1 += (((v0 << 4) ^ (v0 >> 5)) + v0) ^ (num + k[(num >> 11) & 3]);
		}
	}

	public static string Gerar()
	{
		uint[] array = new uint[4];
		for (int i = 0; i < 4; i++)
		{
			array[i] = Rotr(A[i], R[i]) ^ B[i];
		}
		byte[] array2 = new byte[24];
		BitConverter.GetBytes((ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds()).CopyTo(array2, 0);
		BitConverter.GetBytes((uint)Process.GetCurrentProcess().Id).CopyTo(array2, 8);
		byte[] array3 = new byte[4];
		using (RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create())
		{
			randomNumberGenerator.GetBytes(array3);
		}
		array3.CopyTo(array2, 12);
		uint v = 0u;
		uint v2 = 0u;
		for (int j = 0; j < 16; j += 8)
		{
			v ^= BitConverter.ToUInt32(array2, j);
			v2 ^= BitConverter.ToUInt32(array2, j + 4);
			Xtea(array, ref v, ref v2);
		}
		Array.Clear(array, 0, 4);
		BitConverter.GetBytes(v).CopyTo(array2, 16);
		BitConverter.GetBytes(v2).CopyTo(array2, 20);
		StringBuilder stringBuilder = new StringBuilder(48);
		byte[] array4 = array2;
		foreach (byte b in array4)
		{
			stringBuilder.Append(b.ToString("X2"));
		}
		return stringBuilder.ToString();
	}
}
