using System;
using System.Collections.Generic;
using AryTickets.Models;

namespace AryTickets.Data
{
    public static class TheaterSeedData
    {
        public static List<Production> GetProductions()
        {
            var now = DateTime.UtcNow;
            return new List<Production>
            {
                new Production
                {
                    Title = "Хамлет",
                    TitleOriginal = "Hamlet",
                    Playwright = "Уилям Шекспир",
                    Director = "Иван Добчев",
                    Cast = "Захари Бахаров, Снежина Петрова, Малин Кръстев, Деян Донков",
                    Genre = "Трагедия",
                    DurationMinutes = 175,
                    Synopsis = "Принц Хамлет се завръща в Елсинор след смъртта на баща си и научава, че духът на покойния крал търси отмъщение. Класическата шекспирова трагедия за съмнението, лудостта и предателството в нова интерпретация, която преоткрива монолога „Да бъдеш или не“ за съвременния зрител.",
                    PosterUrl = "https://images.unsplash.com/photo-1503095396549-807759245b35?q=80&w=900&auto=format&fit=crop",
                    BackdropUrl = "https://images.unsplash.com/photo-1507901747481-84a4f64fda6d?q=80&w=2400&auto=format&fit=crop",
                    TrailerUrl = "https://www.youtube.com/embed/Yh87Pkki5tk",
                    PremiereDate = now.AddDays(-180),
                    Rating = 9.2,
                    IsActive = true
                },
                new Production
                {
                    Title = "Ромео и Жулиета",
                    TitleOriginal = "Romeo and Juliet",
                    Playwright = "Уилям Шекспир",
                    Director = "Александър Морфов",
                    Cast = "Стефан Денолюбов, Радина Кърджилова, Йосиф Сърчаджиев, Параскева Джукелова",
                    Genre = "Трагедия",
                    DurationMinutes = 160,
                    Synopsis = "Двама млади влюбени от враждуващи фамилии се сблъскват с непримиримостта на своя свят. Поетичен прочит на най-известната любовна история на западния театър, в който страстта и обречеността са преплетени в безсмъртен танц.",
                    PosterUrl = "https://images.unsplash.com/photo-1518998053901-5348d3961a04?q=80&w=900&auto=format&fit=crop",
                    BackdropUrl = "https://images.unsplash.com/photo-1574155376612-bfa4ed8aabfd?q=80&w=2400&auto=format&fit=crop",
                    PremiereDate = now.AddDays(-60),
                    Rating = 8.7,
                    IsActive = true
                },
                new Production
                {
                    Title = "Чайка",
                    TitleOriginal = "Чайка",
                    Playwright = "Антон П. Чехов",
                    Director = "Явор Гърдев",
                    Cast = "Светлана Янчева, Самуел Финци, Ирмена Чичикова, Леонид Йовчев",
                    Genre = "Драма",
                    DurationMinutes = 195,
                    Synopsis = "В семейното имение край езерото се срещат изкуство, амбиция и неразделена любов. Чеховата меланхолия е поставена с фина ирония и нежност — едно мъчително красиво размишление върху провала, надеждата и невъзможността на изкуството.",
                    PosterUrl = "https://images.unsplash.com/photo-1602848597941-0d3d3a2c1241?q=80&w=900&auto=format&fit=crop",
                    BackdropUrl = "https://images.unsplash.com/photo-1545987796-200677ee1011?q=80&w=2400&auto=format&fit=crop",
                    PremiereDate = now.AddDays(-30),
                    Rating = 9.0,
                    IsActive = true
                },
                new Production
                {
                    Title = "Вуйчо Ваньо",
                    TitleOriginal = "Дядо Ваньо",
                    Playwright = "Антон П. Чехов",
                    Director = "Крикор Азарян",
                    Cast = "Деян Донков, Параскева Джукелова, Малин Кръстев, Лилия Маравиля",
                    Genre = "Драма",
                    DurationMinutes = 165,
                    Synopsis = "След години на саможертва за чужда амбиция, Вуйчо Ваньо открива, че целият му живот е минал на халост. Деликатна психологическа драма за хората, които не са имали смелостта да живеят за себе си — и за тихата трагедия на пропуснатите шансове.",
                    PosterUrl = "https://images.unsplash.com/photo-1485846234645-a62644f84728?q=80&w=900&auto=format&fit=crop",
                    BackdropUrl = "https://images.unsplash.com/photo-1499364615650-ec38552f4f34?q=80&w=2400&auto=format&fit=crop",
                    PremiereDate = now.AddDays(-90),
                    Rating = 8.5,
                    IsActive = true
                },
                new Production
                {
                    Title = "Едип цар",
                    TitleOriginal = "Οἰδίπους Τύραννος",
                    Playwright = "Софокъл",
                    Director = "Иван Добчев",
                    Cast = "Леонид Йовчев, Снежина Петрова, Самуел Финци, Михаил Билалов",
                    Genre = "Трагедия",
                    DurationMinutes = 110,
                    Synopsis = "Кралят на Тива търси истината за чумата, която поразява царството му — и открива, че причината е самият той. Античната трагедия е поставена като метафизичен трилър за съдбата, властта и непоносимата истина за себе си.",
                    PosterUrl = "https://images.unsplash.com/photo-1601933470928-c2efbb86cb47?q=80&w=900&auto=format&fit=crop",
                    BackdropUrl = "https://images.unsplash.com/photo-1493804714600-6edb1cd93080?q=80&w=2400&auto=format&fit=crop",
                    PremiereDate = now.AddDays(-200),
                    Rating = 9.4,
                    IsActive = true
                },
                new Production
                {
                    Title = "Майстора и Маргарита",
                    TitleOriginal = "Мастер и Маргарита",
                    Playwright = "Михаил Булгаков",
                    Director = "Александър Морфов",
                    Cast = "Захари Бахаров, Ирмена Чичикова, Александър Морфов, Стефан Вълдобрев",
                    Genre = "Сатира",
                    DurationMinutes = 210,
                    Synopsis = "Дяволът пристига в Москва и обръща света наопаки, докато един отчаян писател и възлюбената му се борят за любовта и истината. Магически реализъм, политическа сатира и неугасваща любов в едно от най-смелите театрални платна на десетилетието.",
                    PosterUrl = "https://images.unsplash.com/photo-1518621736915-f3b1c41bfd00?q=80&w=900&auto=format&fit=crop",
                    BackdropUrl = "https://images.unsplash.com/photo-1518105779142-d975f22f1b0a?q=80&w=2400&auto=format&fit=crop",
                    PremiereDate = now.AddDays(-15),
                    Rating = 9.1,
                    IsActive = true
                },
                new Production
                {
                    Title = "Чичовци",
                    TitleOriginal = "Чичовци",
                    Playwright = "Иван Вазов",
                    Director = "Бина Харалампиева",
                    Cast = "Йосиф Сърчаджиев, Албена Колева, Васил Михайлов, Иван Юруков",
                    Genre = "Комедия",
                    DurationMinutes = 130,
                    Synopsis = "Възрожденският град оживява чрез своите чудати първенци — наивни, суетни и неподправено искрени. Вазовата проза, превърната в сценична поема за българската душа, която се смее на себе си, без да губи нежност.",
                    PosterUrl = "https://images.unsplash.com/photo-1551817958-d9d86fb29431?q=80&w=900&auto=format&fit=crop",
                    BackdropUrl = "https://images.unsplash.com/photo-1492684223066-81342ee5ff30?q=80&w=2400&auto=format&fit=crop",
                    PremiereDate = now.AddDays(-45),
                    Rating = 8.3,
                    IsActive = true
                },
                new Production
                {
                    Title = "Албена",
                    TitleOriginal = "Албена",
                    Playwright = "Йордан Йовков",
                    Director = "Маргарита Младенова",
                    Cast = "Снежина Петрова, Деян Донков, Радина Кърджилова, Стефан Денолюбов",
                    Genre = "Драма",
                    DurationMinutes = 120,
                    Synopsis = "Красотата на Албена разбунва тихото село и поставя цяла общност пред моралния си избор. Изящна психологическа драма за съблазънта, греха и милостта — поставена с почти иконографска визуалност.",
                    PosterUrl = "https://images.unsplash.com/photo-1565035010268-a3816f98589a?q=80&w=900&auto=format&fit=crop",
                    BackdropUrl = "https://images.unsplash.com/photo-1503095396549-807759245b35?q=80&w=2400&auto=format&fit=crop",
                    PremiereDate = now.AddDays(20),
                    Rating = 8.6,
                    IsActive = true
                },
                new Production
                {
                    Title = "Балкански синдром",
                    TitleOriginal = "Балкански синдром",
                    Playwright = "Станислав Стратиев",
                    Director = "Александър Морфов",
                    Cast = "Стефан Вълдобрев, Параскева Джукелова, Михаил Билалов, Албена Колева",
                    Genre = "Сатира",
                    DurationMinutes = 145,
                    Synopsis = "Гротеска и горчив смях за абсурдите на нашата география и нрави. Стратиев — както винаги — реже до кост, но в края все пак прегръща. Поставена с електрически ритъм и неустоима актьорска енергия.",
                    PosterUrl = "https://images.unsplash.com/photo-1571227196732-23d70864f24c?q=80&w=900&auto=format&fit=crop",
                    BackdropUrl = "https://images.unsplash.com/photo-1518609878373-06d740f60d8b?q=80&w=2400&auto=format&fit=crop",
                    PremiereDate = now.AddDays(35),
                    Rating = 8.4,
                    IsActive = true
                },
                new Production
                {
                    Title = "Три сестри",
                    TitleOriginal = "Три сестры",
                    Playwright = "Антон П. Чехов",
                    Director = "Явор Гърдев",
                    Cast = "Параскева Джукелова, Радина Кърджилова, Снежина Петрова, Леонид Йовчев",
                    Genre = "Драма",
                    DurationMinutes = 200,
                    Synopsis = "Три сестри мечтаят да се върнат в Москва, докато животът в провинцията безшумно ги пресушава. Хроника на надеждата и нейната тиха ерозия — Чехов в неговата най-крехка и най-човешка форма.",
                    PosterUrl = "https://images.unsplash.com/photo-1530021232320-687d8e3dba54?q=80&w=900&auto=format&fit=crop",
                    BackdropUrl = "https://images.unsplash.com/photo-1481277542470-605612bd2d61?q=80&w=2400&auto=format&fit=crop",
                    PremiereDate = now.AddDays(-110),
                    Rating = 9.0,
                    IsActive = true
                }
            };
        }
    }
}
