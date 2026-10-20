using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace PangyaLeagueLauncher;

internal static class Idioma
{
	public static readonly string[] Codigos = new string[3] { "pt", "en", "es" };

	public static readonly string[] Nomes = new string[3] { "Português", "English", "Español" };

	private static readonly Dictionary<string, string[]> T_ = new Dictionary<string, string[]>
	{
		{
			"preparando",
			new string[3] { "Preparando…", "Preparing…", "Preparando…" }
		},
		{
			"conectando",
			new string[3] { "Conectando ao servidor de atualização…", "Connecting to the update server…", "Conectando al servidor de actualización…" }
		},
		{
			"verificando_arq",
			new string[3] { "Verificando {0}…", "Checking {0}…", "Verificando {0}…" }
		},
		{
			"verificando_n",
			new string[3] { "Verificando arquivos… ({0} para atualizar)", "Checking files… ({0} to update)", "Verificando archivos… ({0} por actualizar)" }
		},
		{
			"baixando",
			new string[3] { "Baixando {0} ({1} / {2})", "Downloading {0} ({1} / {2})", "Descargando {0} ({1} / {2})" }
		},
		{
			"instalando",
			new string[3] { "Instalando {0}…", "Installing {0}…", "Instalando {0}…" }
		},
		{
			"corrompido",
			new string[3] { "o arquivo baixado {0} veio diferente do esperado (download corrompido)", "the downloaded file {0} does not match (corrupted download)", "el archivo descargado {0} no coincide (descarga dañada)" }
		},
		{
			"zip_sem_arquivo",
			new string[3] { "o zip de {0} não contém o arquivo", "the zip of {0} does not contain the file", "el zip de {0} no contiene el archivo" }
		},
		{
			"sem_baixar",
			new string[3] { "Arquivos que não puderam ser baixados: {0}", "Files that could not be downloaded: {0}", "Archivos que no se pudieron descargar: {0}" }
		},
		{
			"atualizados",
			new string[3] { "{0} arquivo(s) atualizado(s). Pronto para jogar!", "{0} file(s) updated. Ready to play!", "{0} archivo(s) actualizado(s). ¡Listo para jugar!" }
		},
		{
			"tudo_ok",
			new string[3] { "Tudo atualizado. Pronto para jogar!", "Everything is up to date. Ready to play!", "Todo actualizado. ¡Listo para jugar!" }
		},
		{
			"incompleta",
			new string[3] { "Atualização incompleta.", "Update incomplete.", "Actualización incompleta." }
		},
		{
			"cancelada",
			new string[3] { "Atualização cancelada.", "Update cancelled.", "Actualización cancelada." }
		},
		{
			"sem_servidor",
			new string[3] { "Não foi possível falar com o servidor de atualização ({0}). Verifique a internet.", "Could not reach the update server ({0}). Check your internet.", "No se pudo conectar al servidor de actualización ({0}). Revisa tu internet." }
		},
		{
			"falha",
			new string[3] { "Falha na atualização: {0}", "Update failed: {0}", "Error en la actualización: {0}" }
		},
		{
			"feche_jogo",
			new string[3] { "Feche o jogo antes de atualizar.", "Close the game before updating.", "Cierra el juego antes de actualizar." }
		},
		{
			"launcher_novo",
			new string[3] { "Launcher atualizado. Reiniciando…", "Launcher updated. Restarting…", "Launcher actualizado. Reiniciando…" }
		},
		{
			"sem_jogo",
			new string[3] { "Não encontrei o {0} nesta pasta. Coloque o update.exe na pasta do jogo.", "{0} was not found in this folder. Put update.exe in the game folder.", "No encontré {0} en esta carpeta. Pon update.exe en la carpeta del juego." }
		},
		{
			"nao_abriu",
			new string[3] { "Não foi possível abrir o jogo: {0}", "Could not start the game: {0}", "No se pudo abrir el juego: {0}" }
		},
		{
			"reg_preparar",
			new string[3] { "Não foi possível preparar o registro do jogo.", "Could not prepare the game registry.", "No se pudo preparar el registro del juego." }
		},
		{
			"reg_gravar",
			new string[3] { "Não foi possível gravar o registro do jogo. Abra o update.exe como administrador e tente de novo.", "Could not write the game registry. Run update.exe as administrator and try again.", "No se pudo escribir el registro del juego. Abre update.exe como administrador y inténtalo de nuevo." }
		},
		{
			"reg_permissao",
			new string[3] { "O jogo precisa da sua permissão (aviso do Windows) na primeira vez. Clique em Jogar de novo e aceite.", "The game needs your permission (Windows prompt) the first time. Click Play again and accept.", "El juego necesita tu permiso (aviso de Windows) la primera vez. Pulsa Jugar de nuevo y acepta." }
		},
		{
			"reg_erro",
			new string[3] { "Não foi possível preparar o registro do jogo: {0}", "Could not prepare the game registry: {0}", "No se pudo preparar el registro del juego: {0}" }
		},
		{
			"verificar",
			new string[3] { "Verificar arquivos", "Repair files", "Verificar archivos" }
		},
		{
			"site",
			new string[3] { "Site", "Website", "Sitio" }
		},
		{
			"ranking",
			new string[3] { "Ranking", "Ranking", "Ranking" }
		},
		{
			"servidor",
			new string[3] { "Servidor", "Server", "Servidor" }
		},
		{
			"srv_online",
			new string[3] { "Online", "Online", "En línea" }
		},
		{
			"srv_offline",
			new string[3] { "Offline", "Offline", "Fuera de línea" }
		},
		{
			"srv_testando",
			new string[3] { "Conectando…", "Connecting…", "Conectando…" }
		},
		{
			"jogadores",
			new string[3] { "{0} jogando agora", "{0} playing now", "{0} jugando ahora" }
		},
		{
			"contas",
			new string[3] { "{0} jogadores cadastrados", "{0} registered players", "{0} jugadores registrados" }
		},
		{
			"patch_sub",
			new string[3] { "O que mudou no jogo", "What changed in the game", "Qué cambió en el juego" }
		},
		{
			"news_sub",
			new string[3] { "Notícias e eventos", "News and events", "Noticias y eventos" }
		},
		{
			"idioma",
			new string[3] { "Idioma", "Language", "Idioma" }
		},
		{
			"escolha_servidor",
			new string[3] { "Escolha o servidor", "Choose your server", "Elige tu servidor" }
		},
		{
			"atualizando",
			new string[3] { "Atualizando", "Updating", "Actualizando" }
		},
		{
			"op_btn",
			new string[3] { "Opções", "Options", "Opciones" }
		},
		{
			"op_titulo",
			new string[3] { "Opções do jogo", "Game options", "Opciones del juego" }
		},
		{
			"op_modo",
			new string[3] { "Modo", "Mode", "Modo" }
		},
		{
			"op_janela",
			new string[3] { "Janela", "Window", "Ventana" }
		},
		{
			"op_tela_cheia",
			new string[3] { "Tela cheia", "Fullscreen", "Pantalla completa" }
		},
		{
			"op_resolucao",
			new string[3] { "Resolução", "Resolution", "Resolución" }
		},
		{
			"op_graficos",
			new string[3] { "Gráficos", "Graphics", "Gráficos" }
		},
		{
			"op_graficos_dica",
			new string[3] { "Luz, sombra e cor. Se o jogo ficar lento, use um nível mais baixo.", "Light, shadow and color. If the game is slow, pick a lower level.", "Luz, sombra y color. Si el juego va lento, usa un nivel más bajo." }
		},
		{
			"op_personalizado",
			new string[3] { "Hoje: personalizado dentro do jogo.", "Now: custom settings from inside the game.", "Ahora: personalizado dentro del juego." }
		},
		{
			"op_som",
			new string[3] { "Som", "Sound", "Sonido" }
		},
		{
			"op_pasta",
			new string[3] { "Abrir pasta do jogo", "Open game folder", "Abrir carpeta del juego" }
		},
		{
			"op_salvar",
			new string[3] { "Salvar opções", "Save options", "Guardar opciones" }
		},
		{
			"op_salvo",
			new string[3] { "Opções salvas! Valem ao abrir o jogo.", "Options saved! They apply when the game starts.", "¡Opciones guardadas! Se aplican al abrir el juego." }
		},
		{
			"op_feche",
			new string[3] { "Feche o jogo antes de salvar as opções.", "Close the game before saving the options.", "Cierra el juego antes de guardar las opciones." }
		},
		{
			"op_permissao",
			new string[3] { "O Windows pede permissão para salvar. Clique em Salvar e aceite o aviso.", "Windows needs your permission to save. Click Save and accept the prompt.", "Windows necesita tu permiso para guardar. Pulsa Guardar y acepta el aviso." }
		},
		{
			"op_erro",
			new string[3] { "Não foi possível salvar as opções ({0}).", "Could not save the options ({0}).", "No se pudieron guardar las opciones ({0})." }
		}
	};

