using System;
using System.Collections.Generic;
using AryTickets.Models;

namespace AryTickets.Data
{
    public static class TheaterSeedData
    {
        // Real public-domain imagery sourced from Wikimedia Commons (historical
        // paintings, original production photos, first-edition covers).
        // Keyed by Production.Title. Missing keys fall back to the generated SVG.
        private static readonly Dictionary<string, string> RealImages = new()
        {
            { "Hamlet", "https://upload.wikimedia.org/wikipedia/commons/b/bd/Stane_Stare%C5%A1ini%C4%8D_kot_Hamlet_1961.jpg" },
            { "Romeo and Juliet", "https://upload.wikimedia.org/wikipedia/commons/5/55/Romeo_and_juliet_brown.jpg" },
            { "The Seagull", "https://upload.wikimedia.org/wikipedia/commons/0/04/Maly_Theatre_foto_4.jpg" },
            { "Uncle Vanya", "https://upload.wikimedia.org/wikipedia/commons/9/94/Uncle_Vanya_MAT.jpg" },
            { "Oedipus Rex", "https://upload.wikimedia.org/wikipedia/commons/6/69/Oedipus.jpg" },
            { "The Master and Margarita", "https://upload.wikimedia.org/wikipedia/commons/e/e6/MasterandMargaritaFirstEdition.jpg" },
            { "The Old-Timers", "https://upload.wikimedia.org/wikipedia/commons/c/ce/%D0%98%D0%B2%D0%B0%D0%BD_%D0%92%D0%B0%D0%B7%D0%BE%D0%B2_-_%D0%A7%D0%B8%D1%87%D0%BE%D0%B2%D1%86%D0%B8_%281_%D0%B8%D0%B7%D0%B4.%2C_1885%29.jpg" },
            { "Albena", "https://upload.wikimedia.org/wikipedia/commons/9/92/BASA-2128K-1-251-2-Yordan_Yovkov.jpg" },
            { "Balkan Syndrome", "https://upload.wikimedia.org/wikipedia/commons/1/16/IvanVazov_National_Theatre_7.jpg" },
            { "Three Sisters", "https://upload.wikimedia.org/wikipedia/commons/1/13/Three_Sisters_cover_1901.jpg" },
            { "Macbeth", "https://upload.wikimedia.org/wikipedia/commons/4/40/First-page-first-folio-macbeth.jpg" },
            { "King Lear", "https://upload.wikimedia.org/wikipedia/commons/3/31/Cordelia%27s_Portion.jpg" },
            { "Othello", "https://upload.wikimedia.org/wikipedia/commons/thumb/4/4b/Othello_et_Desd%C3%A9mone_%C3%A0_Venise_-_Th%C3%A9odore_Chass%C3%A9riau_-_Mus%C3%A9e_du_Louvre_Peintures_RF_3897.jpg/1280px-Othello_et_Desd%C3%A9mone_%C3%A0_Venise_-_Th%C3%A9odore_Chass%C3%A9riau_-_Mus%C3%A9e_du_Louvre_Peintures_RF_3897.jpg" },
            { "A Midsummer Night's Dream", "https://upload.wikimedia.org/wikipedia/commons/6/60/John_Simmons_-_Titania_sleeping_in_the_moonlight_protected_by_her_fairies.jpg" },
            { "Tartuffe", "https://upload.wikimedia.org/wikipedia/commons/d/d2/Tartuffe.jpg" },
            { "A Doll's House", "https://upload.wikimedia.org/wikipedia/commons/f/fd/A_Doll%27s_House.jpeg" },
            { "Waiting for Godot", "https://upload.wikimedia.org/wikipedia/commons/thumb/d/d6/En_attendant_Godot%2C_Festival_d%27Avignon%2C_1978.jpeg/1280px-En_attendant_Godot%2C_Festival_d%27Avignon%2C_1978.jpeg" },
            { "Antigone", "https://upload.wikimedia.org/wikipedia/commons/thumb/6/6d/Darius_Painter_-_RVAp_18-23_-_Antigone_-_judgement_of_Paris_-_Eros_with_youths_and_women_-_Berlin_AS_F_3240_-_04.jpg/1280px-Darius_Painter_-_RVAp_18-23_-_Antigone_-_judgement_of_Paris_-_Eros_with_youths_and_women_-_Berlin_AS_F_3240_-_04.jpg" },
            { "Medea", "https://upload.wikimedia.org/wikipedia/commons/d/d8/Relief_of_Medea_and_the_Peliades_Antikensammlung_Berlin.jpg" },
            { "The Glass Menagerie", "https://upload.wikimedia.org/wikipedia/en/3/33/The_Glass_Menagerie_%28play%29_1st_edition_cover.jpg" },
            { "Mother Courage and Her Children", "https://upload.wikimedia.org/wikipedia/commons/2/2c/Bundesarchiv_Bild_183-T0927-019%2C_Berliner_Ensemble%2C_Probe_Mutter_Courage.jpg" },
            { "Under the Yoke", "https://upload.wikimedia.org/wikipedia/commons/e/ee/%D0%9F%D0%BE%D0%B4_%D0%B8%D0%B3%D0%BE%D1%82%D0%BE_%D0%A2%D0%B8%D1%82%D1%83%D0%BB.jpg" },
            { "Misunderstood Civilisation", "https://upload.wikimedia.org/wikipedia/commons/8/86/%D0%9A%D1%80%D0%B8%D0%B2%D0%BE%D1%80%D0%B0%D0%B7%D0%B1%D1%80%D0%B0%D0%BD%D0%B0%D1%82%D0%B0_%D1%86%D0%B8%D0%B2%D0%B8%D0%BB%D0%B8%D0%B7%D0%B0%D1%86%D0%B8%D1%8F.jpg" },
            { "Caligula", "https://upload.wikimedia.org/wikipedia/commons/1/1c/Caligula.Carlsberg_Glyptotek.%28cropped%29.jpg" },
            { "Art", "https://upload.wikimedia.org/wikipedia/en/0/09/Art_Reza-Hampton.jpg" },
        };

