namespace chrysalis
{
    class Program
    {
        static void Main(string[] args)
        {
            HttpFrontend htf = new HttpFrontend(new[] {"http://localhost:9200/"});
            htf.Start().Wait();
        }
    }
}
