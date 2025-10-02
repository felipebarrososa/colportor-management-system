// ================== Loading Utilities ==================

class LoadingManager {
    constructor() {
        this.activeLoadings = new Set();
        this.createGlobalOverlay();
    }

    // Criar overlay global de loading
    createGlobalOverlay() {
        if (document.getElementById('globalLoadingOverlay')) return;
        
        const overlay = document.createElement('div');
        overlay.id = 'globalLoadingOverlay';
        overlay.className = 'loading-overlay';
        overlay.innerHTML = `
            <div class="loading-card">
                <div class="loading-spinner"></div>
                <p class="loading-text">Carregando...</p>
                <p class="loading-subtext">Aguarde um momento</p>
            </div>
        `;
        document.body.appendChild(overlay);
    }

    // Mostrar loading global
    showGlobal(text = 'Carregando...', subtext = 'Aguarde um momento') {
        const overlay = document.getElementById('globalLoadingOverlay');
        if (!overlay) return;

        const textEl = overlay.querySelector('.loading-text');
        const subtextEl = overlay.querySelector('.loading-subtext');
        
        if (textEl) textEl.textContent = text;
        if (subtextEl) subtextEl.textContent = subtext;
        
        overlay.classList.add('show');
        this.activeLoadings.add('global');
    }

    // Esconder loading global
    hideGlobal() {
        const overlay = document.getElementById('globalLoadingOverlay');
        if (overlay) {
            overlay.classList.remove('show');
        }
        this.activeLoadings.delete('global');
    }

