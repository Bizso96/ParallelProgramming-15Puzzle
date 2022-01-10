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

        public Threaded (bool logInFile, bool logInConsole, HeuristicsEnum heuristicMethod)
        {
            this._logInFile = logInFile;
            this._logInConsole = logInConsole;
            this._heuristicMethod = heuristicMethod;

            int maxWorker, maxIOC;
            ThreadPool.GetMaxThreads(out maxWorker, out maxIOC);
            ThreadPool.SetMaxThreads(Config.NUMBER_OF_THREADS, maxIOC);
        }


        public void Run() {
            Console.WriteLine($"Initial matrix: \n\n {Matrix.ToString()}");

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

        /*
         * Attempts to solve the puzzle.
         * 
         * @returns a tuple containing:
         *  1. the final puzzle layout (the furthest we could reach)
         *  2. a boolean indicating whether we found a solution or not
         *  3. the best f
         */
        private Tuple<Matrix, bool, int> Solve()
        {
            int bestF = Matrix.GetHeuristic(_heuristicMethod);
            int newF;

            // While no solution found
            while (true)
            {
                // Search again for a solution, based on the previously found best f
                var solution = SearchParallel(Matrix, 0, bestF, Config.NUMBER_OF_THREADS);
                newF = solution.Item1;

                // Log iteration results
                Console.WriteLine($"Solution matrix:\n{solution.Item2}\nf: {solution.Item1}\nManhattan: {solution.Item2.GetHeuristic(HeuristicsEnum.Manhattan)}");
                
                // If distance = 1, which by conventions means that we found a solution to the puzzle, then return the solution
                if (newF == -1 || newF >= 80) return new Tuple<Matrix, bool, int>(solution.Item2, newF == -1, newF);

                // Otherwise, just update the best f with the newly found f, which will be used on the next iteration.
                bestF = newF;
            }
        }
        
        private Tuple<int, Matrix> SearchSequential(Matrix current, int stepsCount, int bestF)
        {
            // Check base conditions and stop(return) if needed
            var baseCondition = SearchBaseConditions(current, stepsCount, bestF);
            if (baseCondition != null) return baseCondition;
            
            int min = Int32.MaxValue;
            Matrix bestOutcome = null;

            //Console.WriteLine("==========================================");
            //if (current._previousState != null) Console.WriteLine("Previous matrix:\n" + current._previousState.ToString() + "\n");
            //else Console.WriteLine("***");
            //Console.WriteLine("Origin matrix:\n" + current.ToString() + "\n");

            foreach (Matrix nextMatrix in current.generateMoves()) {
                var result = SearchSequential(nextMatrix, stepsCount + 1, bestF);
                var resultEstimation = result.Item1;

                if (resultEstimation == -1) return new Tuple<int, Matrix>(-1, result.Item2);
                if (resultEstimation < min) {
                    min = resultEstimation;
                    bestOutcome = result.Item2;
                }
            }

            return new Tuple<int, Matrix>(min, bestOutcome);
        }

        private Tuple<int,Matrix> SearchParallel(Matrix current, int stepsCount, int bestF, int threadCount)
        {
            // If no more threads available to do parralel search, then go for a sequential search
            if (threadCount <= 1) return SearchSequential(current, stepsCount, bestF);

            // Check base conditions and stop (return) if needed
            var baseCondition = SearchBaseConditions(current, stepsCount, bestF);
            if (baseCondition != null) return baseCondition;


            int min = Int32.MaxValue;
            var generatedMoves = current.generateMoves();
            List<Tuple<int, Matrix>> results = new List<Tuple<int, Matrix>>();
            var countdownEvent = new CountdownEvent(1);
            var bestOutcome = current;
            foreach (Matrix nextMatrix in generatedMoves)
            {
                countdownEvent.AddCount();
                ThreadPool.QueueUserWorkItem(state =>
                {
                    var localResult = SearchParallel(nextMatrix, stepsCount + 1, bestF, threadCount / generatedMoves.Count);
                    results.Add(localResult);
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

        /*
         * @param current           - the current puzzle layouyt
         * @param stepsCount        - the number of steps (slides) performed so far
         * @param bestF - the best (i.e. minimum / smallest) distance to the target
         * 
         * Handles the base conditions of the reccursive search functions
         * 
         * @returns - if the current f is worse that the best f found so far, then stop looking further on this reccursion branch (we cannot find a better solution)
         *          - if the current h is zero, then we solved the puzzle, so we should return the new best solution (we use -1 for distance to indicate that we found a solution)
         */
        private Tuple<int, Matrix>? SearchBaseConditions(Matrix current, int stepsCount, int bestF)
        {
            int g = stepsCount;
            int h = current.GetHeuristic(_heuristicMethod);
            int f = g + h;

            if (f > bestF || f > Config.MOVE_THRESHOLD) return new Tuple<int, Matrix>(f, current);
            if (h == 0) return new Tuple<int, Matrix>(-1, current);
            return null;
        }
    }
}
