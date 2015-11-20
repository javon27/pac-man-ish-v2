using System;
using System.Threading;
using System.Collections.Generic;
using System.Media;
using System.Reflection;

namespace Console_Play
{
    class Program
    {
        const int TICK = 50;
        protected static int numEnemies = 50;
        protected static int newObjID = 0;
        protected static int score = 0;
        protected static SoundPlayer munchGhost;
        protected static int TIMER = 0;
        protected static int statusRow;
        protected static int width;
        protected static int height;
        protected static string TL = "╔";
        protected static string TR = "╗";
        protected static string BL = "╚";
        protected static string BR = "╝";
        protected static string H = "═";
        protected static string V = "║";
        private static bool rungame = true;
        private static List<Player> enemies;
        private static ConsoleKey[] keys = {
                ConsoleKey.UpArrow,
                ConsoleKey.DownArrow,
                ConsoleKey.LeftArrow,
                ConsoleKey.RightArrow
        };
        protected class Player
        {
            public int id;
            public string icon;
            public int x;
            public int y;
            public int last_x;
            public int last_y;
            public bool human;
            public ConsoleColor color;
            public ConsoleKey dir;

            public Player()
            {
                id = ++newObjID;
            }

        }

        protected static Player p1;


        static void Main(string[] args)
        {
            init();
            PrintStatus("Press ESC to exit... ");
            munchGhost = new SoundPlayer(Assembly.GetExecutingAssembly().GetManifestResourceStream("Console_Play.pacman_eatghost.wav"));

            RunGame();
        }

        private static void RunGame()
        {
            DrawPlayer(p1);
            Thread t_game = new Thread(GameTicker);
            t_game.Start();
            Thread t_player = new Thread(PlayerListener);
            t_player.Start();
            Thread t_enemies = new Thread(MoveAllThread);
            t_enemies.Start();
            Thread randomizeEnemies = new Thread(RandomizeMoves);
            randomizeEnemies.Start();

        }

        private static void ClearRow(int v, int length, int start = 1)
        {
            for (var i = 0; i < length; i++)
            {
                WriteAt(" ", start + i, v);
            }
        }

        static void DrawBorder()
        {
            width = Console.WindowWidth - 1;
            statusRow = Console.WindowHeight - 1;
            height = statusRow - 1;
            WriteAt(TL, 0, 0);
            WriteAt(TR, width, 0);
            WriteAt(BL, 0, height);
            WriteAt(BR, width, height);
            for (var i = 1; i < width; i++)
            {
                WriteAt(H, i, 0);
                WriteAt(H, i, height);
            }
            for (var i = 1; i < height; i++)
            {
                WriteAt(V, 0, i);
                WriteAt(V, width, i);
            }
        }

        private static void DrawPlayer(Player p)
        {
            WriteAt(" ", p.last_x, p.last_y);
            Console.ForegroundColor = p.color;
            WriteAt(p.icon, p.x, p.y);
        }

        private static void GameTicker()
        {
            while (rungame)
            {
                TIMER += 1;
                TIMER %= 100;
                if (TIMER == 0)
                {
                    --score;
                }
                Thread.Sleep(TICK);
            }
        }

        private static void init()
        {
            Console.Clear();
            Console.CursorVisible = false;
            DrawBorder();
            p1 = new Player
            {
                x = width / 2,
                last_x = width / 2,
                y = height / 2,
                last_y = height / 2,
                icon = "█",
                human = true,
                color = ConsoleColor.Yellow,
                dir = ConsoleKey.RightArrow
            };
            Random rand = new Random(DateTime.Now.GetHashCode());
            ConsoleColor[] colors =
            {
                ConsoleColor.Blue,
                ConsoleColor.Red,
                ConsoleColor.Magenta,
                ConsoleColor.Green
            };
            enemies = new List<Player>();
            for (var i = 0; i < numEnemies; i++)
            {
                Player x = new Player
                {
                    x = rand.Next() % (width - 1) + 1,
                    y = rand.Next() % (height - 1) + 1,
                    icon = "@",
                    human = false,
                    color = colors[i % 4],
                    dir = keys[i % 4]
                };
                enemies.Add(x);
            }
        }

        private static void MoveAllThread()
        {

            Random rand = new Random(DateTime.Now.GetHashCode());
            int timeTracker = TIMER;
            do
            {
                if (TIMER != timeTracker)
                {
                    timeTracker = TIMER;
                    MovePlayer(p1, p1.dir);
                    if (timeTracker % 3 == 0)
                    {
                        foreach (Player enemy in enemies.ToArray())
                        {
                            while (enemies.Contains(enemy) && !MovePlayer(enemy, enemy.dir))
                            {
                                enemy.dir = keys[rand.Next() % 4];
                            }
                        }
                    }
                }
            } while (rungame);
        }

