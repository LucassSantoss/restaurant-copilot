// Generates the fictitious seed data used by the API, the tests and the agent evaluation.
//
//   dotnet run seed/generate.cs            (from the repository root)
//
// The generator is deterministic (fixed random seed), so the committed JSON files are
// reproducible and the evaluation's expected answers stay valid. It plants a few business
// patterns on purpose, so the agent has something real to find:
//   - Tuesdays are the weakest sales day;
//   - Friday and Saturday dinner deliveries collect delay complaints;
//   - in October the lasagna has a packaging problem (leaking sauce);
//   - the shrimp risotto draws price complaints;
//   - the most recent week sells more than the previous ones.
#:property PublishAot=false

using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

var random = new Random(20261111);
var outputDirectory = args.FirstOrDefault() ?? "seed";
var brazilOffset = TimeSpan.FromHours(-3);

var firstDay = new DateOnly(2026, 8, 3);  // Monday
var lastDay = new DateOnly(2026, 11, 8);  // Sunday (14 full weeks)
var lastWeekStart = lastDay.AddDays(-6);

var restaurant = new
{
    Name = "Bella Massa Cantina",
    Cuisine = "Italiana",
    City = "São Paulo",
    TimeZoneId = "America/Sao_Paulo",
};

var menu = new MenuEntry[]
{
    new("bruschetta", "Bruschetta Tradicional", "Pão italiano, tomate, manjericão e azeite.", "Starter", 32.90m, 4),
    new("burrata", "Burrata com Tomates", "Burrata cremosa, tomates confitados e pesto.", "Starter", 58.90m, 2),
    new("arancini", "Arancini de Queijo", "Bolinhos de risoto recheados com muçarela (6 unidades).", "Starter", 36.90m, 3),
    new("pizza-margherita", "Pizza Margherita", "Molho de tomate, muçarela de búfala e manjericão.", "Pizza", 64.90m, 10),
    new("pizza-calabresa", "Pizza Calabresa", "Calabresa artesanal, cebola roxa e azeitonas.", "Pizza", 62.90m, 8),
    new("pizza-quatro-queijos", "Pizza Quatro Queijos", "Muçarela, gorgonzola, parmesão e provolone.", "Pizza", 69.90m, 6),
    new("pizza-pepperoni", "Pizza Pepperoni", "Pepperoni fatiado fino e muçarela.", "Pizza", 71.90m, 6),
    new("pizza-portuguesa", "Pizza Portuguesa", "Presunto, ovos, cebola, ervilha e muçarela.", "Pizza", 66.90m, 4),
    new("lasanha-bolonhesa", "Lasanha à Bolonhesa", "Massa fresca, ragu de carne e molho branco gratinado.", "Pasta", 54.90m, 7),
    new("spaghetti-carbonara", "Spaghetti à Carbonara", "Guanciale, gema, pecorino e pimenta-do-reino.", "Pasta", 52.90m, 6),
    new("fettuccine-alfredo", "Fettuccine Alfredo", "Molho de manteiga e parmesão.", "Pasta", 49.90m, 4),
    new("nhoque-sugo", "Nhoque ao Sugo", "Nhoque de batata com molho de tomate fresco.", "Pasta", 46.90m, 4),
    new("ravioli-ricota", "Ravioli de Ricota e Espinafre", "Ao molho de manteiga e sálvia.", "Pasta", 56.90m, 3),
    new("risoto-camarao", "Risoto de Camarão", "Arbóreo, camarões salteados e limão-siciliano.", "Risotto", 89.90m, 4),
    new("risoto-funghi", "Risoto de Funghi", "Arbóreo com mix de cogumelos e parmesão.", "Risotto", 68.90m, 3),
    new("parmegiana", "Filé à Parmegiana", "Filé empanado, molho de tomate e muçarela, com arroz.", "MainCourse", 74.90m, 5),
    new("frango-limao", "Frango ao Limão", "Peito grelhado ao molho de limão, com legumes.", "MainCourse", 48.90m, 3),
    new("tiramisu", "Tiramisù", "Biscoito champagne, café, mascarpone e cacau.", "Dessert", 29.90m, 5),
    new("panna-cotta", "Panna Cotta de Frutas Vermelhas", "Creme cozido com calda de frutas vermelhas.", "Dessert", 24.90m, 3),
    new("cannoli", "Cannoli Siciliano", "Massa crocante recheada com ricota doce (2 unidades).", "Dessert", 26.90m, 2),
    new("refrigerante", "Refrigerante Lata", "350 ml.", "Drink", 7.90m, 8),
    new("suco-laranja", "Suco de Laranja", "Natural, 500 ml.", "Drink", 12.90m, 4),
    new("agua", "Água Mineral", "500 ml, com ou sem gás.", "Drink", 5.90m, 4),
    new("vinho-taca", "Taça de Vinho Tinto", "Vinho tinto da casa, 150 ml.", "Drink", 28.90m, 2),
};
var mains = menu.Where(m => m.Category is "Pizza" or "Pasta" or "Risotto" or "MainCourse").ToArray();
var extras = menu.Where(m => m.Category is "Starter" or "Dessert" or "Drink").ToArray();

