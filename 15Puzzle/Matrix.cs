using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _15Puzzle
{
    [Serializable]
    public class Matrix
    {
        private Dictionary<DirectionEnum, Tuple<int, int>> _directions = new Dictionary<DirectionEnum, Tuple<int, int>>
        {
            { DirectionEnum.Up, new Tuple<int, int>(-1, 0) },
            { DirectionEnum.Down, new Tuple<int, int>(1, 0) },
            { DirectionEnum.Left, new Tuple<int, int>(0, -1) },
            { DirectionEnum.Right, new Tuple<int, int>(0, 1) }
        };

        public int[,] Layout = new int[4, 4];

        public int stepsCount = 0;
        public Matrix _previousState;
        private DirectionEnum _previousMove;


        public Matrix(string filePath)
        {
            string[] lines = System.IO.File.ReadAllLines(filePath);
            
            for (int lineNumber = 0; lineNumber < lines.Length; lineNumber++)
            {
                var line = lines[lineNumber].Split();

                for (int columnNumber = 0; columnNumber < line.Length; columnNumber++)
                {
                    Layout[lineNumber, columnNumber] = Int32.Parse(line[columnNumber]);
                }
            }
        }

        public Matrix(int[,] layout, int stepsCount, Matrix previousState, DirectionEnum move)
        {
            Layout = layout;

            this.stepsCount = stepsCount;
            this._previousState = previousState;
            this._previousMove = move;

        }

        public override string ToString()
        {
            string buffer = "";

            for (int i = 0; i < Layout.GetLength(0); i++)
            {
                for (int j = 0; j < Layout.GetLength(1); j++)
                {
                    buffer += Layout[i, j] + " ";
                }

                buffer += "\n";
            }

            return buffer;
        }

        public int GetHeuristic(HeuristicsEnum heuristicsType)
        {
            if (heuristicsType == HeuristicsEnum.Manhattan) return Manhattan();
            else return -1;
        }

        public int Manhattan()
        {
            int totalManhattanDistance = 0;

            for (int i = 0; i < Layout.GetLength(0); i++)
            {
                for (int j = 0; j < Layout.GetLength(0); j++)
                {
                    if (Layout[i, j] != 0)
                    {
                        int targetRow = (Layout[i, j] - 1) / 4;
                        int targetColumn = (Layout[i, j] - 1) % 4;
                        totalManhattanDistance += Math.Abs(i - targetRow) + Math.Abs(j - targetColumn);
                    }
                }
            }

            return totalManhattanDistance;

        }

        private Tuple<int, int> FindFreePosition()
        {
            for (int i = 0; i < Layout.GetLength(0); i++)
                for (int j = 0; j < Layout.GetLength(1); j++)
                    if (Layout[i, j] == 0) return new Tuple<int, int>(i, j);

            return null;
        }

        public List<Matrix> generateMoves()
        {
            var resultingMatrices = new List<Matrix>();

            var freePositionTuple = FindFreePosition();
            var freePositionRow = freePositionTuple.Item1;
            var freePositionColumn = freePositionTuple.Item2;

            foreach (DirectionEnum direction in Enum.GetValues(typeof(DirectionEnum)))
            {
                var currentPosition = new Tuple<int, int>(freePositionRow + _directions[direction].Item1, freePositionColumn + _directions[direction].Item2);

                if (InBounds(currentPosition.Item1, currentPosition.Item2))
                {
                    if (_previousState != null && currentPosition.Equals(_previousState.FindFreePosition())) continue;

                    int[,] result = (int[,])Layout.Clone();
                    result[freePositionRow, freePositionColumn] = Layout[currentPosition.Item1, currentPosition.Item2];
                    result[currentPosition.Item1, currentPosition.Item2] = 0;

                    resultingMatrices.Add(new Matrix(result, stepsCount + 1, this, direction));
                }
            }

            return resultingMatrices;
        }

        private bool InBounds(int i, int j)
        {
            return (i >= 0 && i < 4) && (j >= 0 && j < 4);
        }
    }
}
