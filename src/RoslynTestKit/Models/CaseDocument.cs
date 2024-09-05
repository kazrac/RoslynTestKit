namespace RoslynTestKit.Models
{
	public class CaseDocument
	{
		public string? Path { get; set; }
		public string Code { get; set; } = string.Empty;
		public string? FinalCode { get; set; }
	}
}