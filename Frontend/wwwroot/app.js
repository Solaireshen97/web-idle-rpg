const resultElement = document.getElementById("result");
const playerIdInput = document.getElementById("playerId");
const playerNameInput = document.getElementById("playerName");
const playerStatusIdElement = document.getElementById("playerStatusId");
const playerStatusNameElement = document.getElementById("playerStatusName");
const playerStatusLevelElement = document.getElementById("playerStatusLevel");
const playerStatusExperienceElement = document.getElementById("playerStatusExperience");
const playerStatusGoldElement = document.getElementById("playerStatusGold");
const playerStatusAttackElement = document.getElementById("playerStatusAttack");
const playerStatusMaxHpElement = document.getElementById("playerStatusMaxHp");
const playerStatusCurrentHpElement = document.getElementById("playerStatusCurrentHp");
const playerStatusCreatedAtElement = document.getElementById("playerStatusCreatedAt");
const playerStatusUpdatedAtElement = document.getElementById("playerStatusUpdatedAt");
const fightResultElement = document.getElementById("fightResult");
const currentEnemyStatusElement = document.getElementById("currentEnemyStatus");

const EXP_PER_LEVEL = 10;

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
    playerStatusAttackElement.textContent = "-";
    playerStatusMaxHpElement.textContent = "-";
    playerStatusCurrentHpElement.textContent = "-";
    playerStatusCreatedAtElement.textContent = "-";
    playerStatusUpdatedAtElement.textContent = "-";
    return;
  }

  playerStatusIdElement.textContent = player.id;
  playerStatusNameElement.textContent = player.name;
  playerStatusLevelElement.textContent = player.level;
  playerStatusExperienceElement.textContent = `${player.experience} / ${EXP_PER_LEVEL}`;
  playerStatusGoldElement.textContent = player.gold;
  playerStatusAttackElement.textContent = player.attack;
  playerStatusMaxHpElement.textContent = player.maxHp;
  playerStatusCurrentHpElement.textContent = player.currentHp;
  playerStatusCreatedAtElement.textContent = player.createdAt;
  playerStatusUpdatedAtElement.textContent = player.updatedAt;
}

function showEnemyStatus(fightResult) {
  if (!fightResult || fightResult.isVictory) {
    currentEnemyStatusElement.textContent = "No active enemy.";
    return;
  }
  currentEnemyStatusElement.textContent =
    `${fightResult.enemyName} — HP: ${fightResult.enemyCurrentHp} / ${fightResult.enemyMaxHp} | ATK: ${fightResult.enemyAttack}`;
}

async function loadPlayer() {
  const id = playerIdInput.value;
  const response = await fetch(`/api/players/${encodeURIComponent(id)}`);
  const text = await response.text();

  if (!response.ok) {
    showPlayerStatus(null);
    fightResultElement.textContent = "Load failed.";
    showResult({ error: `Load failed (${response.status})`, detail: text });
    return;
  }

  const player = JSON.parse(text);
  showPlayerStatus(player);
  showResult(player);
}

async function createPlayer() {
  const name = playerNameInput.value.trim();
  const response = await fetch("/api/players", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ name })
  });

  const text = await response.text();
  if (!response.ok) {
    showPlayerStatus(null);
    fightResultElement.textContent = "Create failed.";
    showResult({ error: `Create failed (${response.status})`, detail: text });
    return;
  }

  const player = JSON.parse(text);
  playerIdInput.value = player.id;
  showPlayerStatus(player);
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
    fightResultElement.textContent = "Add gold failed.";
    showResult({ error: `Add gold failed (${response.status})`, detail: text });
    return;
  }

  const player = JSON.parse(text);
  showPlayerStatus(player);
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
    fightResultElement.textContent = "Fight failed.";
    showResult({ error: `Fight failed (${response.status})`, detail: text });
    return;
  }

  const fightResult = JSON.parse(text);
  showPlayerStatus(fightResult.player);
  showEnemyStatus(fightResult);

  let resultText = fightResult.summary;
  if (fightResult.isVictory) {
    resultText += ` | Gold: +${fightResult.goldReward} | EXP: +${fightResult.experienceReward}`;
    if (fightResult.leveledUp) {
      resultText += " | *** LEVEL UP! ***";
    }
  }
  fightResultElement.textContent = resultText;
  showResult(fightResult);
}

async function rest() {
  const id = playerIdInput.value;
  const response = await fetch(`/api/players/${encodeURIComponent(id)}/rest`, {
    method: "POST"
  });

  const text = await response.text();
  if (!response.ok) {
    fightResultElement.textContent = "Rest failed.";
    showResult({ error: `Rest failed (${response.status})`, detail: text });
    return;
  }

  const player = JSON.parse(text);
  showPlayerStatus(player);
  fightResultElement.textContent = "Rested and recovered to full HP.";
  showResult(player);
}

document.getElementById("loadPlayerButton").addEventListener("click", loadPlayer);
document.getElementById("createPlayerButton").addEventListener("click", createPlayer);
document.getElementById("addGoldButton").addEventListener("click", addGold);
document.getElementById("fightButton").addEventListener("click", fight);
document.getElementById("restButton").addEventListener("click", rest);
