const resultElement = document.getElementById("result");
const playerIdInput = document.getElementById("playerId");
const playerNameInput = document.getElementById("playerName");
const playerStatusIdElement = document.getElementById("playerStatusId");
const playerStatusNameElement = document.getElementById("playerStatusName");
const playerStatusGoldElement = document.getElementById("playerStatusGold");
const playerStatusFoodElement = document.getElementById("playerStatusFood");
const playerStatusLevelElement = document.getElementById("playerStatusLevel");
const playerStatusExperienceElement = document.getElementById("playerStatusExperience");
const playerStatusAttackElement = document.getElementById("playerStatusAttack");
const playerStatusMaxHpElement = document.getElementById("playerStatusMaxHp");
const playerStatusCurrentHpElement = document.getElementById("playerStatusCurrentHp");
const playerStatusPreferredEnemyElement = document.getElementById("playerStatusPreferredEnemy");
const playerStatusCreatedAtElement = document.getElementById("playerStatusCreatedAt");
const playerStatusUpdatedAtElement = document.getElementById("playerStatusUpdatedAt");
const currentEnemyNameElement = document.getElementById("currentEnemyName");
const currentEnemyHpElement = document.getElementById("currentEnemyHp");
const currentEnemyAttackElement = document.getElementById("currentEnemyAttack");
const fightResultElement = document.getElementById("fightResult");
const autoFightStatusElement = document.getElementById("autoFightStatus");
const startAutoFightButton = document.getElementById("startAutoFightButton");
const stopAutoFightButton = document.getElementById("stopAutoFightButton");
const autoUseFoodCheckbox = document.getElementById("autoUseFoodCheckbox");
const autoUseFoodThresholdSelect = document.getElementById("autoUseFoodThresholdSelect");
const autoUseFoodStatusElement = document.getElementById("autoUseFoodStatus");
const powerStrikeCheckbox = document.getElementById("powerStrikeCheckbox");
const powerStrikeStatusElement = document.getElementById("powerStrikeStatus");
const preferredEnemySelect = document.getElementById("preferredEnemySelect");
const preferredEnemyStatusElement = document.getElementById("preferredEnemyStatus");
const currentAreaSelect = document.getElementById("currentAreaSelect");
const currentAreaStatusElement = document.getElementById("currentAreaStatus");
const currentEncounterNameElement = document.getElementById("currentEncounterName");
const currentEncounterTypeElement = document.getElementById("currentEncounterType");
const currentEncounterWaveElement = document.getElementById("currentEncounterWave");
const shopItemsContainerElement = document.getElementById("shopItemsContainer");
const playerHoldingsContainerElement = document.getElementById("playerHoldingsContainer");
const defaultAutoUseFoodThresholdPercent = 50;
const defaultPreferredEnemyKey = "random";
const defaultPowerStrikeEnabled = true;
const autoFightIntervalMs = 1000;
const defaultShopItem = {
  itemKey: "food",
  displayName: "Food",
  goldPrice: 5,
  effect: { foodDelta: 1 },
  consumableUse: { consumedAmount: 1, hpRecover: 10 }
};
const defaultShopItems = [defaultShopItem];
let autoFightTimerId = null;
let autoFightTickInProgress = false;
let currentPreferredEnemyKey = defaultPreferredEnemyKey;
let currentPowerStrikeEnabled = defaultPowerStrikeEnabled;
let currentShopItems = [...defaultShopItems];
let currentAreas = [];

const preferredEnemyDisplayNameByKey = {
  random: "Random",
  "training-slime": "Training Slime",
  wolf: "Wolf",
  goblin: "Goblin"
};

function getNormalizedPreferredEnemyKey(preferredEnemyKey, allowedEnemyKeys = null) {
  if (typeof preferredEnemyKey !== "string") {
    return defaultPreferredEnemyKey;
  }

  const normalized = preferredEnemyKey.trim().toLowerCase();
  if (!normalized) {
    return defaultPreferredEnemyKey;
  }

  if (!Array.isArray(allowedEnemyKeys) || allowedEnemyKeys.length <= 0) {
    return Object.prototype.hasOwnProperty.call(preferredEnemyDisplayNameByKey, normalized)
      ? normalized
      : defaultPreferredEnemyKey;
  }

  return allowedEnemyKeys.includes(normalized)
    ? normalized
    : defaultPreferredEnemyKey;
}

function getPreferredEnemyDisplayName(preferredEnemyKey) {
  const normalizedKey = getNormalizedPreferredEnemyKey(preferredEnemyKey);
  if (Object.prototype.hasOwnProperty.call(preferredEnemyDisplayNameByKey, normalizedKey)) {
    return preferredEnemyDisplayNameByKey[normalizedKey];
  }

  return humanizeHoldingItemKey(normalizedKey);
}

function getCurrentArea(player) {
  if (currentAreas.length <= 0) {
    return null;
  }

  const playerAreaKey = typeof player?.currentAreaKey === "string" && player.currentAreaKey.trim().length > 0
    ? player.currentAreaKey.trim().toLowerCase()
    : "";
  const fallbackArea = currentAreas.find(area => area.isStartingArea) ?? currentAreas[0];
  return currentAreas.find(area => area.areaKey === playerAreaKey) ?? fallbackArea;
}

function getPreferredEnemyOptionsForArea(area) {
  const options = [defaultPreferredEnemyKey];
  if (!area) {
    return options;
  }

  const areaEnemyKeys = Array.isArray(area.normalEnemyKeys) ? area.normalEnemyKeys : [];
  for (const enemyKey of areaEnemyKeys) {
    const normalizedEnemyKey = typeof enemyKey === "string" ? enemyKey.trim().toLowerCase() : "";
    if (normalizedEnemyKey !== defaultPreferredEnemyKey && !options.includes(normalizedEnemyKey)) {
      options.push(normalizedEnemyKey);
    }
  }

  return options;
}

