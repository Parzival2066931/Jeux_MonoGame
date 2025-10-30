using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TetrisMonoGame
{
    class Tetris
    {
        public int X { get; set; }
        public int Y { get; set; }
        private int _rot;
        public int Rot
        {
            get => _rot;
            set => _rot = (value + 4) % 4;
        }
        public PieceType Type { get; private set; }
        private int[,] _shape;

    }
}
