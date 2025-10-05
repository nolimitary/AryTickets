using System.Collections.Generic;

namespace AryTickets.Models;
using AryTickets.Models;
using YourAppName.Models;

public class HomeViewModel
{
    public List<Movie> NowShowingMovies { get; set; }
    public List<Movie> ComingSoonMovies { get; set; }
}