using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace TetrisMonoGame;

public enum GameState { Running, GameOver }
public enum PieceType { None = -1, I, O, T, S, Z, J, L }

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    

    const int COLS = 10;
    const int ROWS = 20;
    const int BORDER = 10;
    const int CELL = 30;

    const int GRAVITY_SPEED = 800;
    const double LOCK_DELAY_MS = 500;
    const double DAS_MS = 150;
    const double ARR_MS = 50;
    const int LINES_PER_LEVEL = 5;

    double _gravityMs = GRAVITY_SPEED;
    double _fallAccum = 0;
    double _lockAccum = 0;
    double _dasAccum = 0;
    double _arrAccum = 0;
    int _moveDir = 0;
    double _elapsedTime = 0;
    double dt = 0;

    int _lines = 0, _level = 1, _score = 0, _highscore = 0, _bestLevel = 1;
    bool IsPaused = false;
    string _highscoreFile = "highscore.txt";



    SpriteFont _font;

    public static Color[] PieceColors { get; } = {
        Color.Blue, Color.Purple, Color.Yellow, Color.Cyan, Color.Red, Color.Green, Color.Orange
    };
    public Queue<PieceType> Bag { get; } = new();
    public Random Rng { get; } = new();

    GameState _state = GameState.Running;
    KeyboardState _ks, _ksPrev;
    int[,] _grid = new int[ROWS, COLS];

    Texture2D _pixel;

    Tetris _current,_ghost;


    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _graphics.IsFullScreen = false;

        int width = BORDER * 2 + COLS * CELL + 240;
        int height = BORDER * 2 + ROWS * CELL + 80;
        _graphics.PreferredBackBufferWidth = width;
        _graphics.PreferredBackBufferHeight = height;
    }

    protected override void Initialize()
    {
        // TODO: Add your initialization logic here

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _font = Content.Load<SpriteFont>("Default");

        LoadHighscore();
        ResetGrid();
        RefillBag();
        Spawn();
        UpdateGhost();
    }

    void SaveHighscore()
    {
        try
        {
            string[] lines = { _highscore.ToString(), _bestLevel.ToString() };
            System.IO.File.WriteAllLines(_highscoreFile, lines);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur sauvegarde highscore: {ex.Message}");
        }
    }


    void LoadHighscore()
    {
        try
        {
            if (System.IO.File.Exists(_highscoreFile))
            {
                string[] lines = System.IO.File.ReadAllLines(_highscoreFile);

                if (lines.Length > 0 && int.TryParse(lines[0], out int savedScore))
                    _highscore = savedScore;

                if (lines.Length > 1 && int.TryParse(lines[1], out int savedLevel))
                    _bestLevel = savedLevel;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lecture highscore: {ex.Message}");
        }
    }



    protected override void Update(GameTime gameTime)
    {
        ReadInput();

        if (_state == GameState.GameOver)
        {
            if (IsPressed(Keys.Enter)) ResetAll();
            return;
        }

        if (_ks.IsKeyDown(Keys.P) && !_ksPrev.IsKeyDown(Keys.P))
            IsPaused = !IsPaused;

        if (IsPaused) return;

        _elapsedTime += gameTime.ElapsedGameTime.TotalSeconds;
        dt = gameTime.ElapsedGameTime.TotalMilliseconds;

        HandleHorizontal(dt);
        if (IsPressed(Keys.Up) || IsPressed(Keys.X)) TryRotate(+1);
        if (_ks.IsKeyDown(Keys.Down)) SoftDrop();

        _fallAccum += dt;
        while (_fallAccum >= _gravityMs)
        {
            _fallAccum -= _gravityMs;
            StepDown();
        }

        bool newRecord = false;

        if (_score > _highscore)
        {
            _highscore = _score;
            newRecord = true;
        }

        if (_level > _bestLevel)
        {
            _bestLevel = _level;
            newRecord = true;
        }

        if (newRecord)
            SaveHighscore();


        base.Update(gameTime);
    }


    bool Collides(Tetris t)
    {
        for (int r = 0; r < 4; r++)
        {
            for (int c = 0; c < 4; c++)
            {
                if (t.Cell(c, r) != 0)
                {
                    int gx = t.X + c;
                    int gy = t.Y + r;

                    if (gx < 0 || gx >= COLS || gy >= ROWS) return true;

                    if (gy >= 0 && _grid[gy, gx] != 0) return true;
                }

                
            }
        }

        return false;
    }

    void StepDown()
    {
        var test = _current.Clone();
        test.Y += 1;

        if (!Collides(test))
        {
            _current = test;
            UpdateGhost();
            _lockAccum = 0;
        }
        else
        {
            _lockAccum += _gravityMs;
            if (_lockAccum >= LOCK_DELAY_MS)
            {
                LockPiece();
                ClearLines();
                Spawn();
            }
        }
    }


    void LockPiece()
    {
        bool topOut = false;

        foreach (var (rx, ry) in _current.IterCells())
        {
            int gx = _current.X + rx;
            int gy = _current.Y + ry;

            if (gy >= 0)
            {
                if (gy >= 0 && gy < ROWS && gx >= 0 && gx < COLS)
                    _grid[gy, gx] = (int)_current.Type + 1;
            }
            else { topOut = true; }

            
        }

        if (topOut) _state = GameState.GameOver;
    }

    void ResetAll()
    {
        _elapsedTime = 0;
        _state = GameState.Running;
        _level = 1; 
        _score = 0;
        _lines = 0;
        _gravityMs = GRAVITY_SPEED;
        ResetGrid();
        while (Bag.Count > 0) Bag.Dequeue();
        RefillBag();
        Spawn();
    }

    void ResetGrid()
    {
        for (int r = 0; r < ROWS; r++)
            for (int c = 0; c < COLS; c++)
                _grid[r, c] = 0;
    }

    void RefillBag()
    {
        var types = new List<PieceType>((PieceType[])Enum.GetValues(typeof(PieceType)));
        types.Remove(PieceType.None);
        for (int i = types.Count - 1; i > 0; i--)
        {
            int j = Rng.Next(i + 1);
            (types[i], types[j]) = (types[j], types[i]);
        }
        foreach (var t in types) Bag.Enqueue(t);
    }

    

    void Spawn()
    {
        if (Bag.Count < 7) RefillBag();
        _current = MakeTetris(Bag.Dequeue());
        _current.X = COLS / 2 - 2;
        _current.Y = -2; // 

        _fallAccum = 0;
        _lockAccum = 0;

        if (Collides(_current))
            _state = GameState.GameOver;

        UpdateGhost();

    }

    void ReadInput()
    {
        _ksPrev = _ks;
        _ks = Keyboard.GetState();
        if (IsPressed(Keys.Escape)) Exit();
    }

    bool IsPressed(Keys k) => _ks.IsKeyDown(k) && !_ksPrev.IsKeyDown(k);

    void TryRotate(int dir)
    {
        Tetris test = _current.Clone();
        test.Rotate(dir);

        if (!Collides(test))
        {
            _current = test;
            _lockAccum = 0;
            UpdateGhost();
        }
    }

    void HandleHorizontal(double dt)
    {
        int dirWanted = 0;
        if (_ks.IsKeyDown(Keys.Left)) dirWanted -= 1;
        if (_ks.IsKeyDown(Keys.Right)) dirWanted += 1;

        if (dirWanted == 0)
        {
            _moveDir = 0;
            _dasAccum = 0;
            _arrAccum = 0;
            return;
        }

        if (dirWanted != _moveDir)
        {
            _moveDir = dirWanted;
            _dasAccum = 0;
            MoveHorizontal(_moveDir);
            return;
        }

        if (IsPressed(Keys.Left) || IsPressed(Keys.Right))
        {
            _dasAccum = 0;
            _arrAccum = 0;
            MoveHorizontal(_moveDir);
            return;
        }

        if (_dasAccum < DAS_MS)
        {
            _dasAccum += dt;
            return;
        }

        _arrAccum += dt;
        while (_arrAccum >= ARR_MS)
        {
            _arrAccum -= ARR_MS;
            MoveHorizontal(_moveDir);
        }
    }

    void MoveHorizontal(int dir)
    {
        var test = _current.Clone();
        test.X += dir;
        if (!Collides(test))
        {
            _current = test;
            _lockAccum = 0;
            UpdateGhost();
        }
    }

    void SoftDrop()
    {
        Tetris test = _current.Clone();
        test.Y += 1;
        if (!Collides(test))
        {
            _score++;
            _current = test;
            _lockAccum = 0;
            UpdateGhost();
        }
        else
        {
            _lockAccum += 16;
        }
    }

    void ClearLines()
    {
        int cleared = 0;
        for (int r = ROWS - 1; r >= 0; r--)
        {
            bool full = true;
            for (int c = 0; c < COLS; c++)
                if (_grid[r, c] == 0) { full = false; break; }
            if (full)
            {
                cleared++;
                for (int rr = r; rr > 0; rr--)
                    for (int c = 0; c < COLS; c++)
                        _grid[rr, c] = _grid[rr - 1, c];
                for (int c = 0; c < COLS; c++) _grid[0, c] = 0;
                r++;
            }
        }
        _lines += cleared;
        if (cleared > 0)
        {
            int basePoints = 0;
            switch (cleared)
            {
                case 1: basePoints = 100; break;
                case 2: basePoints = 300; break;
                case 3: basePoints = 500; break;
                case 4: basePoints = 800; break;
            }
            _score += basePoints * _level;
            _lines += cleared;
            if (_lines / LINES_PER_LEVEL + 1 > _level)
            {
                _level = _lines / LINES_PER_LEVEL + 1;
                _gravityMs = Math.Max(60, GRAVITY_SPEED * Math.Pow(0.85, _level - 1));
            }
        }
    }

    //features

    void UpdateGhost()
    {
        _ghost = _current.Clone();
        while (true)
        {
            var t = _ghost.Clone(); t.Y += 1;
            if (Collides(t)) break;
            _ghost = t;
        }
    }


    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(18, 18, 22));

        _spriteBatch.Begin();

        Point origin = new Point(BORDER, BORDER);
        DrawBoard(origin);
        DrawGrid(origin);
        DrawGhost(origin, _ghost, Color.White * 0.25f);
        if (_current != null) DrawPiece(origin, _current, PieceColors[(int)_current.Type]);

        int sideX = BORDER + COLS * CELL + 20;
        DrawNextQueue(new Point(sideX, BORDER));

        if (_font != null)
        {
            _spriteBatch.DrawString(_font, $"Highscore: {_highscore}", new Vector2(sideX + 100, BORDER), Color.Orchid);
            _spriteBatch.DrawString(_font, $"Best Level: {_bestLevel}", new Vector2(sideX + 100, BORDER + 20), Color.Gold);

            _spriteBatch.DrawString(_font, $"Lines: {_lines}", new Vector2(sideX, BORDER + 480), Color.Aqua);
            _spriteBatch.DrawString(_font, $"Level: {_level}", new Vector2(sideX, BORDER + 500), Color.Green);
            _spriteBatch.DrawString(_font, $"Score: {_score}", new Vector2(sideX, BORDER + 520), Color.White);

            int minutes = (int)(_elapsedTime / 60);
            int seconds = (int)(_elapsedTime % 60);
            string timerText = $"Time: {minutes:D2}:{seconds:D2}";
            _spriteBatch.DrawString(_font, timerText, new Vector2(sideX, BORDER + 460), Color.White);

            if (IsPaused)
            {
                string pauseText = "PAUSE";
                Vector2 size = _font.MeasureString(pauseText);
                float scale = 2.0f;
                Vector2 pos = new Vector2(BORDER + (COLS * CELL) / 2 - (size.X * scale) / 2, BORDER + (ROWS * CELL) / 2 - (size.Y * scale) / 2);
                _spriteBatch.DrawString(_font, pauseText, pos, Color.Yellow, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }

            if (_state == GameState.GameOver)
            {
                if (_highscore == _score)
                {
                    _spriteBatch.DrawString(_font, "NOUVEAU RECORD!!! \nRejouer(Enter) - Quitter(Echap)", new Vector2(sideX, BORDER + 540), Color.Orchid);
                }
                else
                {
                    _spriteBatch.DrawString(_font, "GAME OVER \nRejouer(Enter) - Quitter(Echap)", new Vector2(sideX, BORDER + 540), Color.Red);
                }
            }
        }
        

        _spriteBatch.End();
        base.Draw(gameTime);
    }


    void EnsurePixel()
    {
        if (_pixel != null) return;
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    void DrawRect(Rectangle rect, Color color)
    {
        EnsurePixel();
        _spriteBatch.Draw(_pixel, rect, color);
    }

    void DrawBoard(Point p)
    {
        Rectangle rect = new Rectangle(BORDER - 2, BORDER - 2, COLS * CELL + 4, ROWS * CELL + 4);
        DrawRect(rect, new Color(40, 40, 48));
    }

    void DrawGrid(Point p)
    {
        for(int r = 0;r < ROWS; r++)
        {
            for(int c = 0;c < COLS; c++)
            {
                Rectangle backGrid = new Rectangle(p.X + c * CELL, p.Y + r * CELL, CELL, CELL);
                DrawRect(backGrid, new Color(32, 32, 38));

                if (_grid[r, c] != 0)
                {
                    Color color = PieceColors[_grid[r, c] - 1];
                    DrawCell(backGrid, color);
                }
            }
        }
    }

    void DrawPiece(Point p, Tetris t, Color color)
    {
        for (int r = 0; r < 4; r++)
        {
            for (int c = 0; c < 4; c++)
            {
                int gx = t.X + c;
                int gy = t.Y + r;
                if (t.Cell(c, r) == 1)
                {
                    if (gy >= 0)
                    {
                        Rectangle cellRect = new Rectangle(p.X + gx * CELL, p.Y + gy * CELL, CELL, CELL);
                        DrawCell(cellRect, color);
                    }
                }
            }
        }
    }

    void DrawCell(Rectangle r, Color color)
    {
        EnsurePixel();
        
        _spriteBatch.Draw(_pixel, r, color * 0.9f);
        
        Rectangle inner = new Rectangle(r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2);
        _spriteBatch.Draw(_pixel, inner, Color.Black * 0.25f);
        
        Rectangle top = new Rectangle(r.X, r.Y, r.Width, 2);
        Rectangle left = new Rectangle(r.X, r.Y, 2, r.Height);
        _spriteBatch.Draw(_pixel, top, Color.White * 0.15f);
        _spriteBatch.Draw(_pixel, left, Color.White * 0.15f);
    }

    void DrawMini(Point p, PieceType type, Color color)
    {
        Tetris t = MakeTetris(type);
        int baseX = p.X + 8;
        int baseY = p.Y + 8;
        int mini = CELL / 2;
        foreach (var (rx, ry) in t.IterCells())
        {
            Rectangle r = new Rectangle(baseX + rx * mini, baseY + ry * mini, mini, mini);
            DrawCell(r, color);
        }
    }

    void DrawLabel(Point p, string text)
    {
        if (_font == null) return;
        _spriteBatch.DrawString(_font, text, new Vector2(p.X, p.Y), Color.Yellow);
    }

    void DrawNextQueue(Point topLeft)
    {
        DrawLabel(topLeft, "NEXT");

        int nextPiecePosition = topLeft.Y + 24;
        
        if (Bag.Count > 0)
        {
            // Prend la première pièce du sac (sans la retirer)
            PieceType next = Bag.Peek();

            DrawMini(new Point(topLeft.X, nextPiecePosition), next, PieceColors[(int)next]);
        }
    }

    void DrawGhost(Point origin, Tetris t, Color color)
    {
        foreach (var (rx, ry) in t.IterCells())
        {
            int gx = t.X + rx;
            int gy = t.Y + ry;
            if (gy >= 0)
            {
                Rectangle cellRect = new Rectangle(origin.X + gx * CELL, origin.Y + gy * CELL, CELL, CELL);
                DrawRect(cellRect, color);
            }
            
        }
    }


    Tetris MakeTetris(PieceType type)
    {
        switch(type)
        {
            case PieceType.I:
                return Tetris.FromMatrix(type, new int[,]
                {
                {0,0,0,0},
                {1,1,1,1},
                {0,0,0,0},
                {0,0,0,0}
                });

            case PieceType.O:
                return Tetris.FromMatrix(type, new int[,]
                {
                {0,1,1,0},
                {0,1,1,0},
                {0,0,0,0},
                {0,0,0,0}
                });

            case PieceType.T:
                return Tetris.FromMatrix(type, new int[,]
                {
                {0,1,0,0},
                {1,1,1,0},
                {0,0,0,0},
                {0,0,0,0}
                });

            case PieceType.S:
                return Tetris.FromMatrix(type, new int[,]
                {
                {0,1,1,0},
                {1,1,0,0},
                {0,0,0,0},
                {0,0,0,0}
                });

            case PieceType.Z:
                return Tetris.FromMatrix(type, new int[,]
                {
                {1,1,0,0},
                {0,1,1,0},
                {0,0,0,0},
                {0,0,0,0}
                });

            case PieceType.J:
                return Tetris.FromMatrix(type, new int[,]
                {
                {1,0,0,0},
                {1,1,1,0},
                {0,0,0,0},
                {0,0,0,0}
                });

            case PieceType.L:
                return Tetris.FromMatrix(type, new int[,]
                {
                {0,0,1,0},
                {1,1,1,0},
                {0,0,0,0},
                {0,0,0,0}
                });

            default:
                return null;
        }
    }
}
