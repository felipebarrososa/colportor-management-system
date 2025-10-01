// ===== helpers =====
const $ = (q) => document.querySelector(q);
const api = (path, opts = {}) =>
    fetch(path, {
        ...opts,
        headers: {
            "Content-Type": "application/json",
            ...(localStorage.getItem("token")
                ? { Authorization: "Bearer " + localStorage.getItem("token") }
                : {}),
            ...(opts.headers || {}),
        },
    });

function showScreen(selector) {
    document.querySelectorAll(".screen").forEach((s) => s.classList.remove("active"));
    document.querySelector(selector).classList.add("active");
}
function toast(msg, ms = 2600) {
    const t = $("#toast");
    if (!t) { alert(msg); return; }
    t.textContent = msg;
    t.classList.add("show");
    setTimeout(() => t.classList.remove("show"), ms);
}
function parseJwt(token) {
    try {
        const base = token.split(".")[1];
        const json = atob(base.replace(/-/g, "+").replace(/_/g, "/"));
        return JSON.parse(decodeURIComponent(escape(json)));
    } catch { return {}; }
}
function esc(s) {
    return String(s ?? "")
        .replaceAll("&", "&amp;").replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;").replaceAll('"', "&quot;")
        .replaceAll("'", "&#39;");
}
function toUtcMidnightIso(yyyyMmDd) {
    const [y, m, d] = yyyyMmDd.split("-").map(n => parseInt(n, 10));
    const dt = new Date(Date.UTC(y, m - 1, d, 0, 0, 0));
    return dt.toISOString();
}

// ===== Geo (carrega País/Região em <select>) =====
async function loadCountries(selectEl) {
    if (!selectEl) return;
    selectEl.innerHTML = `<option value="">Selecione…</option>`;
    const res = await fetch("/geo/countries");
    if (!res.ok) return;
    const data = await res.json();
    selectEl.innerHTML =
        `<option value="">Selecione…</option>` +
        data.map(c => `<option value="${c.id}">${esc(c.name)}</option>`).join("");
}
async function loadRegions(countryId, selectEl) {
    if (!selectEl) return;
    selectEl.innerHTML = `<option value="">Selecione…</option>`;
    if (!countryId) return;
    const res = await fetch(`/geo/regions?countryId=${encodeURIComponent(countryId)}`);
    if (!res.ok) return;
    const data = await res.json();
    selectEl.innerHTML =
        `<option value="">Selecione…</option>` +
        data.map(r => `<option value="${r.id}">${esc(r.name)}</option>`).join("");
}

// ===== Upload foto =====
async function uploadPhoto(file) {
    const fd = new FormData();
    fd.append("photo", file);
    const res = await fetch("/upload/photo", { method: "POST", body: fd });
    if (!res.ok) throw new Error(await res.text().catch(() => "Falha no upload"));
    const j = await res.json().catch(() => ({}));
    return j.url || null; // backend devolve { url: "/uploads/..." }
}

// ===== modal register =====
const modal = $("#registerModal");
const rCountrySel = $("#rCountry");
const rRegionSel = $("#rRegion");
const rLeaderSel = $("#rLeader");
const rPhotoFile = $("#rPhotoFile");
const rPhotoUrlH = $("#rPhotoUrl");
const rLastVisit = $("#rLastVisit");

async function hydrateRegisterGeo() {
    await loadCountries(rCountrySel);
    if (rCountrySel && !rCountrySel.value && rCountrySel.options.length > 1) {
        rCountrySel.selectedIndex = 1;
    }
    await loadRegions(rCountrySel?.value, rRegionSel);
}

