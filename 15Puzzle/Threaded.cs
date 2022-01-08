using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _15Puzzle
{
    public class Threaded
    {
        public Matrix Matrix = new Matrix(Config.INPUT_PATH);
        private bool _logInFile = true;
        private bool _logInConsole = true;
        private HeuristicsEnum _heuristicMethod;
        private Mutex _resultMutex = new Mutex();

        public Threaded (bool logInFile, bool logInConsole, HeuristicsEnum heuristicMethod)
        {
            this._logInFile = logInFile;
            this._logInConsole = logInConsole;
            this._heuristicMethod = heuristicMethod;

            int maxWorker, maxIOC;
            // Get the current settings.
            ThreadPool.GetMaxThreads(out maxWorker, out maxIOC);
            Console.WriteLine(maxWorker + " " + maxIOC);
            ThreadPool.SetMaxThreads(Config.NUMBER_OF_THREADS, maxIOC);
        }


        public void Run() {
            Console.WriteLine(Matrix.ToString());

            var startTime = DateTime.Now;
            var solveResult = Solve();
            var endTime = DateTime.Now;

            var finalMatrix = solveResult.Item1;
            var foundSolution = solveResult.Item2;
            var finalHeuristicDistance = solveResult.Item3;

            if (_logInConsole)
            {
                Console.WriteLine($"Run time: {(endTime - startTime).TotalMilliseconds / 1000} seconds");

                Console.WriteLine($"Resulting matrix: \n{finalMatrix}\nSteps:{finalMatrix.stepsCount}\nFinal heuristic distance: {finalHeuristicDistance}\n\n");
            }

            if (_logInFile)
            {
                File.AppendAllText(Config.LOG_PATH, "Regular sequential algorithm: " + (endTime - startTime).TotalMilliseconds / 1000 + " seconds\n");
            }
        }

        private Tuple<Matrix, bool, int> Solve()
        {
            int bestHDistance = Matrix.GetHeuristic(_heuristicMethod);
            int distance = 0;

            while (true)
            {
                var solution = SearchParallel(Matrix, 0, bestHDistance, Config.NUMBER_OF_THREADS);

                Console.WriteLine($"Solution matrix:\n{solution.Item2}\nSteps: {solution.Item1}\nManhattan: {solution.Item2.GetHeuristic(HeuristicsEnum.Manhattan)}");

                distance = solution.Item1;

                if (distance == -1)
                {
                    return new Tuple<Matrix, bool, int>(solution.Item2, distance == -1, distance);
                }

                bestHDistance = distance;
            }
        }
        
        private Tuple<int, Matrix> SearchSequential(Matrix current, int stepsCount, int bestHDistance, int unused)
        {
            var currentStateEvaluation = EvaluateCurrentState(current, stepsCount, bestHDistance);
            if (currentStateEvaluation != null) return currentStateEvaluation;
            
            int min = Int32.MaxValue;
            Matrix bestOutcome = null;

            //Console.WriteLine("==========================================");
            //if (current._previousState != null) Console.WriteLine("Previous matrix:\n" + current._previousState.ToString() + "\n");
            //else Console.WriteLine("***");
            //Console.WriteLine("Origin matrix:\n" + current.ToString() + "\n");

            foreach (Matrix nextMatrix in current.generateMoves())
            {
                //Console.WriteLine("Next matrix:\n" + nextMatrix.ToString() + "\n");
                var result = SearchSequential(nextMatrix, stepsCount + 1, bestHDistance, 0);

                var resultEstimation = result.Item1;

                if (resultEstimation == -1) return new Tuple<int, Matrix>(-1, result.Item2);

                if (resultEstimation < min)
                {
                    min = resultEstimation;
                    bestOutcome = result.Item2;
                }
            }

            return new Tuple<int, Matrix>(min, bestOutcome);
        }

        private Tuple<int,Matrix> SearchParallel(Matrix current, int stepsCount, int bestHDistance, int threadCount)
        {
            if (threadCount <= 1) return SearchSequential(current, stepsCount, bestHDistance, 0);

            var currentStateEvaluation = EvaluateCurrentState(current, stepsCount, bestHDistance);
            if (currentStateEvaluation != null) return currentStateEvaluation;

            int min = Int32.MaxValue;
            var generatedMoves = current.generateMoves();

            //foreach(var m in generatedMoves)
            //{
            //    Console.WriteLine(m);
            //}
            
            List<Tuple<int, Matrix>> results = new List<Tuple<int, Matrix>>();

            var countdownEvent = new CountdownEvent(1);

            var bestOutcome = current;

            foreach (Matrix nextMatrix in generatedMoves)
            {
                countdownEvent.AddCount();
                ThreadPool.QueueUserWorkItem(state =>
                {
                    var localResult = SearchParallel(nextMatrix, stepsCount + 1, bestHDistance, threadCount / generatedMoves.Count);

                    _resultMutex.WaitOne();
                    results.Add(localResult);
                    _resultMutex.ReleaseMutex();
                    countdownEvent.Signal();
                });
            }

            countdownEvent.Signal();
            countdownEvent.Wait();

            foreach (var r in results)
            {
                if (r.Item1 == -1) return r;

                if (r.Item1 < min)
                {
                    min = r.Item1;
                    bestOutcome = r.Item2;
                }
            }

            return new Tuple<int, Matrix>(min, bestOutcome);
        }

        private Tuple<int, Matrix> EvaluateCurrentState(Matrix current, int stepsCount, int bestHDistance)
        {
            int f = stepsCount + current.GetHeuristic(_heuristicMethod);

            if (f > bestHDistance || f > Config.MOVE_THRESHOLD) return new Tuple<int, Matrix>(f, current);

            if (current.GetHeuristic(_heuristicMethod) == 0) return new Tuple<int, Matrix>(-1, current);

            return null;
        }
    }
}
