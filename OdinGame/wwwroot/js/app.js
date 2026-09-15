const connection = new signalR.HubConnectionBuilder().withUrl("/gameHub").build();

let myName = "";
let currentRoomCode = "";
let selectedCards = [];
let myHand = [];
let currentPlayerName = "";

function showScreen(id){
  document.querySelectorAll('.screen').forEach(s => s.classList.add('hidden'));
  document.getElementById(id).classList.remove('hidden');
}

// ---------- Card rendering ----------
function cardEl(card, extraClass){
  const color = (card.Color || card.color).toLowerCase();
  const number = card.Number ?? card.number;

  const el = document.createElement('div');
  el.className = `card ${color}${extraClass ? ' ' + extraClass : ''}`;

  const img = document.createElement('img');
  img.className = 'card-art';
  img.src = `images/cards/${color}/${number}.png`;
  img.alt = `${color} ${number}`;
  img.onerror = () => { img.remove(); };
  el.appendChild(img);

  const corner = document.createElement('div');
  corner.className = `card-corner ${color}`;
  corner.innerText = number;
  el.appendChild(corner);

  return el;
}

function cardBackEl(mini){
  const el = document.createElement('div');
  el.className = 'card-back' + (mini ? ' mini' : '');

  const img = document.createElement('img');
  img.src = 'images/card-back.png';
  img.alt = 'card back';
  img.onerror = () => el.classList.add('no-back-image');
  el.appendChild(img);

  const fallback = document.createElement('div');
  fallback.className = 'back-fallback';
  fallback.innerText = 'ODIN';
  el.appendChild(fallback);

  return el;
}

// ---------- Landing ----------
document.getElementById('createBtn').addEventListener('click', () => {
  myName = document.getElementById('playerName').value.trim();
  if(!myName){ setLandingError('نام را وارد کنید'); return; }
  connection.invoke('CreateRoom', myName).catch(console.error);
});

document.getElementById('joinBtn').addEventListener('click', () => {
  myName = document.getElementById('playerName').value.trim();
  const code = document.getElementById('roomCodeInput').value.trim().toUpperCase();
  if(!myName || !code){ setLandingError('نام و کد روم را وارد کنید'); return; }
  connection.invoke('JoinRoom', code, myName).catch(console.error);
});

function setLandingError(msg){
  document.getElementById('landingError').innerText = msg;
}

// ---------- Lobby ----------
document.getElementById('copyCodeBtn').addEventListener('click', () => {
  navigator.clipboard.writeText(currentRoomCode);
  const btn = document.getElementById('copyCodeBtn');
  const old = btn.innerText;
  btn.innerText = 'کپی شد';
  setTimeout(() => btn.innerText = old, 1200);
});

document.getElementById('readyBtn').addEventListener('click', () => {
  const btn = document.getElementById('readyBtn');
  const isReady = btn.dataset.ready !== 'true';
  btn.dataset.ready = isReady;
  btn.innerText = isReady ? 'لغو آماده‌باش' : 'آماده‌ام';
  connection.invoke('SetReady', isReady).catch(console.error);
});

function renderLobbyPlayers(players){
  const container = document.getElementById('lobbyPlayerList');
  container.innerHTML = '';
  players.forEach(p => {
    const name = typeof p === 'string' ? p : p.Name;
    const ready = typeof p === 'string' ? false : p.IsReady;
    const row = document.createElement('div');
    row.className = 'player-row';
    row.innerHTML = `
      <span class="name">${name}</span>
      <span class="status-chip ${ready ? 'ready' : ''}">
        <span class="ready-dot ${ready ? 'on' : ''}"></span>${ready ? 'آماده' : 'در انتظار'}
      </span>`;
    container.appendChild(row);
  });
}

// ---------- Game rendering ----------
function renderGame(data){
  myHand = Array.from(data.YourHand);
  currentPlayerName = data.CurrentPlayerName;
  selectedCards = [];

  const oppRow = document.getElementById('opponentsRow');
  oppRow.innerHTML = '';
  data.AllPlayers.filter(p => p.Name !== myName).forEach(p => {
    const div = document.createElement('div');
    div.className = 'opponent' + (p.Name === currentPlayerName ? ' active-turn' : '');
    div.appendChild(cardBackEl(true));
    const info = document.createElement('div');
    info.innerHTML = `<div class="oname">${p.Name}</div><div class="ocount">${p.CardCount} کارت</div>`;
    div.appendChild(info);
    oppRow.appendChild(div);
  });

  const banner = document.getElementById('turnBanner');
  banner.innerHTML = currentPlayerName === myName
    ? `<span class="me">نوبت شماست</span>`
    : `نوبت ${currentPlayerName}`;

  const center = document.getElementById('centerCards');
  const valueBadge = document.getElementById('centerValue');
  center.innerHTML = '';
  const combo = data.CurrentCombination ? Array.from(data.CurrentCombination) : [];
  if(combo.length){
    combo.forEach(c => center.appendChild(cardEl(c)));
    valueBadge.innerText = `مقدار: ${data.CurrentValue}`;
  } else {
    center.innerHTML = '<div class="center-empty">هنوز کارتی بازی نشده</div>';
    valueBadge.innerText = '';
  }

  renderHand();
  updateActionBar();
}