        // Real photos when available; fall back to the SVG generator otherwise.
        private static string Poster(string title, string? playwright = null, string? genre = null)
        {
            if (RealImages.TryGetValue(title, out var url)) return url;
            var q = $"title={Uri.EscapeDataString(title)}";
            if (!string.IsNullOrEmpty(playwright)) q += $"&playwright={Uri.EscapeDataString(playwright)}";
            if (!string.IsNullOrEmpty(genre)) q += $"&genre={Uri.EscapeDataString(genre)}";
            return $"/posters/poster?{q}";
        }

        private static string Backdrop(string title, string? playwright = null)
        {
            if (RealImages.TryGetValue(title, out var url)) return url;
            var q = $"title={Uri.EscapeDataString(title)}";
            if (!string.IsNullOrEmpty(playwright)) q += $"&playwright={Uri.EscapeDataString(playwright)}";
            return $"/posters/backdrop?{q}";
        }

        public static List<Production> GetProductions()
        {
            var now = DateTime.UtcNow;
            var productions = BuildProductions(now);
            // Auto-derive poster/backdrop URLs from each production's title, playwright and genre.
            foreach (var p in productions)
            {
                p.PosterUrl = Poster(p.Title, p.Playwright, p.Genre);
                p.BackdropUrl = Backdrop(p.Title, p.Playwright);
            }
            return productions;
        }

