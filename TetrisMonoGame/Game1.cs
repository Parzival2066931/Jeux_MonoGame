using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using static System.Formats.Asn1.AsnWriter;

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

    double _gravityMs = GRAVITY_SPEED;
    double _fallAccum = 0;
    double _lockAccum = 0;
    double _dasAccum = 0;
    double _arrAccum = 0;
    int _moveDir = 0;

    
    SpriteFont _font;

    public static Color[] PieceColors { get; } = {
        Color.Cyan, Color.Yellow, Color.Purple, Color.Green, Color.Red, Color.Blue, Color.Orange
    };
    public Queue<PieceType> Bag { get; } = new();
    public Random Rng { get; } = new();

    GameState _state = GameState.Running;
    KeyboardState _ks, _ksPrev;
    int[,] _grid = new int[ROWS, COLS];

    Texture2D _pixel;

    Tetris _current;


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

        ResetGrid();
        RefillBag();
        Spawn();
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        double dt = gameTime.ElapsedGameTime.TotalMilliseconds;

        ReadInput();

        if (IsPressed(Keys.Up) || IsPressed(Keys.X)) TryRotate(+1);
        if (_ks.IsKeyDown(Keys.Down)) SoftDrop();
        if (_ks.IsKeyDown(Keys.Left)) MoveHorizontal(-1);
        if (_ks.IsKeyDown(Keys.Right)) MoveHorizontal(1);

        _fallAccum += dt;
        while (_fallAccum >= _gravityMs)
        {
            _fallAccum -= _gravityMs;
            StepDown();
        }




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
            _lockAccum = 0;
        }
        else
        {
            _lockAccum += _gravityMs;
            if (_lockAccum >= LOCK_DELAY_MS)
            {
                LockPiece();
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

            if (gy >= 0 && gy < ROWS && gx >= 0 && gx < COLS)
                _grid[gy, gx] = (int)_current.Type + 1;
        }

        if (topOut) _state = GameState.GameOver;
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
        }
    }

    void SoftDrop()
    {
        Tetris test = _current.Clone();
        test.Y += 1;
        if (!Collides(test))
        {
            _current = test;
            _lockAccum = 0;
        }
        else
        {
            _lockAccum += 16;
        }
    }


    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(18, 18, 22));

        _spriteBatch.Begin();

        Point origin = new Point(BORDER, BORDER);
        DrawBoard(origin);
        DrawGrid(origin);
        if(_current != null) DrawPiece(origin, _current, PieceColors[(int)_current.Type]);

        int sideX = BORDER + COLS * CELL + 20;
        DrawNextQueue(new Point(sideX, BORDER));

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
