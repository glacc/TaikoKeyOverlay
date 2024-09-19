using SFML.Graphics;
using SFML.Window;

using Glacc.Config;
using System.Reflection;

namespace KeyOverlayTaiko
{
	internal class Program
	{
		static ConfigFile? configFile;

		static int windowWidth = 240;
		static int windowHeight = 640;

		static int maxFramerate = 60;

		static double spacingPercent = 40;

		static string fontFileName = "HarmonyOS_Sans_Regular.ttf";

		static int keySize = 80;

		static Keyboard.Key keyKatsuL = Keyboard.Key.Z;
		static Keyboard.Key keyDonL = Keyboard.Key.X;
		static Keyboard.Key keyDonR = Keyboard.Key.Period;
		static Keyboard.Key keyKatsuR = Keyboard.Key.Slash;

		static Keyboard.Key keyResetCounter = Keyboard.Key.R;

		static double barSpeed = 12.5;

		static int releaseFadeTime = 8;

		static RenderWindow? window;

		static Font? font;

		// Don:     Red     (235, 69,  44)
		// Katsu:   Blue    (68,  141, 171)
		// Rolled:  Yellow  (252, 83,  6)

		static Color colorDon = new Color(235, 69, 44);
		static Color colorKatsu = new Color(68, 141, 171);
		static Color colorRoll = new Color(252, 83, 6);

		class Bar
		{
			int startLineThickness = 6;

			public int px;
			public int width;

			public double startY;
			public double endY;
			public bool hold;

			public bool toDelete = false;

			public Color color;

			RectangleShape startRect;
			RectangleShape barRect;

			public void UpdateDraw(RenderTarget? renderTarget)
			{
				// Update

				if (startY > startLineThickness)
					startY -= barSpeed;

				if (!hold)
					endY -= barSpeed;

				if (endY <= startLineThickness)
				{
					toDelete = true;
					return;
				}

				// Draw

				if (renderTarget == null)
					return;

				barRect.Position = new SFML.System.Vector2f(px, (float)startY);
				barRect.Size = new SFML.System.Vector2f(width, (float)(endY - startY));

				startRect.Position = new SFML.System.Vector2f(px, (float)startY);

				renderTarget.Draw(barRect);
				renderTarget.Draw(startRect);
			}

			public Bar(int px, int width, double endY, Color color)
			{
				hold = true;

				this.px = px;
				this.width = width;

				startY = this.endY = endY;
				this.color = color;

				startRect = new RectangleShape();
				startRect.Size = new SFML.System.Vector2f(width, startLineThickness);
				startRect.FillColor = new Color(255, 255, 255, 192);

				barRect = new RectangleShape();
				barRect.FillColor = this.color;
			}
		}

		class Key
		{
			public RenderTarget? renderTarget;

			public Font? font;

			public Keyboard.Key keyDon;
			public Keyboard.Key keyKatsu;

			public int px;
			public int py;
			public int width;
			public int height;

			public int borderSize = 8;

			public int keyCount = 0;

			public int releaseTimer = 0;

			bool keyDonPressed = false;
			bool keyKatsuPressed = false;
			enum PressedKey
			{
				Don,
				Katsu,
				None
			}
			PressedKey lastPressed = PressedKey.None;

			Color borderColor = Color.White;
			Color keyColor = Color.Black;
			Color keyColorTarget = Color.Black;

			RectangleShape borderRect;
			RectangleShape keyRect;
			Text keyCountTxt;

			List<Bar> bars = new List<Bar>();

			void UpdateKeyPress()
			{
				if (Keyboard.IsKeyPressed(keyDon))
				{
					if (!keyDonPressed)
					{
						keyCount++;

						borderColor = keyColorTarget = colorDon;
						keyColorTarget.R = (byte)(keyColorTarget.R * 4 / 5);
						keyColorTarget.G = (byte)(keyColorTarget.G * 4 / 5);
						keyColorTarget.B = (byte)(keyColorTarget.B * 4 / 5);

						lastPressed = PressedKey.Don;

						if (bars.Count > 0)
							bars[bars.Count - 1].hold = false;
						
						bars.Add(new Bar(px - width / 2, width, py - height / 2, colorDon));
					}
				}

				if (Keyboard.IsKeyPressed(keyKatsu))
				{
					if (!keyKatsuPressed)
					{
						keyCount++;

						borderColor = keyColorTarget = colorKatsu;
						keyColorTarget.R = (byte)(keyColorTarget.R * 4 / 5);
						keyColorTarget.G = (byte)(keyColorTarget.G * 4 / 5);
						keyColorTarget.B = (byte)(keyColorTarget.B * 4 / 5);

						lastPressed = PressedKey.Katsu;

						if (bars.Count > 0)
							bars[bars.Count - 1].hold = false;

						bars.Add(new Bar(px - width / 2, width, py - height / 2, colorKatsu));
					}
				}

				keyDonPressed = Keyboard.IsKeyPressed(keyDon);
				keyKatsuPressed = Keyboard.IsKeyPressed(keyKatsu);

				if ((lastPressed == PressedKey.Don && !keyDonPressed) || (lastPressed == PressedKey.Katsu && !keyKatsuPressed))
				{
					if (bars.Count > 0)
						bars[bars.Count - 1].hold = false;

					if (releaseTimer > 0)
					{
						releaseTimer--;
						keyColor.R = (byte)(keyColorTarget.R * releaseTimer / releaseFadeTime);
						keyColor.G = (byte)(keyColorTarget.G * releaseTimer / releaseFadeTime);
						keyColor.B = (byte)(keyColorTarget.B * releaseTimer / releaseFadeTime);
					}
					else
						keyColor = Color.Black;
				}
				else
				{
					releaseTimer = releaseFadeTime;
					keyColor = keyColorTarget;
				}
			}

