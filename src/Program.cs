using System.Threading.Tasks;

namespace chrysalis
{
    class Program
    {
        static void Main(string[] args)
        {
            HttpFrontend htf = new HttpFrontend(new[] {"http://localhost:9200/"});
            htf.AddHandler("GET", "/", (context, request) =>
            {
                context.Response.Redirect("http://www.google.com");
                context.Response.Close();
                return Task.FromResult(0);
            });

            htf.Start().Wait();
        }
    }
}
