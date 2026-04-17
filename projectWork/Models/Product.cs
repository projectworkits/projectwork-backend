namespace projectWork.Models;

public class Product
{
    public int Id { get; set; }
    public int Available { get; set; }
    public int Booked { get; set; }
    public int Sold { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
}