$("#btnOpenRegister")?.addEventListener("click", async () => {
    modal.classList.add("show");
    await hydrateRegisterGeo();
    $("#rFullName")?.focus();
});
$("#btnCloseRegister")?.addEventListener("click", () => modal.classList.remove("show"));
modal?.addEventListener("click", (e) => { if (e.target === modal) modal.classList.remove("show"); });
rCountrySel?.addEventListener("change", () => loadRegions(rCountrySel.value, rRegionSel));
rRegionSel?.addEventListener("change", async () => {
    const regionId = parseInt(rRegionSel.value || "0", 10);
    if (!regionId) {
        rLeaderSel.innerHTML = `<option value="">Selecione uma região primeiro...</option>`;
        return;
    }
    try {
        rLeaderSel.innerHTML = `<option value="">Carregando...</option>`;
        const res = await fetch(`/geo/leaders?regionId=${regionId}`);
        const list = (await res.json()) || [];
        if (!list.length) {
            rLeaderSel.innerHTML = `<option value="">Nenhum líder nesta região</option>`;
        } else {
            rLeaderSel.innerHTML = `<option value="">Opcional - selecione seu líder...</option>` + 
                list.map(l => `<option value="${l.id}">${escapeHtml(l.name)}</option>`).join("");
        }
    } catch (err) {
        console.error("Erro ao carregar líderes:", err);
        rLeaderSel.innerHTML = `<option value="">Erro ao carregar líderes</option>`;
    }
});

// ===== login =====
$("#loginForm")?.addEventListener("submit", async (e) => {
    e.preventDefault();
    const btn = $("#btnLogin");
    const spinner = btn.querySelector(".spinner");
    $("#loginError").hidden = true;
    btn.disabled = true;
    if (spinner) spinner.hidden = false;

    try {
        const email = $("#loginEmail").value.trim();
        const password = $("#loginPassword").value;
        const res = await api("/auth/login", { method: "POST", body: JSON.stringify({ email, password }) });
        if (!res.ok) { $("#loginError").hidden = false; return; }

        const j = await res.json();
        const token = j.token || j.Token || j.access_token || j.AccessToken || j.jwt || j.JWT;
        if (!token) { $("#loginError").hidden = false; return; }

        const claims = parseJwt(token);
        const role = (claims.role || claims.Role || "").toString().toLowerCase();
        if (role.includes("admin") || role.includes("leader")) {
            toast("Essa conta é Admin/Líder. Acesse /admin.");
            return;
        }

        localStorage.setItem("token", token);
        const ok = await renderWallet();
        if (ok) toast("Login realizado!");
        else $("#loginError").hidden = false;
    } catch (err) {
        console.error(err);
        $("#loginError").hidden = false;
    } finally {
        btn.disabled = false;
        if (spinner) spinner.hidden = true;
    }
});

// ===== register =====
$("#registerForm")?.addEventListener("submit", async (e) => {
    e.preventDefault();
    const btn = $("#btnRegister");
    const spinner = btn.querySelector(".spinner");
    $("#registerError").hidden = true;
    btn.disabled = true;
    if (spinner) spinner.hidden = false;

    // guarda data escolhida (string yyyy-mm-dd)
    const lastVisitStr = (rLastVisit?.value || "").trim();

    try {
        // Foto: file > hidden URL > nada
        let photoUrl = null;
        if (rPhotoFile?.files && rPhotoFile.files[0]) {
            try { photoUrl = await uploadPhoto(rPhotoFile.files[0]); }
            catch { toast("Falha ao enviar foto."); }
        } else if (rPhotoUrlH?.value) {
            photoUrl = rPhotoUrlH.value.trim() || null;
        }

        const countryId = rCountrySel?.value ? parseInt(rCountrySel.value, 10) : null;
        const regionId = rRegionSel?.value ? parseInt(rRegionSel.value, 10) : null;
        const leaderId = rLeaderSel?.value ? parseInt(rLeaderSel.value, 10) : null;

        const body = {
            fullName: $("#rFullName").value.trim(),
            cpf: $("#rCPF").value.trim(),
            city: $("#rCity").value.trim() || null,
            photoUrl,
            email: $("#rEmail").value.trim(),
            password: $("#rPass").value,
            countryId,
            regionId,
            leaderId,
            // envia a última visita já no cadastro, se informada
            lastVisitDate: lastVisitStr ? toUtcMidnightIso(lastVisitStr) : null,
        };

        console.log("DEBUG: Registration payload:", body);

        if (!body.fullName || !body.cpf || !body.email || !body.password) {
            $("#registerError").hidden = false; return;
        }
        if (!body.regionId) { toast("Selecione a região."); btn.disabled = false; if (spinner) spinner.hidden = true; return; }

        // 1) Cria conta
        const res = await api("/auth/register", { method: "POST", body: JSON.stringify(body) });
        if (!res.ok) { $("#registerError").hidden = false; return; }

        // 2) Login automático
        const resLogin = await api("/auth/login", {
            method: "POST",
            body: JSON.stringify({ email: body.email, password: body.password }),
        });

        modal.classList.remove("show");

        if (resLogin.ok) {
            const j = await resLogin.json();
            const token = j.token || j.Token || j.access_token || j.AccessToken || j.jwt || j.JWT;
            if (token) {
                localStorage.setItem("token", token);

                // 3) Última visita já foi enviada no cadastro; não precisa criar aqui

                // 4) Atualiza carteira
                const ok = await renderWallet();
                if (ok) toast("Conta criada e login efetuado!");
                return;
            }
        }

        // fallback: só preenche o e-mail e pede login manual
        $("#loginEmail").value = body.email;
        toast("Conta criada. Faça o login.");
    } catch (err) {
        console.error(err);
        $("#registerError").hidden = false;
    } finally {
        btn.disabled = false;
        if (spinner) spinner.hidden = true;
    }
});

