using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PangyaLeagueLauncher;

public sealed class FrmLauncher : Form
{
	private enum Srv
	{
		Testando,
		Online,
		Offline
	}

	private sealed class Zona
	{
		public string Id;

		public Rectangle R;

		public Action Clique;

		public Func<bool> Ativa = () => true;
	}

	private const int W = 1280;

	private const int H = 720;

	private const int BarraY = 560;

	private const int TopoH = 52;

	private readonly Config _cfg;

	private readonly string _pasta;

	private readonly bool _demo;

	private readonly Bitmap[] _fundos;

	private readonly Bitmap _play;

	private readonly Bitmap _playOff;

	private readonly Bitmap _patch;

	private readonly Bitmap _news;

	private int _fundoAtual;

	private int _fundoAnterior = -1;

	private DateTime _trocaFundo = DateTime.MinValue;

	private DateTime _proximaTroca;

	private readonly System.Windows.Forms.Timer _relogio = new System.Windows.Forms.Timer
	{
		Interval = 33
	};

	private string _status;

	private double _geral;

	private double _arquivo = -1.0;

	private bool _ocupado;

	private bool _pronto;

	private bool _podeJogar;

	private CancellationTokenSource _cts;

	private Srv _srv;

	private int _online = -1;

	private int _contas = -1;

	private readonly List<Zona> _zonas = new List<Zona>();

	private string _sobre;

	private string _apertado;

	private bool _menuIdioma;

	private bool _menuServidor;

	private bool _menuOpcoes;

	private bool _menuRes;

	private OpcoesJogo.Dados _op;

	private OpcoesJogo.Dados _opSalvo;

	private string _opMsg;

	private bool _opMsgOk;

	private Point _arrasto;

	private bool _arrastando;

	private static readonly Color Laranja = Color.FromArgb(255, 122, 26);

	private static readonly Color Vermelho = Color.FromArgb(255, 70, 85);

	private static readonly Color Escuro = Color.FromArgb(16, 9, 26);

	private static readonly Color Painel = Color.FromArgb(200, 28, 20, 38);

	private static readonly Rectangle RIdioma = new Rectangle(16, 11, 92, 30);

	private static readonly Rectangle RSite = new Rectangle(122, 11, 64, 30);

	private static readonly Rectangle RRanking = new Rectangle(190, 11, 80, 30);

	private static readonly Rectangle ROpcoes = new Rectangle(280, 11, 108, 30);

	private static readonly Rectangle ROpPainel = new Rectangle(280, 48, 340, 448);

	private static readonly Rectangle RMin = new Rectangle(1196, 8, 36, 32);

	private static readonly Rectangle RFechar = new Rectangle(1236, 8, 36, 32);

	private static readonly Rectangle RServidor = new Rectangle(40, 500, 360, 44);

	private static readonly Rectangle RPlay = new Rectangle(40, 580, 360, 86);

	private static readonly Rectangle RBarra = new Rectangle(40, 684, 300, 10);

	private static readonly Rectangle RPatch = new Rectangle(926, 526, 330, 93);

	private static readonly Rectangle RNews = new Rectangle(926, 629, 330, 79);

	private static readonly Rectangle RVerificar = new Rectangle(428, 668, 190, 28);

	private const int ColX = 428;

	private const int ColW = 470;

