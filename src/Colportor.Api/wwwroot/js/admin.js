// /js/admin.js
// ================== Auth ==================
const token =
    sessionStorage.getItem("token") || localStorage.getItem("token") || "";

if (!token || token.split(".").length !== 3) {
    window.location.href = "/admin/login.html";
    throw new Error("No auth token");
}

// Role do usuário para controle de UI
function decodeJwt(t) {
    try {
        const p = t.split(".")[1];
        const json = atob(p.replace(/-/g, "+").replace(/_/g, "/"));
        return JSON.parse(json);
    } catch { return {}; }
}
function getRole(token) {
    const p = decodeJwt(token);
    return (
        p.role || p.Role ||
        p["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ||
        p["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role"] ||
        ""
    ).toString();
}
const ROLE = (getRole(token) || "").toLowerCase();
console.log('Detected role:', ROLE);

async function authFetch(path, init = {}) {
    const res = await fetch(path, {
        ...init,
        headers: {
            "Content-Type": "application/json",
            Authorization: "Bearer " + token,
            ...(init.headers || {}),
        },
    });
    if (res.status === 401) {
        sessionStorage.removeItem("token");
        localStorage.removeItem("token");
        window.location.href = "/admin/login.html";
        throw new Error("401");
    }
    return res;
}

// ================== DOM refs ==================
const $ = (s) => document.querySelector(s);

// Topbar / Drawer
const menuBtn = $("#menuBtn");
const drawer = $("#drawer");
const closeDrawerBtn = $("#closeDrawer");
const drawerBackdrop = $("#drawerBackdrop");
const openLeaderBtn = $("#openLeader");
const openLeader_fromDrawer = $("#openLeader_fromDrawer");
const openApprove_fromDrawer = $("#openApprove_fromDrawer");
const openLeaders_fromDrawer = $("#openLeaders_fromDrawer");
const openPacLeader_fromDrawer = $("#openPacLeader_fromDrawer");
const openPacAdmin_fromDrawer = $("#openPacAdmin_fromDrawer");

// Regions modal
const openRegionsBtn = $("#openRegions");
const regionsModal = $("#regionsModal");
const closeRegionsBtn = $("#closeRegions");
const regCountry = $("#regCountry");
const regName = $("#regName");
const addRegionBtn = $("#addRegion");
const regionsList = $("#regionsList");

// Create Colportor modal
const openCreateBtn = $("#openCreate");
const openCreate_fromDrawer = $("#openCreate_fromDrawer");
const createModal = $("#createModal");
const closeCreateBtn = $("#closeCreate");
const createForm = $("#createForm");
const cCountry = $("#cCountry");
const cRegion = $("#cRegion");
const cLeader = $("#cLeader");
const cPhoto = $("#cPhoto");
const cLastVisit = $("#cLastVisit"); // <<< NOVO
const btnCreateColp = $("#btnCreateColp");

// Leader modal
const leaderModal = $("#leaderModal");
const closeLeaderBtn = $("#closeLeader");
const leaderForm = $("#leaderForm");
const lCountry = $("#lCountry");
const lRegion = $("#lRegion");
const btnCreateLeader = $("#btnCreateLeader");

// Approve leaders modal
const approveModal = $("#approveModal");
const closeApproveBtn = $("#closeApprove");
const pendingList = $("#pendingList");

// View leaders modal
const leadersModal = $("#leadersModal");
const closeLeadersBtn = $("#closeLeaders");
const leadersList = $("#leadersList");
const leadersCountry = $("#leadersCountry");
const leadersRegion = $("#leadersRegion");
const leadersEmail = $("#leadersEmail");
const applyLeaderFilters = $("#applyLeaderFilters");
const clearLeaderFilters = $("#clearLeaderFilters");

// PAC modals (Leader/Admin)
const pacLeaderModal = $("#pacLeaderModal");
const closePacLeaderBtn = $("#closePacLeader");
const pacStart = $("#pacStart");
const pacEnd = $("#pacEnd");
const pacColportors = $("#pacColportors");
const pacSearch = $("#pacSearch");
const pacSelectAll = $("#pacSelectAll");
const pacClearAll = $("#pacClearAll");
const pacCounter = $("#pacCounter");
const submitPacLeader = $("#submitPacLeader");

const pacAdminModal = $("#pacAdminModal");
const closePacAdminBtn = $("#closePacAdmin");
const pacAdminCountry = $("#pacAdminCountry");
const pacAdminRegion = $("#pacAdminRegion");
const pacStatus = $("#pacStatus");
const pacFrom = $("#pacFrom");
const pacTo = $("#pacTo");
const applyPacFilters = $("#applyPacFilters");
const clearPacFilters = $("#clearPacFilters");
const pacAdminList = $("#pacAdminList");

// Leader PAC overview elements
const leaderPacPanel = document.getElementById("leaderPacPanel");
const leaderPacList = document.getElementById("leaderPacList");
const leaderPacEmpty = document.getElementById("leaderPacEmpty");
const lPacPend = document.getElementById("lPacPend");
const lPacApr = document.getElementById("lPacApr");
const lPacRej = document.getElementById("lPacRej");

// Esconde/mostra itens conforme role
function hide(el) { try { el?.classList?.add("hidden"); el?.setAttribute?.("hidden", "true"); el?.style && (el.style.display = "none"); } catch { }
}
function show(el) { try { el?.classList?.remove("hidden"); el?.removeAttribute?.("hidden"); el?.style && (el.style.display = ""); } catch { }
}
if (ROLE === "leader") {
    console.log('Setting up leader UI...');
    hide(document.querySelector("#openRegions"));
    hide(document.querySelector("#openLeader"));
    hide(document.querySelector("#openLeader_fromDrawer"));
    hide(document.querySelector("#openLeaders_fromDrawer"));
    hide(document.querySelector("#openApprove_fromDrawer"));
    hide(document.querySelector("#openPacAdmin_fromDrawer"));
    hide(document.querySelector("#openCreate"));
    hide(document.querySelector("#openCreate_fromDrawer"));
    show(document.querySelector("#openPacLeader_fromDrawer"));
    // Exibe painel de resumo PAC para líderes
    if (leaderPacPanel) {
        console.log('Showing PAC panel for leader');
        leaderPacPanel.removeAttribute("hidden");
        leaderPacPanel.style.display = "block";
    } else {
        console.log('PAC panel not found!');
    }
    loadLeaderPacOverview();
} else {
    console.log('Not a leader, hiding PAC panel');
    if (leaderPacPanel) {
        leaderPacPanel.style.display = "none";
    }
}

// KPIs
const kpiTotal = $("#kpiTotal");
const kpiOk = $("#kpiOk");
const kpiWarn = $("#kpiWarn");
const kpiDanger = $("#kpiDanger");

// Filtros
const fCity = $("#fCity");
const fCpf = $("#fCpf");
const fStatus = $("#fStatus");
const applyFilters = $("#applyFilters");
const clearFilters = $("#clearFilters");

// Tabela
const rows = $("#rows");
const empty = $("#empty");

// Logout
$("#logoutBtn")?.addEventListener("click", () => {
    sessionStorage.removeItem("token");
    localStorage.removeItem("token");
    window.location.href = "/admin/login.html";
});

// ================== Drawer ==================
function openDrawer() {
    drawer.classList.add("open");
    drawer.setAttribute("aria-hidden", "false");
    drawerBackdrop.classList.add("show");
    document.body.classList.add("nav-open");
}
function closeDrawer() {
    drawer.classList.remove("open");
    drawer.setAttribute("aria-hidden", "true");
    drawerBackdrop.classList.remove("show");
    document.body.classList.remove("nav-open");
}
menuBtn?.addEventListener("click", (e) => { e.preventDefault?.(); openDrawer(); });
closeDrawerBtn?.addEventListener("click", (e) => { e.preventDefault?.(); closeDrawer(); });
drawerBackdrop?.addEventListener("click", closeDrawer);

// ================== Proteção de modais (backdrop fecha, conteúdo não) ==================
document.querySelectorAll(".modal").forEach((m) => {
    m.addEventListener("click", (e) => { if (e.target === m) m.setAttribute("aria-hidden", "true"); });
    m.querySelector(".modal-dialog")?.addEventListener("click", (e) => e.stopPropagation());
    m.querySelector(".modal-backdrop")?.addEventListener("click", () => m.setAttribute("aria-hidden", "true"));
});

// ================== Regiões (modal) ==================
function openRegions() {
    closeDrawer();
    regionsModal.setAttribute("aria-hidden", "false");
    loadCountriesForModal();
}
function closeRegions() { regionsModal.setAttribute("aria-hidden", "true"); }
openRegionsBtn?.addEventListener("click", (e) => { e.preventDefault?.(); openRegions(); });
closeRegionsBtn?.addEventListener("click", (e) => { e.preventDefault?.(); closeRegions(); });

async function loadCountriesForModal() {
    const res = await fetch("/geo/countries");
    const list = (await res.json()) || [];
    regCountry.innerHTML = list.map((c) => `<option value="${c.id}">${escapeHtml(c.name)}</option>`).join("");
    await refreshRegionList();
}
regCountry?.addEventListener("change", refreshRegionList);

async function refreshRegionList() {
    const countryId = parseInt(regCountry.value || "0", 10);
    if (!countryId) { regionsList.innerHTML = ""; return; }
    const res = await fetch(`/geo/regions?countryId=${countryId}`);
    const list = (await res.json()) || [];
    regionsList.innerHTML = list.length
        ? list.map((r) => `<div class="item"><span>${escapeHtml(r.name)}</span></div>`).join("")
        : `<div class="muted">Nenhuma região cadastrada para este país.</div>`;
}

addRegionBtn?.addEventListener("click", async (e) => {
    e.preventDefault();
    const name = (regName.value || "").trim();
    const countryId = parseInt(regCountry.value || "0", 10);
    if (!countryId || !name) { toast("Informe o país e o nome da região."); return; }
    const res = await authFetch("/admin/regions", { method: "POST", body: JSON.stringify({ countryId, name }) });
    if (!res.ok) { toast("Não foi possível criar a região."); return; }
    regName.value = "";
    await refreshRegionList();
    toast("Região criada!");
});

// ================== Cadastrar Colportor (modal) ==================
function openCreate() {
    closeDrawer();
    createModal.setAttribute("aria-hidden", "false");
    hydrateCreateGeo();
}
function closeCreate() { createModal.setAttribute("aria-hidden", "true"); }
openCreateBtn?.addEventListener("click", (e) => { e.preventDefault?.(); openCreate(); });
openCreate_fromDrawer?.addEventListener("click", (e) => { e.preventDefault?.(); openCreate(); });
closeCreateBtn?.addEventListener("click", (e) => { e.preventDefault?.(); closeCreate(); });

// ================== Cadastrar Líder (modal) ==================
function openLeader() {
    closeDrawer();
    leaderModal.setAttribute("aria-hidden", "false");
    hydrateLeaderGeo();
}
function closeLeader() { leaderModal.setAttribute("aria-hidden", "true"); }
openLeaderBtn?.addEventListener("click", (e) => { e.preventDefault?.(); openLeader(); });
openLeader_fromDrawer?.addEventListener("click", (e) => { e.preventDefault?.(); openLeader(); });
closeLeaderBtn?.addEventListener("click", (e) => { e.preventDefault?.(); closeLeader(); });

async function hydrateCreateGeo() {
    const cRes = await fetch("/geo/countries");
    const cList = (await cRes.json()) || [];
    cCountry.innerHTML = `<option value="">Selecione…</option>` + cList.map(c => `<option value="${c.id}">${escapeHtml(c.name)}</option>`).join("");
    if (!cCountry.value && cCountry.options.length > 1) cCountry.selectedIndex = 1;
    await refreshCreateRegions();
}
cCountry?.addEventListener("change", refreshCreateRegions);
cRegion?.addEventListener("change", refreshCreateLeaders);

async function refreshCreateLeaders() {
    cLeader.innerHTML = `<option value="">Carregando...</option>`;
    const regionId = parseInt(cRegion.value || "0", 10);
    if (!regionId) {
        cLeader.innerHTML = `<option value="">Selecione uma região primeiro...</option>`;
        return;
    }
    try {
        const res = await fetch(`/geo/leaders?regionId=${regionId}`);
        const list = (await res.json()) || [];
        if (!list.length) {
            cLeader.innerHTML = `<option value="">Nenhum líder nesta região</option>`;
        } else {
            cLeader.innerHTML = `<option value="">Opcional - selecione um líder...</option>` + 
                list.map(l => `<option value="${l.id}">${escapeHtml(l.name)}</option>`).join("");
        }
    } catch (err) {
        console.error("Erro ao carregar líderes:", err);
        cLeader.innerHTML = `<option value="">Erro ao carregar líderes</option>`;
    }
}

async function hydrateLeaderGeo() {
    const cRes = await fetch("/geo/countries");
    const cList = (await cRes.json()) || [];
    lCountry.innerHTML = `<option value="">Selecione…</option>` + cList.map(c => `<option value="${c.id}">${escapeHtml(c.name)}</option>`).join("");
    if (!lCountry.value && lCountry.options.length > 1) lCountry.selectedIndex = 1;
    await refreshLeaderRegions();
}
lCountry?.addEventListener("change", refreshLeaderRegions);
async function refreshLeaderRegions() {
    lRegion.innerHTML = `<option value="">Selecione…</option>`;
    const id = parseInt(lCountry.value || "0", 10);
    if (!id) return;
    const rRes = await fetch(`/geo/regions?countryId=${id}`);
    const rList = (await rRes.json()) || [];
    lRegion.innerHTML = `<option value="">Selecione…</option>` + rList.map(r => `<option value="${r.id}">${escapeHtml(r.name)}</option>`).join("");
}

leaderForm?.addEventListener("submit", async (e) => {
    e.preventDefault();
    const btn = btnCreateLeader;
    setBusy(btn, true);
    try {
        const payload = {
            email: document.querySelector("#lEmail").value.trim(),
            password: document.querySelector("#lPass").value,
            regionId: lRegion.value ? parseInt(lRegion.value, 10) : null,
        };
        if (!payload.email || !payload.password || !payload.regionId) {
            toast("Preencha e-mail, senha e região."); setBusy(btn, false); return;
        }
        const res = await authFetch("/admin/leaders", { method: "POST", body: JSON.stringify(payload) });
        if (!res.ok) { toast("Não foi possível criar o líder (e-mail em uso?)."); return; }
        leaderForm.reset();
        closeLeader();
        toast("Líder criado!");
    } finally { setBusy(btn, false); }
});

// ================== Aprovar Líderes ==================
function openApprove() {
    closeDrawer();
    approveModal.setAttribute("aria-hidden", "false");
    loadPendingLeaders();
}
function closeApprove() { approveModal.setAttribute("aria-hidden", "true"); }
openApprove_fromDrawer?.addEventListener("click", (e) => { e.preventDefault?.(); openApprove(); });
closeApproveBtn?.addEventListener("click", (e) => { e.preventDefault?.(); closeApprove(); });

async function loadPendingLeaders() {
    const res = await authFetch("/admin/leaders/pending");
    if (!res.ok) { pendingList.innerHTML = `<div class="muted">Falha ao carregar.</div>`; return; }
    const list = await res.json();
    if (!list.length) { pendingList.innerHTML = `<div class="muted">Nenhum líder pendente.</div>`; return; }
    pendingList.innerHTML = list.map(x => `
        <div class="item">
            <div>
                <strong>${escapeHtml(x.email)}</strong>
                <div class="muted">Região: ${escapeHtml(x.region || `#${x.regionId}`)}</div>
            </div>
            <div>
                <button class="btn primary" data-approve="${x.id}">Aprovar</button>
            </div>
        </div>
    `).join("");
}
pendingList?.addEventListener("click", async (e) => {
    const btn = e.target.closest("[data-approve]");
    if (!btn) return;
    const id = parseInt(btn.getAttribute("data-approve"), 10);
    btn.disabled = true;
    try {
        const res = await authFetch(`/admin/leaders/${id}/approve`, { method: "POST" });
        if (!res.ok) { toast("Falha ao aprovar."); return; }
        await loadPendingLeaders();
        toast("Líder aprovado!");
    } finally { btn.disabled = false; }
});

// ================== Ver Líderes ==================
function openLeaders() {
    closeDrawer();
    leadersModal.setAttribute("aria-hidden", "false");
    hydrateLeadersGeo().then(loadLeaders);
}
function closeLeaders() { leadersModal.setAttribute("aria-hidden", "true"); }
openLeaders_fromDrawer?.addEventListener("click", (e) => { e.preventDefault?.(); openLeaders(); });
closeLeadersBtn?.addEventListener("click", (e) => { e.preventDefault?.(); closeLeaders(); });

async function loadLeaders() {
    const qs = new URLSearchParams();
    const rid = leadersRegion?.value ? parseInt(leadersRegion.value, 10) : 0;
    if (rid) qs.set("regionId", String(rid));
    if (leadersEmail?.value.trim()) qs.set("email", leadersEmail.value.trim());
    const res = await authFetch(`/admin/leaders?${qs.toString()}`);
    if (!res.ok) { leadersList.innerHTML = `<div class="muted">Falha ao carregar.</div>`; return; }
    const list = await res.json();
    if (!list.length) { leadersList.innerHTML = `<div class="muted">Nenhum líder cadastrado.</div>`; return; }
    leadersList.innerHTML = list.map(x => `
        <div class="item">
            <div>
                <strong>${escapeHtml(x.email)}</strong>
                <div class="muted">Região: ${escapeHtml(x.region || `#${x.regionId}`)}</div>
            </div>
            <div>
                <button class="btn" data-edit="${x.id}">Editar</button>
                <button class="btn danger" data-del="${x.id}">Excluir</button>
            </div>
        </div>
    `).join("");
}

async function hydrateLeadersGeo() {
    const cRes = await fetch("/geo/countries");
    const cList = (await cRes.json()) || [];
    leadersCountry.innerHTML = `<option value="">Todos</option>` + cList.map(c => `<option value="${c.id}">${escapeHtml(c.name)}</option>`).join("");
    leadersRegion.innerHTML = `<option value="">Todas</option>`;
    if (leadersCountry.options.length > 1) leadersCountry.selectedIndex = 1;
    await refreshLeadersRegions();
}
leadersCountry?.addEventListener("change", refreshLeadersRegions);
async function refreshLeadersRegions() {
    leadersRegion.innerHTML = `<option value="">Todas</option>`;
    const id = parseInt(leadersCountry.value || "0", 10);
    if (!id) return;
    const rRes = await fetch(`/geo/regions?countryId=${id}`);
    const rList = (await rRes.json()) || [];
    leadersRegion.innerHTML = `<option value="">Todas</option>` + rList.map(r => `<option value="${r.id}">${escapeHtml(r.name)}</option>`).join("");
}
applyLeaderFilters?.addEventListener("click", loadLeaders);
clearLeaderFilters?.addEventListener("click", () => { leadersCountry.selectedIndex = 0; leadersRegion.innerHTML = `<option value="">Todas</option>`; leadersEmail.value = ""; loadLeaders(); });

leadersList?.addEventListener("click", async (e) => {
    const delBtn = e.target.closest("[data-del]");
    if (delBtn) {
        const id = parseInt(delBtn.getAttribute("data-del"), 10);
        if (!confirm("Excluir este líder?")) return;
        const res = await authFetch(`/admin/leaders/${id}`, { method: "DELETE" });
        if (!res.ok) { toast("Falha ao excluir."); return; }
        await loadLeaders();
        toast("Líder excluído.");
        return;
    }
    const editBtn = e.target.closest("[data-edit]");
    if (editBtn) {
        const id = parseInt(editBtn.getAttribute("data-edit"), 10);
        const email = prompt("Novo e-mail (deixe em branco para manter):") || undefined;
        const password = prompt("Nova senha (deixe em branco para manter):") || "";
        const regionId = leadersRegion?.value ? parseInt(leadersRegion.value, 10) : 0;
        if (!regionId) { toast("Selecione a região alvo nos filtros para editar."); return; }
        const body = { email: email ?? undefined, password, regionId };
        const res = await authFetch(`/admin/leaders/${id}`, { method: "PUT", body: JSON.stringify(body) });
        if (!res.ok) { toast("Falha ao atualizar."); return; }
        await loadLeaders();
        toast("Líder atualizado.");
    }
});

async function refreshCreateRegions() {
    cRegion.innerHTML = `<option value="">Selecione…</option>`;
    const id = parseInt(cCountry.value || "0", 10);
    if (!id) return;
    const rRes = await fetch(`/geo/regions?countryId=${id}`);
    const rList = (await rRes.json()) || [];
    cRegion.innerHTML = `<option value="">Selecione…</option>` + rList.map(r => `<option value="${r.id}">${escapeHtml(r.name)}</option>`).join("");
}

createForm?.addEventListener("submit", async (e) => {
    e.preventDefault();
    const btn = btnCreateColp;
    
    // Validações
    const fullName = $("#cFullName").value.trim();
    const cpf = $("#cCpf").value.trim();
    const email = $("#cEmail").value.trim();
    const password = $("#cPass").value;
    
    if (!fullName) {
        alert("❌ O campo 'Nome Completo' é obrigatório!");
        $("#cFullName").focus();
        return;
    }
    if (!cpf) {
        alert("❌ O campo 'CPF' é obrigatório!");
        $("#cCpf").focus();
        return;
    }
    if (!email) {
        alert("❌ O campo 'E-mail' é obrigatório!");
        $("#cEmail").focus();
        return;
    }
    if (!password || password.length < 6) {
        alert("❌ A senha deve ter pelo menos 6 caracteres!");
        $("#cPass").focus();
        return;
    }
    
    setBusy(btn, true);

    // Foto (opcional)
    let photoUrl = null;
    const file = cPhoto?.files && cPhoto.files[0];
    if (file) {
        try {
            const fd = new FormData(); fd.append("photo", file);
            const u = await fetch("/upload/photo", { method: "POST", body: fd });
            if (u.ok) { const j = await u.json(); photoUrl = j.url || null; }
        } catch { /* sem foto */ }
    }

    // Payload do colportor
    const payload = {
        fullName,
        cpf,
        city: $("#cCity").value.trim() || null,
        photoUrl,
        email,
        password,
        countryId: cCountry.value ? parseInt(cCountry.value, 10) : null,
        regionId: cRegion.value ? parseInt(cRegion.value, 10) : null,
        leaderId: cLeader.value ? parseInt(cLeader.value, 10) : null,
    };

    const lastVisitStr = (cLastVisit?.value || "").trim(); // yyyy-mm-dd
    const lastVisitIso = lastVisitStr ? toUtcMidnightIso(lastVisitStr) : null;

    if (!payload.fullName || !payload.cpf || !payload.email || !payload.password) {
        toast("Preencha: Nome, CPF, E-mail e Senha."); setBusy(btn, false); return;
    }
    if (!payload.regionId) { toast("Selecione a região."); setBusy(btn, false); return; }

    try {
        // 1) Cria o colportor
        const res = await authFetch("/admin/colportors", { method: "POST", body: JSON.stringify(payload) });
        if (!res.ok) {
            const t = await res.text().catch(() => "");
            console.error("Create error:", t);
            toast("Erro ao criar (CPF duplicado ou dados inválidos).");
            return;
        }

        // Tenta obter o id retornado (preferível)
        let created = null;
        try { created = await res.json(); } catch { created = null; }
        let newId = created?.id;

        // Fallback: busca pelo CPF caso a API não retorne corpo
        if (!newId) {
            const find = await authFetch(`/admin/colportors?cpf=${encodeURIComponent(payload.cpf)}`);
            if (find.ok) {
                const list = await find.json();
                newId = list?.[0]?.id || null;
            }
        }

        // 2) Se foi informada uma última visita, cria um registro na tabela de visitas
        if (newId && lastVisitIso) {
            const v = await authFetch("/admin/visits", {
                method: "POST",
                body: JSON.stringify({ colportorId: newId, date: lastVisitIso }),
            });
            if (!v.ok) console.warn("Falha ao criar visita inicial:", await v.text().catch(() => ""));
        }

        createForm.reset();
        closeCreate();
        await loadColportors();
        toast("Colportor criado!");
    } finally {
        setBusy(btn, false);
    }
});

// ================== Filtros / Tabela ==================
applyFilters?.addEventListener("click", loadColportors);
clearFilters?.addEventListener("click", () => { fCity.value = ""; fCpf.value = ""; fStatus.value = ""; loadColportors(); });
[fCity, fCpf, fStatus].forEach((el) => el?.addEventListener("keydown", (e) => { if (e.key === "Enter") { e.preventDefault(); loadColportors(); } }));

// Delegação para ações por linha (check-in)
rows?.addEventListener("click", async (e) => {
    const btn = e.target.closest("[data-action]");
    if (!btn) return;

    const id = parseInt(btn.dataset.id, 10);
    if (!id) return;

    if (btn.dataset.action === "checkin") {
        // Envia "meia-noite UTC" do dia atual (evita Kind=Unspecified no backend)
        const today = new Date();
        const y = today.getUTCFullYear();
        const m = String(today.getUTCMonth() + 1).padStart(2, "0");
        const d = String(today.getUTCDate()).padStart(2, "0");
        const iso = `${y}-${m}-${d}T00:00:00.000Z`;

        setBusy(btn, true);
        try {
            const res = await authFetch("/admin/visits", { method: "POST", body: JSON.stringify({ colportorId: id, date: iso }) });
            if (!res.ok) { toast("Falha ao registrar check-in."); return; }
            await loadColportors();
            toast("Check-in registrado!");
        } finally { setBusy(btn, false); }
    }
});

async function loadColportors() {
    const qs = new URLSearchParams();
    if (fCity.value.trim()) qs.set("city", fCity.value.trim());
    if (fCpf.value.trim()) qs.set("cpf", fCpf.value.trim());
    if (fStatus.value) qs.set("status", fStatus.value);

    const res = await authFetch(`/admin/colportors?${qs.toString()}`);
    if (!res.ok) {
        rows.innerHTML = "";
        empty.hidden = false;
        kpiTotal.textContent = "0"; kpiOk.textContent = "0"; kpiWarn.textContent = "0"; kpiDanger.textContent = "0";
        return;
    }
    const list = (await res.json()) || [];

    // KPIs
    const total = list.length;
    const ok = list.filter((x) => (x.status || "").toUpperCase() === "EM DIA").length;
    const warn = list.filter((x) => (x.status || "").toUpperCase() === "AVISO").length;
    const danger = list.filter((x) => (x.status || "").toUpperCase() === "VENCIDO").length;
    kpiTotal.textContent = total; kpiOk.textContent = ok; kpiWarn.textContent = warn; kpiDanger.textContent = danger;

    if (!total) { rows.innerHTML = ""; empty.hidden = false; return; }
    empty.hidden = true;

    // Desktop table
    rows.innerHTML = list.map((x) => {
        const last = x.lastVisitDate ? new Date(x.lastVisitDate) : null;
        const status = (x.status || "—").toUpperCase();
        const place = [x.region, x.country].filter(Boolean).join(" / ") || "—";
        return `
<tr>
  <td>${x.id}</td>
  <td>${x.photoUrl ? `<img class="avatar" src="${escapeAttr(x.photoUrl)}" alt="foto">` : "—"}</td>
  <td>${escapeHtml(x.fullName || "—")}</td>
  <td>${escapeHtml(x.cpf || "—")}</td>
  <td>${escapeHtml(place)}</td>
  <td>${escapeHtml(x.city || "—")}</td>
  <td>${last ? last.toLocaleDateString("pt-BR") : "—"}</td>
  <td><span class="pill ${status}">${status}</span></td>
  <td>
    <div class="row-actions">
      <button class="btn sm ghost primary-ghost" data-action="checkin" data-id="${x.id}" title="Registrar check-in agora">Check-in</button>
    </div>
  </td>
</tr>`;
    }).join("");

    // Mobile cards
    const mobileCards = document.getElementById('mobileCards');
    if (mobileCards) {
        mobileCards.innerHTML = list.map((x) => {
            const last = x.lastVisitDate ? new Date(x.lastVisitDate) : null;
            const status = (x.status || "—").toUpperCase();
            const place = [x.region, x.country].filter(Boolean).join(" / ") || "—";
            return `
<div class="mobile-card">
  <div class="mobile-card-header">
    <div>
      <div class="mobile-card-id">#${x.id}</div>
      <div class="mobile-card-name">${escapeHtml(x.fullName || "—")}</div>
      <div class="mobile-card-cpf">${escapeHtml(x.cpf || "—")}</div>
    </div>
  </div>
  <div class="mobile-card-details">
    <div class="mobile-card-detail">
      <strong>Região</strong>
      ${escapeHtml(place)}
    </div>
    <div class="mobile-card-detail">
      <strong>Cidade</strong>
      ${escapeHtml(x.city || "—")}
    </div>
    <div class="mobile-card-detail">
      <strong>Últ. Visita</strong>
      ${last ? last.toLocaleDateString("pt-BR") : "—"}
    </div>
  </div>
  <div class="mobile-card-footer">
    <div class="mobile-card-status">
      <span class="pill ${status}">${status}</span>
    </div>
    <div class="mobile-card-actions">
      <button class="btn sm ghost primary-ghost" data-action="checkin" data-id="${x.id}" title="Registrar check-in agora">Check-in</button>
    </div>
  </div>
</div>`;
        }).join("");
    }
}

// ================== Utils ==================
function toast(msg) {
    const t = document.createElement("div");
    t.className = "toast show";
    t.textContent = msg;
    document.body.appendChild(t);
    setTimeout(() => {
        t.classList.remove("show");
        t.addEventListener("transitionend", () => t.remove(), { once: true });
    }, 2200);
}
function setBusy(btn, isBusy) {
    if (!btn) return;
    const sp = btn.querySelector(".spinner");
    btn.disabled = !!isBusy;
    if (sp) sp.hidden = !isBusy;
}
function escapeHtml(s) {
    return String(s ?? "").replaceAll("&", "&amp;").replaceAll("<", "&lt;").replaceAll(">", "&gt;").replaceAll('"', "&quot;").replaceAll("'", "&#39;");
}
function escapeAttr(s) { return escapeHtml(s).replaceAll("`", "&#96;"); }
function toUtcMidnightIso(yyyyMmDd) {
    // "2025-09-29" -> "2025-09-29T00:00:00.000Z"
    const [y, m, d] = yyyyMmDd.split("-").map((n) => parseInt(n, 10));
    const dt = new Date(Date.UTC(y, (m - 1), d, 0, 0, 0));
    return dt.toISOString();
}

// ================== Boot ==================
loadColportors();

async function loadLeaderPacOverview() {
    try {
        console.log('Loading leader PAC overview...');
        const res = await authFetch('/leader/pac/enrollments');
        console.log('PAC response:', res.status);
        if (!res.ok) { 
            console.log('PAC fetch failed:', res.status);
            if (leaderPacPanel) leaderPacPanel.hidden = true; 
            return; 
        }
        const list = await res.json();
        console.log('PAC list:', list);
        if (!list || !list.length) {
            if (leaderPacList) leaderPacList.innerHTML = '';
            if (leaderPacEmpty) leaderPacEmpty.hidden = false;
            if (lPacPend) lPacPend.textContent = '0'; 
            if (lPacApr) lPacApr.textContent = '0'; 
            if (lPacRej) lPacRej.textContent = '0';
            return;
        }
        if (leaderPacEmpty) leaderPacEmpty.hidden = true;
        const pend = list.filter(x => x.status === 'Pending').length;
        const apr = list.filter(x => x.status === 'Approved').length;
        const rej = list.filter(x => x.status === 'Rejected').length;
        if (lPacPend) lPacPend.textContent = pend; 
        if (lPacApr) lPacApr.textContent = apr; 
        if (lPacRej) lPacRej.textContent = rej;
        if (leaderPacList) {
            leaderPacList.innerHTML = list.slice(0, 10).map(x => {
                const range = `${new Date(x.startDate).toLocaleDateString('pt-BR')} - ${new Date(x.endDate).toLocaleDateString('pt-BR')}`;
                const cls = `pill ${x.status}`;
                return `<div class="item"><div><strong>${escapeHtml(x.colportor.fullName)}</strong> — ${escapeHtml(x.colportor.cpf)}<div class="muted">${range}</div></div><div><span class="${cls}">${escapeHtml(x.status)}</span></div></div>`;
            }).join('');
        }
    } catch (err) { 
        console.error('Error loading PAC overview:', err);
    }
}

// ================== PAC Leader ==================
function openPacLeader() {
    closeDrawer();
    pacLeaderModal.setAttribute("aria-hidden", "false");
    loadLeaderColportors();
}
function closePacLeader() { pacLeaderModal.setAttribute("aria-hidden", "true"); }
openPacLeader_fromDrawer?.addEventListener("click", (e) => { e.preventDefault?.(); openPacLeader(); });
closePacLeaderBtn?.addEventListener("click", (e) => { e.preventDefault?.(); closePacLeader(); });

async function loadLeaderColportors() {
    const res = await authFetch(`/admin/colportors`);
    if (!res.ok) { pacColportors.innerHTML = `<div class="muted">Falha ao carregar.</div>`; return; }
    const list = await res.json();
    if (!list.length) { pacColportors.innerHTML = `<div class="muted">Nenhum colportor na sua região.</div>`; return; }
    pacColportors.innerHTML = list.map(x => `
        <label class="pac-checkbox-item">
            <input type="checkbox" value="${x.id}" class="pac-checkbox">
            <div class="pac-checkbox-label">
                <strong>${escapeHtml(x.fullName)}</strong>
                <span class="muted">${escapeHtml(x.cpf)}</span>
            </div>
            <span class="pac-checkbox-check"></span>
        </label>
    `).join("");
    updatePacCounter();
}

function updatePacCounter(){
    const n = pacColportors.querySelectorAll('input[type=checkbox]:checked').length;
    if (pacCounter) pacCounter.textContent = `${n} selecionados`;
}
pacColportors?.addEventListener('change', updatePacCounter);
pacSearch?.addEventListener('input', () => {
    const q = (pacSearch.value || '').toLowerCase();
    pacColportors.querySelectorAll('.item').forEach(el => {
        const txt = el.textContent.toLowerCase();
        el.style.display = txt.includes(q) ? '' : 'none';
    });
});
pacSelectAll?.addEventListener('click', () => {
    pacColportors.querySelectorAll('input[type=checkbox]').forEach(i => i.checked = true);
    updatePacCounter();
});
pacClearAll?.addEventListener('click', () => {
    pacColportors.querySelectorAll('input[type=checkbox]').forEach(i => i.checked = false);
    updatePacCounter();
});

submitPacLeader?.addEventListener("click", async () => {
    const ids = Array.from(pacColportors.querySelectorAll("input[type=checkbox]:checked")).map(i => parseInt(i.value, 10));
    
    if (!ids.length) {
        alert("❌ Selecione pelo menos um colportor para o PAC!");
        return;
    }
    
    if (!pacStart.value) {
        alert("❌ Informe a data de início do período!");
        pacStart.focus();
        return;
    }
    
    if (!pacEnd.value) {
        alert("❌ Informe a data de fim do período!");
        pacEnd.focus();
        return;
    }
    
    const startDate = new Date(pacStart.value);
    const endDate = new Date(pacEnd.value);
    
    if (endDate < startDate) {
        alert("❌ A data de fim deve ser posterior à data de início!");
        pacEnd.focus();
        return;
    }
    
    const body = {
        colportorIds: ids,
        startDate: new Date(pacStart.value + "T00:00:00Z").toISOString(),
        endDate: new Date(pacEnd.value + "T00:00:00Z").toISOString(),
    };
    const res = await authFetch(`/leader/pac/enrollments`, { method: "POST", body: JSON.stringify(body) });
    if (!res.ok) { 
        alert("❌ Falha ao enviar. Tente novamente.");
        return;
    }
    alert("✅ Solicitação enviada para aprovação do PAC!");
    closePacLeader();
    loadLeaderPacOverview(); // Atualiza o painel
});

// ================== PAC Admin ==================
function openPacAdmin() {
    closeDrawer();
    pacAdminModal.setAttribute("aria-hidden", "false");
    hydratePacAdminGeo().then(loadPacAdmin);
}
function closePacAdmin() { pacAdminModal.setAttribute("aria-hidden", "true"); }
openPacAdmin_fromDrawer?.addEventListener("click", (e) => { e.preventDefault?.(); openPacAdmin(); });
closePacAdminBtn?.addEventListener("click", (e) => { e.preventDefault?.(); closePacAdmin(); });

async function hydratePacAdminGeo() {
    const cRes = await fetch("/geo/countries");
    const cList = (await cRes.json()) || [];
    pacAdminCountry.innerHTML = `<option value="">Todos</option>` + cList.map(c => `<option value="${c.id}">${escapeHtml(c.name)}</option>`).join("");
    pacAdminRegion.innerHTML = `<option value="">Todas</option>`;
    if (pacAdminCountry.options.length > 1) pacAdminCountry.selectedIndex = 1;
    await refreshPacAdminRegions();
}
pacAdminCountry?.addEventListener("change", refreshPacAdminRegions);
async function refreshPacAdminRegions() {
    pacAdminRegion.innerHTML = `<option value="">Todas</option>`;
    const id = parseInt(pacAdminCountry.value || "0", 10);
    if (!id) return;
    const rRes = await fetch(`/geo/regions?countryId=${id}`);
    const rList = (await rRes.json()) || [];
    pacAdminRegion.innerHTML = `<option value="">Todas</option>` + rList.map(r => `<option value="${r.id}">${escapeHtml(r.name)}</option>`).join("");
}

applyPacFilters?.addEventListener("click", loadPacAdmin);
clearPacFilters?.addEventListener("click", () => { pacAdminCountry.selectedIndex = 0; pacAdminRegion.innerHTML = `<option value="">Todas</option>`; pacStatus.value = ""; pacFrom.value = ""; pacTo.value = ""; loadPacAdmin(); });

async function loadPacAdmin() {
    const qs = new URLSearchParams();
    const rid = pacAdminRegion?.value ? parseInt(pacAdminRegion.value, 10) : 0;
    if (rid) qs.set("regionId", String(rid));
    if (pacStatus?.value) qs.set("status", pacStatus.value);
    if (pacFrom?.value) qs.set("from", new Date(pacFrom.value + "T00:00:00Z").toISOString());
    if (pacTo?.value) qs.set("to", new Date(pacTo.value + "T00:00:00Z").toISOString());
    const res = await authFetch(`/admin/pac/enrollments?${qs.toString()}`);
    if (!res.ok) { pacAdminList.innerHTML = `<div class=\"muted\">Falha ao carregar.</div>`; return; }
    const list = await res.json();
    if (!list.length) { pacAdminList.innerHTML = `<div class=\"muted\">Nenhuma solicitação.</div>`; document.getElementById('kpiPend').textContent='0'; document.getElementById('kpiApr').textContent='0'; document.getElementById('kpiRej').textContent='0'; return; }
    // KPIs
    const pend = list.filter(x => (x.status||'') === 'Pending').length;
    const apr = list.filter(x => (x.status||'') === 'Approved').length;
    const rej = list.filter(x => (x.status||'') === 'Rejected').length;
    document.getElementById('kpiPend').textContent = pend;
    document.getElementById('kpiApr').textContent = apr;
    document.getElementById('kpiRej').textContent = rej;
    pacAdminList.innerHTML = list.map(x => `
        <div class=\"item\">
            <div>
                <strong>${escapeHtml(x.colportor.fullName)}</strong> — ${escapeHtml(x.colportor.cpf)}
                <div class=\"muted\">${new Date(x.startDate).toLocaleDateString("pt-BR")} a ${new Date(x.endDate).toLocaleDateString("pt-BR")} • ${escapeHtml(x.status)} • Líder: ${escapeHtml(x.leader)}</div>
            </div>
            <div>
                ${x.status === 'Pending' ? `<button class=\"btn\" data-approve=\"${x.id}\">Aprovar</button> <button class=\"btn danger\" data-reject=\"${x.id}\">Rejeitar</button>` : ''}
            </div>
        </div>
    `).join("");
}

pacAdminList?.addEventListener("click", async (e) => {
    const a = e.target.closest("[data-approve]");
    if (a) {
        const id = parseInt(a.getAttribute("data-approve"), 10);
        const res = await authFetch(`/admin/pac/enrollments/${id}/approve`, { method: "POST" });
        if (!res.ok) { toast("Falha ao aprovar."); return; }
        await loadPacAdmin();
        toast("Aprovado."); return;
    }
    const r = e.target.closest("[data-reject]");
    if (r) {
        const id = parseInt(r.getAttribute("data-reject"), 10);
        const res = await authFetch(`/admin/pac/enrollments/${id}/reject`, { method: "POST" });
        if (!res.ok) { toast("Falha ao rejeitar."); return; }
        await loadPacAdmin();
        toast("Rejeitado.");
    }
});
