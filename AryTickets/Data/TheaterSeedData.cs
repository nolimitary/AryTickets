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
                },
                new Production
                {
                    Title = "Макбет",
                    TitleOriginal = "Macbeth",
                    Playwright = "Уилям Шекспир",
                    Director = "Лилия Абаджиева",
                    Cast = "Захари Бахаров, Светлана Янчева, Михаил Билалов, Стефан Денолюбов",
                    Genre = "Трагедия",
                    DurationMinutes = 155,
                    Synopsis = "Шотландски пълководец чуе предсказание и тръгва по пътя на властта през кръв. Тъмната тъкан на амбицията и съвестта, поставена в почти ритуална визуалност, където всеки шепот е удар, а всяко решение — неотменимо.",
                    PosterUrl = "https://images.unsplash.com/photo-1547036967-23d11aacaee0?q=80&w=900&auto=format&fit=crop",
                    BackdropUrl = "https://images.unsplash.com/photo-1503343384830-d34da40c5fc1?q=80&w=2400&auto=format&fit=crop",
                    PremiereDate = now.AddDays(-75),
                    Rating = 8.9,
                    IsActive = true
                },
                new Production
                {
                    Title = "Крал Лир",
                    TitleOriginal = "King Lear",
                    Playwright = "Уилям Шекспир",
                    Director = "Иван Добчев",
                    Cast = "Йосиф Сърчаджиев, Снежина Петрова, Параскева Джукелова, Деян Донков",
                    Genre = "Трагедия",
                    DurationMinutes = 205,
                    Synopsis = "Един остаряващ крал раздава царството си според любовта, която дъщерите му успяват да изрекат — и обрича себе си на буря. Шекспирова медитация за гордостта, безумието и късното прозрение.",
                    PosterUrl = "https://images.unsplash.com/photo-1518997554305-5eea2f04e384?q=80&w=900&auto=format&fit=crop",
                    BackdropUrl = "https://images.unsplash.com/photo-1465512859089-99c2a4a3c92e?q=80&w=2400&auto=format&fit=crop",
                    PremiereDate = now.AddDays(-220),
                    Rating = 9.3,
                    IsActive = true
                },
                new Production
                {
                    Title = "Отело",
                    TitleOriginal = "Othello",
                    Playwright = "Уилям Шекспир",
                    Director = "Александър Морфов",
                    Cast = "Самуел Финци, Ирмена Чичикова, Малин Кръстев, Радина Кърджилова",
                    Genre = "Трагедия",
                    DurationMinutes = 175,
                    Synopsis = "Венециански пълководец е разяден отвътре от семето на едно подмятане. Трагедия за ревността, доверието и онзи отровен глас, който винаги намира кому да шепне.",
                    PosterUrl = "https://images.unsplash.com/photo-1564732005956-20420ebdab60?q=80&w=900&auto=format&fit=crop",
                    BackdropUrl = "https://images.unsplash.com/photo-1502743638961-3ec1ec1f6f25?q=80&w=2400&auto=format&fit=crop",
                    PremiereDate = now.AddDays(-50),
                    Rating = 8.7,
                    IsActive = true
                },
                new Production
                {
                    Title = "Сън в лятна нощ",
                    TitleOriginal = "A Midsummer Night's Dream",
                    Playwright = "Уилям Шекспир",
                    Director = "Стайко Мурджев",
                    Cast = "Радина Кърджилова, Стефан Денолюбов, Албена Колева, Иван Юруков",
                    Genre = "Комедия",
                    DurationMinutes = 135,
                    Synopsis = "Гора, в която феи бъркат любовни магии, а влюбените се преследват в кръг. Лятна нощ, безсънна и палава — Шекспир в най-лекия си, най-светъл регистър.",
                    PosterUrl = "https://images.unsplash.com/photo-1485394935761-8dab3f70be4f?q=80&w=900&auto=format&fit=crop",
                    BackdropUrl = "https://images.unsplash.com/photo-1611323099253-2c95f0a5f8ec?q=80&w=2400&auto=format&fit=crop",
                    PremiereDate = now.AddDays(15),
                    Rating = 8.5,
                    IsActive = true
                },
                new Production
                {
                    Title = "Тартюф",
                    TitleOriginal = "Tartuffe",
                    Playwright = "Молиер",
                    Director = "Александър Морфов",
                    Cast = "Йосиф Сърчаджиев, Васил Михайлов, Лилия Маравиля, Стефан Вълдобрев",
                    Genre = "Комедия",
                    DurationMinutes = 140,
                    Synopsis = "Един набожен измамник се настанява в дома на богат буржоа и без капка усилие прекатурва цялото семейство. Молиер — както винаги — реже най-нежно, най-смъртоносно: с усмивка.",
                    PosterUrl = "https://images.unsplash.com/photo-1551817958-d9d86fb29431?q=80&w=900&auto=format&fit=crop",
                    BackdropUrl = "https://images.unsplash.com/photo-1518609878373-06d740f60d8b?q=80&w=2400&auto=format&fit=crop",
                    PremiereDate = now.AddDays(-130),
                    Rating = 8.4,
                    IsActive = true
                },
                new Production
                {
                    Title = "Дом на куклата",
                    TitleOriginal = "Et dukkehjem",
                    Playwright = "Хенрик Ибсен",
                    Director = "Маргарита Младенова",
                    Cast = "Снежина Петрова, Деян Донков, Параскева Джукелова, Леонид Йовчев",
                    Genre = "Драма",
                    DurationMinutes = 155,
                    Synopsis = "Нора затваря вратата след себе си — и един свят се срутва. Хроника на едно пробуждане, която преди век е била скандал, а днес остава неприятно близо до огледалото.",
                    PosterUrl = "https://images.unsplash.com/photo-1499364615650-ec38552f4f34?q=80&w=900&auto=format&fit=crop",
                    BackdropUrl = "https://images.unsplash.com/photo-1485846234645-a62644f84728?q=80&w=2400&auto=format&fit=crop",
                    PremiereDate = now.AddDays(-25),
                    Rating = 8.8,
                    IsActive = true
                },
                new Production
                {
                    Title = "Чакайки Годо",
                    TitleOriginal = "En attendant Godot",
                    Playwright = "Самюъл Бекет",
                    Director = "Явор Гърдев",
                    Cast = "Самуел Финци, Леонид Йовчев, Михаил Билалов, Иван Юруков",
                    Genre = "Абсурд",
                    DurationMinutes = 145,
                    Synopsis = "Двама мъже чакат под едно дърво. Годо не идва. Тогава пак чакат. И пак. Едно от най-важните представления на XX век — за чакането, за приятелството, за способността да продължиш.",
                    PosterUrl = "https://images.unsplash.com/photo-1567593810070-7a3d471af022?q=80&w=900&auto=format&fit=crop",
                    BackdropUrl = "https://images.unsplash.com/photo-1583912086096-8c60d75a53f9?q=80&w=2400&auto=format&fit=crop",
                    PremiereDate = now.AddDays(-160),
                    Rating = 9.1,
                    IsActive = true
                },
                new Production
                {
                    Title = "Антигона",
                    TitleOriginal = "Ἀντιγόνη",
                    Playwright = "Софокъл",
                    Director = "Десислава Шпатова",
                    Cast = "Радина Кърджилова, Леонид Йовчев, Светлана Янчева, Стефан Денолюбов",
                    Genre = "Трагедия",
                    DurationMinutes = 105,
                    Synopsis = "Една сестра погребва брат си — и плаща с живота си за това. Сблъсъкът между държавния закон и неписания дълг, написан преди две и половина хилядолетия и все още жив.",
                    PosterUrl = "https://images.unsplash.com/photo-1601933470928-c2efbb86cb47?q=80&w=900&auto=format&fit=crop",
                    BackdropUrl = "https://images.unsplash.com/photo-1493804714600-6edb1cd93080?q=80&w=2400&auto=format&fit=crop",
                    PremiereDate = now.AddDays(-95),
                    Rating = 8.9,
                    IsActive = true
                },
                new Production
                {
                    Title = "Медея",
                    TitleOriginal = "Μήδεια",
                    Playwright = "Еврипид",
                    Director = "Възкресия Вихърова",
                    Cast = "Снежина Петрова, Захари Бахаров, Албена Колева, Йосиф Сърчаджиев",
                    Genre = "Трагедия",
                    DurationMinutes = 115,
                    Synopsis = "Изоставена от мъжа, за когото е предала всичко, тя избира най-страшното отмъщение. Античната трагедия, в която майчиното и женското се разкъсват — и публиката остава без дъх.",
                    PosterUrl = "https://images.unsplash.com/photo-1565035010268-a3816f98589a?q=80&w=900&auto=format&fit=crop",
                    BackdropUrl = "https://images.unsplash.com/photo-1574155376612-bfa4ed8aabfd?q=80&w=2400&auto=format&fit=crop",
                    PremiereDate = now.AddDays(50),
                    Rating = 8.6,
                    IsActive = true
                },
                new Production
                {
                    Title = "Стъкленият зверилник",
                    TitleOriginal = "The Glass Menagerie",
                    Playwright = "Тенеси Уилямс",
                    Director = "Бина Харалампиева",
                    Cast = "Параскева Джукелова, Деян Донков, Радина Кърджилова, Малин Кръстев",
                    Genre = "Драма",
                    DurationMinutes = 150,
                    Synopsis = "Едно семейство в малък американски град, разпънато между мечтата и реалността. Крехка като стъклена фигурка драма-памет, в която Уилямс пише за собствената си сестра и за всички, които светът обича твърде грубо.",
                    PosterUrl = "https://images.unsplash.com/photo-1602848597941-0d3d3a2c1241?q=80&w=900&auto=format&fit=crop",
                    BackdropUrl = "https://images.unsplash.com/photo-1545987796-200677ee1011?q=80&w=2400&auto=format&fit=crop",
                    PremiereDate = now.AddDays(-40),
                    Rating = 8.7,
                    IsActive = true
                },
                new Production
                {
                    Title = "Майка Кураж и нейните деца",
                    TitleOriginal = "Mutter Courage und ihre Kinder",
                    Playwright = "Бертолт Брехт",
                    Director = "Иван Добчев",
                    Cast = "Светлана Янчева, Леонид Йовчев, Михаил Билалов, Параскева Джукелова",
                    Genre = "Драма",
                    DurationMinutes = 195,
                    Synopsis = "Жена, която преживява войната, като я обслужва — и губи всичко свое в нея. Брехтова епическа хроника за капитала, който винаги печели, и хората, които винаги плащат.",
                    PosterUrl = "https://images.unsplash.com/photo-1518621736915-f3b1c41bfd00?q=80&w=900&auto=format&fit=crop",
                    BackdropUrl = "https://images.unsplash.com/photo-1518105779142-d975f22f1b0a?q=80&w=2400&auto=format&fit=crop",
                    PremiereDate = now.AddDays(-65),
                    Rating = 8.5,
                    IsActive = true
                },
                new Production
                {
                    Title = "Под игото",
                    TitleOriginal = "Под игото",
                    Playwright = "Иван Вазов",
                    Director = "Мариус Куркински",
                    Cast = "Захари Бахаров, Радина Кърджилова, Йосиф Сърчаджиев, Стефан Денолюбов",
                    Genre = "Драма",
                    DurationMinutes = 175,
                    Synopsis = "Сценична адаптация на първия български роман — Бяла Черква, бунтът, любовта, предателството. Вазовите страници оживяват като фреска на едно поколение, което избира свободата над живота.",
                    PosterUrl = "https://images.unsplash.com/photo-1492684223066-81342ee5ff30?q=80&w=900&auto=format&fit=crop",
                    BackdropUrl = "https://images.unsplash.com/photo-1571227196732-23d70864f24c?q=80&w=2400&auto=format&fit=crop",
                    PremiereDate = now.AddDays(70),
                    Rating = 8.8,
                    IsActive = true
                },
                new Production
                {
                    Title = "Криворазбраната цивилизация",
                    TitleOriginal = "Криворазбраната цивилизация",
                    Playwright = "Добри Войников",
                    Director = "Бина Харалампиева",
                    Cast = "Васил Михайлов, Лилия Маравиля, Иван Юруков, Албена Колева",
                    Genre = "Комедия",
                    DurationMinutes = 125,
                    Synopsis = "Възрожденска комедия за това как „европейщината“ опъва патриархалния бит докато не се пукне на смях. Класиката на българската сцена, в която се смеем точно на себе си — но с обич.",
                    PosterUrl = "https://images.unsplash.com/photo-1551817958-d9d86fb29431?q=80&w=900&auto=format&fit=crop",
                    BackdropUrl = "https://images.unsplash.com/photo-1481277542470-605612bd2d61?q=80&w=2400&auto=format&fit=crop",
                    PremiereDate = now.AddDays(-15),
                    Rating = 8.2,
                    IsActive = true
                },
                new Production
                {
                    Title = "Калигула",
                    TitleOriginal = "Caligula",
                    Playwright = "Албер Камю",
                    Director = "Александър Морфов",
                    Cast = "Захари Бахаров, Самуел Финци, Снежина Петрова, Михаил Билалов",
                    Genre = "Драма",
                    DurationMinutes = 165,
                    Synopsis = "Млад император, който след една смърт решава да докаже, че светът е абсурден — като сам го направи такъв. Хладна, бляскава, екзистенциалистка драма за свободата, доведена до край.",
                    PosterUrl = "https://images.unsplash.com/photo-1503095396549-807759245b35?q=80&w=900&auto=format&fit=crop",
                    BackdropUrl = "https://images.unsplash.com/photo-1518105779142-d975f22f1b0a?q=80&w=2400&auto=format&fit=crop",
                    PremiereDate = now.AddDays(-105),
                    Rating = 8.6,
                    IsActive = true
                },
                new Production
                {
                    Title = "Изкуството",
                    TitleOriginal = "Art",
                    Playwright = "Ясмина Реза",
                    Director = "Стайко Мурджев",
                    Cast = "Деян Донков, Малин Кръстев, Стефан Вълдобрев",
                    Genre = "Комедия",
                    DurationMinutes = 95,
                    Synopsis = "Един човек купува бяла картина за абсурдно много пари. Тримата му най-добри приятели не оцеляват без последствия. Лек и жесток разговор за приятелството, естетиката и онова, което всъщност купуваме, когато купуваме изкуство.",
                    PosterUrl = "https://images.unsplash.com/photo-1465339002023-13c0c5e0bcae?q=80&w=900&auto=format&fit=crop",
                    BackdropUrl = "https://images.unsplash.com/photo-1518997554305-5eea2f04e384?q=80&w=2400&auto=format&fit=crop",
                    PremiereDate = now.AddDays(40),
                    Rating = 8.3,
                    IsActive = true
                }
            };
        }
    }
}
