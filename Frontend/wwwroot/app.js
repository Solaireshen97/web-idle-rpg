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
  const statusText = fightResult.isVictory ? "Yes" : "No";
  const enemyText = `Enemy: ${fightResult.enemyName} (HP ${fightResult.enemyMaxHp}, ATK ${fightResult.enemyAttack})`;
  const rewardText = `Gold: +${fightResult.goldReward} | EXP: +${fightResult.experienceReward}`;
  const levelText = fightResult.leveledUp ? ` | LEVEL UP! Lv${fightResult.player.level}` : "";
  const resultText = `Victory: ${statusText} | ${rewardText}${levelText} | HP: ${fightResult.player.currentHp}/${fightResult.player.maxHp}`;
  fightResultElement.textContent = `${enemyText} | ${resultText} | ${fightResult.summary}`;
  showResult(fightResult);
}

document.getElementById("loadPlayerButton").addEventListener("click", loadPlayer);
document.getElementById("createPlayerButton").addEventListener("click", createPlayer);
document.getElementById("addGoldButton").addEventListener("click", addGold);
document.getElementById("fightButton").addEventListener("click", fight);
