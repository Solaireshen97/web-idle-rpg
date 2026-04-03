using Shared.Players;

namespace Shared.Shop;

public sealed record ShopPurchaseResultDto(
    string ItemKey,
    string DisplayName,
    int SpentGold,
    ShopItemEffectDto Effect,
    PlayerDto Player);