function renderHand(){
  const fan = document.getElementById('handFan');
  fan.innerHTML = '';
  const n = myHand.length;
  myHand.forEach((c, i) => {
    const el = cardEl(c, 'hand-card');
    const angle = (i - (n - 1) / 2) * 4;
    el.style.transform = `rotate(${angle}deg)`;
    if(isSelected(c)) el.classList.add('selected');
    el.addEventListener('click', () => toggleCard(c, el));
    fan.appendChild(el);
  });
}

function isSelected(c){
  return selectedCards.some(s => s.Color === (c.Color||c.color) && s.Number === (c.Number??c.number));
}

function toggleCard(c, el){
  const color = c.Color || c.color, number = c.Number ?? c.number;
  const idx = selectedCards.findIndex(s => s.Color === color && s.Number === number);
  if(idx >= 0){
    selectedCards.splice(idx, 1);
    el.classList.remove('selected');
  } else {
    selectedCards.push({ Color: color, Number: number });
    el.classList.add('selected');
  }
  updateActionBar();
}

function updateActionBar(){
  const isMyTurn = currentPlayerName === myName;
  document.getElementById('playBtn').disabled = !isMyTurn || selectedCards.length === 0;
  document.getElementById('passBtn').disabled = !isMyTurn;
  document.querySelector('.action-bar').classList.toggle('my-turn', isMyTurn);
}

document.getElementById('playBtn').addEventListener('click', () => {
  if(selectedCards.length === 0) return;
  connection.invoke('PlayCards', selectedCards).catch(console.error);
});

document.getElementById('passBtn').addEventListener('click', () => {
  connection.invoke('Pass').catch(console.error);
});

// ---------- Server events ----------
connection.on('RoomCreated', code => enterLobby(code));
connection.on('RoomJoined', code => enterLobby(code));
connection.on('JoinFailed', msg => setLandingError(msg));

function enterLobby(code){
  currentRoomCode = code;
  document.getElementById('lobbyRoomCode').innerText = code;
  showScreen('lobbyScreen');
}

connection.on('PlayerListUpdated', players => renderLobbyPlayers(players));
connection.on('PlayerReadyUpdated', players => renderLobbyPlayers(players));

connection.on('GameStarted', data => {
  showScreen('gameScreen');
  renderGame(data);
});

connection.on('StateUpdated', data => renderGame(data));

connection.on('ActionFailed', msg => {
  const el = document.getElementById('actionError');
  el.innerText = msg;
  setTimeout(() => el.innerText = '', 2500);
});

connection.on('ChooseCardToTake', cards => {
  const container = document.getElementById('choiceCards');
  container.innerHTML = '';
  Array.from(cards).forEach(c => {
    const el = cardEl(c);
    el.style.cursor = 'pointer';
    el.addEventListener('click', () => {
      connection.invoke('ChooseCard', { Color: c.Color, Number: c.Number }).catch(console.error);
      document.getElementById('choiceOverlay').classList.add('hidden');
    });
    container.appendChild(el);
  });
  document.getElementById('choiceOverlay').classList.remove('hidden');
});

connection.on('NewRoundStarted', data => {
  document.getElementById('roundOverlay').classList.add('hidden');
  renderGame(data);
});

connection.on('GameOver', data => {
  document.getElementById('winnerTitle').innerText = `برنده: ${data.WinnerName}`;
  const scoresDiv = document.getElementById('finalScores');
  scoresDiv.innerHTML = '';
  Array.from(data.Scores).sort((a,b)=>a.TotalScore-b.TotalScore).forEach(s => {
    const row = document.createElement('div');
    row.className = 'score-row';
    row.innerHTML = `<span>${s.Name}</span><span>${s.TotalScore} امتیاز</span>`;
    scoresDiv.appendChild(row);
  });
  document.getElementById('gameOverOverlay').classList.remove('hidden');
});

connection.start().catch(console.error);