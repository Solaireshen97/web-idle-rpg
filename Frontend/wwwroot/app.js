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
const shopItemsContainerElement = document.getElementById("shopItemsContainer");
const defaultAutoUseFoodThresholdPercent = 50;
const defaultPreferredEnemyKey = "random";
const defaultPowerStrikeEnabled = true;
const autoFightIntervalMs = 1000;
const defaultShopItem = {
  itemKey: "food",
  displayName: "Food",
  goldPrice: 5,
  effect: { foodDelta: 1 }
};
const defaultShopItems = [defaultShopItem];
let autoFightTimerId = null;
let autoFightTickInProgress = false;
let currentPreferredEnemyKey = defaultPreferredEnemyKey;
let currentPowerStrikeEnabled = defaultPowerStrikeEnabled;
let currentShopItems = [...defaultShopItems];

const preferredEnemyDisplayNameByKey = {
  random: "Random",
  "training-slime": "Training Slime",
  wolf: "Wolf",
  goblin: "Goblin"
};

function getNormalizedPreferredEnemyKey(preferredEnemyKey) {
  if (typeof preferredEnemyKey !== "string") {
    return defaultPreferredEnemyKey;
  }

  const normalized = preferredEnemyKey.trim().toLowerCase();
  return Object.prototype.hasOwnProperty.call(preferredEnemyDisplayNameByKey, normalized)
    ? normalized
    : defaultPreferredEnemyKey;
}

function getPreferredEnemyDisplayName(preferredEnemyKey) {
  const normalizedKey = getNormalizedPreferredEnemyKey(preferredEnemyKey);
  return preferredEnemyDisplayNameByKey[normalizedKey];
}

function syncPreferredEnemyUi(player) {
  const preferredEnemyKey = getNormalizedPreferredEnemyKey(player?.preferredEnemyKey);
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
  const foodDelta = Number.isFinite(shopItem?.effect?.foodDelta) ? shopItem.effect.foodDelta : 0;
  return `+${foodDelta} Food`;
}

function formatShopPurchaseResultSummary(purchaseResult) {
  const player = purchaseResult?.player ?? null;
  const displayName = typeof purchaseResult?.displayName === "string" && purchaseResult.displayName.trim().length > 0
    ? purchaseResult.displayName.trim()
    : "item";
  const spentGold = Number.isFinite(purchaseResult?.spentGold) ? purchaseResult.spentGold : 0;
  const foodDelta = Number.isFinite(purchaseResult?.effect?.foodDelta) ? purchaseResult.effect.foodDelta : 0;
  const currentGold = Number.isFinite(player?.gold) ? player.gold : "?";
  const currentFood = Number.isFinite(player?.food) ? player.food : "?";
  return `Buy ${displayName} success: Spent ${spentGold} Gold, gained ${foodDelta} Food. Remaining Gold: ${currentGold}, Current Food: ${currentFood}.`;
}

function formatUseFoodResultSummary(useFoodResult, source) {
  const player = useFoodResult?.player ?? null;
  const actionName = source === "auto" ? "Auto Use Food" : (typeof useFoodResult?.actionName === "string" && useFoodResult.actionName.trim().length > 0
    ? useFoodResult.actionName.trim()
    : "Use Food");
  const consumedAmount = Number.isFinite(useFoodResult?.consumedAmount) ? useFoodResult.consumedAmount : 1;
  const recoveredHp = Number.isFinite(useFoodResult?.recoveredHp) ? useFoodResult.recoveredHp : 0;
  const currentHp = Number.isFinite(player?.currentHp) ? player.currentHp : "?";
  const maxHp = Number.isFinite(player?.maxHp) ? player.maxHp : "?";
  return `${actionName}: used ${consumedAmount} Food and recovered ${recoveredHp} HP. Current HP: ${currentHp}/${maxHp}.`;
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

  return {
    itemKey,
    displayName,
    goldPrice,
    effect: { foodDelta }
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
  playerStatusPreferredEnemyElement.textContent = getPreferredEnemyDisplayName(player.preferredEnemyKey);
  playerStatusCreatedAtElement.textContent = player.createdAt;
  playerStatusUpdatedAtElement.textContent = player.updatedAt;
  syncPreferredEnemyUi(player);
  syncPowerStrikeUi(player);
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

async function loadPlayer() {
  stopAutoFight();
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
  showCurrentEnemy(player);
  writeLastResultMessages([`Load Player success: ${player.name} (ID ${player.id}).`]);
  showResult(player);
}

async function createPlayer() {
  stopAutoFight();
  const name = playerNameInput.value.trim();
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
  showCurrentEnemy(player);
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
  showCurrentEnemy(player);
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
  const goldReward = getNumericValueWithFallback(rewards?.gold, fightResult?.goldReward);
  const experienceReward = getNumericValueWithFallback(rewards?.experience, fightResult?.experienceReward);
  const foodReward = getNumericValueWithFallback(rewards?.food, 0);
  return `Rewards: Gold +${goldReward}, EXP +${experienceReward}, Food +${foodReward}`;
}

function getNumericValueWithFallback(primaryValue, fallbackValue) {
  if (Number.isFinite(primaryValue)) {
    return primaryValue;
  }

  if (Number.isFinite(fallbackValue)) {
    return fallbackValue;
  }

  return 0;
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
  showCurrentEnemy(fightResult.player);
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
  showCurrentEnemy(player);
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
    showCurrentEnemy(player);
  } else {
    showPlayerStatus(null);
    showCurrentEnemy(null);
  }

  writeLastResultMessages([formatShopPurchaseResultSummary(purchaseResult)]);
  showResult(purchaseResult);
}

async function setPreferredEnemy() {
  const id = playerIdInput.value;
  const previousPreferredEnemyKey = currentPreferredEnemyKey;
  const enemyKey = getNormalizedPreferredEnemyKey(preferredEnemySelect.value);
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
  showCurrentEnemy(player);
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
  showCurrentEnemy(player);
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
document.getElementById("useFoodButton").addEventListener("click", useFood);
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
powerStrikeCheckbox.addEventListener("change", setPowerStrikeEnabled);
setAutoFightStatus(false);
setAutoUseFoodStatus();
loadShopItems();
syncPreferredEnemyUi(null);
syncPowerStrikeUi(null);
