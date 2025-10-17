// ===== helpers =====
const $ = (q) => document.querySelector(q);
const api = (path, opts = {}) => {
    const token = localStorage.getItem("token");
    console.log("API call to:", path, "Token exists:", !!token);
    return fetch(path, {
        ...opts,
        headers: {
            "Content-Type": "application/json",
            ...(token
                ? { Authorization: "Bearer " + token }
                : {}),
            ...(opts.headers || {}),
        },
    });
};

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
    return j.url || null; // backend devolve { url: "data:image/..." }
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
                list.map(l => `<option value="${l.id}">${escapeHtml(l.fullName || l.name || 'Nome não informado')}</option>`).join("");
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
            gender: $("#rGender").value || null,
            birthDate: $("#rBirthDate").value || null,
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


        if (!body.fullName || !body.cpf || !body.email || !body.password) {
            $("#registerError").hidden = false; return;
        }
        if (!body.regionId) { toast("Selecione a região."); btn.disabled = false; if (spinner) spinner.hidden = true; return; }

        // 1) Cria conta
        const res = await api("/auth/register", { method: "POST", body: JSON.stringify(body) });
        if (!res.ok) { 
            $("#registerError").hidden = false; 
            btn.disabled = false; 
            if (spinner) spinner.hidden = true; 
            return; 
        }

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
            console.error("Erro na carteira:", me.status, me.statusText);
            localStorage.removeItem("token");
            showScreen("#authScreen");
            return false;
        }
        const x = await me.json();


        // Acessar dados do colportor se existir
        const colportor = x.colportor || {};
        const region = x.region || {};
        
        // Definir foto ou emoji baseado no gênero
        const photoElement = $("#photo");
        if (colportor.photoUrl) {
            photoElement.src = colportor.photoUrl;
            photoElement.alt = "foto do colportor";
        } else {
            // Usar emoji baseado no gênero
            const gender = colportor.gender || "";
            if (gender.toLowerCase() === "masculino") {
                photoElement.src = "data:image/svg+xml;base64," + btoa(`
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="100" height="100">
                        <circle cx="50" cy="50" r="50" fill="#e3f2fd"/>
                        <text x="50" y="65" font-size="60" text-anchor="middle" font-family="Arial, sans-serif">👨</text>
                    </svg>
                `);
                photoElement.alt = "emoji masculino";
            } else if (gender.toLowerCase() === "feminino") {
                photoElement.src = "data:image/svg+xml;base64," + btoa(`
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="100" height="100">
                        <circle cx="50" cy="50" r="50" fill="#fce4ec"/>
                        <text x="50" y="65" font-size="60" text-anchor="middle" font-family="Arial, sans-serif">👩</text>
                    </svg>
                `);
                photoElement.alt = "emoji feminino";
            } else {
                // Gênero não definido - usar emoji neutro
                photoElement.src = "data:image/svg+xml;base64," + btoa(`
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="100" height="100">
                        <circle cx="50" cy="50" r="50" fill="#f5f5f5"/>
                        <text x="50" y="65" font-size="60" text-anchor="middle" font-family="Arial, sans-serif">👤</text>
                    </svg>
                `);
                photoElement.alt = "emoji neutro";
            }
        }
        
        $("#name").textContent = colportor.fullName || x.fullName || "—";
        $("#cpf").textContent = colportor.cpf || x.cpf || "—";
        $("#gender").textContent = colportor.gender ?? "—";
        
        // Calcular idade
        let age = "—";
        if (colportor.birthDate) {
            const birth = new Date(colportor.birthDate);
            const today = new Date();
            const ageCalc = today.getFullYear() - birth.getFullYear();
            const monthDiff = today.getMonth() - birth.getMonth();
            if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birth.getDate())) {
                age = ageCalc - 1;
            } else {
                age = ageCalc;
            }
        }
        $("#age").textContent = age;
        
        $("#place").textContent = [colportor.city || x.city, region.name].filter(Boolean).join(" / ") || "—";
        $("#leader").textContent = colportor.leaderName ?? "—";
        $("#lastVisit").textContent = colportor.lastVisitDate ? new Date(colportor.lastVisitDate).toLocaleDateString("pt-BR") : "—";
        $("#due").textContent = colportor.dueDate ? new Date(colportor.dueDate).toLocaleDateString("pt-BR") : "—";

        const pill = $("#status");
        pill.textContent = x.status ?? "—";
        // Aplicar classe correta para status
        const status = (x.status || "").toUpperCase();
        console.log("🔍 Status recebido:", status);
        if (status === "EM DIA") {
            pill.className = "pill EM\\ DIA";
            // APLICAR ESTILO DIRETAMENTE VIA JAVASCRIPT
            pill.style.background = "linear-gradient(135deg, #0f2d1f, #1d7a55)";
            pill.style.color = "#19da86";
            pill.style.border = "1px solid #1d7a55";
            pill.style.boxShadow = "0 2px 8px rgba(25,218,134,.2)";
            console.log("✅ Aplicada classe: pill EM\\ DIA + estilo direto");
        } else if (status === "AVISO") {
            pill.className = "pill AVISO";
            console.log("✅ Aplicada classe: pill AVISO");
        } else if (status === "VENCIDO") {
            pill.className = "pill VENCIDO";
            console.log("✅ Aplicada classe: pill VENCIDO");
        } else {
            pill.className = "pill";
            console.log("✅ Aplicada classe: pill (padrão)");
        }
        console.log("🔍 Classe final aplicada:", pill.className);

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

