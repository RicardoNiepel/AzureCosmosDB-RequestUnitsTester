
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public class QueryDataScenario
{

    public QueryDataScenario(string name, string json)
    {
        Name = name;
        Documents = JArray.Parse(json).ToObject<List<JObject>>();
    }

    public string Name { get; private set; }

    public string DocumentId { get; private set; }

    public List<JObject> Documents { get; private set; }
}