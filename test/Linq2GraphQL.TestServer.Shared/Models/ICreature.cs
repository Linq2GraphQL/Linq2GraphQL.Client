namespace Linq2GraphQL.TestServer.Models
{
    [InterfaceType("ICanRun")]
    public interface ICanRun
    {
       public int Speed { get; set; }
       
    }

    [InterfaceType("IAnimal")]
    public interface IAnimal 
    {
        string Name { get; set; }
        int NumberOfLegs { get; set; }
    }



    public class Pig : IAnimal, ICanRun
    {
        public string Name { get; set; } = "";
        public int NumberOfLegs { get; set; }
        public int Speed { get; set; }

        public string Spices { get; set; } = "";
    }

    public class Spider: IAnimal, ICanRun
    {
        public string Name { get; set; } = "";
        public int NumberOfLegs { get; set; }
        public int Speed { get; set; }

        public bool Poisonous { get; set; }
    }

}
