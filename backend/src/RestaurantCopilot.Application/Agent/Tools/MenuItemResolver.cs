using System.Globalization;
using System.Text;
using RestaurantCopilot.Application.Abstractions.Persistence;
using RestaurantCopilot.Domain.Common;
using RestaurantCopilot.Domain.MenuItems;

namespace RestaurantCopilot.Application.Agent.Tools;

/// <summary>
/// Maps the dish name the model wrote ("lasanha", "Pizza margherita") to a menu item, ignoring case
/// and accents. The menu is loaded once per request.
/// </summary>
internal sealed class MenuItemResolver(IMenuItemRepository menuItems)
{
    private IReadOnlyList<MenuItem>? _menu;

    public async Task<Result<MenuItem>> ResolveAsync(string name, CancellationToken cancellationToken)
    {
        _menu ??= await menuItems.ListAsync(category: null, availableOnly: false, cancellationToken);
        var wanted = Normalize(name);

        var exact = _menu.Where(item => Normalize(item.Name) == wanted).ToList();
        var matches = exact.Count > 0 ? exact : _menu.Where(item => Normalize(item.Name).Contains(wanted, StringComparison.Ordinal)).ToList();

        return matches.Count == 1
            ? matches[0]
            : AgentToolErrors.UnknownMenuItem(name, (matches.Count > 1 ? matches : _menu).Select(item => item.Name));
    }

    private static string Normalize(string text)
    {
        var decomposed = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        return new string(decomposed.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
    }
}
