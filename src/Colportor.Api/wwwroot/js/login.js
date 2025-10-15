// /js/login.js
import { apiJSON, saveToken, setBusy } from "/js/shared.js";

// ===== UTILITIES =====
function parseJwt(token) {
    try {
        const base = token.split(".")[1];
        const json = atob(base.replace(/-/g, "+").replace(/_/g, "/"));
        return JSON.parse(decodeURIComponent(escape(json)));
    } catch { return {}; }
}

const form = document.getElementById("loginForm");
const btn = document.getElementById("loginBtn");

// login
form.addEventListener("submit", async (e) => {
    e.preventDefault();
    console.log("🔐 Login started, setting busy...");
    
    // Limpa tokens antigos antes do login
    sessionStorage.clear();
    localStorage.clear();
    
    setBusy(btn, true);

    try {
        const email = form.email.value.trim();
        const password = form.password.value;

        const res = await apiJSON("/api/auth/login", {
            method: "POST",
            body: JSON.stringify({ email, password }),
        });

        // extrai string do token de vários formatos
        const token = res.data?.token || res.token || res.Token || res.access_token || res.AccessToken || res.value || (res.jwt?.token);
        if (!token) {
            console.error("Resposta completa:", res);
            throw new Error("Token ausente na resposta.");
        }

        // salva a STRING na sessão/local
        saveToken(token);

        // (debug opcional)
        console.log("JWT salvo:", sessionStorage.getItem("token"));

        // Verificar role do usuário antes de redirecionar
        const claims = parseJwt(token);
        const role = (claims.role || claims.Role || "").toString().toLowerCase();
        
        console.log("Detected role:", role);

        // Redirecionar baseado no role
        if (role.includes("admin") || role.includes("leader")) {
            location.href = "/admin/dashboard.html";
            return;
        } else if (role.includes("colportor")) {
            alert("Esta conta é de Colportor. Acesse a carteira digital.");
            location.href = "/colportor/";
            return;
        } else {
            alert("Role não reconhecido. Entre em contato com o administrador.");
            return;
        }
    } catch (err) {
        console.error("❌ Login error:", err);
        alert("Falha no login. Verifique e-mail e senha.");
    } finally {
        console.log("✅ Login finished, removing busy...");
        setBusy(btn, false);
    }
});

// Abrir/fechar modal de líder
const registerModal = document.getElementById("registerModal");
document.getElementById("openLeader").addEventListener("click", () => {
    registerModal.setAttribute("aria-hidden", "false");
});
document.getElementById("closeRegister").addEventListener("click", () => {
    registerModal.setAttribute("aria-hidden", "true");
});
registerModal.addEventListener("click", (e) => {
    if (e.target.classList?.contains("modal-backdrop"))
        registerModal.setAttribute("aria-hidden", "true");
});

// ====== Cadastro de Líder (com token temporário Admin) ======
const leaderCountry = document.getElementById("leaderCountry");
const leaderRegion = document.getElementById("leaderRegion");
const registerLeaderForm = document.getElementById("registerLeaderForm");
const btnCreateLeader = document.getElementById("btnCreateLeader");

async function loadCountries(sel) {
    const list = await apiJSON("/geo/countries");
    sel.innerHTML = list.map(c => `<option value="${c.id}">${c.name}</option>`).join("");
}
async function loadRegions(countryId, sel) {
    if (!countryId) { sel.innerHTML = ""; return; }
    const list = await apiJSON(`/geo/regions?countryId=${countryId}`);
    sel.innerHTML = list.map(r => `<option value="${r.id}">${r.name}</option>`).join("");
}

document.getElementById("openLeader").addEventListener("click", async () => {
    await loadCountries(leaderCountry);
    if (leaderCountry.options.length > 0) leaderCountry.selectedIndex = 0;
    await loadRegions(leaderCountry.value, leaderRegion);
});
leaderCountry.addEventListener("change", async () => {
    await loadRegions(leaderCountry.value, leaderRegion);
});

registerLeaderForm.addEventListener("submit", async (e) => {
    e.preventDefault();
    setBusy(btnCreateLeader, true);
    try {
        const fullName = document.getElementById("leaderFullName").value.trim();
        const cpf = document.getElementById("leaderCPF").value.trim();
        const city = document.getElementById("leaderCity").value.trim() || null;
        const leaderEmail = document.getElementById("leaderEmail").value.trim();
        const leaderPassword = document.getElementById("leaderPassword").value;
        const regionId = parseInt(leaderRegion.value || "0", 10);
        
        // Validações
        if (!fullName) {
            alert("❌ Nome completo é obrigatório!");
            return;
        }
        if (!cpf) {
            alert("❌ CPF é obrigatório!");
            return;
        }
        if (!leaderEmail) {
            alert("❌ E-mail é obrigatório!");
            return;
        }
        if (!leaderPassword || leaderPassword.length < 6) {
            alert("❌ A senha deve ter pelo menos 6 caracteres!");
            return;
        }
        if (!regionId) {
            alert("❌ Selecione uma região!");
            return;
        }

        // Registro público: cria líder pendente
        const res = await fetch("/leaders/register", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify({ 
                fullName, 
                cpf, 
                city,
                email: leaderEmail, 
                password: leaderPassword, 
                regionId 
            })
        });
        if (!res.ok) {
            const t = await res.text().catch(() => "");
            throw new Error(t || "Falha ao solicitar criação de líder");
        }

        registerModal.setAttribute("aria-hidden", "true");
        alert("✅ Solicitação enviada! Aguarde aprovação do Admin.");
        registerLeaderForm.reset();
    } catch (err) {
        console.error(err);
        alert("❌ Não foi possível solicitar o cadastro de líder.");
    } finally {
        setBusy(btnCreateLeader, false);
    }
});