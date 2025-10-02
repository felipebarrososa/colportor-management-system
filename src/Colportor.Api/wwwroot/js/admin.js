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
const pacAdminLeader = $("#pacAdminLeader");
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
        leaderPacPanel.style.display = "block";
        leaderPacPanel.removeAttribute("hidden");
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
    <div class="mobile-card-avatar">
      ${x.photoUrl ? `<img src="${escapeHtml(x.photoUrl)}" alt="Foto" style="width: 100%; height: 100%; object-fit: cover; border-radius: 8px;">` : '👤'}
    </div>
    <div class="mobile-card-info">
      <div class="mobile-card-name">${escapeHtml(x.fullName || "—")}</div>
      <div class="mobile-card-cpf">${escapeHtml(x.cpf || "—")}</div>
    </div>
    <div class="mobile-card-status">
      <span class="pill ${status}">${status}</span>
    </div>
  </div>
  <div class="mobile-card-details">
    <div class="mobile-card-detail">
      <div class="mobile-card-detail-label">Região</div>
      <div class="mobile-card-detail-value">${escapeHtml(place)}</div>
    </div>
    <div class="mobile-card-detail">
      <div class="mobile-card-detail-label">Cidade</div>
      <div class="mobile-card-detail-value">${escapeHtml(x.city || "—")}</div>
    </div>
    <div class="mobile-card-detail">
      <div class="mobile-card-detail-label">Últ. Visita</div>
      <div class="mobile-card-detail-value">${last ? last.toLocaleDateString("pt-BR") : "—"}</div>
    </div>
    <div class="mobile-card-detail">
      <div class="mobile-card-detail-label">ID</div>
      <div class="mobile-card-detail-value">#${x.id}</div>
    </div>
  </div>
  <div class="mobile-card-actions">
    <button class="btn sm ghost primary-ghost" data-action="checkin" data-id="${x.id}" title="Registrar check-in agora">Check-in</button>
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
        console.log('Leader PAC panel element:', leaderPacPanel);
        const res = await authFetch('/leader/pac/enrollments');
        console.log('PAC response:', res.status);
        if (!res.ok) { 
            console.log('PAC fetch failed:', res.status);
            if (leaderPacPanel) leaderPacPanel.style.display = "none"; 
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
    pacColportors.innerHTML = list.map(x => {
        const isDisabled = x.pacStatus && x.pacStatus !== "Nenhum";
        const statusClass = isDisabled ? `pac-status-${x.pacStatus.toLowerCase()}` : '';
        const statusText = isDisabled ? `Status: ${x.pacStatus}` : '';
        
        return `
        <div class="pac-checkbox-item ${isDisabled ? 'disabled' : ''} ${statusClass}">
            <input type="checkbox" value="${x.id}" class="pac-checkbox" id="pac-${x.id}" ${isDisabled ? 'disabled' : ''}>
            <label for="pac-${x.id}" class="pac-checkbox-label">
                <strong>${escapeHtml(x.fullName)}</strong>
                <span class="muted">${escapeHtml(x.cpf)}</span>
                ${statusText ? `<span class="pac-status-text">${statusText}</span>` : ''}
            </label>
            <span class="pac-checkbox-check"></span>
        </div>
    `;
    }).join("");
    
    // Adicionar event listeners para sincronizar checkbox real com visual
    pacColportors.querySelectorAll('.pac-checkbox').forEach(checkbox => {
        checkbox.addEventListener('change', function() {
            const checkVisual = this.parentElement.querySelector('.pac-checkbox-check');
            if (this.checked) {
                checkVisual.style.background = 'linear-gradient(135deg, #14b86a 0%, #10a55c 100%)';
                checkVisual.style.borderColor = '#14b86a';
                checkVisual.innerHTML = '✓';
            } else {
                checkVisual.style.background = 'rgba(255,255,255,0.05)';
                checkVisual.style.borderColor = 'rgba(255,255,255,0.3)';
                checkVisual.innerHTML = '';
            }
            updatePacCounter();
        });
    });
    
    // Adicionar event listener para clicar no checkbox visual
    pacColportors.querySelectorAll('.pac-checkbox-check').forEach(checkVisual => {
        checkVisual.addEventListener('click', function() {
            const checkbox = this.parentElement.querySelector('.pac-checkbox');
            checkbox.checked = !checkbox.checked;
            checkbox.dispatchEvent(new Event('change'));
        });
    });
    
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
    pacColportors.querySelectorAll('input[type=checkbox]').forEach(checkbox => {
        checkbox.checked = true;
        const checkVisual = checkbox.parentElement.querySelector('.pac-checkbox-check');
        checkVisual.style.background = 'linear-gradient(135deg, #14b86a 0%, #10a55c 100%)';
        checkVisual.style.borderColor = '#14b86a';
        checkVisual.innerHTML = '✓';
    });
    updatePacCounter();
});
pacClearAll?.addEventListener('click', () => {
    pacColportors.querySelectorAll('input[type=checkbox]').forEach(checkbox => {
        checkbox.checked = false;
        const checkVisual = checkbox.parentElement.querySelector('.pac-checkbox-check');
        checkVisual.style.background = 'rgba(255,255,255,0.05)';
        checkVisual.style.borderColor = 'rgba(255,255,255,0.3)';
        checkVisual.innerHTML = '';
    });
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
function closePacAdmin() { 
    pacAdminModal.setAttribute("aria-hidden", "true");
    unlockBodyScroll();
}
openPacAdmin_fromDrawer?.addEventListener("click", (e) => { e.preventDefault?.(); openPacAdmin(); });
closePacAdminBtn?.addEventListener("click", (e) => { e.preventDefault?.(); closePacAdmin(); });

// Fechar modal ao clicar fora dele
pacAdminModal?.addEventListener("click", (e) => {
    if (e.target === pacAdminModal) {
        closePacAdmin();
    }
});

async function hydratePacAdminGeo() {
    const cRes = await fetch("/geo/countries");
    const cList = (await cRes.json()) || [];
    pacAdminCountry.innerHTML = `<option value="">Todos</option>` + cList.map(c => `<option value="${c.id}">${escapeHtml(c.name)}</option>`).join("");
    pacAdminRegion.innerHTML = `<option value="">Todas</option>`;
    if (pacAdminCountry.options.length > 1) pacAdminCountry.selectedIndex = 1;
    await refreshPacAdminRegions();
    
    // Carregar líderes
    const lRes = await authFetch("/admin/leaders");
    const lList = (await lRes.json()) || [];
    pacAdminLeader.innerHTML = `<option value="">Todos</option>` + lList.map(l => `<option value="${l.id}">${escapeHtml(l.fullName || l.email)}</option>`).join("");
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
clearPacFilters?.addEventListener("click", () => { pacAdminCountry.selectedIndex = 0; pacAdminRegion.innerHTML = `<option value="">Todas</option>`; pacAdminLeader.value = ""; pacStatus.value = ""; pacFrom.value = ""; pacTo.value = ""; loadPacAdmin(); });

async function loadPacAdmin() {
    const qs = new URLSearchParams();
    const rid = pacAdminRegion?.value ? parseInt(pacAdminRegion.value, 10) : 0;
    if (rid) qs.set("regionId", String(rid));
    if (pacStatus?.value) qs.set("status", pacStatus.value);
    if (pacFrom?.value) qs.set("from", new Date(pacFrom.value + "T00:00:00Z").toISOString());
    if (pacTo?.value) qs.set("to", new Date(pacTo.value + "T00:00:00Z").toISOString());
    if (pacAdminLeader?.value) qs.set("leaderId", pacAdminLeader.value);
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
    // Usar a função renderPacAdminList para consistência
    renderPacAdminList(list);
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

// ================== PAC DASHBOARD (Admin) ==================
// Elementos do dashboard PAC
const pacDashboardPanel = $("#pacDashboardPanel");
const pacTotalPending = $("#pacTotalPending");
const pacTotalApproved = $("#pacTotalApproved");
const pacTotalRejected = $("#pacTotalRejected");
const pacTotalRequests = $("#pacTotalRequests");
const pacPeriodsGrid = $("#pacPeriodsGrid");
const pacPeriodsEmpty = $("#pacPeriodsEmpty");
const pacRecentList = $("#pacRecentList");
const refreshPacDashboard = $("#refreshPacDashboard");
const openPacAdmin_fromDashboard = $("#openPacAdmin_fromDashboard");
const viewAllPacRequests = $("#viewAllPacRequests");

// Mostrar dashboard PAC apenas para admin
if (ROLE === "admin") {
    console.log('Setting up PAC dashboard for admin...');
    if (pacDashboardPanel) {
        pacDashboardPanel.style.display = "block";
    }
    loadPacDashboard();
} else {
    console.log('Not an admin, hiding PAC dashboard');
    if (pacDashboardPanel) {
        pacDashboardPanel.style.display = "none";
    }
}

// Funções para controlar scroll do body
function lockBodyScroll() {
    document.body.style.overflow = 'hidden';
    document.body.style.position = 'fixed';
    document.body.style.width = '100%';
}

function unlockBodyScroll() {
    document.body.style.overflow = '';
    document.body.style.position = '';
    document.body.style.width = '';
}

// Event listeners do dashboard PAC
refreshPacDashboard?.addEventListener("click", loadPacDashboard);
openPacAdmin_fromDashboard?.addEventListener("click", (e) => {
    e.preventDefault();
    openPacAdmin();
    lockBodyScroll();
});
viewAllPacRequests?.addEventListener("click", (e) => {
    e.preventDefault();
    openPacAdmin();
    lockBodyScroll();
});

// Event listener para clicar nos grupos de líder
pacRecentList?.addEventListener("click", (e) => {
    const leaderGroup = e.target.closest(".leader-group");
    if (leaderGroup) {
        const leaderId = leaderGroup.getAttribute("data-leader-id");
        const startDate = leaderGroup.getAttribute("data-start-date");
        const endDate = leaderGroup.getAttribute("data-end-date");
        
        // Abrir modal de gerenciamento PAC com filtros
        openPacAdmin();
        pacAdminModal.classList.add("show");
        lockBodyScroll();
        
        // Carregar dados específicos da solicitação
        loadPacAdminSpecific(leaderId, startDate, endDate);
    }
});

// Carregar dados específicos de uma solicitação PAC
async function loadPacAdminSpecific(leaderId, startDate, endDate) {
    try {
        console.log('Loading specific PAC data for leader:', leaderId, 'period:', startDate, 'to', endDate);
        
        // Definir filtros visuais
        if (pacFrom) pacFrom.value = startDate;
        if (pacTo) pacTo.value = endDate;
        if (pacAdminLeader) {
            const leaderOption = Array.from(pacAdminLeader.options).find(opt => 
                opt.value === leaderId
            );
            if (leaderOption) {
                pacAdminLeader.value = leaderId;
            }
        }
        
        // Carregar dados específicos usando filtros normais
        const qs = new URLSearchParams();
        qs.set("leaderId", leaderId);
        qs.set("from", startDate);
        qs.set("to", endDate);
        
        const res = await authFetch(`/admin/pac/enrollments?${qs.toString()}`);
        if (!res.ok) {
            console.error('Failed to load specific PAC data:', res.status);
            return;
        }
        
        const specificData = await res.json();
        console.log('Specific PAC data loaded:', specificData);
        
        // Atualizar KPIs com dados específicos
        const pending = specificData.filter(r => r.status === 'Pending').length;
        const approved = specificData.filter(r => r.status === 'Approved').length;
        const rejected = specificData.filter(r => r.status === 'Rejected').length;
        const total = specificData.length;
        
        // Atualizar KPIs no modal
        if (pacAdminPending) pacAdminPending.textContent = pending;
        if (pacAdminApproved) pacAdminApproved.textContent = approved;
        if (pacAdminRejected) pacAdminRejected.textContent = rejected;
        if (pacAdminTotal) pacAdminTotal.textContent = total;
        
        // Renderizar lista específica
        renderPacAdminList(specificData);
        
    } catch (error) {
        console.error('Error loading specific PAC data:', error);
    }
}

// Renderizar lista de solicitações PAC no modal
function renderPacAdminList(requests) {
    if (!pacAdminList) return;
    
    if (!requests || !requests.length) {
        pacAdminList.innerHTML = '<div class="muted">Nenhuma solicitação encontrada</div>';
        return;
    }
    
    pacAdminList.innerHTML = requests.map(request => {
        const startDate = new Date(request.startDate).toLocaleDateString('pt-BR');
        const endDate = new Date(request.endDate).toLocaleDateString('pt-BR');
        const statusClass = `pill ${request.status}`;
        
        const actionButtons = request.status === 'Pending' 
            ? `<div class="actions">
                <button class="btn" data-approve="${request.id}">Aprovar</button>
                <button class="btn danger" data-reject="${request.id}">Rejeitar</button>
               </div>`
            : `<div class="actions">
                <span class="muted">${request.status === 'Approved' ? 'Aprovado' : 'Rejeitado'}</span>
               </div>`;
        
        return `
            <div class="item">
                <div>
                    <strong>${escapeHtml(request.colportor.fullName)}</strong>
                    <div class="muted">
                        ${startDate} - ${endDate}
                    </div>
                    <div class="muted">
                        Líder: ${escapeHtml(request.leader)}
                    </div>
                </div>
                <div>
                    <span class="${statusClass}">${escapeHtml(request.status)}</span>
                    ${actionButtons}
                </div>
            </div>
        `;
    }).join('');
}

// Carregar dados do dashboard PAC
async function loadPacDashboard() {
    try {
        console.log('Loading PAC dashboard...');
        
        // Carregar todas as solicitações PAC
        const res = await authFetch('/admin/pac/enrollments');
        if (!res.ok) {
            console.error('Failed to load PAC data:', res.status);
            return;
        }
        
        const allRequests = await res.json();
        console.log('PAC requests loaded:', allRequests.length);
        
        // Atualizar KPIs gerais
        updatePacKPIs(allRequests);
        
        // Atualizar visualização por períodos
        updatePacPeriods(allRequests);
        
        // Atualizar lista recente
        updatePacRecent(allRequests);
        
    } catch (err) {
        console.error('Error loading PAC dashboard:', err);
    }
}

// Atualizar KPIs do dashboard
function updatePacKPIs(requests) {
    const pending = requests.filter(r => r.status === 'Pending').length;
    const approved = requests.filter(r => r.status === 'Approved').length;
    const rejected = requests.filter(r => r.status === 'Rejected').length;
    const total = requests.length;
    
    if (pacTotalPending) pacTotalPending.textContent = pending;
    if (pacTotalApproved) pacTotalApproved.textContent = approved;
    if (pacTotalRejected) pacTotalRejected.textContent = rejected;
    if (pacTotalRequests) pacTotalRequests.textContent = total;
}

// Atualizar visualização por períodos
function updatePacPeriods(requests) {
    if (!pacPeriodsGrid || !pacPeriodsEmpty) return;
    
    if (!requests.length) {
        pacPeriodsGrid.innerHTML = '';
        pacPeriodsEmpty.style.display = 'block';
        return;
    }
    
    pacPeriodsEmpty.style.display = 'none';
    
    // Agrupar por período (semana)
    const periods = groupRequestsByPeriod(requests);
    
    pacPeriodsGrid.innerHTML = periods.map(period => {
        const pending = period.requests.filter(r => r.status === 'Pending').length;
        const approved = period.requests.filter(r => r.status === 'Approved').length;
        const rejected = period.requests.filter(r => r.status === 'Rejected').length;
        
        return `
            <div class="period-card">
                <div class="period-header">
                    <h4 class="period-title">${period.title}</h4>
                    <span class="period-date">${period.dateRange}</span>
                </div>
                <div class="period-stats">
                    <div class="period-stat">
                        <span class="period-stat-value">${pending}</span>
                        <span class="period-stat-label">Pendentes</span>
                    </div>
                    <div class="period-stat">
                        <span class="period-stat-value">${approved}</span>
                        <span class="period-stat-label">Aprovados</span>
                    </div>
                    <div class="period-stat">
                        <span class="period-stat-value">${rejected}</span>
                        <span class="period-stat-label">Rejeitados</span>
                    </div>
                </div>
                <div class="period-actions">
                    <button class="btn sm" onclick="filterByPeriod('${period.startDate}', '${period.endDate}')">
                        Ver Detalhes
                    </button>
                </div>
            </div>
        `;
    }).join('');
}

// Atualizar lista recente
function updatePacRecent(requests) {
    if (!pacRecentList) return;
    
    if (!requests.length) {
        pacRecentList.innerHTML = '<div class="muted">Nenhuma solicitação encontrada</div>';
        return;
    }
    
    // Agrupar por líder e período
    const leaderGroups = groupRequestsByLeader(requests);
    
    // Pegar os 5 grupos mais recentes
    const recentGroups = leaderGroups
        .sort((a, b) => new Date(b.latestDate) - new Date(a.latestDate))
        .slice(0, 5);
    
    pacRecentList.innerHTML = recentGroups.map(group => {
        const startDate = new Date(group.startDate).toLocaleDateString('pt-BR');
        const endDate = new Date(group.endDate).toLocaleDateString('pt-BR');
        const statusClass = `pill ${group.status}`;
        
        return `
            <div class="recent-item leader-group" data-leader-id="${group.leaderId}" data-start-date="${group.startDate}" data-end-date="${group.endDate}">
                <div class="recent-info">
                    <div class="recent-name">👤 ${escapeHtml(group.leaderName)}</div>
                    <div class="recent-details">
                        <span>📅 ${startDate} - ${endDate}</span>
                        <span>🌍 ${escapeHtml(group.regionName || 'Região não informada')}</span>
                        <span>👥 ${group.colportorCount} colportor(es)</span>
                    </div>
                </div>
                <div class="recent-status">
                    <span class="${statusClass}">${escapeHtml(group.status)}</span>
                </div>
            </div>
        `;
    }).join('');
}

// Agrupar solicitações por líder e período
function groupRequestsByLeader(requests) {
    const groups = new Map();
    
    requests.forEach(request => {
        const leaderId = request.leaderId;
        const startDate = request.startDate;
        const endDate = request.endDate;
        
        // Criar chave única para líder + período
        const key = `${leaderId}_${startDate}_${endDate}`;
        
        if (!groups.has(key)) {
            groups.set(key, {
                key,
                leaderId: request.leaderId,
                leaderName: request.leader,
                regionName: request.region || 'Região não informada',
                startDate,
                endDate,
                status: request.status,
                colportorCount: 0,
                colportors: [],
                latestDate: request.createdAt || request.startDate
            });
        }
        
        const group = groups.get(key);
        group.colportorCount++;
        group.colportors.push(request.colportor);
        
        // Atualizar data mais recente
        const requestDate = new Date(request.createdAt || request.startDate);
        const groupDate = new Date(group.latestDate);
        if (requestDate > groupDate) {
            group.latestDate = request.createdAt || request.startDate;
        }
    });
    
    return Array.from(groups.values());
}

// Agrupar solicitações por período (semana)
function groupRequestsByPeriod(requests) {
    const periods = new Map();
    
    requests.forEach(request => {
        const startDate = new Date(request.startDate);
        const endDate = new Date(request.endDate);
        
        // Criar chave única para o período (semana)
        const weekStart = getWeekStart(startDate);
        const weekEnd = getWeekEnd(weekStart);
        
        const key = weekStart.toISOString().split('T')[0];
        
        if (!periods.has(key)) {
            periods.set(key, {
                key,
                startDate: weekStart.toISOString().split('T')[0],
                endDate: weekEnd.toISOString().split('T')[0],
                title: `Semana ${getWeekNumber(weekStart)}`,
                dateRange: `${weekStart.toLocaleDateString('pt-BR')} - ${weekEnd.toLocaleDateString('pt-BR')}`,
                requests: []
            });
        }
        
        periods.get(key).requests.push(request);
    });
    
    // Converter para array e ordenar por data
    return Array.from(periods.values())
        .sort((a, b) => new Date(a.startDate) - new Date(b.startDate));
}

// Funções auxiliares para datas
function getWeekStart(date) {
    const d = new Date(date);
    const day = d.getDay();
    const diff = d.getDate() - day + (day === 0 ? -6 : 1); // Segunda-feira
    return new Date(d.setDate(diff));
}

function getWeekEnd(weekStart) {
    const end = new Date(weekStart);
    end.setDate(end.getDate() + 6); // Domingo
    return end;
}

function getWeekNumber(date) {
    const d = new Date(date);
    const onejan = new Date(d.getFullYear(), 0, 1);
    const week = Math.ceil((((d - onejan) / 86400000) + onejan.getDay() + 1) / 7);
    return week;
}

// Filtrar por período (função global para usar nos botões)
window.filterByPeriod = function(startDate, endDate) {
    if (pacFrom) pacFrom.value = startDate;
    if (pacTo) pacTo.value = endDate;
    openPacAdmin();
    loadPacAdmin();
};
