const resultElement = document.getElementById("result");
const playerIdInput = document.getElementById("playerId");
const playerNameInput = document.getElementById("playerName");
const playerStatusIdElement = document.getElementById("playerStatusId");
const playerStatusNameElement = document.getElementById("playerStatusName");
const playerStatusGoldElement = document.getElementById("playerStatusGold");
const playerStatusLevelElement = document.getElementById("playerStatusLevel");
const playerStatusExperienceElement = document.getElementById("playerStatusExperience");
const playerStatusAttackElement = document.getElementById("playerStatusAttack");
const playerStatusMaxHpElement = document.getElementById("playerStatusMaxHp");
const playerStatusCurrentHpElement = document.getElementById("playerStatusCurrentHp");
const playerStatusCreatedAtElement = document.getElementById("playerStatusCreatedAt");
const playerStatusUpdatedAtElement = document.getElementById("playerStatusUpdatedAt");
const currentEnemyNameElement = document.getElementById("currentEnemyName");
const currentEnemyHpElement = document.getElementById("currentEnemyHp");
const currentEnemyAttackElement = document.getElementById("currentEnemyAttack");
const fightResultElement = document.getElementById("fightResult");

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
  playerStatusExperienceElement.textContent = player.experience;
  playerStatusGoldElement.textContent = player.gold;
  playerStatusAttackElement.textContent = player.attack;
  playerStatusMaxHpElement.textContent = player.maxHp;
  playerStatusCurrentHpElement.textContent = player.currentHp;
  playerStatusCreatedAtElement.textContent = player.createdAt;
  playerStatusUpdatedAtElement.textContent = player.updatedAt;
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
    return;
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
  showResult(fightResult);
}

async function rest() {
  const id = playerIdInput.value;
  const response = await fetch(`/api/players/${encodeURIComponent(id)}/rest`, {
    method: "POST"
  });

  const text = await response.text();
  if (!response.ok) {
    showPlayerStatus(null);
    showCurrentEnemy(null);
    fightResultElement.textContent = "Rest failed.";
    showResult({ error: `Rest failed (${response.status})`, detail: text });
    return;
  }

  const player = JSON.parse(text);
  showPlayerStatus(player);
  showCurrentEnemy(player);
  fightResultElement.textContent = `${player.name} rested and recovered to full HP (${player.currentHp}/${player.maxHp}).`;
  showResult(player);
}

document.getElementById("loadPlayerButton").addEventListener("click", loadPlayer);
document.getElementById("createPlayerButton").addEventListener("click", createPlayer);
document.getElementById("addGoldButton").addEventListener("click", addGold);
document.getElementById("fightButton").addEventListener("click", fight);
document.getElementById("restButton").addEventListener("click", rest);