        private static List<Production> BuildProductions(DateTime now)
        {
            return new List<Production>
            {
                new Production
                {
                    Title = "Hamlet",
                    TitleOriginal = "Hamlet",
                    Playwright = "William Shakespeare",
                    Director = "Ivan Dobchev",
                    Cast = "Zachary Baharov, Snezhina Petrova, Malin Krastev, Deyan Donkov",
                    Genre = "Tragedy",
                    DurationMinutes = 175,
                    Synopsis = "Prince Hamlet returns to Elsinore after his father's death and learns that the late king's ghost demands revenge. A fresh reading of Shakespeare's classic tragedy of doubt, madness and betrayal — one that rediscovers the famous \"To be, or not to be\" for the contemporary audience.",
                    TrailerUrl = "https://www.youtube.com/embed/Yh87Pkki5tk",
                    PremiereDate = now.AddDays(-180),
                    Rating = 9.2,
                    IsActive = true
                },
                new Production
                {
                    Title = "Romeo and Juliet",
                    TitleOriginal = "Romeo and Juliet",
                    Playwright = "William Shakespeare",
                    Director = "Alexander Morfov",
                    Cast = "Stefan Denolyubov, Radina Kardzhilova, Yosif Sarchadzhiev, Paraskeva Dzhukelova",
                    Genre = "Tragedy",
                    DurationMinutes = 160,
                    Synopsis = "Two young lovers from feuding houses collide with the unbending world around them. A poetic reading of the western theatre's most famous love story, in which passion and doom are interwoven in an immortal dance.",
                    PremiereDate = now.AddDays(-60),
                    Rating = 8.7,
                    IsActive = true
                },
                new Production
                {
                    Title = "The Seagull",
                    TitleOriginal = "Чайка",
                    Playwright = "Anton P. Chekhov",
                    Director = "Yavor Gardev",
                    Cast = "Svetlana Yancheva, Samuel Fintzi, Irmena Chichikova, Leonid Yovchev",
                    Genre = "Drama",
                    DurationMinutes = 195,
                    Synopsis = "At a country estate by the lake, art, ambition and unrequited love meet. Chekhov's melancholy is staged with delicate irony and tenderness — a painfully beautiful meditation on failure, hope and the impossibility of art.",
                    PremiereDate = now.AddDays(-30),
                    Rating = 9.0,
                    IsActive = true
                },
                new Production
                {
                    Title = "Uncle Vanya",
                    TitleOriginal = "Дядя Ваня",
                    Playwright = "Anton P. Chekhov",
                    Director = "Krikor Azaryan",
                    Cast = "Deyan Donkov, Paraskeva Dzhukelova, Malin Krastev, Lilia Maravilya",
                    Genre = "Drama",
                    DurationMinutes = 165,
                    Synopsis = "After years of sacrifice for someone else's ambition, Uncle Vanya discovers that his whole life has gone to waste. A delicate psychological drama about people who lacked the courage to live for themselves — and the quiet tragedy of missed chances.",
                    PremiereDate = now.AddDays(-90),
                    Rating = 8.5,
                    IsActive = true
                },
                new Production
                {
                    Title = "Oedipus Rex",
                    TitleOriginal = "Οἰδίπους Τύραννος",
                    Playwright = "Sophocles",
                    Director = "Ivan Dobchev",
                    Cast = "Leonid Yovchev, Snezhina Petrova, Samuel Fintzi, Mihail Bilalov",
                    Genre = "Tragedy",
                    DurationMinutes = 110,
                    Synopsis = "The king of Thebes seeks the truth behind the plague ravaging his kingdom — and discovers that the cause is himself. The ancient tragedy is staged as a metaphysical thriller about fate, power, and the unbearable truth about oneself.",
                    PremiereDate = now.AddDays(-200),
                    Rating = 9.4,
                    IsActive = true
                },
                new Production
                {
                    Title = "The Master and Margarita",
                    TitleOriginal = "Мастер и Маргарита",
                    Playwright = "Mikhail Bulgakov",
                    Director = "Alexander Morfov",
                    Cast = "Zachary Baharov, Irmena Chichikova, Alexander Morfov, Stefan Valdobrev",
                    Genre = "Satire",
                    DurationMinutes = 210,
                    Synopsis = "The Devil arrives in Moscow and turns the world inside out while a desperate writer and his beloved fight for love and truth. Magical realism, political satire and undying love woven into one of the boldest theatrical canvases of the decade.",
                    PremiereDate = now.AddDays(-15),
                    Rating = 9.1,
                    IsActive = true
                },
                new Production
                {
                    Title = "The Old-Timers",
                    TitleOriginal = "Чичовци",
                    Playwright = "Ivan Vazov",
                    Director = "Bina Haralampieva",
                    Cast = "Yosif Sarchadzhiev, Albena Koleva, Vasil Mihaylov, Ivan Yurukov",
                    Genre = "Comedy",
                    DurationMinutes = 130,
                    Synopsis = "A 19th-century Bulgarian town comes alive through its eccentric notables — naive, vain and genuinely sincere. Vazov's prose turned into a scenic poem of a national soul that laughs at itself without losing tenderness.",
                    PremiereDate = now.AddDays(-45),
                    Rating = 8.3,
                    IsActive = true
                },
                new Production
                {
                    Title = "Albena",
                    TitleOriginal = "Албена",
                    Playwright = "Yordan Yovkov",
                    Director = "Margarita Mladenova",
                    Cast = "Snezhina Petrova, Deyan Donkov, Radina Kardzhilova, Stefan Denolyubov",
                    Genre = "Drama",
                    DurationMinutes = 120,
                    Synopsis = "Albena's beauty unsettles a quiet village and forces an entire community to face its moral choice. A graceful psychological drama about temptation, sin and mercy — staged with an almost iconographic visual language.",
                    PremiereDate = now.AddDays(20),
                    Rating = 8.6,
                    IsActive = true
                },
                new Production
                {
                    Title = "Balkan Syndrome",
                    TitleOriginal = "Балкански синдром",
                    Playwright = "Stanislav Stratiev",
                    Director = "Alexander Morfov",
                    Cast = "Stefan Valdobrev, Paraskeva Dzhukelova, Mihail Bilalov, Albena Koleva",
                    Genre = "Satire",
                    DurationMinutes = 145,
                    Synopsis = "A grotesque, bittersweet laugh at the absurdities of our geography and our temperament. Stratiev — as always — cuts to the bone and still manages an embrace at the end. Staged with electric rhythm and irresistible acting energy.",
                    PremiereDate = now.AddDays(35),
                    Rating = 8.4,
                    IsActive = true
                },
                new Production
                {
                    Title = "Three Sisters",
                    TitleOriginal = "Три сестры",
                    Playwright = "Anton P. Chekhov",
                    Director = "Yavor Gardev",
                    Cast = "Paraskeva Dzhukelova, Radina Kardzhilova, Snezhina Petrova, Leonid Yovchev",
                    Genre = "Drama",
                    DurationMinutes = 200,
                    Synopsis = "Three sisters dream of returning to Moscow while life in the province quietly drains them. A chronicle of hope and its silent erosion — Chekhov in his most fragile and most human form.",
                    PremiereDate = now.AddDays(-110),
                    Rating = 9.0,
                    IsActive = true
                },
                new Production
                {
                    Title = "Macbeth",
                    TitleOriginal = "Macbeth",
                    Playwright = "William Shakespeare",
                    Director = "Lilia Abadzhieva",
                    Cast = "Zachary Baharov, Svetlana Yancheva, Mihail Bilalov, Stefan Denolyubov",
                    Genre = "Tragedy",
                    DurationMinutes = 155,
                    Synopsis = "A Scottish warlord hears a prophecy and walks the bloody road to power. The dark fabric of ambition and conscience, staged with near-ritual visuality where every whisper lands as a blow and every decision is irreversible.",
                    PremiereDate = now.AddDays(-75),
                    Rating = 8.9,
                    IsActive = true
                },
                new Production
                {
                    Title = "King Lear",
                    TitleOriginal = "King Lear",
                    Playwright = "William Shakespeare",
                    Director = "Ivan Dobchev",
                    Cast = "Yosif Sarchadzhiev, Snezhina Petrova, Paraskeva Dzhukelova, Deyan Donkov",
                    Genre = "Tragedy",
                    DurationMinutes = 205,
                    Synopsis = "An aging king divides his kingdom by the love his daughters can put into words — and condemns himself to the storm. A Shakespearean meditation on pride, madness and late, hard-earned clarity.",
                    PremiereDate = now.AddDays(-220),
                    Rating = 9.3,
                    IsActive = true
                },
                new Production
                {
                    Title = "Othello",
                    TitleOriginal = "Othello",
                    Playwright = "William Shakespeare",
                    Director = "Alexander Morfov",
                    Cast = "Samuel Fintzi, Irmena Chichikova, Malin Krastev, Radina Kardzhilova",
                    Genre = "Tragedy",
                    DurationMinutes = 175,
                    Synopsis = "A Venetian general is eaten from within by the seed of a single insinuation. A tragedy about jealousy, trust, and that poisonous voice that always finds an ear to whisper into.",
                    PremiereDate = now.AddDays(-50),
                    Rating = 8.7,
                    IsActive = true
                },
                new Production
                {
                    Title = "A Midsummer Night's Dream",
                    TitleOriginal = "A Midsummer Night's Dream",
                    Playwright = "William Shakespeare",
                    Director = "Stayko Murdzhev",
                    Cast = "Radina Kardzhilova, Stefan Denolyubov, Albena Koleva, Ivan Yurukov",
                    Genre = "Comedy",
                    DurationMinutes = 135,
                    Synopsis = "A forest where fairies meddle in love spells and the lovers chase each other in circles. A summer night, sleepless and mischievous — Shakespeare at his lightest, brightest register.",
                    PremiereDate = now.AddDays(15),
                    Rating = 8.5,
                    IsActive = true
                },
                new Production
                {
                    Title = "Tartuffe",
                    TitleOriginal = "Tartuffe",
                    Playwright = "Molière",
                    Director = "Alexander Morfov",
                    Cast = "Yosif Sarchadzhiev, Vasil Mihaylov, Lilia Maravilya, Stefan Valdobrev",
                    Genre = "Comedy",
                    DurationMinutes = 140,
                    Synopsis = "A pious con-man moves into a wealthy bourgeois household and effortlessly tips the whole family overboard. Molière — as always — cuts most tenderly, most lethally: with a smile.",
                    PremiereDate = now.AddDays(-130),
                    Rating = 8.4,
                    IsActive = true
                },
                new Production
                {
                    Title = "A Doll's House",
                    TitleOriginal = "Et dukkehjem",
                    Playwright = "Henrik Ibsen",
                    Director = "Margarita Mladenova",
                    Cast = "Snezhina Petrova, Deyan Donkov, Paraskeva Dzhukelova, Leonid Yovchev",
                    Genre = "Drama",
                    DurationMinutes = 155,
                    Synopsis = "Nora closes the door behind her — and a whole world collapses. A chronicle of an awakening that was a scandal a century ago and today remains uncomfortably close to the mirror.",
                    PremiereDate = now.AddDays(-25),
                    Rating = 8.8,
                    IsActive = true
                },
                new Production
                {
                    Title = "Waiting for Godot",
                    TitleOriginal = "En attendant Godot",
                    Playwright = "Samuel Beckett",
                    Director = "Yavor Gardev",
                    Cast = "Samuel Fintzi, Leonid Yovchev, Mihail Bilalov, Ivan Yurukov",
                    Genre = "Absurdism",
                    DurationMinutes = 145,
                    Synopsis = "Two men wait under a tree. Godot doesn't come. Then they wait again. And again. One of the most important plays of the twentieth century — about waiting, friendship, and the ability to keep going.",
                    PremiereDate = now.AddDays(-160),
                    Rating = 9.1,
                    IsActive = true
                },
                new Production
                {
                    Title = "Antigone",
                    TitleOriginal = "Ἀντιγόνη",
                    Playwright = "Sophocles",
                    Director = "Desislava Shpatova",
                    Cast = "Radina Kardzhilova, Leonid Yovchev, Svetlana Yancheva, Stefan Denolyubov",
                    Genre = "Tragedy",
                    DurationMinutes = 105,
                    Synopsis = "A sister buries her brother — and pays for it with her life. The clash between the law of the state and the unwritten duty, written two and a half millennia ago and still painfully alive.",
                    PremiereDate = now.AddDays(-95),
                    Rating = 8.9,
                    IsActive = true
                },
                new Production
                {
                    Title = "Medea",
                    TitleOriginal = "Μήδεια",
                    Playwright = "Euripides",
                    Director = "Vazkresia Viharova",
                    Cast = "Snezhina Petrova, Zachary Baharov, Albena Koleva, Yosif Sarchadzhiev",
                    Genre = "Tragedy",
                    DurationMinutes = 115,
                    Synopsis = "Abandoned by the man for whom she betrayed everything, she chooses the most terrible revenge. The ancient tragedy in which motherhood and womanhood are torn apart — and the audience is left breathless.",
                    PremiereDate = now.AddDays(50),
                    Rating = 8.6,
                    IsActive = true
                },
                new Production
                {
                    Title = "The Glass Menagerie",
                    TitleOriginal = "The Glass Menagerie",
                    Playwright = "Tennessee Williams",
                    Director = "Bina Haralampieva",
                    Cast = "Paraskeva Dzhukelova, Deyan Donkov, Radina Kardzhilova, Malin Krastev",
                    Genre = "Drama",
                    DurationMinutes = 150,
                    Synopsis = "A family in a small American town, stretched between dream and reality. A memory-play as fragile as a glass figurine, in which Williams writes about his own sister — and about everyone the world loves too roughly.",
                    PremiereDate = now.AddDays(-40),
                    Rating = 8.7,
                    IsActive = true
                },
                new Production
                {
                    Title = "Mother Courage and Her Children",
                    TitleOriginal = "Mutter Courage und ihre Kinder",
                    Playwright = "Bertolt Brecht",
                    Director = "Ivan Dobchev",
                    Cast = "Svetlana Yancheva, Leonid Yovchev, Mihail Bilalov, Paraskeva Dzhukelova",
                    Genre = "Drama",
                    DurationMinutes = 195,
                    Synopsis = "A woman who survives the war by serving it — and loses everything she has to it. Brecht's epic chronicle of the capital that always wins and the people who always pay.",
                    PremiereDate = now.AddDays(-65),
                    Rating = 8.5,
                    IsActive = true
                },
                new Production
                {
                    Title = "Under the Yoke",
                    TitleOriginal = "Под игото",
                    Playwright = "Ivan Vazov",
                    Director = "Marius Kurkinski",
                    Cast = "Zachary Baharov, Radina Kardzhilova, Yosif Sarchadzhiev, Stefan Denolyubov",
                    Genre = "Drama",
                    DurationMinutes = 175,
                    Synopsis = "A stage adaptation of the first Bulgarian novel — Byala Cherkva, the uprising, love, betrayal. Vazov's pages come alive as a fresco of a generation that chose freedom over life.",
                    PremiereDate = now.AddDays(70),
                    Rating = 8.8,
                    IsActive = true
                },
                new Production
                {
                    Title = "Misunderstood Civilisation",
                    TitleOriginal = "Криворазбраната цивилизация",
                    Playwright = "Dobri Voynikov",
                    Director = "Bina Haralampieva",
                    Cast = "Vasil Mihaylov, Lilia Maravilya, Ivan Yurukov, Albena Koleva",
                    Genre = "Comedy",
                    DurationMinutes = 125,
                    Synopsis = "A 19th-century comedy about how imported \"Europeanism\" stretches the patriarchal way of life until it bursts into laughter. A staple of the Bulgarian stage in which we laugh at ourselves — but with love.",
                    PremiereDate = now.AddDays(-15),
                    Rating = 8.2,
                    IsActive = true
                },
                new Production
                {
                    Title = "Caligula",
                    TitleOriginal = "Caligula",
                    Playwright = "Albert Camus",
                    Director = "Alexander Morfov",
                    Cast = "Zachary Baharov, Samuel Fintzi, Snezhina Petrova, Mihail Bilalov",
                    Genre = "Drama",
                    DurationMinutes = 165,
                    Synopsis = "A young emperor who, after a single death, decides to prove that the world is absurd — by making it so. A cold, glittering, existentialist drama about freedom taken to its end.",
                    PremiereDate = now.AddDays(-105),
                    Rating = 8.6,
                    IsActive = true
                },
                new Production
                {
                    Title = "Art",
                    TitleOriginal = "Art",
                    Playwright = "Yasmina Reza",
                    Director = "Stayko Murdzhev",
                    Cast = "Deyan Donkov, Malin Krastev, Stefan Valdobrev",
                    Genre = "Comedy",
                    DurationMinutes = 95,
                    Synopsis = "A man buys a white painting for an absurd amount of money. His three best friends don't survive without consequences. A light and cruel conversation about friendship, aesthetics, and what we actually buy when we buy art.",
                    PremiereDate = now.AddDays(40),
                    Rating = 8.3,
                    IsActive = true
                }
            };
        }
    }
}
