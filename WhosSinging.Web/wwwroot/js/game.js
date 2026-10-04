let connection = null;
let myName = "";
let roomCode = "";
let isHost = false;
let isReferee = false;

let answered = false;
let countdownInterval = null;
let countdownRemaining = 0;
let roundToken = 0;

// ===== Pantallas =====
function showScreen(id) {
    document.querySelectorAll(".pantalla").forEach(p => p.classList.add("d-none"));
    document.getElementById(id).classList.remove("d-none");
}

// ===== Audio =====
function stopAudio() {
    const audio = document.getElementById("audio-juego");
    audio.pause();
    audio.currentTime = 0;
}

// ===== Conexión =====
async function ensureConnection() {
    if (connection) return;

    connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/game")
        .withAutomaticReconnect()
        .build();

    connection.on("RoomCreated", (data) => {
        roomCode = data.roomCode;
        isHost = true;
        isReferee = data.isReferee;
        document.getElementById("codigo-sala").textContent = roomCode;
        showScreen("pantalla-lobby");
    });

    connection.on("JoinedRoom", (data) => {
        roomCode = data.roomCode;
        isHost = data.isHost;
        isReferee = data.isReferee;
        document.getElementById("codigo-sala").textContent = roomCode;
        showScreen("pantalla-lobby");
    });

    connection.on("RoomUpdated", (data) => {
        renderPlayers(data.players);
        document.getElementById("btn-iniciar").classList.toggle("d-none", !isHost);
    });

    connection.on("PlayerLeft", (name) => {
        console.log("Se fue: " + name);
    });

    connection.on("Error", (msg) => {
        alert(msg);
    });

    connection.on("RoundStarted", (data) => startRound(data));

    connection.on("AnswerProgress", (respondieron, total) => {
        const el = document.getElementById("progreso-respuestas");
        el.classList.remove("d-none");
        el.textContent = "Respondieron " + respondieron + " de " + total;
    });

    connection.on("AnswerReceived", () => {
        lockOptions();
        document.getElementById("estado-respuesta").classList.remove("d-none");
    });

    connection.on("RoundResult", (data) => showResults(data));

    connection.on("GameFinished", (data) => showPodium(data));

    connection.on("RematchReady", () => {
        showScreen("pantalla-lobby");
    });

    connection.onreconnected(async () => {
        if (roomCode && myName) {
            try {
                await connection.invoke("JoinRoom", roomCode, myName);
            } catch (e) {
                console.warn("No se pudo reconectar a la sala", e);
            }
        }
    });

    connection.onclose(async () => {
        if (!roomCode || !myName) return;
        try {
            connection = null;
            await ensureConnection();
            await connection.invoke("JoinRoom", roomCode, myName);
        } catch (e) {
            console.warn("Reconexión manual falló", e);
        }
    });

    await connection.start();
    console.log("Conectado a SignalR");
}

// ===== Lobby =====
function renderPlayers(players) {
    const lista = document.getElementById("lista-jugadores");
    lista.innerHTML = "";

    players.forEach(p => {
        const li = document.createElement("li");
        li.className = "list-group-item d-flex justify-content-between align-items-center";
        const estado = p.connected ? "" : " 📴";
        li.innerHTML =
            "<span>" + p.name + (p.isReferee ? " 🎤" : "") + (p.isHost ? " 👑" : "") + estado + "</span>" +
            "<span class='badge bg-secondary'>" + p.score + " pts</span>";
        lista.appendChild(li);
    });
}

// ===== Ronda =====
function startRound(data) {
    answered = false;
    roundToken++;
    const token = roundToken;

    showScreen("pantalla-juego");

    document.getElementById("ronda-titulo").textContent =
        "Ronda " + data.round + " de " + data.totalRounds;

    document.getElementById("contador").textContent = "--";
    document.getElementById("aviso-carga").classList.remove("d-none");
    document.getElementById("estado-respuesta").classList.add("d-none");
    document.getElementById("progreso-respuestas").classList.add("d-none");
    document.getElementById("btn-escuchar").classList.add("d-none");
    document.getElementById("btn-cerrar-ronda").classList.toggle("d-none", !isHost);

    // Opciones: los jugadores ven botones, el presentador ve el panel
    const cont = document.getElementById("lista-opciones");
    cont.innerHTML = "";
    if (isReferee) {
        cont.innerHTML =
            "<p class='text-muted fs-5'>Modo presentador: esperando respuestas de los jugadores...</p>";
    } else {
        data.options.forEach(opt => {
            const col = document.createElement("div");
            col.className = "col-12 col-md-6";
            const btn = document.createElement("button");
            btn.className = "btn btn-outline-primary btn-lg w-100 option-btn";
            btn.textContent = opt;
            btn.addEventListener("click", () => chooseOption(btn, opt));
            col.appendChild(btn);
            cont.appendChild(col);
        });
    }

    clearInterval(countdownInterval);

    const audio = document.getElementById("audio-juego");
    audio.pause();

    const onReady = () => {
        if (token !== roundToken) return;
        audio.removeEventListener("canplay", onReady);
        clearTimeout(fallback);
        beginPlayback(data, token);
    };

    audio.addEventListener("canplay", onReady);
    audio.src = data.audioUrl;
    audio.load();

    const fallback = setTimeout(onReady, 8000);
}

