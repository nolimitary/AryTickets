using System.Collections.Generic;

namespace AryTickets.Models;

public class HomeViewModel
{
    public List<Movie> NowShowingMovies { get; set; }
    public List<Movie> ComingSoonMovies { get; set; }
    public List<Genre> AllGenres { get; set; } = new();
    public int? SelectedGenreId { get; set; }
}