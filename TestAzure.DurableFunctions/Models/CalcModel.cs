namespace TestAzure.DurableFunctions.Models;

public record CalcModel
{
    public long StartFactorial { get; set; }
    public int StartValue { get; set; }
    public int EndValue { get; set; }
}
