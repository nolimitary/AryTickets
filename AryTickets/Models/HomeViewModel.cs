using System.Collections.Generic;

namespace AryTickets.Models;

public class HomeViewModel
{
    public List<Production> CurrentRepertoire { get; set; } = new();
    public List<Production> UpcomingPremieres { get; set; } = new();
    public List<string> AllGenres { get; set; } = new();
    public string SelectedGenre { get; set; }
}
