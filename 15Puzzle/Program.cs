// See https://aka.ms/new-console-template for more information
using _15Puzzle;
using MPI;

//var threaded = new Threaded(false, true, HeuristicsEnum.Manhattan);
//threaded.Run();


using (new MPI.Environment(ref args)) {
    Console.WriteLine($"World Rank: {Communicator.world.Rank}");

    var distributed = new Distributed(false, true, HeuristicsEnum.Manhattan, Communicator.world.Rank == 0);
    distributed.Run();
}