function syncPreferredEnemyUi(player) {
  const currentArea = getCurrentArea(player);
  const preferredEnemyOptions = getPreferredEnemyOptionsForArea(currentArea);
  preferredEnemySelect.innerHTML = preferredEnemyOptions
    .map(enemyKey => `<option value="${enemyKey}">${getPreferredEnemyDisplayName(enemyKey)}</option>`)
    .join("");

  const preferredEnemyKey = getNormalizedPreferredEnemyKey(player?.preferredEnemyKey, preferredEnemyOptions);
  currentPreferredEnemyKey = preferredEnemyKey;
  preferredEnemySelect.value = preferredEnemyKey;
  const preferredEnemyName = getPreferredEnemyDisplayName(preferredEnemyKey);
  preferredEnemyStatusElement.textContent = `Preferred Enemy: ${preferredEnemyName}`;
  playerStatusPreferredEnemyElement.textContent = preferredEnemyName;
}

function getNormalizedPowerStrikeEnabled(value) {
  return typeof value === "boolean" ? value : defaultPowerStrikeEnabled;
}

function syncPowerStrikeUi(player) {
  const powerStrikeEnabled = getNormalizedPowerStrikeEnabled(player?.powerStrikeEnabled);
  currentPowerStrikeEnabled = powerStrikeEnabled;
  powerStrikeCheckbox.checked = powerStrikeEnabled;
  powerStrikeStatusElement.textContent = `Power Strike: ${powerStrikeEnabled ? "On" : "Off"}`;
}

function normalizeArea(rawArea) {
  const areaKey = typeof rawArea?.areaKey === "string" && rawArea.areaKey.trim().length > 0
    ? rawArea.areaKey.trim().toLowerCase()
    : "";
  const displayName = typeof rawArea?.displayName === "string" && rawArea.displayName.trim().length > 0
    ? rawArea.displayName.trim()
    : areaKey;
  const unlockLevel = Number.isFinite(rawArea?.unlockLevel) ? rawArea.unlockLevel : 1;
  const isStartingArea = !!rawArea?.isStartingArea;
  const normalEnemyKeys = Array.isArray(rawArea?.normalEnemyKeys)
    ? rawArea.normalEnemyKeys
      .filter(enemyKey => typeof enemyKey === "string" && enemyKey.trim().length > 0)
      .map(enemyKey => enemyKey.trim().toLowerCase())
    : [];
  const dungeonKeys = Array.isArray(rawArea?.dungeonKeys)
    ? rawArea.dungeonKeys
      .filter(dungeonKey => typeof dungeonKey === "string" && dungeonKey.trim().length > 0)
      .map(dungeonKey => dungeonKey.trim().toLowerCase())
    : [];
  return { areaKey, displayName, unlockLevel, isStartingArea, normalEnemyKeys, dungeonKeys };
}

function syncAreaSelectUi(player) {
  if (currentAreas.length <= 0) {
    currentAreaSelect.innerHTML = "";
    currentAreaStatusElement.textContent = "Current Area: -";
    return;
  }

  const playerLevel = Number.isFinite(player?.level) ? player.level : 1;
  const selectedArea = getCurrentArea(player);

  currentAreaSelect.innerHTML = currentAreas.map(area => {
    const locked = playerLevel < area.unlockLevel;
    const lockSuffix = locked ? ` (Unlock Lv${area.unlockLevel})` : "";
    return `<option value="${area.areaKey}" ${locked ? "disabled" : ""}>${area.displayName}${lockSuffix}</option>`;
  }).join("");

  if (selectedArea) {
    currentAreaSelect.value = selectedArea.areaKey;
    currentAreaStatusElement.textContent = `Current Area: ${selectedArea.displayName}`;
  }
}

function showCurrentEncounter(player) {
  const encounter = player?.currentEncounter ?? null;
  if (!encounter?.isActive) {
    currentEncounterNameElement.textContent = "No active encounter";
    currentEncounterTypeElement.textContent = "-";
    currentEncounterWaveElement.textContent = "-";
    return;
  }

  currentEncounterNameElement.textContent = encounter.encounterName ?? "Encounter";
  currentEncounterTypeElement.textContent = encounter.encounterType ?? "-";
  const waveIndex = Number.isFinite(encounter.waveIndex) ? encounter.waveIndex : 1;
  const totalWaves = Number.isFinite(encounter.totalWaves) ? encounter.totalWaves : 1;
  currentEncounterWaveElement.textContent = `${waveIndex}/${totalWaves}`;
}

function showResult(data) {
  resultElement.textContent = JSON.stringify(data, null, 2);
}

function writeLastResultMessages(messages) {
  const normalizedMessages = (messages ?? [])
    .filter(message => typeof message === "string" && message.trim().length > 0)
    .map(message => message.trim());

  fightResultElement.textContent = normalizedMessages.length > 0
    ? normalizedMessages.join(" | ")
    : "No action yet.";
}

function getShopItemEffectText(shopItem) {
  if (isPureHpRecoverShopItem(shopItem)) {
    return `Recover ${shopItem.effect.hpRecover} HP`;
  }
  const foodDelta = Number.isFinite(shopItem?.effect?.foodDelta) ? shopItem.effect.foodDelta : 0;
  return `+${foodDelta} Food`;
}

function isPureHpRecoverShopItem(shopItem) {
  const hpRecover = Number.isFinite(shopItem?.effect?.hpRecover) ? shopItem.effect.hpRecover : 0;
  const foodDelta = Number.isFinite(shopItem?.effect?.foodDelta) ? shopItem.effect.foodDelta : 0;
  return hpRecover > 0 && foodDelta <= 0;
}

function resolveConsumableUseMetadata(shopItem) {
  const consumedAmount = Number.isFinite(shopItem?.consumableUse?.consumedAmount) && shopItem.consumableUse.consumedAmount > 0
    ? shopItem.consumableUse.consumedAmount
    : 0;
  const hpRecover = Number.isFinite(shopItem?.consumableUse?.hpRecover) && shopItem.consumableUse.hpRecover >= 0
    ? shopItem.consumableUse.hpRecover
    : 0;
  return { consumedAmount, hpRecover };
}

