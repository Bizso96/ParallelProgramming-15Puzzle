using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _15Puzzle
{
    public static class Config
    {
        public static int NUMBER_OF_THREADS = 12;
        public static int NUMBER_OF_WORKERS = 5;
        public static string INPUT_PATH = "../../../../input/puzzle-51.txt";
        public static string LOG_PATH = "../../../../logs/puzzle1_log.txt";

        public static int PUZZLE_SIZE = 4;
        public static int MOVE_THRESHOLD = 80;
    }
}
