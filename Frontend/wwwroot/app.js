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
    fightResultElement.textContent = "Load failed.";
    showResult({ error: `Load failed (${response.status})`, detail: text });
    return;
  }

  const player = JSON.parse(text);
  showPlayerStatus(player);
  showCurrentEnemy(player);
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
    fightResultElement.textContent = "Create failed.";
    showResult({ error: `Create failed (${response.status})`, detail: text });
    return;
  }

  const player = JSON.parse(text);
  playerIdInput.value = player.id;
  showPlayerStatus(player);
  showCurrentEnemy(player);
  fightResultElement.textContent = "Player created.";
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
    fightResultElement.textContent = "Add gold failed.";
    showResult({ error: `Add gold failed (${response.status})`, detail: text });
    return;
  }

  const player = JSON.parse(text);
  showPlayerStatus(player);
  showCurrentEnemy(player);
  fightResultElement.textContent = "Gold +10 applied.";
  showResult(player);
}

async function fight() {
  const id = playerIdInput.value;
  const response = await fetch(`/api/players/${encodeURIComponent(id)}/fight`, {
    method: "POST"
  });

  const text = await response.text();
  if (!response.ok) {
    showPlayerStatus(null);
    showCurrentEnemy(null);
    fightResultElement.textContent = "Fight failed.";
    showResult({ error: `Fight failed (${response.status})`, detail: text });
    return null;
  }

  const fightResult = JSON.parse(text);
  showPlayerStatus(fightResult.player);
  showCurrentEnemy(fightResult.player);
  const statusText = fightResult.enemyDefeated ? "Enemy Defeated" : (fightResult.playerDefeated ? "Player Defeated" : "Ongoing");
  const enemyText = `Enemy: ${fightResult.enemyName} (ATK ${fightResult.enemyAttack})`;
  const enemyHpText = `Enemy HP: ${fightResult.enemyCurrentHp}/${fightResult.enemyMaxHp}`;
  const roundDamageText = `Round Damage -> You: ${fightResult.playerDamageDealt}, Enemy: ${fightResult.enemyDamageDealt}`;
  const rewardText = fightResult.enemyDefeated
    ? `Rewards: Gold +${fightResult.goldReward}, EXP +${fightResult.experienceReward}`
    : "Rewards: none";
  const levelText = fightResult.leveledUp ? ` | LEVEL UP! Lv${fightResult.player.level}` : "";
  const resultText = `Status: ${statusText} | ${enemyHpText} | ${roundDamageText} | ${rewardText}${levelText} | Player HP: ${fightResult.player.currentHp}/${fightResult.player.maxHp}`;
  fightResultElement.textContent = `${enemyText} | ${resultText} | ${fightResult.summary}`;
  if (fightResult.playerDefeated && autoFightTimerId !== null) {
    stopAutoFight();
    fightResultElement.textContent = `${fightResultElement.textContent} | Auto Fight stopped: player defeated.`;
  }
  showResult(fightResult);
  return fightResult.player;
}

async function useFood() {
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

    fightResultElement.textContent = message;
    showResult({ error: `Use food failed (${response.status})`, detail: text });
    return;
  }

  const player = JSON.parse(text);
  showPlayerStatus(player);
  showCurrentEnemy(player);
  const recoveredHp = Number.isFinite(hpBeforeUseFood)
    ? Math.max(0, player.currentHp - hpBeforeUseFood)
    : null;
  if (recoveredHp === null) {
    fightResultElement.textContent = `${player.name} used 1 Food. Current HP: ${player.currentHp}/${player.maxHp}.`;
  } else {
    fightResultElement.textContent = `${player.name} used 1 Food and recovered ${recoveredHp} HP. Current HP: ${player.currentHp}/${player.maxHp}.`;
  }
  showResult(player);
  return player;
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
    fightResultElement.textContent = message;
    showResult({ error: `Set preferred enemy failed (${response.status})`, detail: text });
    return;
  }

  const player = JSON.parse(text);
  showPlayerStatus(player);
  showCurrentEnemy(player);
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
      const playerAfterFight = await fight();
      if (autoFightTimerId !== null && shouldAutoUseFood(playerAfterFight)) {
        const playerAfterFood = await useFood();
        if (playerAfterFood) {
          fightResultElement.textContent = `${fightResultElement.textContent} | Auto Use Food triggered.`;
        }
      }
    } catch (error) {
      stopAutoFight();
      fightResultElement.textContent = "Auto fight stopped due to request error.";
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