function isManuallyConsumableShopItem(shopItem) {
  const consumableUse = resolveConsumableUseMetadata(shopItem);
  return consumableUse.consumedAmount > 0 && consumableUse.hpRecover > 0;
}

function getManualConsumableItemKeyByDisplayName(displayName) {
  const normalizedDisplayName = typeof displayName === "string" ? displayName.trim().toLowerCase() : "";
  const match = currentShopItems.find(item =>
    isManuallyConsumableShopItem(item)
    && item.itemKey !== "food"
    && (typeof item.displayName === "string" ? item.displayName.trim().toLowerCase() : "") === normalizedDisplayName);
  return typeof match?.itemKey === "string" && match.itemKey.trim().length > 0
    ? match.itemKey.trim().toLowerCase()
    : null;
}

async function useConsumableItem(itemKey, options = {}) {
  const normalizedItemKey = typeof itemKey === "string" ? itemKey.trim().toLowerCase() : "";
  if (!normalizedItemKey) {
    if (options.writeLastResult !== false) {
      writeLastResultMessages(["Use item failed: Invalid consumable item key."]);
    }
    showResult({ error: "Use item failed.", detail: "Invalid consumable item key." });
    return null;
  }

  if (normalizedItemKey === "food") {
    return useFood(options);
  }

  return useItem(normalizedItemKey, options);
}

function formatShopPurchaseResultSummary(purchaseResult) {
  const player = purchaseResult?.player ?? null;
  const displayName = typeof purchaseResult?.displayName === "string" && purchaseResult.displayName.trim().length > 0
    ? purchaseResult.displayName.trim()
    : "item";
  const resourceDelta = resolveShopPurchaseResourceDelta(purchaseResult);
  const holdingDelta = resolveHoldingDelta(purchaseResult?.holdingDelta);
  const currentGold = Number.isFinite(player?.gold) ? player.gold : "?";
  const currentExperience = Number.isFinite(player?.experience) ? player.experience : "?";
  const currentFood = Number.isFinite(player?.food) ? player.food : "?";
  return `Buy ${displayName} success: Resource Delta ${formatResourceDeltaText(resourceDelta.goldDelta, resourceDelta.experienceDelta, resourceDelta.foodDelta)}, Holding Delta ${formatHoldingDeltaText(holdingDelta)}. Current Resources: Gold ${currentGold}, EXP ${currentExperience}, Food ${currentFood}.`;
}

function formatUseFoodResultSummary(useFoodResult, source) {
  return formatConsumableUseResultSummary(useFoodResult, {
    defaultItemKey: "food",
    actionName: source === "auto" ? "Auto Use Food" : undefined
  });
}

function formatUseItemResultSummary(useItemResult) {
  return formatConsumableUseResultSummary(useItemResult);
}

function formatConsumableUseResultSummary(useResult, options = {}) {
  const player = useResult?.player ?? null;
  const itemKey = resolveConsumableUseItemKey(useResult, options.defaultItemKey);
  const consumedAmount = Number.isFinite(useResult?.consumedAmount) ? useResult.consumedAmount : 1;
  const actionName = typeof options.actionName === "string" && options.actionName.trim().length > 0
    ? options.actionName.trim()
    : (typeof useResult?.actionName === "string" && useResult.actionName.trim().length > 0
      ? useResult.actionName.trim()
      : `Use ${humanizeHoldingItemKey(itemKey)}`);
  const recoveredHp = Number.isFinite(useResult?.recoveredHp) ? useResult.recoveredHp : 0;
  const resourceDelta = resolveConsumableUseResourceDelta(useResult, itemKey, consumedAmount);
  const holdingDelta = resolveHoldingDelta(useResult?.holdingDelta, {
    itemKey,
    quantityDelta: -consumedAmount
  });
  const currentHp = Number.isFinite(player?.currentHp) ? player.currentHp : "?";
  const maxHp = Number.isFinite(player?.maxHp) ? player.maxHp : "?";
  return `${actionName}: Resource Delta ${formatResourceDeltaText(resourceDelta.goldDelta, resourceDelta.experienceDelta, resourceDelta.foodDelta)}, Holding Delta ${formatHoldingDeltaText(holdingDelta)}, recovered ${recoveredHp} HP. Current HP: ${currentHp}/${maxHp}.`;
}

function syncShopItemsUi() {
  const rows = currentShopItems.map(item =>
    `<div>${item.displayName} | ${item.goldPrice} Gold | ${getShopItemEffectText(item)} <button type="button" data-shop-item-key="${item.itemKey}">Buy</button></div>`);
  shopItemsContainerElement.innerHTML = rows.length > 0 ? rows.join("") : "No shop items.";
}

function normalizeShopItem(rawItem) {
  const itemKey = typeof rawItem?.itemKey === "string" && rawItem.itemKey.trim().length > 0
    ? rawItem.itemKey.trim().toLowerCase()
    : defaultShopItem.itemKey;
  const displayName = typeof rawItem?.displayName === "string" && rawItem.displayName.trim().length > 0
    ? rawItem.displayName.trim()
    : defaultShopItem.displayName;
  const goldPrice = Number.isFinite(rawItem?.goldPrice) && rawItem.goldPrice >= 0
    ? rawItem.goldPrice
    : defaultShopItem.goldPrice;
  const foodDelta = Number.isFinite(rawItem?.effect?.foodDelta)
    ? rawItem.effect.foodDelta
    : defaultShopItem.effect.foodDelta;
  const hpRecover = Number.isFinite(rawItem?.effect?.hpRecover) && rawItem.effect.hpRecover >= 0
    ? rawItem.effect.hpRecover
    : 0;
  const consumableUseConsumedAmount = Number.isFinite(rawItem?.consumableUse?.consumedAmount) && rawItem.consumableUse.consumedAmount > 0
    ? rawItem.consumableUse.consumedAmount
    : 0;
  const consumableUseHpRecover = Number.isFinite(rawItem?.consumableUse?.hpRecover) && rawItem.consumableUse.hpRecover >= 0
    ? rawItem.consumableUse.hpRecover
    : 0;

  return {
    itemKey,
    displayName,
    goldPrice,
    effect: { foodDelta, hpRecover },
    consumableUse: {
      consumedAmount: consumableUseConsumedAmount,
      hpRecover: consumableUseHpRecover
    }
  };
}

