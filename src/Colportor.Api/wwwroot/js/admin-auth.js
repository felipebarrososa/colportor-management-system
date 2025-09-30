// admin-auth.js — login do painel admin

const LS_KEY = "colp_token";
const SS_KEY = "token";

function saveTokenEverywhere(t) {
    try { sessionStorage.setItem(SS_KEY, t); } catch { }
    try { localStorage.setItem(LS_KEY, t); } catch { }
}
function getAnyToken() {
    try {
        const a = (sessionStorage.getItem(SS_KEY) || "").trim();
        if (a) return a;
    } catch { }
    try {
        const b = (localStorage.getItem(LS_KEY) || "").trim();
        if (b) return b;
    } catch { }
    return "";
}

async function authApi(path, opts = {}) {
    const headers = { "Content-Type": "application/json", ...(opts.headers || {}) };
    return fetch(path, { ...opts, headers });
}

const form = document.getElementById("loginForm");
const err = document.getElementById("error");

// Já logado? Vai direto pro dashboard
if (getAnyToken()) {
    window.location.replace("/admin/dashboard.html");
}

form.addEventListener("submit", async (e) => {
    e.preventDefault();
    err && (err.hidden = true);

    const email = document.getElementById("email").value.trim();
    const password = document.getElementById("password").value;

    const res = await authApi("/auth/login", {
        method: "POST",
        body: JSON.stringify({ email, password }),
    });

    if (!res.ok) {
        if (err) err.hidden = false;
        return;
    }

    // aceita diferentes formatos de resposta
    let data;
    try { data = await res.json(); } catch { data = {}; }
    const token =
        data.token || data.Token || data.access_token || data.AccessToken || "";

    if (!token || token.split(".").length !== 3) {
        if (err) err.hidden = false;
        console.error("Token ausente ou inválido:", data);
        return;
    }

    saveTokenEverywhere(token);
    window.location.replace("/admin/dashboard.html");
});
