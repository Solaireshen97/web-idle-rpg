namespace Shared.Shop;

public sealed record ShopItemEffectDto(int FoodDelta);

public sealed record ShopItemDefinitionDto(
    string ItemKey,
    string DisplayName,
    int GoldPrice,
    ShopItemEffectDto Effect);
