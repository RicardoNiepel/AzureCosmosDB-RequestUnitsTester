using System;
using Newtonsoft.Json.Linq;

public class QueryScenario
{

    public QueryScenario(string name, string query)
    {
        Name = name;
        Query = query;
    }

    public string Name { get; private set; }

    public string Query { get; private set; }

    public double RequestCharge { get; set; }

    public int ItemCount { get; set; }

    public double TotalSize { get; set; }

    public void WriteToConsole()
    {
        Console.WriteLine($"{Name} query: {RequestCharge} RUs for {ItemCount} items with {TotalSize} KB total");
        Console.WriteLine();
    }
}