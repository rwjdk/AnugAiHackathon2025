using System.ComponentModel;

namespace StructuredOutputExample.Models;

public class MovieResult
{
    public required string MessageBack { get; set; }

    [Description("Order them by IMDB Score")] //Sometimes the Structured output properties need a bit of description to get desired effect
    public required Movie[] Top10Movies { get; set; }

    //NB: Please note that structured output do not support DateTime/DateTimeOffset types :-( ... Instead, get it as string and used description to inform AI of date format. Then parse it manually
}