async function loadShopItems() {
  const response = await fetch("/api/shop/items");
  if (!response.ok) {
    currentShopItems = [...defaultShopItems];
    syncShopItemsUi();
    return;
  }

  const text = await response.text();
  const items = JSON.parse(text);
  const normalizedItems = Array.isArray(items)
    ? items.map(normalizeShopItem)
    : [];
  currentShopItems = normalizedItems.length > 0 ? normalizedItems : [...defaultShopItems];
  syncShopItemsUi();
}

async function loadAreas() {
  const response = await fetch("/api/areas");
  if (!response.ok) {
    currentAreas = [];
    syncAreaSelectUi(null);
    return;
  }

  const text = await response.text();
  const areas = JSON.parse(text);
  const normalizedAreas = Array.isArray(areas)
    ? areas.map(normalizeArea).filter(area => area.areaKey.length > 0)
    : [];
  currentAreas = normalizedAreas;
  syncPreferredEnemyUi(null);
}

function showPlayerStatus(player) {
  if (!player) {
    playerStatusIdElement.textContent = "-";
    playerStatusNameElement.textContent = "-";
    playerStatusLevelElement.textContent = "-";
    playerStatusExperienceElement.textContent = "-";
    playerStatusGoldElement.textContent = "-";
    playerStatusFoodElement.textContent = "-";
    playerStatusAttackElement.textContent = "-";
    playerStatusMaxHpElement.textContent = "-";
    playerStatusCurrentHpElement.textContent = "-";
    playerStatusPreferredEnemyElement.textContent = "-";
    playerStatusCreatedAtElement.textContent = "-";
    playerStatusUpdatedAtElement.textContent = "-";
    syncPreferredEnemyUi(null);
    syncPowerStrikeUi(null);
    syncAreaSelectUi(null);
    showCurrentEncounter(null);
    return;
  }

  playerStatusIdElement.textContent = player.id;
  playerStatusNameElement.textContent = player.name;
  playerStatusLevelElement.textContent = player.level;
  playerStatusExperienceElement.textContent = player.experience;
  playerStatusGoldElement.textContent = player.gold;
  playerStatusFoodElement.textContent = player.food;
  playerStatusAttackElement.textContent = player.attack;
  playerStatusMaxHpElement.textContent = player.maxHp;
  playerStatusCurrentHpElement.textContent = player.currentHp;
  playerStatusPreferredEnemyElement.textContent = "-";
  playerStatusCreatedAtElement.textContent = player.createdAt;
  playerStatusUpdatedAtElement.textContent = player.updatedAt;
  syncPowerStrikeUi(player);
  syncAreaSelectUi(player);
  syncPreferredEnemyUi(player);
}

function showCurrentEnemy(player) {
  if (!player || !player.currentEnemyName) {
    currentEnemyNameElement.textContent = "No active enemy";
    currentEnemyHpElement.textContent = "-";
    currentEnemyAttackElement.textContent = "-";
    return;
  }

  currentEnemyNameElement.textContent = player.currentEnemyName;
  currentEnemyHpElement.textContent = `${player.currentEnemyCurrentHp}/${player.currentEnemyMaxHp}`;
  currentEnemyAttackElement.textContent = player.currentEnemyAttack;
}

async function setCurrentArea() {
  const id = playerIdInput.value;
  const areaKey = typeof currentAreaSelect.value === "string" ? currentAreaSelect.value.trim().toLowerCase() : "";
  if (!areaKey) {
    return;
  }

  const response = await fetch(`/api/players/${encodeURIComponent(id)}/current-area`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ areaKey })
  });

  const text = await response.text();
  if (!response.ok) {
    let message = "Set current area failed.";
    try {
      const errorPayload = JSON.parse(text);
      if (errorPayload?.message) {
        message = errorPayload.message;
      }
    } catch {
      // keep fallback message
    }

    writeLastResultMessages([message]);
    showResult({ error: `Set current area failed (${response.status})`, detail: text });
    return;
  }

  const player = JSON.parse(text);
  showPlayerStatus(player);
  showCurrentEncounter(player);
  showCurrentEnemy(player);
  await loadHoldings(player.id);
  writeLastResultMessages([`Current Area updated: ${player.currentAreaDisplayName}.`]);
  showResult(player);
}

function formatHoldingRow(holding) {
  const displayName = typeof holding?.displayName === "string" && holding.displayName.trim().length > 0
    ? holding.displayName.trim()
    : (typeof holding?.itemKey === "string" && holding.itemKey.trim().length > 0 ? holding.itemKey.trim() : "item");
  const quantity = Number.isFinite(holding?.quantity) ? holding.quantity : 0;
  return `${displayName} x ${quantity}`;
}

function showHoldings(holdings) {
  if (!Array.isArray(holdings) || holdings.length <= 0) {
    playerHoldingsContainerElement.textContent = "No holdings.";
    return;
  }

  playerHoldingsContainerElement.innerHTML = holdings
    .map(formatHoldingRow)
    .map(row => `<div>${row}</div>`)
    .join("");
}

async function loadHoldings(playerId) {
  if (!Number.isFinite(playerId)) {
    showHoldings([]);
    return;
  }

  const response = await fetch(`/api/players/${encodeURIComponent(playerId)}/holdings`);
  if (!response.ok) {
    showHoldings([]);
    return;
  }

  const text = await response.text();
  const holdings = JSON.parse(text);
  showHoldings(holdings);
}

