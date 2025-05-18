namespace TestAzure.Shared.Models.Dto;

public record OrderErrorDto
{
    public string Reason { get; set; }
    public string Description { get; set; }
}