function beginPlayback(data, token) {
    if (token !== roundToken) return;

    document.getElementById("aviso-carga").classList.add("d-none");

    const audio = document.getElementById("audio-juego");
    const playPromise = audio.play();
    if (playPromise !== undefined) {
        playPromise.catch(() => {
            document.getElementById("btn-escuchar").classList.remove("d-none");
        });
    }

    countdownRemaining = data.timeLimitSeconds;
    paintCountdown();
    countdownInterval = setInterval(() => {
        countdownRemaining--;
        paintCountdown();
        if (countdownRemaining <= 0) {
            clearInterval(countdownInterval);
            onTimeUp();
        }
    }, 1000);
}

function paintCountdown() {
    const el = document.getElementById("contador");
    el.textContent = countdownRemaining;
    el.className = "display-1 fw-bold " +
        (countdownRemaining <= 5 ? "text-danger" : "text-warning");
}

async function onTimeUp() {
    if (isReferee) { stopAudio(); return; }
    if (answered) return;
    answered = true;
    lockOptions();
    stopAudio();
    try {
        await connection.invoke("SubmitAnswer", "");
    } catch (e) { console.warn(e); }
}

function lockOptions() {
    document.querySelectorAll(".option-btn").forEach(b => b.disabled = true);
}

async function chooseOption(btn, option) {
    if (answered || isReferee) return;
    answered = true;

    lockOptions();
    btn.classList.remove("btn-outline-primary");
    btn.classList.add("btn-primary");

    try {
        await connection.invoke("SubmitAnswer", option);
    } catch (e) { console.warn(e); }
}

// ===== Resultados =====
function showResults(data) {
    clearInterval(countdownInterval);
    stopAudio();
    showScreen("pantalla-resultados");

    document.getElementById("resultado-titulo").textContent = data.correctTitle;
    document.getElementById("resultado-artista").textContent = data.correctArtist;

    const lista = document.getElementById("lista-resultados");
    lista.innerHTML = "";
    data.results.forEach(r => {
        const li = document.createElement("li");
        li.className = "list-group-item d-flex justify-content-between";
        const icon = r.correct ? "✅" : "❌";
        li.innerHTML =
            "<span>" + icon + " " + r.name + "</span>" +
            "<span>+" + r.pointsEarned + " (total " + r.totalScore + ")</span>";
        lista.appendChild(li);
    });

    document.getElementById("btn-siguiente").classList.toggle("d-none", !isHost);
    document.getElementById("esperando-host").classList.toggle("d-none", isHost);
}

// ===== Podio =====
function showPodium(data) {
    clearInterval(countdownInterval);
    stopAudio();
    showScreen("pantalla-final");

    const medals = ["🥇", "🥈", "🥉"];
    const lista = document.getElementById("lista-podio");
    lista.innerHTML = "";
    data.forEach((p, i) => {
        const li = document.createElement("li");
        li.className = "list-group-item d-flex justify-content-between fs-4";
        const medal = i < 3 ? medals[i] : "•";
        li.innerHTML =
            "<span>" + medal + " " + p.name + "</span>" +
            "<span class='badge bg-primary'>" + p.score + " pts</span>";
        lista.appendChild(li);
    });

    document.getElementById("btn-revancha").classList.toggle("d-none", !isHost);
    document.getElementById("esperando-host-final").classList.toggle("d-none", isHost);
}

// ===== Botones =====
document.getElementById("btn-crear").addEventListener("click", async () => {
    myName = document.getElementById("input-nombre").value.trim();
    if (!myName) { alert("Escribí tu nombre para jugar."); return; }
    const modoPresentador = document.getElementById("chk-arbitro").checked;
    await ensureConnection();
    await connection.invoke("CreateRoom", myName, modoPresentador);
});

document.getElementById("btn-unirse").addEventListener("click", async () => {
    myName = document.getElementById("input-nombre").value.trim();
    roomCode = document.getElementById("input-codigo").value.trim().toUpperCase();
    if (!myName) { alert("Escribí tu nombre para jugar."); return; }
    if (!roomCode) { alert("Escribí el código de la sala."); return; }
    await ensureConnection();
    await connection.invoke("JoinRoom", roomCode, myName);
});

document.getElementById("btn-iniciar").addEventListener("click", async () => {
    try { await connection.invoke("StartGame"); } catch (e) { console.warn(e); }
});

document.getElementById("btn-escuchar").addEventListener("click", () => {
    document.getElementById("audio-juego").play();
    document.getElementById("btn-escuchar").classList.add("d-none");
});

document.getElementById("btn-cerrar-ronda").addEventListener("click", async () => {
    try { await connection.invoke("FinishRoundNow"); } catch (e) { console.warn(e); }
});

document.getElementById("btn-siguiente").addEventListener("click", async () => {
    try { await connection.invoke("NextRound"); } catch (e) { console.warn(e); }
});

document.getElementById("btn-revancha").addEventListener("click", async () => {
    try { await connection.invoke("Rematch"); } catch (e) { console.warn(e); }
});