async function loadPlayer() {
  stopAutoFight();
  await loadAreas();
  const id = playerIdInput.value;
  const response = await fetch(`/api/players/${encodeURIComponent(id)}`);
  const text = await response.text();

  if (!response.ok) {
    showPlayerStatus(null);
    showCurrentEnemy(null);
    writeLastResultMessages([`Load Player failed (${response.status}).`]);
    showResult({ error: `Load failed (${response.status})`, detail: text });
    return;
  }

  const player = JSON.parse(text);
  showPlayerStatus(player);
  showCurrentEncounter(player);
  showCurrentEnemy(player);
  await loadHoldings(player.id);
  writeLastResultMessages([`Load Player success: ${player.name} (ID ${player.id}).`]);
  showResult(player);
}

async function createPlayer() {
  stopAutoFight();
  const name = playerNameInput.value.trim();
  await loadAreas();
  const response = await fetch("/api/players", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ name })
  });

  const text = await response.text();
  if (!response.ok) {
    showPlayerStatus(null);
    showCurrentEnemy(null);
    writeLastResultMessages([`Create Player failed (${response.status}).`]);
    showResult({ error: `Create failed (${response.status})`, detail: text });
    return;
  }

  const player = JSON.parse(text);
  playerIdInput.value = player.id;
  showPlayerStatus(player);
  showCurrentEncounter(player);
  showCurrentEnemy(player);
  await loadHoldings(player.id);
  writeLastResultMessages([`Create Player success: ${player.name} (ID ${player.id}).`]);
  showResult(player);
}

async function addGold() {
  const id = playerIdInput.value;
  const response = await fetch(`/api/players/${encodeURIComponent(id)}/gold`, {
    method: "POST"
  });

  const text = await response.text();
  if (!response.ok) {
    showPlayerStatus(null);
    showCurrentEnemy(null);
    writeLastResultMessages([`Add Gold failed (${response.status}).`]);
    showResult({ error: `Add gold failed (${response.status})`, detail: text });
    return;
  }

  const player = JSON.parse(text);
  showPlayerStatus(player);
  showCurrentEncounter(player);
  showCurrentEnemy(player);
  await loadHoldings(player.id);
  writeLastResultMessages([`Add Gold success: +10 Gold (Current: ${player.gold}).`]);
  showResult(player);
}

function buildFightMessage(fightResult) {
  const statusText = fightResult.enemyDefeated ? "Enemy Defeated" : (fightResult.playerDefeated ? "Player Defeated" : "Ongoing");
  const playerActions = Array.isArray(fightResult.playerActions) ? fightResult.playerActions : [];
  const enemyActions = Array.isArray(fightResult.enemyActions) ? fightResult.enemyActions : [];
  const actionOrderText = formatPlayerActionOrder(playerActions);
  const actionDamageText = formatPlayerActionDamage(playerActions, fightResult.playerDamageDealt);
  const enemyActionOrderText = formatEnemyActionOrder(enemyActions);
  const enemyActionDamageText = formatEnemyActionDamage(enemyActions, fightResult.enemyDamageDealt);
  const enemyText = `Enemy: ${fightResult.enemyName} (ATK ${fightResult.enemyAttack})`;
  const enemyHpText = `Enemy HP: ${fightResult.enemyCurrentHp}/${fightResult.enemyMaxHp}`;
  const roundDamageText = `Round Damage -> You: ${fightResult.playerDamageDealt}, Enemy: ${fightResult.enemyDamageDealt} | Enemy Turn: ${enemyActionOrderText} | Enemy Action Damage: ${enemyActionDamageText}`;
  const rewardText = formatFightRewardText(fightResult);
  const levelText = fightResult.leveledUp ? ` | LEVEL UP! Lv${fightResult.player.level}` : "";
  const resultText = `Status: ${statusText} | Player Turn: ${actionOrderText} | Action Damage: ${actionDamageText} | ${enemyHpText} | ${roundDamageText} | ${rewardText}${levelText} | Player HP: ${fightResult.player.currentHp}/${fightResult.player.maxHp}`;
  return `${enemyText} | ${resultText} | ${fightResult.summary}`;
}

function formatFightRewardText(fightResult) {
  if (!fightResult?.enemyDefeated) {
    return "Rewards: none";
  }

  const rewards = fightResult?.rewards ?? null;
  const resourceDelta = resolveFightRewardResourceDelta(fightResult, rewards);
  return `Rewards: Resource Delta ${formatResourceDeltaText(resourceDelta.goldDelta, resourceDelta.experienceDelta, resourceDelta.foodDelta)}`;
}

function coalesceFiniteWithZeroDefault(primaryValue, fallbackValue) {
  if (Number.isFinite(primaryValue)) {
    return primaryValue;
  }

  if (Number.isFinite(fallbackValue)) {
    return fallbackValue;
  }

  return 0;
}

function resolveShopPurchaseResourceDelta(purchaseResult) {
  const resourcesDelta = purchaseResult?.resourcesDelta ?? null;
  const spentGold = Number.isFinite(purchaseResult?.spentGold) ? purchaseResult.spentGold : 0;
  return {
    goldDelta: coalesceFiniteWithZeroDefault(resourcesDelta?.goldDelta, -spentGold),
    experienceDelta: coalesceFiniteWithZeroDefault(resourcesDelta?.experienceDelta, 0),
    foodDelta: coalesceFiniteWithZeroDefault(resourcesDelta?.foodDelta, purchaseResult?.effect?.foodDelta)
  };
}

function resolveConsumableUseResourceDelta(useResult, itemKey, consumedAmount) {
  const resourcesDelta = useResult?.resourcesDelta ?? null;
  const fallbackFoodDelta = itemKey === "food" ? -consumedAmount : 0;
  return {
    goldDelta: coalesceFiniteWithZeroDefault(resourcesDelta?.goldDelta, 0),
    experienceDelta: coalesceFiniteWithZeroDefault(resourcesDelta?.experienceDelta, 0),
    foodDelta: coalesceFiniteWithZeroDefault(resourcesDelta?.foodDelta, fallbackFoodDelta)
  };
}

