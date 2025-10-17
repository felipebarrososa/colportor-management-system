// ===== Helpers de Auth + Fetch =====
const API = ""; // mesmo host
const LS_KEY = "colp_token";     // onde o resto do app já esperava
const SS_KEY = "token";          // onde o admin-auth colocava

export function saveToken(t) {
    try {
        if (t && typeof t === "string") {
            sessionStorage.setItem(SS_KEY, t);
            localStorage.setItem(LS_KEY, t);
        }
    } catch { }
}
export function getToken() {
    try {
        const ss = (sessionStorage.getItem(SS_KEY) || "").trim();
        if (ss) return ss;
    } catch { }
    try {
        const ls = (localStorage.getItem(LS_KEY) || "").trim();
        if (ls) return ls;
    } catch { }
    return "";
}
export function clearToken() {
    try { sessionStorage.removeItem(SS_KEY); } catch { }
    try { localStorage.removeItem(LS_KEY); } catch { }
}

export async function apiFetch(path, opts = {}) {
    const token = getToken();
    const headers = new Headers(opts.headers || {});
    headers.set("Accept", "application/json");
    if (!(opts.body instanceof FormData)) {
        if (!headers.has("Content-Type")) headers.set("Content-Type", "application/json");
    }
    if (token) headers.set("Authorization", "Bearer " + token);

    const res = await fetch(API + path, { ...opts, headers });

    // Se o backend devolver 401, limpa e manda pro login admin
    if (res.status === 401) {
        clearToken();
        // evita loop em páginas que não são do admin
        if (!location.pathname.toLowerCase().includes("/admin/login")) {
            location.replace("/admin/login.html");
        }
        throw new Error("Unauthorized");
    }
    return res;
}
export async function apiJSON(path, opts = {}) {
    const res = await apiFetch(path, opts);
    if (!res.ok) throw await buildError(res);
    return res.json();
}
async function buildError(res) {
    let msg = `${res.status} ${res.statusText}`;
    try {
        const t = await res.text();
        if (t) msg += `\n${t}`;
    } catch { }
    const e = new Error(msg);
    e.status = res.status;
    return e;
}

// ===== JWT decode =====
export function decodeJwt(token) {
    try {
        const [, b] = token.split(".");
        const json = atob(b.replace(/-/g, "+").replace(/_/g, "/"));
        return JSON.parse(json);
    } catch { return {}; }
}
export function getRoleFromToken(token) {
    const p = decodeJwt(token);
    return (
        p.role ||
        p["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ||
        p["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role"] ||
        ""
    );
}

// ===== Geo =====
export async function loadCountries(selectEl) {
    const data = await apiJSON("/geo/countries");
    fillSelect(selectEl, data, "id", "name");
}
export async function loadRegions(countryId, selectEl) {
    if (!countryId) {
        selectEl.innerHTML = "<option value=''>Selecione...</option>";
        return;
    }
    const data = await apiJSON(`/geo/regions?countryId=${encodeURIComponent(countryId)}`);
    fillSelect(selectEl, data, "id", "name");
}
export function fillSelect(sel, items, valueKey = "id", labelKey = "name") {
    sel.innerHTML = items.map(o => `<option value="${o[valueKey]}">${o[labelKey]}</option>`).join("");
}

// ===== Upload =====
export async function uploadPhoto(file) {
    const fd = new FormData();
    fd.append("photo", file);
    const res = await fetch("/upload/photo", { method: "POST", body: fd });
    if (!res.ok) throw new Error(await res.text());
    const data = await res.json();
    return data.url; // data:image/...
}

// ===== UI helpers =====
export function setBusy(button, busy = true) {
    const sp = button?.querySelector?.(".spinner");
    if (sp) sp.hidden = !busy;
    if (button) button.disabled = !!busy;
}
export function toast(msg) {
    // troque por um toast real se quiser
    console.log("[toast]", msg);
    try { alert(msg); } catch { }
}
