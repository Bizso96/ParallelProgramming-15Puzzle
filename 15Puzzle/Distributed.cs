using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MPI;

namespace _15Puzzle {
    public class Distributed {
        public Matrix Matrix = new Matrix(Config.INPUT_PATH);
        private bool _logInFile = true;
        private bool _logInConsole = true;
        private HeuristicsEnum _heuristicMethod;
        private bool _isMasterNode;

        public Distributed(bool logInFile, bool logInConsole, HeuristicsEnum heuristicMethod, bool isMasterNode) {
            this._logInFile = logInFile;
            this._logInConsole = logInConsole;
            this._heuristicMethod = heuristicMethod;
            this._isMasterNode = isMasterNode;
        }

        public void Run() {
            if (_isMasterNode) {
                Console.WriteLine($"Initial matrix: \n\n {Matrix.ToString()}");

                var startTime = DateTime.Now;
                var solveResult = SolveMaster();
                var endTime = DateTime.Now;
                var finalMatrix = solveResult.Item1;
                var foundSolution = solveResult.Item2;
                var finalHeuristicDistance = solveResult.Item3;

                if (_logInConsole) {
                    Console.WriteLine($"Run time: {(endTime - startTime).TotalMilliseconds / 1000} seconds");
                    Console.WriteLine($"Resulting matrix: \n{finalMatrix}\nSteps:{finalMatrix.stepsCount}\nFinal heuristic distance: {finalHeuristicDistance}\n\n");
                }
                if (_logInFile) {
                    File.AppendAllText(Config.LOG_PATH, "Regular sequential algorithm: " + (endTime - startTime).TotalMilliseconds / 1000 + " seconds\n");
                }
                return;
            }

            SolveWorker();
        }

        private Tuple<Matrix, bool, int> SolveMaster() {
            int bestF = Matrix.GetHeuristic(_heuristicMethod);

            // While no solution found
            var generatedMoves = Matrix.generateMoves();
            Console.WriteLine($"Master: generated moves.");
            while (true) {
                // Send moves

                Console.WriteLine($"Master: sending moves.");
                for (int i = 0; i < generatedMoves.Count; ++i) {
                    Communicator.world.Send(false, i + 1, 0);
                    Communicator.world.Send(bestF, i + 1, 0);
                    Communicator.world.Send(generatedMoves[i], i + 1, 0);
                }
                Console.WriteLine($"Master: sent moves.");

                // Receive matrices
                Console.WriteLine($"Master: receiving moves.");
                List<Tuple<int, Matrix>> receivedMatrices = new List<Tuple<int, Matrix>>();
                for (int i = 0; i < generatedMoves.Count; ++i) {
                    var m = Communicator.world.Receive<Tuple<int, Matrix>>(i + 1, 0);
                    receivedMatrices.Add(m);
                }
                Console.WriteLine($"Master: received moves.");

                // Check if we have a solution
                Console.WriteLine($"Master: checking solutions");
                int min = Int32.MaxValue;
                foreach (var receivedMatrix in receivedMatrices) {
                    if (receivedMatrix.Item1 == -1) {
                        Console.WriteLine($"Master: solution found.");

                        // Shut down workers
                        Console.WriteLine($"Master: Shutting down workers.");
                        for (int i = 0; i < generatedMoves.Count; ++i) {
                            Communicator.world.Send(true, i + 1, 0);
                        }
                        Console.WriteLine($"Master: Shutted down workers.");

                        return new Tuple<Matrix, bool, int>(receivedMatrix.Item2, true, receivedMatrix.Item1);
                    }

                    if (receivedMatrix.Item1 < min) { 
                        min = receivedMatrix.Item1;
                    }
                }

                Console.WriteLine($"-------- min: {min}");
                if (min >= 80) {
                    // Shut down workers
                    Console.WriteLine($"Master: Shutting down workers.");
                    for (int i = 0; i < generatedMoves.Count; ++i) {
                        Communicator.world.Send(true, i + 1, 0);
                    }
                    Console.WriteLine($"Master: Shutted down workers.");

                    return new Tuple<Matrix, bool, int>(Matrix, false, Matrix.stepsCount);
                }

                Console.WriteLine($"Master: checked solutions.");

                bestF = min;
            }
        }

        private void SolveWorker() {
            while (true) {
                bool wasSolutionFound = Communicator.world.Receive<bool>(0, 0);
                
                if (wasSolutionFound) {
                    return;
                }

                int bestF = Communicator.world.Receive<int>(0, 0);
                Matrix matrix = Communicator.world.Receive<Matrix>(0, 0);

                var result = Search(matrix, matrix.stepsCount, bestF);
                Communicator.world.Send(result, 0, 0);
            }
        }


        private Tuple<int, Matrix> Search(Matrix current, int stepsCount, int bestF) {
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
                var result = Search(nextMatrix, stepsCount + 1, bestF);
                var resultEstimation = result.Item1;

                if (resultEstimation == -1) return new Tuple<int, Matrix>(-1, result.Item2);
                if (resultEstimation < min) {
                    min = resultEstimation;
                    bestOutcome = result.Item2;
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
        private Tuple<int, Matrix>? SearchBaseConditions(Matrix current, int stepsCount, int bestF) {
            int g = stepsCount;
            int h = current.GetHeuristic(_heuristicMethod);
            int f = g + h;

            if (f > bestF || f > Config.MOVE_THRESHOLD) return new Tuple<int, Matrix>(f, current);
            if (h == 0) return new Tuple<int, Matrix>(-1, current);
            return null;
        }
    }
}