function resolveConsumableUseItemKey(useResult, defaultItemKey = "item") {
  const rawItemKey = typeof useResult?.itemKey === "string" && useResult.itemKey.trim().length > 0
    ? useResult.itemKey.trim().toLowerCase()
    : "";
  if (rawItemKey.length > 0) {
    return rawItemKey;
  }

  const rawResourceKey = typeof useResult?.resourceKey === "string" && useResult.resourceKey.trim().length > 0
    ? useResult.resourceKey.trim().toLowerCase()
    : "";
  if (rawResourceKey.length > 0) {
    return rawResourceKey;
  }

  return defaultItemKey;
}

function resolveUseItemResourceDelta(useItemResult) {
  const itemKey = resolveConsumableUseItemKey(useItemResult);
  const consumedAmount = Number.isFinite(useItemResult?.consumedAmount) ? useItemResult.consumedAmount : 1;
  return resolveConsumableUseResourceDelta(useItemResult, itemKey, consumedAmount);
}

function resolveUseFoodResourceDelta(useFoodResult) {
  const itemKey = resolveConsumableUseItemKey(useFoodResult, "food");
  const consumedAmount = Number.isFinite(useFoodResult?.consumedAmount) ? useFoodResult.consumedAmount : 1;
  return resolveConsumableUseResourceDelta(useFoodResult, itemKey, consumedAmount);
}

function resolveHoldingDelta(rawHoldingDelta, fallback = null) {
  const fallbackItemKey = typeof fallback?.itemKey === "string" && fallback.itemKey.trim().length > 0
    ? fallback.itemKey.trim().toLowerCase()
    : "item";
  const fallbackQuantityDelta = Number.isFinite(fallback?.quantityDelta) ? fallback.quantityDelta : 0;
  const itemKey = typeof rawHoldingDelta?.itemKey === "string" && rawHoldingDelta.itemKey.trim().length > 0
    ? rawHoldingDelta.itemKey.trim().toLowerCase()
    : fallbackItemKey;
  const quantityDelta = Number.isFinite(rawHoldingDelta?.quantityDelta)
    ? rawHoldingDelta.quantityDelta
    : fallbackQuantityDelta;
  const displayName = typeof rawHoldingDelta?.displayName === "string" && rawHoldingDelta.displayName.trim().length > 0
    ? rawHoldingDelta.displayName.trim()
    : humanizeHoldingItemKey(itemKey);

  return { itemKey, quantityDelta, displayName };
}

function humanizeHoldingItemKey(itemKey) {
  if (typeof itemKey !== "string" || itemKey.trim().length <= 0) {
    return "item";
  }

  return itemKey
    .trim()
    .split("-")
    .filter(part => part.length > 0)
    .map(part => `${part.charAt(0).toUpperCase()}${part.slice(1)}`)
    .join(" ");
}

function formatHoldingDeltaText(holdingDelta) {
  if (!holdingDelta || !Number.isFinite(holdingDelta.quantityDelta) || holdingDelta.quantityDelta === 0) {
    return "(none)";
  }

  const displayName = typeof holdingDelta.displayName === "string" && holdingDelta.displayName.trim().length > 0
    ? holdingDelta.displayName.trim()
    : humanizeHoldingItemKey(holdingDelta.itemKey);
  return `(${displayName} ${formatSignedDelta(holdingDelta.quantityDelta)})`;
}

function resolveFightRewardResourceDelta(fightResult, rewards) {
  const resourcesDelta = rewards?.resourcesDelta ?? null;
  const goldReward = coalesceFiniteWithZeroDefault(rewards?.gold, fightResult?.goldReward);
  const experienceReward = coalesceFiniteWithZeroDefault(rewards?.experience, fightResult?.experienceReward);
  const foodReward = coalesceFiniteWithZeroDefault(rewards?.food, 0);
  return {
    goldDelta: coalesceFiniteWithZeroDefault(resourcesDelta?.goldDelta, goldReward),
    experienceDelta: coalesceFiniteWithZeroDefault(resourcesDelta?.experienceDelta, experienceReward),
    foodDelta: coalesceFiniteWithZeroDefault(resourcesDelta?.foodDelta, foodReward)
  };
}

function formatSignedDelta(value) {
  const normalized = Number.isFinite(value) ? value : 0;
  if (normalized > 0) {
    return `+${normalized}`;
  }

  return `${normalized}`;
}

function formatResourceDeltaText(goldDelta, experienceDelta, foodDelta) {
  const entries = [];
  if (goldDelta !== 0) {
    entries.push(`Gold ${formatSignedDelta(goldDelta)}`);
  }

  if (experienceDelta !== 0) {
    entries.push(`EXP ${formatSignedDelta(experienceDelta)}`);
  }

  if (foodDelta !== 0) {
    entries.push(`Food ${formatSignedDelta(foodDelta)}`);
  }

  if (entries.length <= 0) {
    return "(none)";
  }

  return `(${entries.join(", ")})`;
}

function formatPlayerActionOrder(playerActions) {
  if (playerActions.length <= 0) {
    return "None";
  }

  return playerActions.map(action => action.actionName).join(" -> ");
}

function formatPlayerActionDamage(playerActions, fallbackDamage) {
  if (playerActions.length <= 0) {
    return `Total:${fallbackDamage}`;
  }

  return playerActions.map(action => `${action.actionName}:${action.damageDealt}`).join(", ");
}

function formatEnemyActionOrder(enemyActions) {
  if (enemyActions.length > 0) {
    return enemyActions.map(action => action.actionName).join(" -> ");
  }

  return "Skipped";
}

function formatEnemyActionDamage(enemyActions, fallbackEnemyDamage) {
  if (enemyActions.length <= 0) {
    return `Total:${fallbackEnemyDamage}`;
  }

  return enemyActions.map(action => `${action.actionName}:${action.damageDealt}`).join(", ");
}