// Base daily volume by weekday (Sunday = 0). Tuesday is intentionally the weakest day.
int[] baseOrdersByWeekday = [21, 10, 6, 11, 13, 20, 24];

var orders = new List<OrderEntry>();
var reviews = new List<ReviewEntry>();
var orderNumber = 1000;

for (var day = firstDay; day <= lastDay; day = day.AddDays(1))
{
    var weekday = (int)day.DayOfWeek;
    var growth = day >= lastWeekStart ? 1.15 : 1.0;
    var dailyOrders = (int)Math.Round(baseOrdersByWeekday[weekday] * growth * (0.85 + random.NextDouble() * 0.3));

    for (var i = 0; i < dailyOrders; i++)
    {
        var placedAt = new DateTimeOffset(day.ToDateTime(PickOrderTime()), brazilOffset);
        var channel = Pick(("Delivery", 70), ("Pickup", 15), ("DineIn", 15));
        var status = random.NextDouble() < 0.03 ? "Cancelled" : "Completed";
        var items = PickItems();
        var order = new OrderEntry(++orderNumber, placedAt, channel, status, items);
        orders.Add(order);

        if (status == "Completed" && TryCreateReview(order) is { } review)
        {
            reviews.Add(review);
        }
    }
}

var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
};
Write("restaurant.json", restaurant);
Write("menu.json", menu.Select(m => new { m.Key, m.Name, m.Description, m.Category, m.Price }));
Write("orders.json", orders);
Write("reviews.json", reviews);
Console.WriteLine($"Generated {menu.Length} menu items, {orders.Count} orders and {reviews.Count} reviews in {outputDirectory}.");

void Write(string fileName, object data) =>
    File.WriteAllText(Path.Combine(outputDirectory, fileName), JsonSerializer.Serialize(data, jsonOptions) + "\n");

TimeOnly PickOrderTime()
{
    var isLunch = random.NextDouble() < 0.35;
    var hour = isLunch ? random.Next(11, 15) : random.Next(18, 23);
    return new TimeOnly(hour, random.Next(0, 60));
}

List<OrderItemEntry> PickItems()
{
    var items = new Dictionary<string, int>();
    var mainCount = random.NextDouble() < 0.7 ? 1 : 2;
    for (var i = 0; i < mainCount; i++)
    {
        Add(PickWeighted(mains), 1);
    }

    foreach (var extra in extras)
    {
        if (random.NextDouble() < extra.Popularity * 0.035)
        {
            Add(extra, extra.Category == "Drink" ? random.Next(1, 3) : 1);
        }
    }

    return items.Select(pair => new OrderItemEntry(pair.Key, pair.Value)).ToList();

    void Add(MenuEntry item, int quantity) =>
        items[item.Key] = items.GetValueOrDefault(item.Key) + quantity;
}

