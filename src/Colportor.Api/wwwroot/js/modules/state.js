/**
 * Módulo de Gerenciamento de Estado
 * Gerencia estado global da aplicação
 */
export class StateManager {
    constructor() {
        this.state = {
            // Estado de autenticação
            auth: {
                isAuthenticated: false,
                user: null,
                token: null
            },
            
            // Estado do calendário
            calendar: {
                currentYear: new Date().getFullYear(),
                currentMonth: new Date().getMonth() + 1,
                data: {},
                loading: false
            },
            
            // Estado dos colportores
            colportors: {
                list: [],
                current: null,
                loading: false,
                pagination: {
                    page: 1,
                    pageSize: 10,
                    totalCount: 0,
                    totalPages: 0
                }
            },
            
            // Estado das regiões
            regions: {
                list: [],
                current: null,
                loading: false
            },
            
            // Estado da UI
            ui: {
                loading: false,
                error: null,
                success: null,
                currentModal: null
            }
        };

        this.listeners = new Map();
        this.initializeEventListeners();
    }

    /**
     * Obtém um valor do estado
     */
    get(path) {
        const keys = path.split('.');
        let value = this.state;
        
        for (const key of keys) {
            if (value && typeof value === 'object' && key in value) {
                value = value[key];
            } else {
                return undefined;
            }
        }
        
        return value;
    }

    /**
     * Define um valor no estado
     */
    set(path, value) {
        const keys = path.split('.');
        const lastKey = keys.pop();
        let target = this.state;
        
        for (const key of keys) {
            if (!target[key] || typeof target[key] !== 'object') {
                target[key] = {};
            }
            target = target[key];
        }
        
        const oldValue = target[lastKey];
        target[lastKey] = value;
        
        // Notificar listeners
        this.notifyListeners(path, value, oldValue);
    }

    /**
     * Atualiza múltiplos valores no estado
     */
    update(updates) {
        Object.keys(updates).forEach(path => {
            this.set(path, updates[path]);
        });
    }

    /**
     * Incrementa um valor numérico
     */
    increment(path, amount = 1) {
        const currentValue = this.get(path) || 0;
        this.set(path, currentValue + amount);
    }

    /**
     * Decrementa um valor numérico
     */
    decrement(path, amount = 1) {
        const currentValue = this.get(path) || 0;
        this.set(path, Math.max(0, currentValue - amount));
    }

    /**
     * Adiciona item a uma lista
     */
    push(path, item) {
        const list = this.get(path) || [];
        list.push(item);
        this.set(path, list);
    }

    /**
     * Remove item de uma lista
     */
    remove(path, predicate) {
        const list = this.get(path) || [];
        const filteredList = list.filter(item => !predicate(item));
        this.set(path, filteredList);
    }

    /**
     * Atualiza item em uma lista
     */
    updateItem(path, predicate, updates) {
        const list = this.get(path) || [];
        const updatedList = list.map(item => 
            predicate(item) ? { ...item, ...updates } : item
        );
        this.set(path, updatedList);
    }

    /**
     * Inscreve-se em mudanças de estado
     */
    subscribe(path, callback) {
        if (!this.listeners.has(path)) {
            this.listeners.set(path, new Set());
        }
        this.listeners.get(path).add(callback);

        // Retornar função de unsubscribe
        return () => {
            const listeners = this.listeners.get(path);
            if (listeners) {
                listeners.delete(callback);
                if (listeners.size === 0) {
                    this.listeners.delete(path);
                }
            }
        };
    }

    /**
     * Notifica listeners sobre mudanças
     */
    notifyListeners(path, newValue, oldValue) {
        // Notificar listeners específicos do path
        const listeners = this.listeners.get(path);
        if (listeners) {
            listeners.forEach(callback => {
                try {
                    callback(newValue, oldValue, path);
                } catch (error) {
                    console.error('Erro ao notificar listener:', error);
                }
            });
        }

        // Notificar listeners de paths pai
        const pathParts = path.split('.');
        for (let i = pathParts.length - 1; i > 0; i--) {
            const parentPath = pathParts.slice(0, i).join('.');
            const parentListeners = this.listeners.get(parentPath);
            if (parentListeners) {
                parentListeners.forEach(callback => {
                    try {
                        callback(this.get(parentPath), null, parentPath);
                    } catch (error) {
                        console.error('Erro ao notificar listener pai:', error);
                    }
                });
            }
        }

        // Emitir evento customizado
        document.dispatchEvent(new CustomEvent('state:change', {
            detail: { path, newValue, oldValue }
        }));
    }

    /**
     * Inicializa listeners de eventos globais
     */
    initializeEventListeners() {
        // Listeners para eventos de API
        document.addEventListener('api:error', (event) => {
            this.set('ui.error', event.detail.message);
            this.set('ui.loading', false);
        });

        document.addEventListener('api:success', (event) => {
            this.set('ui.success', event.detail.message);
            this.set('ui.loading', false);
        });

        // Listener para mudanças de estado de autenticação
        this.subscribe('auth.isAuthenticated', (isAuthenticated) => {
            if (!isAuthenticated) {
                // Limpar estado quando deslogar
                this.set('colportors.list', []);
                this.set('regions.list', []);
                this.set('calendar.data', {});
            }
        });

        // Listener para mudanças de loading
        this.subscribe('ui.loading', (loading) => {
            const loadingElement = document.getElementById('loading');
            if (loadingElement) {
                loadingElement.style.display = loading ? 'block' : 'none';
            }
        });

        // Listener para erros
        this.subscribe('ui.error', (error) => {
            if (error) {
                this.showNotification(error, 'error');
                // Limpar erro após 5 segundos
                setTimeout(() => this.set('ui.error', null), 5000);
            }
        });

        // Listener para sucesso
        this.subscribe('ui.success', (success) => {
            if (success) {
                this.showNotification(success, 'success');
                // Limpar sucesso após 3 segundos
                setTimeout(() => this.set('ui.success', null), 3000);
            }
        });
    }

    /**
     * Mostra notificação
     */
    showNotification(message, type = 'info') {
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.textContent = message;

        // Adicionar ao DOM
        document.body.appendChild(notification);

        // Animar entrada
        setTimeout(() => notification.classList.add('show'), 100);

        // Remover após 5 segundos
        setTimeout(() => {
            notification.classList.remove('show');
            setTimeout(() => document.body.removeChild(notification), 300);
        }, 5000);
    }

    /**
     * Obtém estado completo (para debug)
     */
    getState() {
        return JSON.parse(JSON.stringify(this.state));
    }

    /**
     * Reseta estado para valores iniciais
     */
    reset() {
        this.state = {
            auth: { isAuthenticated: false, user: null, token: null },
            calendar: { currentYear: new Date().getFullYear(), currentMonth: new Date().getMonth() + 1, data: {}, loading: false },
            colportors: { list: [], current: null, loading: false, pagination: { page: 1, pageSize: 10, totalCount: 0, totalPages: 0 } },
            regions: { list: [], current: null, loading: false },
            ui: { loading: false, error: null, success: null, currentModal: null }
        };

        // Notificar todos os listeners
        this.listeners.forEach((listeners, path) => {
            listeners.forEach(callback => {
                try {
                    callback(this.get(path), null, path);
                } catch (error) {
                    console.error('Erro ao notificar listener no reset:', error);
                }
            });
        });
    }
}
