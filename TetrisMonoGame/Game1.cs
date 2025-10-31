using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

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
    int timer = 0;

    SpriteFont _font;

    public static Color[] PieceColors { get; } = {
        Color.Cyan, Color.Yellow, Color.Purple, Color.Green, Color.Red, Color.Blue, Color.Orange
    };

    GameState _state = GameState.Running;
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

        _current = MakeTetris(PieceType.J);
        _current.X = COLS / 2 - 2;
        _current.Y = -2;
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        timer++;

        if (timer % 60 == 0) _current.Y += 1;
        if(Collides(_current))
        {
            LockPiece();
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

    void LockPiece()
    {
        foreach (var (rx, ry) in _current.IterCells())
        {
            int gx = _current.X + rx;
            int gy = _current.Y + ry;

            if (gy >= 0 && gy < ROWS && gx >= 0 && gx < COLS)
                _grid[gy, gx] = (int)_current.Type + 1;
        }
    }


    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(18, 18, 22));

        _spriteBatch.Begin();

        Point origin = new Point(BORDER, BORDER);
        DrawBoard(origin);
        DrawGrid(origin);
        if(_current != null)
            DrawPiece(origin, _current, PieceColors[(int)_current.Type]);

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


    //void Spawn()
    //{
        
    //}


}