    // Mostrar loading em elemento específico
    showElement(element, text = 'Carregando...', type = 'default') {
        if (!element) return;

        const loadingId = `loading_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
        
        // Criar elemento de loading
        const loadingEl = document.createElement('div');
        loadingEl.id = loadingId;
        loadingEl.className = `loading-${type}`;
        
        if (type === 'overlay') {
            loadingEl.innerHTML = `
                <div class="loading-card">
                    <div class="loading-spinner"></div>
                    <p class="loading-text">${text}</p>
                </div>
            `;
        } else {
            loadingEl.innerHTML = `
                <div class="spinner"></div>
                <span>${text}</span>
            `;
        }

        // Adicionar ao elemento
        element.style.position = 'relative';
        element.appendChild(loadingEl);
        
        this.activeLoadings.add(loadingId);
        return loadingId;
    }

    // Esconder loading de elemento específico
    hideElement(loadingId) {
        const loadingEl = document.getElementById(loadingId);
        if (loadingEl) {
            loadingEl.remove();
        }
        this.activeLoadings.delete(loadingId);
    }

    // Mostrar loading em botão
    showButton(button, text = 'Carregando...') {
        if (!button) return;

        const originalText = button.textContent;
        button.dataset.originalText = originalText;
        button.innerHTML = `
            <span class="btn-text" style="opacity: 0.7;">${originalText}</span>
            <div class="spinner"></div>
        `;
        button.disabled = true;
        button.classList.add('btn-loading');
    }

    // Esconder loading de botão
    hideButton(button) {
        if (!button) return;

        const originalText = button.dataset.originalText || button.textContent;
        button.innerHTML = originalText;
        button.disabled = false;
        button.classList.remove('btn-loading');
    }

    // Mostrar loading em seção
    showSection(section, text = 'Carregando dados...') {
        if (!section) return;

        const loadingId = this.showElement(section, text, 'overlay');
        section.classList.add('loading-dashboard');
        return loadingId;
    }

    // Esconder loading de seção
    hideSection(section, loadingId) {
        if (loadingId) {
            this.hideElement(loadingId);
        }
        section.classList.remove('loading-dashboard');
    }

    // Mostrar loading em tabela
    showTable(table, text = 'Carregando dados...') {
        if (!table) return;

        const tbody = table.querySelector('tbody');
        if (!tbody) return;

        const loadingId = this.showElement(tbody, text, 'table');
        tbody.classList.add('loading-table');
        return loadingId;
    }

    // Esconder loading de tabela
    hideTable(table, loadingId) {
        if (loadingId) {
            this.hideElement(loadingId);
        }
        const tbody = table.querySelector('tbody');
        if (tbody) {
            tbody.classList.remove('loading-table');
        }
    }

    // Mostrar loading em modal
    showModal(modal, text = 'Processando...') {
        if (!modal) return;

        const loadingId = this.showElement(modal, text, 'overlay');
        modal.classList.add('loading-modal');
        return loadingId;
    }

    // Esconder loading de modal
    hideModal(modal, loadingId) {
        if (loadingId) {
            this.hideElement(loadingId);
        }
        modal.classList.remove('loading-modal');
    }

    // Verificar se há loadings ativos
    hasActiveLoadings() {
        return this.activeLoadings.size > 0;
    }

    // Limpar todos os loadings
    clearAll() {
        this.activeLoadings.forEach(id => {
            if (id === 'global') {
                this.hideGlobal();
            } else {
                this.hideElement(id);
            }
        });
        this.activeLoadings.clear();
    }
}

// Instância global - só criar após DOM carregado
let loadingManager = null;

// Inicializar quando DOM estiver pronto
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        loadingManager = new LoadingManager();
    });
} else {
    loadingManager = new LoadingManager();
}

// Funções de conveniência
export function showLoading(text, subtext) {
    if (loadingManager) loadingManager.showGlobal(text, subtext);
}

export function hideLoading() {
    if (loadingManager) loadingManager.hideGlobal();
}

export function showButtonLoading(button, text) {
    if (loadingManager) loadingManager.showButton(button, text);
}

export function hideButtonLoading(button) {
    if (loadingManager) loadingManager.hideButton(button);
}

export function showSectionLoading(section, text) {
    if (loadingManager) return loadingManager.showSection(section, text);
    return null;
}

export function hideSectionLoading(section, loadingId) {
    if (loadingManager) loadingManager.hideSection(section, loadingId);
}

export function showTableLoading(table, text) {
    if (loadingManager) return loadingManager.showTable(table, text);
    return null;
}

export function hideTableLoading(table, loadingId) {
    if (loadingManager) loadingManager.hideTable(table, loadingId);
}

export function showModalLoading(modal, text) {
    if (loadingManager) return loadingManager.showModal(modal, text);
    return null;
}

export function hideModalLoading(modal, loadingId) {
    if (loadingManager) loadingManager.hideModal(modal, loadingId);
}

// Wrapper para funções assíncronas com loading
export function withLoading(asyncFn, options = {}) {
    return async (...args) => {
        const {
            text = 'Carregando...',
            subtext = 'Aguarde um momento',
            element = null,
            button = null,
            section = null,
            table = null,
            modal = null
        } = options;

        let loadingId = null;

        try {
            // Mostrar loading apropriado
            if (element) {
                loadingId = loadingManager.showElement(element, text);
            } else if (button) {
                loadingManager.showButton(button, text);
            } else if (section) {
                loadingId = loadingManager.showSection(section, text);
            } else if (table) {
                loadingId = loadingManager.showTable(table, text);
            } else if (modal) {
                loadingId = loadingManager.showModal(modal, text);
            } else {
                loadingManager.showGlobal(text, subtext);
            }

            // Executar função
            const result = await asyncFn(...args);
            return result;

        } finally {
            // Esconder loading
            if (element && loadingId) {
                loadingManager.hideElement(loadingId);
            } else if (button) {
                loadingManager.hideButton(button);
            } else if (section && loadingId) {
                loadingManager.hideSection(section, loadingId);
            } else if (table && loadingId) {
                loadingManager.hideTable(table, loadingId);
            } else if (modal && loadingId) {
                loadingManager.hideModal(modal, loadingId);
            } else {
                loadingManager.hideGlobal();
            }
        }
    };
}

// Exportar instância para uso direto
export { loadingManager };
