namespace StructuredOutputExample.Models;

public class Movie
{
    public string Title { get; set; }
    public int YearOfRelease { get; set; }
    public string Director { get; set; }
    public MovieGenre Genre { get; set; }
    public decimal ImdbScore { get; set; }
    
}