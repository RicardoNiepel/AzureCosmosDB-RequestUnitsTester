using System;
using Newtonsoft.Json.Linq;

public class DocumentScenario
{

    public DocumentScenario(string name, string json)
    {
        Name = name;
        Document = JObject.Parse(json);
        DocumentId = (string)Document["id"];
    }

    public string Name { get; private set; }

    public string DocumentId { get; private set; }

    public JObject Document { get; private set; }

    public double OriginalDocumentSize { get; set; }
    public double DocumentSizeWithMetadata { get; set; }
    public double CreateRequestCharge { get; set; }
    public double ReadRequestCharge { get; set; }
    public double ReplaceRequestCharge { get; set; }
    public double DeleteRequestCharge { get; set; }

    public void WriteToConsole()
    {
        // TODO: not using console directly
        Console.WriteLine(Name);
        Console.WriteLine($"\tsize:\t\t{DocumentSizeWithMetadata} KB ({OriginalDocumentSize} KB original without metadata)");
        Console.WriteLine($"\tcreate:\t\t{CreateRequestCharge} RUs");
        Console.WriteLine($"\tread:\t\t{ReadRequestCharge} RUs");
        Console.WriteLine($"\treplace:\t{ReplaceRequestCharge} RUs");
        Console.WriteLine($"\tdelete:\t\t{DeleteRequestCharge} RUs");
        Console.WriteLine();
    }
}