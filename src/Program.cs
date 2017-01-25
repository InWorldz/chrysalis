using System.Threading.Tasks;
using InWorldz.Chrysalis.Controllers;

namespace InWorldz.Chrysalis
{
    class Program
    {
        static void Main(string[] args)
        {
            var htf = new HttpFrontend(new[] {"http://localhost:9200/"});
            var geometryController = new GeometryController(htf);


            htf.Start().Wait();
        }
    }
}