	public static string Atual { get; private set; } = "pt";

	public static string T(string chave, params object[] args)
	{
		if (!T_.TryGetValue(chave, out var value))
		{
			return chave;
		}
		int num = Array.IndexOf(Codigos, Atual);
		string text = value[(num >= 0) ? num : 0];
		if (args.Length != 0)
		{
			return string.Format(text, args);
		}
		return text;
	}

	private static string Arquivo(string pasta)
	{
		return Path.Combine(pasta, "launcher_idioma.txt");
	}

	public static void Carregar(string pasta)
	{
		try
		{
			string path = Arquivo(pasta);
			if (File.Exists(path))
			{
				string text = File.ReadAllText(path).Trim().ToLowerInvariant();
				if (Array.IndexOf(Codigos, text) >= 0)
				{
					Atual = text;
					return;
				}
			}
		}
		catch
		{
		}
		string twoLetterISOLanguageName = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
		string atual;
		if (twoLetterISOLanguageName == "pt")
		{
			atual = "pt";
		}
		else
		{
			atual = ((twoLetterISOLanguageName == "es") ? "es" : "en");
		}
		Atual = atual;
	}

	public static void Trocar(string pasta, string codigo)
	{
		if (Array.IndexOf(Codigos, codigo) < 0)
		{
			return;
		}
		Atual = codigo;
		try
		{
			File.WriteAllText(Arquivo(pasta), codigo);
		}
		catch
		{
		}
	}
}
