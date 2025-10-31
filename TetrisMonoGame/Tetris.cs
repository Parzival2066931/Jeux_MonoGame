using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TetrisMonoGame.Game1;

namespace TetrisMonoGame
{
    public class Tetris
    {
        public PieceType Type;
        public int X, Y;           
        public int Rot;            
        int[,] _shape;             

        Tetris() { }

        public static Tetris FromMatrix(PieceType t, int[,] m)
        {
            return new Tetris { Type = t, X = 3, Y = -2, Rot = 0, _shape = (int[,])m.Clone() };
        }

        public Tetris Clone()
        {
            Tetris clone = new Tetris();
            clone.Type = this.Type;
            clone.X = this.X;
            clone.Y = this.Y;
            clone.Rot = this.Rot;
            clone._shape = (int[,])this._shape.Clone();
            return clone;
        }

        public PieceType ToType() { return Type; }

        public IEnumerable<(int x, int y)> IterCells()
        {
            for (int r = 0; r < 4; r++)
                for (int c = 0; c < 4; c++)
                {
                    if (Cell(c, r) == 1) yield return (c, r);
                }
        }

        public int Cell(int x, int y)
        {
            // rotation 0,90,180,270 d’une matrice 4x4
            switch (Rot)
            {
                case 0:
                    return _shape[y, x];
                case 1:
                    return _shape[3 - x, y];
                case 2:
                    return _shape[3 - y, 3 - x];
                case 3:
                    return _shape[x, 3 - y];
                default:
                    return 0;
            }
        }

        public void Rotate(int dir)
        {
            Rot = (Rot + (dir > 0 ? 1 : 3)) % 4;
        }
    }
}