async function fight(options = {}) {
  const { writeLastResult = true } = options;
  const id = playerIdInput.value;
  const response = await fetch(`/api/players/${encodeURIComponent(id)}/fight`, {
    method: "POST"
  });

  const text = await response.text();
  if (!response.ok) {
    showPlayerStatus(null);
    showCurrentEnemy(null);
    const message = `Fight failed (${response.status}).`;
    if (writeLastResult) {
      writeLastResultMessages([message]);
    }
    showResult({ error: `Fight failed (${response.status})`, detail: text });
    return null;
  }

  const fightResult = JSON.parse(text);
  showPlayerStatus(fightResult.player);
  showCurrentEncounter(fightResult.player);
  showCurrentEnemy(fightResult.player);
  await loadHoldings(fightResult?.player?.id);
  const messages = [buildFightMessage(fightResult)];
  if (fightResult.playerDefeated && autoFightTimerId !== null) {
    stopAutoFight();
    messages.push("Auto Fight stopped: player defeated.");
  }

  if (writeLastResult) {
    writeLastResultMessages(messages);
  }

  showResult(fightResult);
  return {
    player: fightResult.player,
    fightResult,
    messages
  };
}

async function useFood(options = {}) {
  const { writeLastResult = true, source = "manual" } = options;
  const id = playerIdInput.value;
  const response = await fetch(`/api/players/${encodeURIComponent(id)}/use-food`, {
    method: "POST"
  });

  const text = await response.text();
  if (!response.ok) {
    let message = "Use food failed.";
    try {
      const errorPayload = JSON.parse(text);
      if (errorPayload?.message) {
        message = errorPayload.message;
      }
    } catch {
      // keep fallback message
    }

    if (writeLastResult) {
      writeLastResultMessages([message]);
    }
    showResult({ error: `Use food failed (${response.status})`, detail: text });
    return;
  }

  const useFoodResult = JSON.parse(text);
  const player = useFoodResult.player;
  showPlayerStatus(player);
  showCurrentEncounter(player);
  showCurrentEnemy(player);
  await loadHoldings(player?.id);
  const foodMessage = formatUseFoodResultSummary(useFoodResult, source);

  if (writeLastResult) {
    writeLastResultMessages([foodMessage]);
  }

  showResult(useFoodResult);
  return {
    player,
    useFoodResult,
    message: foodMessage
  };
}

async function useItem(itemKey, options = {}) {
  const { writeLastResult = true } = options;
  const id = playerIdInput.value;
  const response = await fetch(`/api/players/${encodeURIComponent(id)}/use-item/${encodeURIComponent(itemKey)}`, {
    method: "POST"
  });

  const text = await response.text();
  if (!response.ok) {
    let message = "Use item failed.";
    try {
      const errorPayload = JSON.parse(text);
      if (errorPayload?.message) {
        message = errorPayload.message;
      }
    } catch {
      // keep fallback message
    }

    if (writeLastResult) {
      writeLastResultMessages([message]);
    }
    showResult({ error: `Use item failed (${response.status})`, detail: text });
    return;
  }

  const useItemResult = JSON.parse(text);
  const player = useItemResult.player;
  showPlayerStatus(player);
  showCurrentEncounter(player);
  showCurrentEnemy(player);
  await loadHoldings(player?.id);
  const message = formatUseItemResultSummary(useItemResult);
  if (writeLastResult) {
    writeLastResultMessages([message]);
  }
  showResult(useItemResult);
  return {
    player,
    useItemResult,
    message
  };
}

async function buyShopItem(itemKey) {
  const id = playerIdInput.value;
  const response = await fetch(`/api/players/${encodeURIComponent(id)}/buy-item/${encodeURIComponent(itemKey)}`, {
    method: "POST"
  });

  const text = await response.text();
  if (!response.ok) {
    let message = "Buy item failed.";
    try {
      const errorPayload = JSON.parse(text);
      if (errorPayload?.message) {
        message = errorPayload.message;
      }
    } catch {
      // keep fallback message
    }

    writeLastResultMessages([`Buy item failed: ${message}`]);
    showResult({ error: `Buy item failed (${response.status})`, detail: text });
    return;
  }

  const purchaseResult = JSON.parse(text);
  const player = purchaseResult.player;
  if (player) {
    showPlayerStatus(player);
    showCurrentEncounter(player);
    showCurrentEnemy(player);
    await loadHoldings(player.id);
  } else {
    showPlayerStatus(null);
    showCurrentEnemy(null);
    showHoldings([]);
  }

  writeLastResultMessages([formatShopPurchaseResultSummary(purchaseResult)]);
  showResult(purchaseResult);
}

async function setPreferredEnemy() {
  const id = playerIdInput.value;
  const previousPreferredEnemyKey = currentPreferredEnemyKey;
  const currentArea = getCurrentArea({
    currentAreaKey: typeof currentAreaSelect.value === "string" ? currentAreaSelect.value : ""
  });
  const preferredEnemyOptions = getPreferredEnemyOptionsForArea(currentArea);
  const enemyKey = getNormalizedPreferredEnemyKey(preferredEnemySelect.value, preferredEnemyOptions);
  const response = await fetch(`/api/players/${encodeURIComponent(id)}/preferred-enemy`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ enemyKey })
  });

  const text = await response.text();
  if (!response.ok) {
    let message = "Set preferred enemy failed.";
    try {
      const errorPayload = JSON.parse(text);
      if (errorPayload?.message) {
        message = errorPayload.message;
      }
    } catch {
      // keep fallback message
    }

    syncPreferredEnemyUi({ preferredEnemyKey: previousPreferredEnemyKey });
    writeLastResultMessages([message]);
    showResult({ error: `Set preferred enemy failed (${response.status})`, detail: text });
    return;
  }

  const player = JSON.parse(text);
  const preferredEnemyName = getPreferredEnemyDisplayName(player.preferredEnemyKey);
  showPlayerStatus(player);
  showCurrentEncounter(player);
  showCurrentEnemy(player);
  await loadHoldings(player.id);
  writeLastResultMessages([`Preferred Enemy updated: ${preferredEnemyName}.`]);
  showResult(player);
}