// ===== Modal de Edição =====
const editModal = $("#editModal");
const eCountrySel = $("#eCountry");
const eRegionSel = $("#eRegion");
const eLeaderSel = $("#eLeader");
const ePhotoFile = $("#ePhotoFile");
const ePhotoUrlH = $("#ePhotoUrl");

let currentColportorData = null;

async function hydrateEditGeo() {
    await loadCountries(eCountrySel);
    if (eCountrySel && !eCountrySel.value && eCountrySel.options.length > 1) {
        eCountrySel.selectedIndex = 1;
    }
    await loadRegions(eCountrySel?.value, eRegionSel);
}

async function loadLeadersForEdit(regionId) {
    if (!regionId) {
        eLeaderSel.innerHTML = `<option value="">Selecione uma região primeiro...</option>`;
        return;
    }
    try {
        eLeaderSel.innerHTML = `<option value="">Carregando...</option>`;
        const res = await fetch(`/geo/leaders?regionId=${regionId}`);
        const list = (await res.json()) || [];
        if (!list.length) {
            eLeaderSel.innerHTML = `<option value="">Nenhum líder nesta região</option>`;
        } else {
            eLeaderSel.innerHTML = `<option value="">Opcional - selecione seu líder...</option>` + 
                list.map(l => `<option value="${l.id}">${escapeHtml(l.fullName || l.name || 'Nome não informado')}</option>`).join("");
        }
    } catch (err) {
        console.error("Erro ao carregar líderes:", err);
        eLeaderSel.innerHTML = `<option value="">Erro ao carregar líderes</option>`;
    }
}

async function loadColportorData() {
    try {
        console.log("🔄 Carregando dados do colportor para edição...");
        const res = await api("/wallet/me");
        if (!res.ok) {
            console.error("❌ Erro na resposta:", res.status, res.statusText);
            throw new Error(`Erro na API: ${res.status}`);
        }
        
        currentColportorData = await res.json();
        console.log("✅ Dados carregados:", currentColportorData);
        
        // Preencher campos do modal
        const colportor = currentColportorData.colportor || {};
        $("#eFullName").value = colportor.fullName || currentColportorData.fullName || "";
        $("#eCPF").value = colportor.cpf || currentColportorData.cpf || "";
        $("#eGender").value = colportor.gender || "";
        $("#eBirthDate").value = colportor.birthDate ? colportor.birthDate.split('T')[0] : "";
        $("#eCity").value = colportor.city || currentColportorData.city || "";
        $("#eEmail").value = currentColportorData.email || "";
        ePhotoUrlH.value = colportor.photoUrl || "";
        
        console.log("✅ Campos preenchidos no modal");
        
        // Carregar dados geográficos
        await hydrateEditGeo();
        
        // Se tiver região, carregar líderes
        const regionId = colportor.regionId || currentColportorData.regionId;
        if (regionId) {
            eRegionSel.value = regionId;
            await loadLeadersForEdit(regionId);
            
            // Se tiver líder, selecionar
            if (colportor.leaderId) {
                eLeaderSel.value = colportor.leaderId;
            }
        }
        
        return true;
    } catch (err) {
        console.error("Erro ao carregar dados:", err);
        return false;
    }
}

// Event listeners do modal de edição
$("#btnEditProfile")?.addEventListener("click", async () => {
    editModal.classList.add("show");
    editModal.setAttribute("aria-hidden", "false");
    
    // mostrar loading do modal
    const editLoading = document.getElementById("editLoading");
    console.log("🔍 Elemento editLoading encontrado:", editLoading);
    if (editLoading) {
        editLoading.hidden = false;
        editLoading.innerHTML = '<span class="spinner"></span><span>Carregando dados...</span>';
        editLoading.classList.remove('success');
        console.log("✅ Loading mostrado");
    } else {
        console.error("❌ Elemento editLoading não encontrado!");
    }
    
    try {
        await loadColportorData();
        
        // esconder loading imediatamente após carregar
        console.log("✅ Dados carregados, escondendo loading...");
        if (editLoading) {
            editLoading.style.display = 'none';
            editLoading.hidden = true;
            editLoading.classList.remove('success');
            console.log("✅ Loading escondido com sucesso");
            console.log("🔍 Estado do elemento:", editLoading.hidden, editLoading.style.display);
        } else {
            console.error("❌ Elemento editLoading não encontrado para esconder!");
        }
    } catch (error) {
        console.error("❌ Erro ao carregar dados:", error);
        // esconder loading em caso de erro
        if (editLoading) {
            editLoading.style.display = 'none';
            editLoading.hidden = true;
            editLoading.classList.remove('success');
            console.log("✅ Loading escondido após erro");
        }
    }
    
    $("#eFullName")?.focus();
});