			void UpdateBars()
			{
				int i = 0;
				while (i < bars.Count)
				{
					bars[i].UpdateDraw(renderTarget);

					if (bars[i].toDelete)
					{
						bars.RemoveAt(i);
						continue;
					}

					i++;
				}
			}

			public void Update()
			{
				if (renderTarget == null)
					return;

				UpdateKeyPress();

				UpdateBars();

				borderRect.Size = new SFML.System.Vector2f(width, height);
				borderRect.Position = new SFML.System.Vector2f(px - width / 2, py - height / 2);
				borderRect.FillColor = borderColor;

				int keyRectWidth = width - borderSize * 2;
				int keyRectHeight = height - borderSize * 2;
				keyRect.Size = new SFML.System.Vector2f(keyRectWidth, keyRectHeight);
				keyRect.Position = new SFML.System.Vector2f(px - keyRectWidth / 2, py - keyRectHeight / 2);
				keyRect.FillColor = keyColor;

				renderTarget.Draw(borderRect);
				renderTarget.Draw(keyRect);

				if (font != null)
				{
					keyCountTxt.DisplayedString = keyCount.ToString();
					keyCountTxt.Origin = new SFML.System.Vector2f(
							(int)(keyCountTxt.GetLocalBounds().Left + keyCountTxt.GetLocalBounds().Width / 2),
							(int)(keyCountTxt.GetLocalBounds().Top + keyCountTxt.GetLocalBounds().Height / 2)
						);
					keyCountTxt.Position = new SFML.System.Vector2f(px, py);

					renderTarget.Draw(keyCountTxt);
				}
			}

			public void Reset()
			{
				borderColor = Color.White;
				keyColor = Color.Black;
				keyColorTarget = Color.Black;

				keyCount = 0;
			}

			public Key(int px, int py, int width, int height, Keyboard.Key keyDon, Keyboard.Key keyKatsu, RenderTarget? renderTarget, Font? font = null)
			{
				this.px = px;
				this.py = py;
				this.width = width;
				this.height = height;
				this.keyDon = keyDon;
				this.keyKatsu = keyKatsu;
				this.renderTarget = renderTarget;

				borderRect = new RectangleShape();
				keyRect = new RectangleShape();

				this.font = font;
				if (font != null)
				{
					keyCountTxt = new Text("", font);
					keyCountTxt.CharacterSize = 24;
					keyCountTxt.FillColor = Color.White;
				}
				else
					keyCountTxt = new Text();
			}
		}

		static void LoadConfig()
		{
			configFile = new ConfigFile(null);

			configFile.SetTag("Settings");
			windowWidth = int.Parse(configFile.Read("Width", "240"));
			windowHeight = int.Parse(configFile.Read("Height", "640"));
			maxFramerate = int.Parse(configFile.Read("MaxFramerate", "60"));
			fontFileName = configFile.Read("FontFileName", "HarmonyOS_Sans_Regular.ttf");
			keySize = int.Parse(configFile.Read("KeySize", "80"));

			spacingPercent = double.Parse(configFile.Read("SpacingPercent", "40"));

			barSpeed = double.Parse(configFile.Read("BarSpeed", "12"));

			releaseFadeTime = int.Parse(configFile.Read("ReleaseFadeTime", "8"));

			configFile.SetTag("Keys");
			Enum.TryParse(configFile.Read("KeyKatsuL", "Z"), out keyKatsuL);
			Enum.TryParse(configFile.Read("KeyDonL", "X"), out keyDonL);
			Enum.TryParse(configFile.Read("KeyDonR", "Period"), out keyDonR);
			Enum.TryParse(configFile.Read("KeyKatsuR", "Slash"), out keyKatsuR);

			configFile.SetTag("Keys2");
			Enum.TryParse(configFile.Read("KeyResetCounter", "R"), out keyResetCounter);

			configFile.Save();
		}

		static void OnClose(object? sender, EventArgs e)
		{
			if (window != null)
				window.Close();
		}

		static void Main(string[] args)
		{
			LoadConfig();

			string assemblyPath = string.Join('\\', Assembly.GetExecutingAssembly().Location.Split("\\").SkipLast(1));
			string fontFilePath = Path.Combine(assemblyPath, fontFileName);
			font = new Font(fontFilePath);

			window = new RenderWindow(new SFML.Window.VideoMode((uint)windowWidth, (uint)windowHeight), "TaikoKeyOverlay");
			window.SetFramerateLimit(60);
			window.Closed += OnClose;
			
			Key keyLeft =	new Key((int)(windowWidth / 2 * (1 - spacingPercent / 100)), windowHeight - keySize, keySize, keySize, keyDonL, keyKatsuL, window, font);
			Key keyRight =	new Key((int)(windowWidth / 2 * (1 + spacingPercent / 100)), windowHeight - keySize, keySize, keySize, keyDonR, keyKatsuR, window, font);

			while (window.IsOpen)
			{
				window.DispatchEvents();

				window.Clear(Color.Black);

				keyLeft.Update();
				keyRight.Update();

				if (Keyboard.IsKeyPressed(keyResetCounter))
				{
					keyLeft.Reset();
					keyRight.Reset();
				}

				window.Display();
			}
		}
	}
}