// Cria visita com o próprio token do colportor
async function createVisitForSelf(isoDate) {
    try {
        // Tenta rota de usuário (se existir no seu back-end)
        let res = await api("/wallet/visits", {
            method: "POST",
            body: JSON.stringify({ date: isoDate }),
        });

        if (!res.ok) {
            // Fallback: usa seu próprio id e tenta na rota de admin (se o back-end permitir)
            const meRes = await api("/wallet/me");
            if (meRes.ok) {
                const me = await meRes.json();
                res = await api("/admin/visits", {
                    method: "POST",
                    body: JSON.stringify({ colportorId: me.id, date: isoDate }),
                });
            }
        }
    } catch (e) {
        console.warn("Não foi possível criar a visita inicial:", e);
    }
}

// ===== render carteira =====
async function renderWallet() {
    try {
        const me = await api("/wallet/me");
        if (!me.ok) {
            localStorage.removeItem("token");
            showScreen("#authScreen");
            return false;
        }
        const x = await me.json();

        $("#photo").src = x.photoUrl || "/css/user.png";
        $("#name").textContent = x.fullName ?? "—";
        $("#cpf").textContent = x.cpf ?? "—";
        $("#place").textContent = [x.city, x.region, x.country].filter(Boolean).join(" / ") || "—";
        $("#leader").textContent = x.leader ?? "—";
        $("#lastVisit").textContent = x.lastVisitDate ? new Date(x.lastVisitDate).toLocaleDateString("pt-BR") : "—";
        $("#due").textContent = x.dueDate ? new Date(x.dueDate).toLocaleDateString("pt-BR") : "—";

        const pill = $("#status");
        pill.textContent = x.status ?? "—";
        pill.className = "pill " + (x.status || "");

        showScreen("#walletScreen");
        return true;
    } catch (err) {
        console.error(err);
        localStorage.removeItem("token");
        showScreen("#authScreen");
        return false;
    }
}

$("#btnLogout")?.addEventListener("click", () => {
    localStorage.removeItem("token");
    showScreen("#authScreen");
    toast("Você saiu.");
});

// boot
(async () => {
    const token = localStorage.getItem("token");
    if (!token) { showScreen("#authScreen"); return; }
    const role = (parseJwt(token).role || "").toString().toLowerCase();
    if (role.includes("admin") || role.includes("leader")) {
        localStorage.removeItem("token");
        showScreen("#authScreen");
        return;
    }
    await renderWallet();
})();

function escapeHtml(s) {
    return String(s ?? "").replaceAll("&", "&amp;").replaceAll("<", "&lt;").replaceAll(">", "&gt;").replaceAll('"', "&quot;").replaceAll("'", "&#39;");
}
