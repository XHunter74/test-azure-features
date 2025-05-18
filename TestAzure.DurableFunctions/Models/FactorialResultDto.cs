namespace TestAzure.DurableFunctions.Models;

public record FactorialResultDto
{
    public int Value { get; set; }
    public long Result { get; set; }
}
