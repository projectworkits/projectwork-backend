namespace projectWork.Models;

public class Product
{
    public int Id { get; set; }
    public int Available { get; set; }
    public int Sold { get; set; } = 0;
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
}
