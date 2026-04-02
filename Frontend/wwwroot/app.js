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
const preferredEnemySelect = document.getElementById("preferredEnemySelect");
const preferredEnemyStatusElement = document.getElementById("preferredEnemyStatus");
const defaultAutoUseFoodThresholdPercent = 50;
const defaultPreferredEnemyKey = "random";
const autoFightIntervalMs = 1000;
let autoFightTimerId = null;
let autoFightTickInProgress = false;
let currentPreferredEnemyKey = defaultPreferredEnemyKey;

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
  const enemyText = `Enemy: ${fightResult.enemyName} (ATK ${fightResult.enemyAttack})`;
  const enemyHpText = `Enemy HP: ${fightResult.enemyCurrentHp}/${fightResult.enemyMaxHp}`;
  const roundDamageText = `Round Damage -> You: ${fightResult.playerDamageDealt}, Enemy: ${fightResult.enemyDamageDealt}`;
  const rewardText = fightResult.enemyDefeated
    ? `Rewards: Gold +${fightResult.goldReward}, EXP +${fightResult.experienceReward}`
    : "Rewards: none";
  const levelText = fightResult.leveledUp ? ` | LEVEL UP! Lv${fightResult.player.level}` : "";
  const resultText = `Status: ${statusText} | ${enemyHpText} | ${roundDamageText} | ${rewardText}${levelText} | Player HP: ${fightResult.player.currentHp}/${fightResult.player.maxHp}`;
  return `${enemyText} | ${resultText} | ${fightResult.summary}`;
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
  const hpBeforeUseFood = Number.parseInt(playerStatusCurrentHpElement.textContent ?? "", 10);
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

  const player = JSON.parse(text);
  showPlayerStatus(player);
  showCurrentEnemy(player);
  const recoveredHp = Number.isFinite(hpBeforeUseFood)
    ? Math.max(0, player.currentHp - hpBeforeUseFood)
    : null;
  const foodActionPrefix = source === "auto" ? "Auto Use Food" : "Use Food";
  const foodMessage = recoveredHp === null
    ? `${foodActionPrefix}: ${player.name} used 1 Food. Current HP: ${player.currentHp}/${player.maxHp}.`
    : `${foodActionPrefix}: ${player.name} used 1 Food and recovered ${recoveredHp} HP. Current HP: ${player.currentHp}/${player.maxHp}.`;

  if (writeLastResult) {
    writeLastResultMessages([foodMessage]);
  }

  showResult(player);
  return {
    player,
    message: foodMessage
  };
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
startAutoFightButton.addEventListener("click", startAutoFight);
stopAutoFightButton.addEventListener("click", stopAutoFight);
autoUseFoodCheckbox.addEventListener("change", setAutoUseFoodStatus);
autoUseFoodThresholdSelect.addEventListener("change", setAutoUseFoodStatus);
preferredEnemySelect.addEventListener("change", setPreferredEnemy);
setAutoFightStatus(false);
setAutoUseFoodStatus();
syncPreferredEnemyUi(null);
