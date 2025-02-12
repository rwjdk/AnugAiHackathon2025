using System.ComponentModel;

namespace StructuredOutputExample.Models;

public class MovieResult
{
    public required string MessageBack { get; set; }
    
    public required decimal AverageScoreOfThe10Movies { get; set; } //Example that we still deal with "stupid AIs" but also that such things are not really needed

    [Description("Order them by IMDB Score")] //Sometimes the Structured output properties need a bit of description to get desired effect
    public required Movie[] Top10Movies { get; set; }

    public decimal RealAverageScore => Top10Movies.Select(x => x.ImdbScore).Average();

    //NB: Please note that structured output are poor with DateTime/DateTimeOffset types at the moment [Need custom instructions to be able to parse]
    //https://github.com/microsoft/semantic-kernel/issues/10507
}