ReviewEntry? TryCreateReview(OrderEntry order)
{
    var reviewChance = order.Channel == "Delivery" ? 0.17 : 0.07;
    if (random.NextDouble() >= reviewChance)
    {
        return null;
    }

    var local = order.PlacedAt;
    var isWeekendDinnerDelivery = order.Channel == "Delivery"
        && local.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday
        && local.Hour >= 20;
    var hasLasagna = order.Items.Any(i => i.MenuItem == "lasanha-bolonhesa");
    var hasShrimpRisotto = order.Items.Any(i => i.MenuItem == "risoto-camarao");

    string theme;
    string sentiment;
    string? dishKey = null;

    if (hasLasagna && local.Month == 10 && order.Channel == "Delivery" && random.NextDouble() < 0.65)
    {
        (theme, sentiment, dishKey) = ("Packaging", "Negative", "lasanha-bolonhesa");
    }
    else if (isWeekendDinnerDelivery && random.NextDouble() < 0.45)
    {
        (theme, sentiment) = ("Delay", "Negative");
    }
    else if (hasShrimpRisotto && random.NextDouble() < 0.4)
    {
        (theme, sentiment, dishKey) = ("Price", "Negative", "risoto-camarao");
    }
    else
    {
        sentiment = Pick(("Positive", 58), ("Neutral", 15), ("Negative", 27));
        theme = sentiment == "Negative"
            ? Pick(("Taste", 35), ("Delay", 25), ("Packaging", 15), ("Price", 10), ("Service", 15))
            : Pick(("Taste", 55), ("Delay", 15), ("Packaging", 10), ("Price", 10), ("Service", 10));
    }

    dishKey ??= order.Items.Select(i => i.MenuItem).Where(k => mains.Any(m => m.Key == k)).FirstOrDefault();
    var dish = menu.First(m => m.Key == dishKey);
    var template = PickFrom(Templates[(sentiment, theme)]);
    var mentionsDish = template.Contains("{dish}", StringComparison.Ordinal);
    var comment = template
        .Replace("{dish}", dish.Name, StringComparison.Ordinal)
        .Replace("{minutes}", random.Next(40, 95).ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    comment += sentiment switch
    {
        "Positive" when random.NextDouble() < 0.3 => PickFrom([" Vou pedir de novo!", " Recomendo.", " Virei cliente."]),
        "Negative" when random.NextDouble() < 0.3 => PickFrom([" Não peço mais.", " Decepcionante.", " Esperava mais."]),
        _ => string.Empty,
    };

    var rating = sentiment switch
    {
        "Positive" => random.NextDouble() < 0.7 ? 5 : 4,
        "Neutral" => 3,
        _ => random.NextDouble() < 0.55 ? 1 : 2,
    };

    return new ReviewEntry(
        order.Number,
        mentionsDish ? dish.Key : null,
        rating,
        comment,
        order.PlacedAt.AddHours(1 + random.Next(0, 20)).AddMinutes(random.Next(0, 60)));
}

MenuEntry PickWeighted(MenuEntry[] candidates)
{
    var roll = random.Next(candidates.Sum(c => c.Popularity));
    foreach (var candidate in candidates)
    {
        roll -= candidate.Popularity;
        if (roll < 0)
        {
            return candidate;
        }
    }

    return candidates[^1];
}

string Pick(params (string Value, int Weight)[] options)
{
    var roll = random.Next(options.Sum(o => o.Weight));
    foreach (var (value, weight) in options)
    {
        roll -= weight;
        if (roll < 0)
        {
            return value;
        }
    }

    return options[^1].Value;
}

string PickFrom(string[] options) => options[random.Next(options.Length)];

partial class Program
{
    private static readonly Dictionary<(string Sentiment, string Theme), string[]> Templates = new()
    {
        [("Negative", "Delay")] =
        [
            "Demorou mais de uma hora para chegar e a comida veio fria.",
            "O pedido atrasou {minutes} minutos e ninguém avisou nada.",
            "Pedi {dish} e a comida até estava boa, mas a entrega atrasou demais.",
            "Esperei quase uma hora e meia pelo pedido numa sexta à noite.",
            "Entrega muito lenta, o pedido ({dish}) chegou frio.",
        ],
        [("Negative", "Packaging")] =
        [
            "O pedido com {dish} chegou todo vazado, molho espalhado pela sacola.",
            "Embalagem frágil, a tampa abriu no caminho e sujou tudo.",
            "O molho de {dish} vazou da embalagem. Precisam rever isso.",
            "Embalagem amassada e a comida chegou toda revirada. Pedi {dish}.",
        ],
        [("Negative", "Taste")] =
        [
            "{dish} veio sem sal e passou do ponto.",
            "Achei {dish} sem graça, esperava mais sabor.",
            "Pedi {dish} e veio cru no meio, bem decepcionante.",
            "Comida gordurosa demais, não gostei do tempero.",
        ],
        [("Negative", "Price")] =
        [
            "Preço de {dish} alto demais para o tamanho da porção.",
            "Preço salgado: a porção de {dish} é pequena.",
            "Pelo que custa {dish}, esperava bem mais na porção.",
        ],
        [("Negative", "Service")] =
        [
            "Atendente foi grosseiro quando liguei para perguntar do pedido.",
            "Pedi para trocar um item pelo chat e ninguém respondeu.",
            "Mandaram o pedido errado e o suporte demorou para resolver.",
        ],
        [("Neutral", "Taste")] =
        [
            "Comida ok, nada de especial.",
            "{dish} estava razoável, já comi melhores.",
            "Bom, mas já foi melhor.",
        ],
        [("Neutral", "Delay")] =
        [
            "Entrega dentro do prazo, comida normal.",
            "Chegou no horário previsto. {dish} estava ok.",
        ],
        [("Neutral", "Packaging")] = ["Embalagem simples, mas cumpriu o papel."],
        [("Neutral", "Price")] = ["Preço justo, nada além do esperado."],
        [("Neutral", "Service")] = ["Atendimento normal, sem problemas."],
        [("Positive", "Taste")] =
        [
            "Que delícia de {dish}, no ponto certinho!",
            "Melhor {dish} que já pedi por delivery.",
            "Sabor incrível, tudo muito bem temperado.",
            "{dish} estava incrível, sabor de cantina de verdade.",
            "Comida deliciosa e bem servida.",
        ],
        [("Positive", "Delay")] =
        [
            "Chegou rapidinho e bem quentinho!",
            "Entrega antes do previsto e {dish} chegou em ótimo estado.",
        ],
        [("Positive", "Packaging")] =
        [
            "Embalagem caprichada, chegou tudo intacto.",
            "Embalagem impecável para {dish}, sem vazar nada.",
        ],
        [("Positive", "Price")] =
        [
            "Ótimo custo-benefício, porção generosa.",
            "Preço justo pela qualidade de {dish}.",
        ],
        [("Positive", "Service")] =
        [
            "Atendimento muito educado, até mandaram um bilhetinho.",
            "Equipe atenciosa, resolveram meu pedido especial sem problema.",
        ],
    };
}

record MenuEntry(
    string Key,
    string Name,
    string Description,
    string Category,
    decimal Price,
    [property: JsonIgnore] int Popularity);

record OrderItemEntry(string MenuItem, int Quantity);

record OrderEntry(int Number, DateTimeOffset PlacedAt, string Channel, string Status, List<OrderItemEntry> Items);

record ReviewEntry(int OrderNumber, string? MenuItem, int Rating, string Comment, DateTimeOffset CreatedAt);