async function setPowerStrikeEnabled() {
  const id = playerIdInput.value;
  const previousEnabled = currentPowerStrikeEnabled;
  const enabled = !!powerStrikeCheckbox.checked;
  const response = await fetch(`/api/players/${encodeURIComponent(id)}/power-strike`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ enabled })
  });

  const text = await response.text();
  if (!response.ok) {
    syncPowerStrikeUi({ powerStrikeEnabled: previousEnabled });
    writeLastResultMessages([`Set Power Strike failed (${response.status}).`]);
    showResult({ error: `Set Power Strike failed (${response.status})`, detail: text });
    return;
  }

  const player = JSON.parse(text);
  showPlayerStatus(player);
  showCurrentEncounter(player);
  showCurrentEnemy(player);
  await loadHoldings(player.id);
  writeLastResultMessages([`Power Strike ${player.powerStrikeEnabled ? "enabled" : "disabled"}.`]);
  showResult(player);
}

function setAutoUseFoodStatus() {
  const thresholdPercent = getAutoUseFoodThresholdPercent();
  const thresholdText = `${thresholdPercent}%`;
  const stateText = autoUseFoodCheckbox.checked ? "On" : "Off";
  autoUseFoodStatusElement.textContent = `Auto Use Food: ${stateText} | Threshold: ${thresholdText}`;
}

function getAutoUseFoodThresholdPercent() {
  const thresholdPercent = Number.parseInt(autoUseFoodThresholdSelect.value, 10);
  return Number.isFinite(thresholdPercent) && thresholdPercent > 0
    ? thresholdPercent
    : defaultAutoUseFoodThresholdPercent;
}

function shouldAutoUseFood(player) {
  if (!player) {
    return false;
  }

  if (autoFightTimerId === null) {
    return false;
  }

  if (!autoUseFoodCheckbox.checked) {
    return false;
  }

  if (!Number.isFinite(player.currentHp) || !Number.isFinite(player.maxHp) || !Number.isFinite(player.food)) {
    return false;
  }

  if (player.food <= 0) {
    return false;
  }

  const thresholdPercent = getAutoUseFoodThresholdPercent();
  const hpThreshold = Math.floor(player.maxHp * thresholdPercent / 100);
  return player.currentHp <= hpThreshold;
}

function setAutoFightStatus(isRunning) {
  autoFightStatusElement.textContent = isRunning ? "Auto Fight: Running" : "Auto Fight: Stopped";
  startAutoFightButton.disabled = isRunning;
  stopAutoFightButton.disabled = !isRunning;
}

function startAutoFight() {
  if (autoFightTimerId !== null) {
    return;
  }

  setAutoFightStatus(true);
  autoFightTimerId = setInterval(async () => {
    if (autoFightTickInProgress) {
      console.warn("Skipped auto-fight tick: previous request still in progress.");
      return;
    }

    autoFightTickInProgress = true;
    try {
      const tickMessages = [];
      const fightOutcome = await fight({ writeLastResult: false });
      const playerAfterFight = fightOutcome?.player ?? null;
      if (fightOutcome?.messages?.length) {
        tickMessages.push(...fightOutcome.messages);
      }

      if (autoFightTimerId !== null && shouldAutoUseFood(playerAfterFight)) {
        const useFoodOutcome = await useFood({ writeLastResult: false, source: "auto" });
        if (useFoodOutcome?.message) {
          tickMessages.push(useFoodOutcome.message);
        }
      }

      if (tickMessages.length > 0) {
        writeLastResultMessages(tickMessages);
      }
    } catch (error) {
      stopAutoFight();
      writeLastResultMessages(["Auto Fight stopped due to request error."]);
      showResult({ error: "Auto fight request failed.", detail: String(error) });
    } finally {
      autoFightTickInProgress = false;
    }
  }, autoFightIntervalMs);
}

function stopAutoFight() {
  if (autoFightTimerId === null) {
    setAutoFightStatus(false);
    return;
  }

  clearInterval(autoFightTimerId);
  autoFightTimerId = null;
  autoFightTickInProgress = false;
  setAutoFightStatus(false);
}

document.getElementById("loadPlayerButton").addEventListener("click", loadPlayer);
document.getElementById("createPlayerButton").addEventListener("click", createPlayer);
document.getElementById("addGoldButton").addEventListener("click", addGold);
document.getElementById("fightButton").addEventListener("click", fight);
document.getElementById("useFoodButton").addEventListener("click", () => useConsumableItem("food"));
document.getElementById("usePotionButton").addEventListener("click", () => {
  const potionItemKey = getManualConsumableItemKeyByDisplayName("Potion");
  if (!potionItemKey) {
    writeLastResultMessages(["Use item failed: Potion consumable is not configured."]);
    showResult({ error: "Use item failed.", detail: "Potion consumable is not configured in shop item metadata." });
    return;
  }

  useConsumableItem(potionItemKey);
});
shopItemsContainerElement.addEventListener("click", event => {
  const target = event.target;
  if (!(target instanceof HTMLElement)) {
    return;
  }

  const itemKey = target.getAttribute("data-shop-item-key");
  if (!itemKey) {
    return;
  }

  buyShopItem(itemKey);
});
startAutoFightButton.addEventListener("click", startAutoFight);
stopAutoFightButton.addEventListener("click", stopAutoFight);
autoUseFoodCheckbox.addEventListener("change", setAutoUseFoodStatus);
autoUseFoodThresholdSelect.addEventListener("change", setAutoUseFoodStatus);
preferredEnemySelect.addEventListener("change", setPreferredEnemy);
currentAreaSelect.addEventListener("change", setCurrentArea);
powerStrikeCheckbox.addEventListener("change", setPowerStrikeEnabled);
setAutoFightStatus(false);
setAutoUseFoodStatus();
loadAreas();
loadShopItems();
syncPreferredEnemyUi(null);
syncPowerStrikeUi(null);
showCurrentEncounter(null);
showHoldings([]);
