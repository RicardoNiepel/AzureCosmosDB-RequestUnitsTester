using Microsoft.Azure.Documents;
using Newtonsoft.Json;

public class IndexPolicyScenario
{

    public IndexPolicyScenario(string name, string json)
    {
        Name = name;
        IndexingPolicy = JsonConvert.DeserializeObject<IndexingPolicy>(json);
    }

    public string Name { get; private set; }

    public string DocumentId { get; private set; }

    public IndexingPolicy IndexingPolicy { get; private set; }
}