$("#btnCloseEdit")?.addEventListener("click", () => {
    editModal.classList.remove("show");
    editModal.setAttribute("aria-hidden", "true");
    
    // esconder loading quando fechar modal
    const editLoading = document.getElementById("editLoading");
    if (editLoading) {
        editLoading.style.display = 'none';
        editLoading.hidden = true;
        editLoading.classList.remove('success');
    }
});
editModal?.addEventListener("click", (e) => { 
    if (e.target === editModal) {
        editModal.classList.remove("show");
        editModal.setAttribute("aria-hidden", "true");
        
        // esconder loading quando fechar modal
        const editLoading = document.getElementById("editLoading");
        if (editLoading) {
            editLoading.style.display = 'none';
            editLoading.hidden = true;
            editLoading.classList.remove('success');
        }
    }
});

eCountrySel?.addEventListener("change", () => loadRegions(eCountrySel.value, eRegionSel));
eRegionSel?.addEventListener("change", () => loadLeadersForEdit(parseInt(eRegionSel.value || "0", 10)));

// Upload de foto no modal de edição
ePhotoFile?.addEventListener("change", async (e) => {
    const file = e.target.files[0];
    if (!file) return;
    
    try {
        const colportorId = currentColportorData?.colportor?.id || currentColportorData?.id;
        const url = await uploadPhoto(file);
        if (url) {
            ePhotoUrlH.value = url;
            toast("Foto carregada com sucesso!");
        }
    } catch (err) {
        console.error("Erro ao fazer upload da foto:", err);
        toast("Erro ao carregar foto.");
    }
});

// Submit do formulário de edição
$("#editForm")?.addEventListener("submit", async (e) => {
    e.preventDefault();
    const btn = $("#btnUpdate");
    const spinner = btn.querySelector(".spinner");
    $("#editError").hidden = true;
    btn.disabled = true;
    if (spinner) spinner.hidden = false;

    try {
        // Validações básicas
        const fullName = $("#eFullName").value.trim();
        const cpf = $("#eCPF").value.trim();
        const email = $("#eEmail").value.trim();
        
        if (!fullName || !cpf || !email) {
            $("#editError").textContent = "Nome, CPF e e-mail são obrigatórios.";
            $("#editError").hidden = false;
            return;
        }

        // Foto: file > hidden URL > manter atual
        let photoUrl = currentColportorData.photoUrl;
        if (ePhotoFile?.files && ePhotoFile.files[0]) {
            try { 
                const colportorId = currentColportorData.colportor?.id || currentColportorData.id;
                photoUrl = await uploadPhoto(ePhotoFile.files[0]); 
            } catch { 
                toast("Falha ao enviar foto."); 
            }
        } else if (ePhotoUrlH?.value) {
            photoUrl = ePhotoUrlH.value.trim() || currentColportorData.photoUrl;
        }

        const regionId = eRegionSel?.value ? parseInt(eRegionSel.value, 10) : null;
        const leaderId = eLeaderSel?.value ? parseInt(eLeaderSel.value, 10) : null;

        const body = {
            fullName,
            cpf,
            gender: $("#eGender").value || null,
            birthDate: $("#eBirthDate").value || null,
            city: $("#eCity").value.trim() || null,
            photoUrl,
            email,
            password: $("#ePass").value.trim() || null, // Opcional
            regionId,
            leaderId
        };

        // Remover campos vazios
        Object.keys(body).forEach(key => {
            if (body[key] === null || body[key] === "") {
                delete body[key];
            }
        });

        const res = await api("/wallet/me", { 
            method: "PUT", 
            body: JSON.stringify(body) 
        });

        if (!res.ok) {
            const errorText = await res.text();
            $("#editError").textContent = errorText || "Erro ao atualizar dados.";
            $("#editError").hidden = false;
            btn.disabled = false;
            if (spinner) spinner.hidden = true;
            return;
        }

        // Sucesso
        editModal.classList.remove("show");
        editModal.setAttribute("aria-hidden", "true");
        toast("Dados atualizados com sucesso!");
        
        // Recarregar carteira
        await renderWallet();
        
    } catch (err) {
        console.error(err);
        $("#editError").textContent = "Erro interno. Tente novamente.";
        $("#editError").hidden = false;
    } finally {
        btn.disabled = false;
        if (spinner) spinner.hidden = true;
    }
});