        private static void RandomizeMoves()
        {
            Random rand = new Random(DateTime.Now.GetHashCode());
            do
            {
                foreach (Player enemy in enemies)
                {
                    enemy.dir = keys[rand.Next() % 4];
                }
                Thread.Sleep(TICK * 20);
            } while (rungame);
        }

        private static bool MovePlayer(Player p, ConsoleKey k)
        {
            if (width + 1 != Console.WindowWidth || height + 2 != Console.WindowHeight)
            {
                RefreshScreen();
            }
            p.last_x = p.x;
            p.last_y = p.y;
            switch (k)
            {
                case ConsoleKey.UpArrow:
                    p.y -= 1;
                    break;
                case ConsoleKey.DownArrow:
                    p.y += 1;
                    break;
                case ConsoleKey.LeftArrow:
                    p.x -= 1;
                    break;
                case ConsoleKey.RightArrow:
                    p.x += 1;
                    break;
            }
            if (NoCollision(p))
            {
                DrawPlayer(p);
                return true;
            }
            else
            {
                p.x = p.last_x;
                p.y = p.last_y;
                return false;
            }
        }

        private static bool NoCollision(Player c)
        {
            if (0 < c.x && c.x < width && 0 < c.y && c.y < height)
            {
                foreach (Player e in enemies.ToArray())
                {
                    if (c.id != e.id && c.x == e.x && c.y == e.y)
                    {
                        if (c.human)
                        {
                            PlayMunch();
                            enemies.Remove(e);
                            return true;
                        }
                        return false;
                    }
                    if (!c.human && c.x == p1.x && c.y == p1.y)
                    {
                        PlayMunch();
                        enemies.Remove(c);
                        return true;
                    }
                }
                return true;
            }
            return false;
        }

        private static void PlayerListener()
        {
            do
            {
                rungame = ProcessInput();
                if (enemies.Count == 0)
                {
                    rungame = false;
                    PrintStatus("You Win!");
                    Console.ReadKey();
                }
            } while (rungame);
            Console.Clear();
            Console.ResetColor();
            Environment.Exit(0);
        }

        private static void PlayMunch()
        {
            ++score;
            PrintScore();
            munchGhost.Stop();
            munchGhost.Play();
        }

        private static void PrintScore()
        {
            if (score < 0)
            {
                score = 0;
            }
            ClearRow(statusRow, 10, start: width - 10);
            Console.ForegroundColor = ConsoleColor.White;
            WriteAt((score).ToString(), width - 10, statusRow);
        }

        private static void PrintStatus(string s)
        {
            ClearRow(statusRow, width / 2);
            Console.ForegroundColor = ConsoleColor.White;
            WriteAt(s, 2, statusRow);
        }

        private static bool ProcessInput()
        {
            ConsoleKeyInfo cki = Console.ReadKey(true);
            switch (cki.Key)
            {
                case ConsoleKey.UpArrow:
                case ConsoleKey.DownArrow:
                case ConsoleKey.LeftArrow:
                case ConsoleKey.RightArrow:
                    p1.dir = cki.Key;
                    break;
                case ConsoleKey.Escape:
                    return false;
            }
            return true;
        }

        private static void RefreshScreen()
        {
            Console.Clear();
            DrawBorder();
            if (p1.x > width)
            {
                p1.x = p1.last_x = width - 1;
            }
            if (p1.y > height)
            {
                p1.y = p1.last_y = height - 1;
            }
            DrawPlayer(p1);
        }

        private static void test()
        {
            ConsoleKeyInfo cki;
            Console.TreatControlCAsInput = true;
            do
            {
                cki = Console.ReadKey(true);
                ClearRow(height / 2, width);
                string key = "";
                if ((cki.Modifiers & ConsoleModifiers.Alt) != 0)
                    key += "ALT+";
                if ((cki.Modifiers & ConsoleModifiers.Shift) != 0)
                    key += "SHIFT+";
                if ((cki.Modifiers & ConsoleModifiers.Control) != 0)
                    key += "CTL+";
                key += cki.Key.ToString() + ": " + cki.KeyChar.ToString();
                WriteAt(key, (width / 2) - (key.Length / 2), height / 2);
            } while (cki.Key != ConsoleKey.Escape);
        }

        protected static void WriteAt(string s, int x, int y)
        {
            try
            {
                Console.SetCursorPosition(x, y);
                Console.Write(s);
            }
            catch (ArgumentOutOfRangeException e)
            {
                Console.Clear();
                Console.WriteLine(e.Message);
            }
        }
    }
}
