using System.Collections.Generic;
using Microsoft.Azure.Documents;

public class RunConfiguration
{
    public ConsistencyLevel ConsistencyLevel { get; set; }
    public IndexPolicyScenario IndexPolicyScenario { get; set; }

    public List<DocumentScenario> DocumentScenarios { get; set; }
    
    public List<QueryDataScenario> QueryDataScenarios { get; set; }

    public List<QueryScenario> QueryScenarios { get; set; }
}