	private static readonly HttpClient Http = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(8.0)
	};

	public FrmLauncher(Config cfg, string pasta, bool demo = false)
	{
		_cfg = cfg;
		_pasta = pasta;
		_demo = demo;
		_status = Idioma.T("preparando");
		Text = cfg.Titulo;
		FormBorderStyle = FormBorderStyle.None;
		ClientSize = new Size(1280, 720);
		StartPosition = FormStartPosition.Manual;
		Rectangle workingArea = Screen.FromPoint(Cursor.Position).WorkingArea;
		if (workingArea.Width < 1280 || workingArea.Height < 720)
		{
			workingArea = Screen.PrimaryScreen.WorkingArea;
		}
		Location = new Point(workingArea.X + Math.Max(0, (workingArea.Width - 1280) / 2), workingArea.Y + Math.Max(0, (workingArea.Height - 720) / 2));
		DoubleBuffered = true;
		BackColor = Escuro;
		try
		{
			Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
		}
		catch
		{
		}
		Bitmap logo = Recurso("logo.png");
		_fundos = (from f in CarregarFundos()
			select Compor(f, logo)).ToArray();
		logo?.Dispose();
		_play = Escalar(Recurso("btn_play.png"), RPlay.Size);
		_playOff = Desbotar(_play);
		_patch = Escalar(Recurso("card_patch.png"), RPatch.Size);
		_news = Escalar(Recurso("card_news.png"), RNews.Size);
		_proximaTroca = DateTime.Now.AddSeconds(9.0);
		MontarZonas();
		_relogio.Tick += (object s, EventArgs e) =>
		{
			Animar();
		};
		_relogio.Start();
		Shown += (object s, EventArgs e) =>
		{
			if (_demo)
			{
				Demo();
			}
			else
			{
				Atualizar(completo: false);
			}
			VigiarServidor();
		};
		FormClosing += (object s, FormClosingEventArgs e) =>
		{
			_cts?.Cancel();
			_relogio.Stop();
		};
	}

	private static Bitmap Recurso(string nome)
	{
		using Stream stream = typeof(FrmLauncher).Assembly.GetManifestResourceStream(nome);
		return (stream == null) ? null : new Bitmap(stream);
	}

	private IEnumerable<Bitmap> CarregarFundos()
	{
		Bitmap bitmap = null;
		try
		{
			string text = Path.Combine(_pasta, _cfg.Fundo);
			if (File.Exists(text))
			{
				using Image original = Image.FromFile(text);
				bitmap = new Bitmap(original);
			}
		}
		catch
		{
		}
		if (bitmap != null)
		{
			yield return bitmap;
		}
		string[] array = new string[1] { "fundo_halloween.jpg" };
		for (int i = 0; i < array.Length; i++)
		{
			Bitmap bitmap2 = Recurso(array[i]);
			if (bitmap2 != null)
			{
				yield return bitmap2;
			}
		}
	}

	private static Bitmap Compor(Bitmap fundo, Bitmap logo)
	{
		Bitmap bitmap = new Bitmap(1280, 720, PixelFormat.Format32bppPArgb);
		using Graphics graphics = Graphics.FromImage(bitmap);
		graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
		graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.Clear(Escuro);
		float num = Math.Max(1280f / (float)fundo.Width, 720f / (float)fundo.Height);
		float num2 = (float)fundo.Width * num;
		float num3 = (float)fundo.Height * num;
		graphics.DrawImage(fundo, (1280f - num2) / 2f, (720f - num3) / 2f, num2, num3);
		fundo.Dispose();
		using (LinearGradientBrush brush = new LinearGradientBrush(new Rectangle(0, 0, 1280, 110), Color.FromArgb(190, 8, 4, 14), Color.FromArgb(0, 8, 4, 14), 90f))
		{
			graphics.FillRectangle(brush, 0, 0, 1280, 110);
		}
		using (LinearGradientBrush brush2 = new LinearGradientBrush(new Rectangle(0, 0, 560, 720), Color.FromArgb(120, 8, 4, 14), Color.FromArgb(0, 8, 4, 14), 0f))
		{
			graphics.FillRectangle(brush2, 0, 0, 560, 720);
		}
		using (LinearGradientBrush brush3 = new LinearGradientBrush(new Rectangle(0, 440, 1280, 121), Color.FromArgb(0, 16, 9, 26), Color.FromArgb(170, 16, 9, 26), 90f))
		{
			graphics.FillRectangle(brush3, 0, 440, 1280, 120);
		}
		if (logo != null)
		{
			int num4 = 440;
			int num5 = num4 * logo.Height / logo.Width;
			graphics.DrawImage(logo, new Rectangle(34, 62, num4, num5));
		}
		using (SolidBrush brush4 = new SolidBrush(Color.FromArgb(232, 16, 9, 26)))
		{
			graphics.FillRectangle(brush4, 0, 560, 1280, 160);
		}
		using LinearGradientBrush brush5 = new LinearGradientBrush(new Rectangle(0, 560, 1280, 2), Color.FromArgb(200, Laranja), Color.FromArgb(0, Laranja), 0f);
		graphics.FillRectangle(brush5, 0, 560, 1280, 2);
		return bitmap;
	}

	private static Bitmap Escalar(Bitmap src, Size s)
	{
		Bitmap bitmap = new Bitmap(s.Width, s.Height, PixelFormat.Format32bppPArgb);
		if (src == null)
		{
			return bitmap;
		}
		using (Graphics graphics = Graphics.FromImage(bitmap))
		{
			graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			graphics.DrawImage(src, new Rectangle(Point.Empty, s));
		}
		src.Dispose();
		return bitmap;
	}

	private static Bitmap Desbotar(Bitmap src)
	{
		Bitmap bitmap = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppPArgb);
		ColorMatrix colorMatrix = new ColorMatrix(new float[5][]
		{
			new float[5] { 0.2f, 0.2f, 0.2f, 0f, 0f },
			new float[5] { 0.45f, 0.45f, 0.45f, 0f, 0f },
			new float[5] { 0.08f, 0.08f, 0.1f, 0f, 0f },
			new float[5] { 0f, 0f, 0f, 1f, 0f },
			new float[5] { 0.02f, 0.01f, 0.05f, 0f, 1f }
		});
		using ImageAttributes imageAttributes = new ImageAttributes();
		using Graphics graphics = Graphics.FromImage(bitmap);
		imageAttributes.SetColorMatrix(colorMatrix);
		graphics.DrawImage(src, new Rectangle(0, 0, src.Width, src.Height), 0, 0, src.Width, src.Height, GraphicsUnit.Pixel, imageAttributes);
		return bitmap;
	}

	private void MontarZonas()
	{
		_zonas.Clear();
		if (_menuOpcoes)
		{
			MontarZonasOpcoes();
		}
		if (_menuIdioma)
		{
			for (int i = 0; i < Idioma.Codigos.Length; i++)
			{
				string cod = Idioma.Codigos[i];
				_zonas.Add(new Zona
				{
					Id = "lang:" + cod,
					R = ItemIdioma(i),
					Clique = () =>
					{
						Idioma.Trocar(_pasta, cod);
						_menuIdioma = false;
						TraduzirStatus();
						MontarZonas();
						Invalidate();
					}
				});
			}
		}
		if (_menuServidor)
		{
			_zonas.Add(new Zona
			{
				Id = "srv:item",
				R = ItemServidor(),
				Clique = () =>
				{
					_menuServidor = false;
					MontarZonas();
					Invalidate();
				}
			});
		}
		_zonas.Add(new Zona
		{
			Id = "idioma",
			R = RIdioma,
			Clique = () =>
			{
				_menuIdioma = !_menuIdioma;
				_menuServidor = (_menuOpcoes = false);
				MontarZonas();
				Invalidate();
			}
		});
		_zonas.Add(new Zona
		{
			Id = "site",
			R = RSite,
			Clique = () =>
			{
				Abrir(_cfg.SiteUrl);
			}
		});
		_zonas.Add(new Zona
		{
			Id = "ranking",
			R = RRanking,
			Clique = () =>
			{
				Abrir(_cfg.RankingUrl);
			}
		});
		_zonas.Add(new Zona
		{
			Id = "opcoes",
			R = ROpcoes,
			Clique = AlternarOpcoes
		});
		_zonas.Add(new Zona
		{
			Id = "min",
			R = RMin,
			Clique = () =>
			{
				WindowState = FormWindowState.Minimized;
			}
		});
		_zonas.Add(new Zona
		{
			Id = "fechar",
			R = RFechar,
			Clique = Close
		});
		_zonas.Add(new Zona
		{
			Id = "servidor",
			R = RServidor,
			Clique = () =>
			{
				_menuServidor = !_menuServidor;
				_menuIdioma = false;
				MontarZonas();
				Invalidate();
			}
		});
		_zonas.Add(new Zona
		{
			Id = "play",
			R = RPlay,
			Clique = Jogar,
			Ativa = () => _podeJogar && !_ocupado
		});
		_zonas.Add(new Zona
		{
			Id = "verificar",
			R = RVerificar,
			Clique = () =>
			{
				Atualizar(completo: true);
			},
			Ativa = () => !_ocupado
		});
		_zonas.Add(new Zona
		{
			Id = "patch",
			R = RPatch,
			Clique = () =>
			{
				Abrir(_cfg.PatchNotesUrl);
			}
		});
		_zonas.Add(new Zona
		{
			Id = "news",
			R = RNews,
			Clique = () =>
			{
				Abrir(_cfg.NoticiasUrl);
			}
		});
		for (int num = 0; num < _fundos.Length; num++)
		{
			if (_fundos.Length <= 1)
			{
				break;
			}
			int k = num;
			_zonas.Add(new Zona
			{
				Id = "dot:" + num,
				R = Ponto(num),
				Clique = () =>
				{
					TrocarFundo(k);
				}
			});
		}
	}

	private static Rectangle ItemIdioma(int i)
	{
		return new Rectangle(RIdioma.X, RIdioma.Bottom + 6 + i * 34, 160, 34);
	}

	private static Rectangle ItemServidor()
	{
		return new Rectangle(RServidor.X, RServidor.Y - 26 - 44, RServidor.Width, 44);
	}

	private Rectangle Ponto(int i)
	{
		return new Rectangle(RBarra.Right + 18 + i * 22, RBarra.Y - 5, 20, 20);
	}

	private Zona Achar(Point p)
	{
		return _zonas.FirstOrDefault((Zona z) => z.R.Contains(p));
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		if (_arrastando)
		{
			Location = new Point(Location.X + e.X - _arrasto.X, Location.Y + e.Y - _arrasto.Y);
			return;
		}
		Zona zona = Achar(e.Location);
		string text = ((zona != null && zona.Ativa()) ? zona.Id : null);
		Cursor = ((text != null) ? Cursors.Hand : Cursors.Default);
		if (text != _sobre)
		{
			_sobre = text;
			Invalidate();
		}
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);
		if (_sobre != null)
		{
			_sobre = null;
			Invalidate();
		}
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		if (e.Button != MouseButtons.Left)
		{
			return;
		}
		Zona zona = Achar(e.Location);
		if (zona != null && zona.Ativa())
		{
			_apertado = zona.Id;
			Invalidate();
			return;
		}
		if (_menuOpcoes && ROpPainel.Contains(e.Location))
		{
			if (_menuRes)
			{
				_menuRes = false;
				MontarZonas();
				Invalidate();
			}
			return;
		}
		if (_menuIdioma || _menuServidor || _menuOpcoes)
		{
			_menuIdioma = (_menuServidor = (_menuOpcoes = (_menuRes = false)));
			MontarZonas();
			Invalidate();
		}
		_arrastando = true;
		_arrasto = e.Location;
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		_arrastando = false;
		Zona zona = Achar(e.Location);
		string apertado = _apertado;
		_apertado = null;
		Invalidate();
		if (zona != null && zona.Id == apertado && zona.Ativa())
		{
			zona.Clique();
		}
	}

	private void Abrir(string url)
	{
		try
		{
			Process.Start(new ProcessStartInfo(url)
			{
				UseShellExecute = true
			})?.Dispose();
		}
		catch
		{
		}
	}

	private void TrocarFundo(int i)
	{
		if (i != _fundoAtual && _fundos.Length >= 2)
		{
			_fundoAnterior = _fundoAtual;
			_fundoAtual = i;
			_trocaFundo = DateTime.Now;
			_proximaTroca = DateTime.Now.AddSeconds(9.0);
		}
	}

	private void Animar()
	{
		if (_fundos.Length > 1 && !_menuIdioma && !_menuServidor && !_menuOpcoes && DateTime.Now >= _proximaTroca)
		{
			TrocarFundo((_fundoAtual + 1) % _fundos.Length);
		}
		if (_fundoAnterior >= 0)
		{
			Invalidate();
		}
	}

	private async void Atualizar(bool completo)
	{
		if (_ocupado)
		{
			return;
		}
		if (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(_cfg.Jogo)).Any())
		{
			_status = Idioma.T("feche_jogo");
			Invalidate();
			return;
		}
		_ocupado = true;
		_pronto = false;
		_podeJogar = false;
		_cts = new CancellationTokenSource();
		Progress<Progresso> prog = new Progress<Progresso>((Progresso p) =>
		{
			_status = p.Texto;
			_geral = p.Geral;
			_arquivo = p.Arquivo;
			Invalidate(new Rectangle(0, 560, RPatch.X - 8, 160));
		});
		Resultado resultado = await new Atualizador(_cfg, _pasta).ExecutarAsync(completo, prog, _cts.Token);
		_ocupado = false;
		if (resultado.LauncherNovo)
		{
			_status = Idioma.T("launcher_novo");
			Invalidate();
			Program.TrocarLauncherEReiniciar();
			return;
		}
		if (resultado.Ok)
		{
			_pronto = true;
			_podeJogar = true;
			_geral = 1.0;
			_arquivo = -1.0;
		}
		else
		{
			_status = resultado.Erro ?? Idioma.T("incompleta");
			_podeJogar = File.Exists(Path.Combine(_pasta, _cfg.Jogo));
		}
		Invalidate();
	}

	private void TraduzirStatus()
	{
		if (!_ocupado)
		{
			if (_pronto)
			{
				_status = Idioma.T("tudo_ok");
			}
			else if (_demo)
			{
				_status = Idioma.T("baixando", "ProjectG999p.pak", "1.2 MB", "3.4 MB");
			}
		}
	}

	private async void Demo()
	{
		_ocupado = true;
		for (int i = 0; i <= 100; i++)
		{
			_geral = (double)i / 100.0;
			_arquivo = (double)(i * 7 % 100) / 100.0;
			_status = Idioma.T("baixando", "ProjectG999p.pak", ((double)i * 0.034).ToString("0.0") + " MB", "3.4 MB");
			Invalidate();
			await Task.Delay(40);
		}
		_ocupado = false;
		_pronto = true;
		_podeJogar = true;
		_arquivo = -1.0;
		_status = Idioma.T("tudo_ok");
		Invalidate();
	}

	private void Jogar()
	{
		if (_demo)
		{
			return;
		}
		string text = Path.Combine(_pasta, _cfg.Jogo);
		if (!File.Exists(text))
		{
			_status = Idioma.T("sem_jogo", _cfg.Jogo);
			Invalidate();
			return;
		}
		string text2 = Registro.Garantir();
		if (text2 != null)
		{
			_status = text2;
			Invalidate();
			return;
		}
		try
		{
			Process.Start(new ProcessStartInfo(text, Ingresso.Argumento())
			{
				WorkingDirectory = _pasta,
				UseShellExecute = true
			})?.Dispose();
			Close();
		}
		catch (Exception ex)
		{
			_status = Idioma.T("nao_abriu", ex.Message);
			Invalidate();
		}
	}

	private async void VigiarServidor()
	{
		while (!IsDisposed)
		{
			bool flag = await Task.Run(() => TestarLogin());
			if (IsDisposed)
			{
				break;
			}
			_srv = (flag ? Srv.Online : Srv.Offline);
			Tuple<int, int> tuple = await Task.Run(() => LerTotais());
			if (IsDisposed)
			{
				break;
			}
			if (tuple != null)
			{
				_online = tuple.Item1;
				_contas = tuple.Item2;
			}
			Invalidate();
			await Task.Delay(TimeSpan.FromSeconds(30.0));
		}
	}

	private bool TestarLogin()
	{
		try
		{
			using TcpClient tcpClient = new TcpClient();
			return tcpClient.ConnectAsync(_cfg.LoginHost, _cfg.LoginPorta).Wait(TimeSpan.FromSeconds(4.0)) && tcpClient.Connected;
		}
		catch
		{
			return false;
		}
	}

	private Tuple<int, int> LerTotais()
	{
		if (string.IsNullOrEmpty(_cfg.TotaisUrl))
		{
			return null;
		}
		try
		{
			string result = Http.GetStringAsync(_cfg.TotaisUrl).GetAwaiter().GetResult();
			Match match = Regex.Match(result, "\"online\"\\s*:\\s*(\\d+)");
			Match match2 = Regex.Match(result, "\"contas\"\\s*:\\s*(\\d+)");
			if (!match.Success)
			{
				return null;
			}
			return Tuple.Create(int.Parse(match.Groups[1].Value), match2.Success ? int.Parse(match2.Groups[1].Value) : (-1));
		}
		catch
		{
			return null;
		}
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		graphics.InterpolationMode = InterpolationMode.Low;
		graphics.CompositingQuality = CompositingQuality.HighSpeed;
		DesenharFundo(graphics);
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
		DesenharTopo(graphics);
		DesenharServidor(graphics);
		DesenharPlay(graphics);
		DesenharColuna(graphics);
		DesenharCard(graphics, _patch, RPatch, "patch");
		DesenharCard(graphics, _news, RNews, "news");
		if (_menuServidor)
		{
			DesenharMenuServidor(graphics);
		}
		if (_menuIdioma)
		{
			DesenharMenuIdioma(graphics);
		}
		if (_menuOpcoes)
		{
			DesenharOpcoes(graphics);
		}
	}

	private void DesenharFundo(Graphics g)
	{
		if (_fundos.Length == 0)
		{
			g.Clear(Escuro);
			return;
		}
		g.DrawImageUnscaled(_fundos[_fundoAtual], 0, 0);
		if (_fundoAnterior < 0)
		{
			return;
		}
		double num = (DateTime.Now - _trocaFundo).TotalMilliseconds / 700.0;
		if (num >= 1.0)
		{
			_fundoAnterior = -1;
			return;
		}
		using ImageAttributes imageAttributes = new ImageAttributes();
		imageAttributes.SetColorMatrix(new ColorMatrix
		{
			Matrix33 = (float)(1.0 - num)
		});
		g.DrawImage(_fundos[_fundoAnterior], new Rectangle(0, 0, 1280, 720), 0, 0, 1280, 720, GraphicsUnit.Pixel, imageAttributes);
	}

	private void DesenharTopo(Graphics g)
	{
		Pilula(g, RIdioma, _sobre == "idioma" || _menuIdioma);
		Globo(g, new Rectangle(RIdioma.X + 9, RIdioma.Y + 7, 16, 16));
		using (Font font = new Font("Segoe UI", 9.5f, FontStyle.Bold))
		{
			g.DrawString(Idioma.Atual.ToUpperInvariant(), font, Brushes.White, RIdioma.X + 30, RIdioma.Y + 6);
		}
		Seta(g, new Point(RIdioma.Right - 16, RIdioma.Y + 15), _menuIdioma);
		Link(g, RSite, Idioma.T("site"), "site");
		Link(g, RRanking, Idioma.T("ranking"), "ranking");
		Pilula(g, ROpcoes, _sobre == "opcoes" || _menuOpcoes);
		Engrenagem(g, new Rectangle(ROpcoes.X + 10, ROpcoes.Y + 7, 16, 16));
		using (Font font2 = new Font("Segoe UI", 9.5f, FontStyle.Bold))
		{
			g.DrawString(Idioma.T("op_btn"), font2, Brushes.White, ROpcoes.X + 31, ROpcoes.Y + 6);
		}
		if (_sobre == "min")
		{
			using SolidBrush brush = new SolidBrush(Color.FromArgb(60, 255, 255, 255));
			g.FillRectangle(brush, RMin);
		}
		if (_sobre == "fechar")
		{
			using SolidBrush brush2 = new SolidBrush(Color.FromArgb(220, 210, 40, 50));
			g.FillRectangle(brush2, RFechar);
		}
		using Pen pen = new Pen(Color.White, 1.6f);
		int num = RMin.X + RMin.Width / 2;
		int num2 = RMin.Y + RMin.Height / 2;
		g.DrawLine(pen, num - 6, num2 + 1, num + 6, num2 + 1);
		num = RFechar.X + RFechar.Width / 2;
		num2 = RFechar.Y + RFechar.Height / 2;
		g.DrawLine(pen, num - 6, num2 - 6, num + 6, num2 + 6);
		g.DrawLine(pen, num + 6, num2 - 6, num - 6, num2 + 6);
	}

	private void Link(Graphics g, Rectangle r, string texto, string id)
	{
		bool flag = _sobre == id;
		using (Font font = new Font("Segoe UI", 10f, FontStyle.Bold))
		{
			using SolidBrush brush = new SolidBrush(flag ? Color.White : Color.FromArgb(200, 255, 255, 255));
			using StringFormat format = new StringFormat
			{
				Alignment = StringAlignment.Center,
				LineAlignment = StringAlignment.Center
			};
			g.DrawString(texto, font, brush, r, format);
		}
		if (flag)
		{
			using (SolidBrush brush2 = new SolidBrush(Vermelho))
			{
				g.FillRectangle(brush2, r.X + 14, r.Bottom - 3, r.Width - 28, 2);
			}
		}
	}

	private void DesenharServidor(Graphics g)
	{
		Rectangle rServidor = RServidor;
		bool flag = _sobre == "servidor" || _menuServidor;
		using (SolidBrush brush = new SolidBrush(flag ? Color.FromArgb(215, 40, 32, 52) : Color.FromArgb(185, 26, 20, 34)))
		{
			g.FillRectangle(brush, rServidor);
		}
		using (Pen pen = new Pen(Color.FromArgb(flag ? 150 : 70, 255, 255, 255), 1f))
		{
			g.DrawRectangle(pen, rServidor.X, rServidor.Y, rServidor.Width - 1, rServidor.Height - 1);
		}
		using (Pen pen2 = new Pen(Color.FromArgb(70, 255, 255, 255), 1f))
		{
			g.DrawLine(pen2, rServidor.X + 48, rServidor.Y + 9, rServidor.X + 48, rServidor.Bottom - 9);
		}
		Confirmado(g, new Rectangle(rServidor.X + 14, rServidor.Y + 12, 20, 20), Color.FromArgb(235, 235, 225));
		using (Font font = new Font("Segoe UI", 12.5f, FontStyle.Bold))
		{
			using StringFormat format = new StringFormat
			{
				Alignment = StringAlignment.Center,
				LineAlignment = StringAlignment.Center
			};
			g.DrawString(_cfg.Servidor, font, Brushes.WhiteSmoke, new Rectangle(rServidor.X + 48, rServidor.Y, rServidor.Width - 48 - 96, rServidor.Height), format);
		}
		Color color;
		string s;
		switch (_srv)
		{
		case Srv.Online:
			color = Color.FromArgb(70, 220, 120);
			s = Idioma.T("srv_online");
			break;
		case Srv.Offline:
			color = Color.FromArgb(230, 70, 70);
			s = Idioma.T("srv_offline");
			break;
		default:
			color = Color.FromArgb(230, 190, 60);
			s = Idioma.T("srv_testando");
			break;
		}
		using (Font font2 = new Font("Segoe UI", 8.5f, FontStyle.Bold))
		{
			SizeF sizeF = g.MeasureString(s, font2);
			float num = (float)(rServidor.Right - 14) - sizeF.Width;
			using (SolidBrush brush2 = new SolidBrush(color))
			{
				g.FillEllipse(brush2, num - 14f, rServidor.Y + rServidor.Height / 2 - 4, 8f, 8f);
			}
			using SolidBrush brush3 = new SolidBrush(Color.FromArgb(220, 255, 255, 255));
			g.DrawString(s, font2, brush3, num, (float)rServidor.Y + ((float)rServidor.Height - sizeF.Height) / 2f);
		}
		using Font font3 = new Font("Segoe UI", 8f, FontStyle.Bold);
		using SolidBrush brush4 = new SolidBrush(Color.FromArgb(170, 255, 255, 255));
		g.DrawString(Idioma.T("servidor").ToUpperInvariant(), font3, brush4, rServidor.X, rServidor.Y - 18);
	}

	private void DesenharMenuServidor(Graphics g)
	{
		Rectangle rect = ItemServidor();
		Rectangle rect2 = new Rectangle(rect.X, rect.Y - 26, rect.Width, rect.Height + 26);
		using (SolidBrush brush = new SolidBrush(Color.FromArgb(240, 22, 16, 30)))
		{
			g.FillRectangle(brush, rect2);
		}
		using (Pen pen = new Pen(Color.FromArgb(90, 255, 255, 255)))
		{
			g.DrawRectangle(pen, rect2.X, rect2.Y, rect2.Width - 1, rect2.Height - 1);
		}
		using (Font font = new Font("Segoe UI", 8f, FontStyle.Bold))
		{
			using SolidBrush brush2 = new SolidBrush(Color.FromArgb(160, 255, 255, 255));
			g.DrawString(Idioma.T("escolha_servidor").ToUpperInvariant(), font, brush2, rect2.X + 14, rect2.Y + 7);
		}
		if (_sobre == "srv:item")
		{
			using SolidBrush brush3 = new SolidBrush(Color.FromArgb(40, 255, 255, 255));
			g.FillRectangle(brush3, rect);
		}
		Confirmado(g, new Rectangle(rect.X + 14, rect.Y + 12, 20, 20), Laranja);
		using (Font font2 = new Font("Segoe UI", 11.5f, FontStyle.Bold))
		{
			g.DrawString(_cfg.Servidor, font2, Brushes.White, rect.X + 48, rect.Y + 11);
		}
		if (_online < 0)
		{
			return;
		}
		using Font font3 = new Font("Segoe UI", 8.5f);
		using SolidBrush brush4 = new SolidBrush(Color.FromArgb(190, 255, 255, 255));
		string s = Idioma.T("jogadores", _online);
		SizeF sizeF = g.MeasureString(s, font3);
		g.DrawString(s, font3, brush4, (float)(rect.Right - 14) - sizeF.Width, (float)rect.Y + ((float)rect.Height - sizeF.Height) / 2f);
	}

	private void DesenharMenuIdioma(Graphics g)
	{
		Rectangle rect = Rectangle.Union(ItemIdioma(0), ItemIdioma(Idioma.Codigos.Length - 1));
		using (SolidBrush brush = new SolidBrush(Color.FromArgb(242, 22, 16, 30)))
		{
			g.FillRectangle(brush, rect);
		}
		using (Pen pen = new Pen(Color.FromArgb(90, 255, 255, 255)))
		{
			g.DrawRectangle(pen, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
		}
		for (int i = 0; i < Idioma.Codigos.Length; i++)
		{
			Rectangle rect2 = ItemIdioma(i);
			bool flag = Idioma.Codigos[i] == Idioma.Atual;
			if (_sobre == "lang:" + Idioma.Codigos[i])
			{
				using SolidBrush brush2 = new SolidBrush(Color.FromArgb(40, 255, 255, 255));
				g.FillRectangle(brush2, rect2);
			}
			Rectangle rectangle = new Rectangle(rect2.X + 10, rect2.Y + 8, 30, 18);
			using (GraphicsPath path = Arredondado(rectangle, 4))
			{
				using SolidBrush brush3 = new SolidBrush(flag ? Vermelho : Color.FromArgb(80, 255, 255, 255));
				g.FillPath(brush3, path);
			}
			using (Font font = new Font("Segoe UI", 7.5f, FontStyle.Bold))
			{
				using StringFormat format = new StringFormat
				{
					Alignment = StringAlignment.Center,
					LineAlignment = StringAlignment.Center
				};
				g.DrawString(Idioma.Codigos[i].ToUpperInvariant(), font, Brushes.White, rectangle, format);
			}
			using Font font2 = new Font("Segoe UI", 9.5f, flag ? FontStyle.Bold : FontStyle.Regular);
			g.DrawString(Idioma.Nomes[i], font2, Brushes.White, rect2.X + 48, rect2.Y + 7);
		}
	}

	private void DesenharPlay(Graphics g)
	{
		bool flag = _podeJogar && !_ocupado;
		bool flag2 = flag && _sobre == "play";
		Rectangle rectangle = ((flag && _apertado == "play") ? new Rectangle(RPlay.X + 2, RPlay.Y + 2, RPlay.Width - 4, RPlay.Height - 4) : RPlay);
		if (flag2)
		{
			for (int num = 3; num >= 1; num--)
			{
				using GraphicsPath path = Arredondado(Rectangle.Inflate(rectangle, num * 3, num * 3), 14 + num * 3);
				using SolidBrush brush = new SolidBrush(Color.FromArgb(22, 255, 90, 60));
				g.FillPath(brush, path);
			}
		}
		g.InterpolationMode = InterpolationMode.HighQualityBilinear;
		g.DrawImage(flag ? _play : _playOff, rectangle);
		if (flag2)
		{
			using GraphicsPath path2 = Arredondado(rectangle, 12);
			using SolidBrush brush2 = new SolidBrush(Color.FromArgb(28, 255, 255, 255));
			g.FillPath(brush2, path2);
		}
		g.InterpolationMode = InterpolationMode.Low;
		Barra(g, RBarra, _geral, Vermelho);
		for (int i = 0; i < _fundos.Length; i++)
		{
			if (_fundos.Length <= 1)
			{
				break;
			}
			Rectangle rectangle2 = Ponto(i);
			int num2 = ((i == _fundoAtual) ? 12 : 9);
			Color color;
			if (i == _fundoAtual)
			{
				color = Vermelho;
			}
			else
			{
				color = ((_sobre == "dot:" + i) ? Color.FromArgb(220, 255, 255, 255) : Color.FromArgb(120, 255, 255, 255));
			}
			using SolidBrush brush3 = new SolidBrush(color);
			g.FillEllipse(brush3, rectangle2.X + (rectangle2.Width - num2) / 2, rectangle2.Y + (rectangle2.Height - num2) / 2, num2, num2);
		}
	}

	private void DesenharColuna(Graphics g)
	{
		using (Font font = new Font("Segoe UI", 11f, FontStyle.Bold))
		{
			using SolidBrush brush = new SolidBrush(Color.FromArgb(160, 0, 0, 0));
			using SolidBrush brush2 = new SolidBrush(_pronto ? Color.FromArgb(120, 230, 150) : Color.White);
			using StringFormat format = new StringFormat
			{
				Trimming = StringTrimming.EllipsisCharacter
			};
			RectangleF layoutRectangle = new RectangleF(428f, 584f, _ocupado ? 380 : 470, 44f);
			g.DrawString(_status, font, brush, new RectangleF(layoutRectangle.X + 1f, layoutRectangle.Y + 1f, layoutRectangle.Width, layoutRectangle.Height), format);
			g.DrawString(_status, font, brush2, layoutRectangle, format);
		}
		if (_ocupado)
		{
			using Font font2 = new Font("Segoe UI", 20f, FontStyle.Bold);
			using StringFormat format2 = new StringFormat
			{
				Alignment = StringAlignment.Far
			};
			g.DrawString((int)Math.Round(_geral * 100.0) + "%", font2, Brushes.White, new RectangleF(428f, 576f, 470f, 40f), format2);
		}
		if (_arquivo >= 0.0)
		{
			Barra(g, new Rectangle(428, 630, 300, 6), _arquivo, Laranja);
		}
		List<string> list = new List<string>();
		if (_online >= 0)
		{
			list.Add(Idioma.T("jogadores", _online));
		}
		if (_contas >= 0)
		{
			list.Add(Idioma.T("contas", _contas));
		}
		if (list.Count > 0)
		{
			using Font font3 = new Font("Segoe UI", 9f);
			using SolidBrush brush3 = new SolidBrush(Color.FromArgb(185, 255, 255, 255));
			g.DrawString(string.Join("   |   ", list), font3, brush3, 428f, 644f);
		}
		bool flag = !_ocupado;
		bool flag2 = flag && _sobre == "verificar";
		Color color;
		if (flag)
		{
			color = (flag2 ? Color.White : Color.FromArgb(205, 255, 255, 255));
		}
		else
		{
			color = Color.FromArgb(90, 255, 255, 255);
		}
		Recarregar(g, new Rectangle(RVerificar.X + 2, RVerificar.Y + 6, 15, 15), color);
		using (Font font4 = new Font("Segoe UI", 9.5f, FontStyle.Bold))
		{
			using SolidBrush brush4 = new SolidBrush(color);
			g.DrawString(Idioma.T("verificar"), font4, brush4, RVerificar.X + 24, RVerificar.Y + 4);
		}
		if (flag2)
		{
			using SolidBrush brush5 = new SolidBrush(Vermelho);
			g.FillRectangle(brush5, RVerificar.X + 24, RVerificar.Bottom - 3, 120, 2);
		}
		using Font font5 = new Font("Segoe UI", 7.5f);
		using SolidBrush brush6 = new SolidBrush(Color.FromArgb(120, 255, 255, 255));
		g.DrawString("Launcher " + Application.ProductVersion, font5, brush6, 428f, 700f);
	}

	private void DesenharCard(Graphics g, Bitmap img, Rectangle r0, string id)
	{
		bool flag = _sobre == id;
		bool flag2 = _apertado == id;
		Rectangle rectangle = ((flag && !flag2) ? new Rectangle(r0.X, r0.Y - 3, r0.Width, r0.Height) : r0);
		using (GraphicsPath path = Arredondado(new Rectangle(rectangle.X + 2, rectangle.Y + 5, rectangle.Width, rectangle.Height), 12))
		{
			using SolidBrush brush = new SolidBrush(Color.FromArgb(110, 0, 0, 0));
			g.FillPath(brush, path);
		}
		g.InterpolationMode = InterpolationMode.HighQualityBilinear;
		g.DrawImage(img, rectangle);
		g.InterpolationMode = InterpolationMode.Low;
		if (!flag)
		{
			return;
		}
		using GraphicsPath path2 = Arredondado(rectangle, 11);
		using (SolidBrush brush2 = new SolidBrush(Color.FromArgb(22, 255, 255, 255)))
		{
			g.FillPath(brush2, path2);
		}
		using Pen pen = new Pen(Color.FromArgb(200, 255, 255, 255), 1.5f);
		g.DrawPath(pen, path2);
	}

	private Rectangle OpLinha(int y, int h)
	{
		return new Rectangle(ROpPainel.X + 18, ROpPainel.Y + y, ROpPainel.Width - 36, h);
	}

	private Rectangle OpModo(int i)
	{
		Rectangle rectangle = OpLinha(70, 34);
		int num = (rectangle.Width - 8) / 2;
		return new Rectangle(rectangle.X + i * (num + 8), rectangle.Y, num, rectangle.Height);
	}

	private Rectangle OpRes()
	{
		return OpLinha(138, 34);
	}

	private Rectangle OpResItem(int i)
	{
		Rectangle rectangle = OpRes();
		return new Rectangle(rectangle.X, rectangle.Bottom + 2 + i * 28, rectangle.Width, 28);
	}

	private Rectangle OpGraf(int i)
	{
		Rectangle rectangle = OpLinha(206, 32);
		int num = (rectangle.Width - 18) / 4;
		return new Rectangle(rectangle.X + i * (num + 6), rectangle.Y, num, rectangle.Height);
	}

	private Rectangle OpSom()
	{
		return OpLinha(300, 24);
	}

	private Rectangle OpPasta()
	{
		return OpLinha(336, 34);
	}

	private Rectangle OpSalvar()
	{
		return OpLinha(380, 38);
	}

	private void AlternarOpcoes()
	{
		_menuOpcoes = !_menuOpcoes;
		_menuRes = false;
		_menuIdioma = (_menuServidor = false);
		if (_menuOpcoes)
		{
			_opSalvo = OpcoesJogo.Ler();
			_op = _opSalvo.Copia();
			_opMsg = null;
		}
		MontarZonas();
		Invalidate();
	}

	private void MontarZonasOpcoes()
	{
		if (_menuRes)
		{
			for (int i = 0; i < OpcoesJogo.Resolucoes.Length; i++)
			{
				int k = i;
				Z("op:res:" + i, OpResItem(i), () =>
				{
					_op.Largura = OpcoesJogo.Resolucoes[k][0];
					_op.Altura = OpcoesJogo.Resolucoes[k][1];
					_menuRes = false;
					_opMsg = null;
				});
			}
		}
		Z("op:janela", OpModo(0), () =>
		{
			_op.Janela = true;
			_opMsg = null;
		});
		Z("op:cheia", OpModo(1), () =>
		{
			_op.Janela = false;
			_opMsg = null;
		});
		Z("op:res", OpRes(), () =>
		{
			_menuRes = !_menuRes;
		});
		for (int num = 0; num < 4; num++)
		{
			int k2 = num;
			Z("op:graf:" + num, OpGraf(num), () =>
			{
				_op.Graficos = k2;
				_opMsg = null;
			});
		}
		Z("op:som", OpSom(), () =>
		{
			_op.Som = !_op.Som;
			_opMsg = null;
		});
		Z("op:pasta", OpPasta(), () =>
		{
			Abrir(_pasta);
		});
		Z("op:salvar", OpSalvar(), SalvarOpcoes, () => !_op.Igual(_opSalvo));
		void Z(string id, Rectangle r, Action a, Func<bool> ativa = null)
		{
			_zonas.Add(new Zona
			{
				Id = id,
				R = r,
				Clique = () =>
				{
					a();
					MontarZonas();
					Invalidate();
				},
				Ativa = (ativa ?? ((Func<bool>)(() => true)))
			});
		}
	}

	private void SalvarOpcoes()
	{
		if (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(_cfg.Jogo)).Any())
		{
			_opMsg = Idioma.T("op_feche");
			_opMsgOk = false;
			return;
		}
		string text = OpcoesJogo.Salvar(_op);
		if (text == null)
		{
			_opSalvo = OpcoesJogo.Ler();
			_op = _opSalvo.Copia();
			_opMsg = Idioma.T("op_salvo");
			_opMsgOk = true;
		}
		else
		{
			_opMsg = text;
			_opMsgOk = false;
		}
	}

	private void DesenharOpcoes(Graphics g)
	{
		Rectangle rOpPainel = ROpPainel;
		using (GraphicsPath path = Arredondado(rOpPainel, 12))
		{
			using (SolidBrush brush = new SolidBrush(Color.FromArgb(246, 18, 12, 30)))
			{
				g.FillPath(brush, path);
			}
			using Pen pen = new Pen(Color.FromArgb(110, 255, 255, 255));
			g.DrawPath(pen, path);
		}
		using (Font font = new Font("Segoe UI", 12f, FontStyle.Bold))
		{
			g.DrawString(Idioma.T("op_titulo"), font, Brushes.White, rOpPainel.X + 16, rOpPainel.Y + 14);
		}
		Rotulo(g, Idioma.T("op_modo"), 50);
		Escolha(g, OpModo(0), Idioma.T("op_janela"), _op.Janela, "op:janela");
		Escolha(g, OpModo(1), Idioma.T("op_tela_cheia"), !_op.Janela, "op:cheia");
		Rotulo(g, Idioma.T("op_resolucao"), 118);
		Rectangle r = OpRes();
		Caixa(g, r, _sobre == "op:res" || _menuRes);
		using (Font font2 = new Font("Segoe UI", 10.5f, FontStyle.Bold))
		{
			g.DrawString(_op.Largura + "x" + _op.Altura, font2, Brushes.White, r.X + 12, r.Y + 7);
		}
		Seta(g, new Point(r.Right - 18, r.Y + r.Height / 2), _menuRes);
		Rotulo(g, Idioma.T("op_graficos"), 186);
		for (int i = 0; i < 4; i++)
		{
			Escolha(g, OpGraf(i), OpcoesJogo.NomesGraficos[i], _op.Graficos == i, "op:graf:" + i);
		}
		using (Font font3 = new Font("Segoe UI", 8.5f))
		{
			using SolidBrush brush2 = new SolidBrush(Color.FromArgb(175, 255, 255, 255));
			g.DrawString(Idioma.T("op_graficos_dica") + ((_op.Graficos < 0) ? (" " + Idioma.T("op_personalizado")) : ""), font3, brush2, new RectangleF(rOpPainel.X + 18, rOpPainel.Y + 244, rOpPainel.Width - 36, 50f));
		}
		Rectangle rectangle = OpSom();
		Rectangle r2 = new Rectangle(rectangle.X, rectangle.Y + 3, 18, 18);
		using (GraphicsPath path2 = Arredondado(r2, 4))
		{
			using (SolidBrush brush3 = new SolidBrush(_op.Som ? Laranja : Color.FromArgb(60, 255, 255, 255)))
			{
				g.FillPath(brush3, path2);
			}
			using Pen pen2 = new Pen(Color.FromArgb((_sobre == "op:som") ? 230 : 140, 255, 255, 255));
			g.DrawPath(pen2, path2);
		}
		if (_op.Som)
		{
			using Pen pen3 = new Pen(Color.White, 2.2f)
			{
				StartCap = LineCap.Round,
				EndCap = LineCap.Round,
				LineJoin = LineJoin.Round
			};
			g.DrawLines(pen3, new PointF[3]
			{
				new PointF(r2.X + 4, (float)r2.Y + 9.5f),
				new PointF((float)r2.X + 7.5f, r2.Y + 13),
				new PointF(r2.X + 14, (float)r2.Y + 5.5f)
			});
		}
		using (Font font4 = new Font("Segoe UI", 10f))
		{
			g.DrawString(Idioma.T("op_som"), font4, Brushes.White, r2.Right + 8, rectangle.Y + 2);
		}
		Rectangle rectangle2 = OpPasta();
		Caixa(g, rectangle2, _sobre == "op:pasta");
		using (Font font5 = new Font("Segoe UI", 10f, FontStyle.Bold))
		{
			using StringFormat format = new StringFormat
			{
				Alignment = StringAlignment.Center,
				LineAlignment = StringAlignment.Center
			};
			g.DrawString(Idioma.T("op_pasta"), font5, Brushes.White, rectangle2, format);
		}
		Rectangle rectangle3 = OpSalvar();
		bool flag = !_op.Igual(_opSalvo);
		using (GraphicsPath path3 = Arredondado(rectangle3, 8))
		{
			Color color;
			if (flag)
			{
				color = ((_sobre == "op:salvar") ? Color.FromArgb(255, 145, 60) : Laranja);
			}
			else
			{
				color = Color.FromArgb(60, 255, 255, 255);
			}
			using SolidBrush brush4 = new SolidBrush(color);
			g.FillPath(brush4, path3);
		}
		using (Font font6 = new Font("Segoe UI", 10.5f, FontStyle.Bold))
		{
			using SolidBrush brush5 = new SolidBrush(flag ? Color.White : Color.FromArgb(150, 255, 255, 255));
			using StringFormat format2 = new StringFormat
			{
				Alignment = StringAlignment.Center,
				LineAlignment = StringAlignment.Center
			};
			g.DrawString(Idioma.T("op_salvar"), font6, brush5, rectangle3, format2);
		}
		if (_opMsg != null)
		{
			using Font font7 = new Font("Segoe UI", 8.5f, FontStyle.Bold);
			using SolidBrush brush6 = new SolidBrush(_opMsgOk ? Color.FromArgb(120, 230, 150) : Color.FromArgb(255, 140, 140));
			g.DrawString(_opMsg, font7, brush6, new RectangleF(rOpPainel.X + 18, rectangle3.Bottom + 4, rOpPainel.Width - 36, 24f));
		}
		if (!_menuRes)
		{
			return;
		}
		Rectangle rect = Rectangle.Union(OpResItem(0), OpResItem(OpcoesJogo.Resolucoes.Length - 1));
		using (SolidBrush brush7 = new SolidBrush(Color.FromArgb(252, 24, 18, 36)))
		{
			g.FillRectangle(brush7, rect);
		}
		using (Pen pen4 = new Pen(Color.FromArgb(120, 255, 255, 255)))
		{
			g.DrawRectangle(pen4, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
		}
		for (int j = 0; j < OpcoesJogo.Resolucoes.Length; j++)
		{
			Rectangle rect2 = OpResItem(j);
			int[] array = OpcoesJogo.Resolucoes[j];
			bool flag2 = array[0] == _op.Largura && array[1] == _op.Altura;
			if (flag2 || _sobre == "op:res:" + j)
			{
				using SolidBrush brush8 = new SolidBrush(flag2 ? Color.FromArgb(150, Laranja) : Color.FromArgb(40, 255, 255, 255));
				g.FillRectangle(brush8, rect2);
			}
			using Font font8 = new Font("Segoe UI", 10f, flag2 ? FontStyle.Bold : FontStyle.Regular);
			g.DrawString(array[0] + "x" + array[1], font8, Brushes.White, rect2.X + 12, rect2.Y + 5);
		}
	}

	private void Rotulo(Graphics g, string t, int y)
	{
		using Font font = new Font("Segoe UI", 9f);
		using SolidBrush brush = new SolidBrush(Color.FromArgb(185, 255, 255, 255));
		g.DrawString(t, font, brush, ROpPainel.X + 18, ROpPainel.Y + y);
	}

	private void Caixa(Graphics g, Rectangle r, bool realce)
	{
		using GraphicsPath path = Arredondado(r, 7);
		using (SolidBrush brush = new SolidBrush(realce ? Color.FromArgb(70, 60, 50, 90) : Color.FromArgb(40, 255, 255, 255)))
		{
			g.FillPath(brush, path);
		}
		using Pen pen = new Pen(Color.FromArgb(realce ? 190 : 90, 255, 255, 255));
		g.DrawPath(pen, path);
	}

	private void Escolha(Graphics g, Rectangle r, string texto, bool marcado, string id)
	{
		using (GraphicsPath path = Arredondado(r, 7))
		{
			Color color;
			if (marcado)
			{
				color = Laranja;
			}
			else
			{
				color = ((_sobre == id) ? Color.FromArgb(70, 255, 255, 255) : Color.FromArgb(30, 255, 255, 255));
			}
			using (SolidBrush brush = new SolidBrush(color))
			{
				g.FillPath(brush, path);
			}
			using Pen pen = new Pen(Color.FromArgb((!marcado) ? 90 : 0, 255, 255, 255));
			g.DrawPath(pen, path);
		}
		using Font font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
		using StringFormat format = new StringFormat
		{
			Alignment = StringAlignment.Center,
			LineAlignment = StringAlignment.Center
		};
		g.DrawString(texto, font, Brushes.White, r, format);
	}

	private static void Engrenagem(Graphics g, Rectangle r)
	{
		float num = (float)r.X + (float)r.Width / 2f;
		float num2 = (float)r.Y + (float)r.Height / 2f;
		using Pen pen = new Pen(Color.White, 1.5f);
		for (int i = 0; i < 8; i++)
		{
			double num3 = (double)i * Math.PI / 4.0;
			g.DrawLine(pen, num + (float)(5.2 * Math.Cos(num3)), num2 + (float)(5.2 * Math.Sin(num3)), num + (float)(7.6 * Math.Cos(num3)), num2 + (float)(7.6 * Math.Sin(num3)));
		}
		g.DrawEllipse(pen, num - 5.2f, num2 - 5.2f, 10.4f, 10.4f);
		g.DrawEllipse(pen, num - 2.2f, num2 - 2.2f, 4.4f, 4.4f);
	}

	private static void Pilula(Graphics g, Rectangle r, bool realce)
	{
		using GraphicsPath path = Arredondado(r, r.Height / 2);
		using (SolidBrush brush = new SolidBrush(realce ? Color.FromArgb(120, 60, 50, 80) : Color.FromArgb(90, 20, 14, 30)))
		{
			g.FillPath(brush, path);
		}
		using Pen pen = new Pen(Color.FromArgb(realce ? 160 : 80, 255, 255, 255));
		g.DrawPath(pen, path);
	}

	private static void Globo(Graphics g, Rectangle r)
	{
		using Pen pen = new Pen(Color.White, 1.4f);
		g.DrawEllipse(pen, r);
		g.DrawEllipse(pen, (float)r.X + (float)r.Width / 4f, r.Y, (float)r.Width / 2f, r.Height);
		g.DrawLine(pen, r.X, (float)r.Y + (float)r.Height / 2f, r.Right, (float)r.Y + (float)r.Height / 2f);
		g.DrawLine(pen, r.X + 2, (float)r.Y + (float)r.Height * 0.27f, r.Right - 2, (float)r.Y + (float)r.Height * 0.27f);
		g.DrawLine(pen, r.X + 2, (float)r.Y + (float)r.Height * 0.73f, r.Right - 2, (float)r.Y + (float)r.Height * 0.73f);
	}

	private static void Seta(Graphics g, Point c, bool paraCima)
	{
		int num = ((!paraCima) ? 1 : (-1));
		using Pen pen = new Pen(Color.White, 1.6f)
		{
			StartCap = LineCap.Round,
			EndCap = LineCap.Round
		};
		g.DrawLines(pen, new Point[3]
		{
			new Point(c.X - 4, c.Y - 2 * num),
			new Point(c.X, c.Y + 2 * num),
			new Point(c.X + 4, c.Y - 2 * num)
		});
	}

	private static void Confirmado(Graphics g, Rectangle r, Color cor)
	{
		using (GraphicsPath path = Arredondado(r, 4))
		{
			using Pen pen = new Pen(cor, 1.6f);
			g.DrawPath(pen, path);
		}
		using Pen pen2 = new Pen(cor, 2.2f)
		{
			StartCap = LineCap.Round,
			EndCap = LineCap.Round,
			LineJoin = LineJoin.Round
		};
		g.DrawLines(pen2, new PointF[3]
		{
			new PointF((float)r.X + (float)r.Width * 0.26f, (float)r.Y + (float)r.Height * 0.52f),
			new PointF((float)r.X + (float)r.Width * 0.44f, (float)r.Y + (float)r.Height * 0.7f),
			new PointF((float)r.X + (float)r.Width * 0.76f, (float)r.Y + (float)r.Height * 0.32f)
		});
	}

	private static void Recarregar(Graphics g, Rectangle r, Color cor)
	{
		using Pen pen = new Pen(cor, 1.8f)
		{
			StartCap = LineCap.Round
		};
		g.DrawArc(pen, r, 40f, 280f);
		double num = 5.585053606381854;
		float num2 = (float)r.X + (float)r.Width / 2f;
		float num3 = (float)r.Y + (float)r.Height / 2f;
		float num4 = (float)r.Width / 2f;
		PointF pointF = new PointF(num2 + (float)((double)num4 * Math.Cos(num)), num3 + (float)((double)num4 * Math.Sin(num)));
		using SolidBrush brush = new SolidBrush(cor);
		g.FillPolygon(brush, new PointF[3]
		{
			new PointF(pointF.X - 1f, pointF.Y - 5f),
			new PointF(pointF.X + 5f, pointF.Y + 1f),
			new PointF(pointF.X - 3f, pointF.Y + 3f)
		});
	}

	private static void Barra(Graphics g, Rectangle r, double v, Color cor)
	{
		using (GraphicsPath path = Arredondado(r, r.Height / 2))
		{
			using SolidBrush brush = new SolidBrush(Color.FromArgb(200, 44, 36, 60));
			g.FillPath(brush, path);
		}
		int num = (int)((double)r.Width * Math.Max(0.0, Math.Min(1.0, v)));
		if (num < r.Height)
		{
			return;
		}
		Rectangle r2 = new Rectangle(r.X, r.Y, num, r.Height);
		using GraphicsPath path2 = Arredondado(r2, r.Height / 2);
		using LinearGradientBrush brush2 = new LinearGradientBrush(new Rectangle(r2.X, r2.Y, r2.Width, r2.Height + 1), ControlPaint.Light(cor), cor, 90f);
		g.FillPath(brush2, path2);
	}

	internal static GraphicsPath Arredondado(Rectangle r, int raio)
	{
		GraphicsPath graphicsPath = new GraphicsPath();
		int num = Math.Max(1, Math.Min(raio * 2, Math.Min(r.Width, r.Height)));
		graphicsPath.AddArc(r.X, r.Y, num, num, 180f, 90f);
		graphicsPath.AddArc(r.Right - num, r.Y, num, num, 270f, 90f);
		graphicsPath.AddArc(r.Right - num, r.Bottom - num, num, num, 0f, 90f);
		graphicsPath.AddArc(r.X, r.Bottom - num, num, num, 90f, 90f);
		graphicsPath.CloseFigure();
		return graphicsPath;
	}
}
