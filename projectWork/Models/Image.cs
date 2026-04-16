namespace projectWork.Models;

public enum PhotoState
{
    available,
    booked,
    sold
}

public class Image
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string OriginalTitle { get; set; }
    public short Year { get; set; }
    public string Place { get; set; }
    public string Path { get; set; }
    public string? Description { get; set; }
    public PhotoState State { get; set; } = PhotoState.available;
    public decimal Price { get; set; } = 0M;
    public int? BookedBy { get; set; }
}
