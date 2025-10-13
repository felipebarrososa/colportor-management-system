/**
 * Aplicação Principal - Colportor Management System
 * Integra todos os módulos e gerencia a aplicação
 */

import { AuthModule } from './modules/auth.js';
import { ApiModule } from './modules/api.js';
import { CalendarModule } from './modules/calendar.js';
import { StateManager } from './modules/state.js';

class ColportorApp {
    constructor() {
        this.auth = new AuthModule();
        this.api = new ApiModule(this.auth);
        this.calendar = new CalendarModule(this.api);
        this.state = new StateManager();
        
        this.isInitialized = false;
        this.currentPage = this.getCurrentPage();
    }

    /**
     * Inicializa a aplicação
     */
    async init() {
        try {
            console.log('🚀 Inicializando Colportor Management System...');
            
            // Sincronizar estado de autenticação
            this.syncAuthState();
            
            // Configurar listeners globais
            this.setupGlobalListeners();
            
            // Inicializar página específica
            await this.initPage();
            
            this.isInitialized = true;
            console.log('✅ Aplicação inicializada com sucesso');
            
            // Emitir evento de inicialização
            document.dispatchEvent(new CustomEvent('app:initialized'));
            
        } catch (error) {
            console.error('❌ Erro ao inicializar aplicação:', error);
            this.showError('Erro ao inicializar aplicação');
        }
    }

    /**
     * Sincroniza estado de autenticação
     */
    syncAuthState() {
        const isAuthenticated = this.auth.isLoggedIn();
        const user = this.auth.getUser();
        const token = this.auth.getToken();

        this.state.update({
            'auth.isAuthenticated': isAuthenticated,
            'auth.user': user,
            'auth.token': token
        });
    }

    /**
     * Configura listeners globais
     */
    setupGlobalListeners() {
        // Listener para mudanças de autenticação
        this.state.subscribe('auth.isAuthenticated', (isAuthenticated) => {
            if (!isAuthenticated && this.requiresAuth()) {
                this.redirectToLogin();
            }
        });

        // Listener para erros globais
        this.state.subscribe('ui.error', (error) => {
            if (error) {
                this.showError(error);
            }
        });

        // Listener para sucesso global
        this.state.subscribe('ui.success', (success) => {
            if (success) {
                this.showSuccess(success);
            }
        });

        // Listener para loading global
        this.state.subscribe('ui.loading', (loading) => {
            this.toggleLoading(loading);
        });

        // Listener para mudanças de estado
        document.addEventListener('state:change', (event) => {
            console.log('Estado alterado:', event.detail);
        });
    }

    /**
     * Inicializa página específica
     */
    async initPage() {
        switch (this.currentPage) {
            case 'dashboard':
                await this.initDashboard();
                break;
            case 'login':
                await this.initLogin();
                break;
            case 'colportors':
                await this.initColportors();
                break;
            default:
                console.log('Página não reconhecida:', this.currentPage);
        }
    }

    /**
     * Inicializa dashboard
     */
    async initDashboard() {
        console.log('Inicializando dashboard...');
        
        if (!this.auth.isAdmin()) {
            this.showError('Acesso negado. Apenas administradores podem acessar o dashboard.');
            this.redirectToLogin();
            return;
        }

        // Carregar calendário
        this.state.set('calendar.loading', true);
        try {
            const result = await this.calendar.loadCalendar();
            if (result.success) {
                this.calendar.renderCalendar();
                this.state.set('calendar.data', result.data);
            } else {
                this.showError(result.message);
            }
        } catch (error) {
            this.showError('Erro ao carregar calendário');
        } finally {
            this.state.set('calendar.loading', false);
        }

        // Configurar navegação do calendário
        window.calendarModule = this.calendar;
    }

    /**
     * Inicializa página de login
     */
    async initLogin() {
        console.log('Inicializando login...');
        
        // Se já estiver logado, redirecionar para dashboard
        if (this.auth.isLoggedIn()) {
            this.redirectToDashboard();
            return;
        }

        // Configurar formulário de login
        const loginForm = document.getElementById('loginForm');
        if (loginForm) {
            loginForm.addEventListener('submit', async (e) => {
                e.preventDefault();
                await this.handleLogin();
            });
        }
    }

    /**
     * Inicializa página de colportores
     */
    async initColportors() {
        console.log('Inicializando colportores...');
        
        if (!this.auth.isLoggedIn()) {
            this.redirectToLogin();
            return;
        }

        // Carregar lista de colportores
        await this.loadColportors();
    }

    /**
     * Manipula login
     */
    async handleLogin() {
        const email = document.getElementById('email')?.value;
        const password = document.getElementById('password')?.value;

        if (!email || !password) {
            this.showError('Por favor, preencha todos os campos');
            return;
        }

        this.state.set('ui.loading', true);
        
        try {
            const result = await this.auth.login(email, password);
            
            if (result.success) {
                this.syncAuthState();
                this.showSuccess('Login realizado com sucesso!');
                
                setTimeout(() => {
                    this.redirectToDashboard();
                }, 1000);
            } else {
                this.showError(result.message);
            }
        } catch (error) {
            this.showError('Erro ao realizar login');
        } finally {
            this.state.set('ui.loading', false);
        }
    }

    /**
     * Carrega lista de colportores
     */
    async loadColportors() {
        this.state.set('colportors.loading', true);
        
        try {
            const response = await this.api.get('/colportor', {
                page: this.state.get('colportors.pagination.page'),
                pageSize: this.state.get('colportors.pagination.pageSize')
            });

            if (response.success) {
                this.state.update({
                    'colportors.list': response.data.items || [],
                    'colportors.pagination.totalCount': response.data.totalCount || 0,
                    'colportors.pagination.totalPages': response.data.totalPages || 0
                });
            } else {
                this.showError(response.message);
            }
        } catch (error) {
            this.showError('Erro ao carregar colportores');
        } finally {
            this.state.set('colportors.loading', false);
        }
    }

    /**
     * Manipula logout
     */
    async handleLogout() {
        try {
            await this.auth.logout();
            this.syncAuthState();
            this.redirectToLogin();
        } catch (error) {
            console.error('Erro no logout:', error);
            // Mesmo com erro, limpar estado local
            this.auth.clearAuthData();
            this.syncAuthState();
            this.redirectToLogin();
        }
    }

    /**
     * Mostra notificação de sucesso
     */
    showSuccess(message) {
        this.state.set('ui.success', message);
    }

    /**
     * Mostra notificação de erro
     */
    showError(message) {
        this.state.set('ui.error', message);
    }

    /**
     * Toggle loading
     */
    toggleLoading(show) {
        const loadingElement = document.getElementById('loading');
        if (loadingElement) {
            loadingElement.style.display = show ? 'block' : 'none';
        }
    }

    /**
     * Verifica se a página atual requer autenticação
     */
    requiresAuth() {
        const publicPages = ['login'];
        return !publicPages.includes(this.currentPage);
    }

    /**
     * Obtém página atual
     */
    getCurrentPage() {
        const path = window.location.pathname;
        if (path.includes('login')) return 'login';
        if (path.includes('dashboard')) return 'dashboard';
        if (path.includes('colportors')) return 'colportors';
        return 'dashboard'; // default
    }

    /**
     * Redireciona para login
     */
    redirectToLogin() {
        window.location.href = '/admin/login.html';
    }

    /**
     * Redireciona para dashboard
     */
    redirectToDashboard() {
        window.location.href = '/admin/dashboard.html';
    }

    /**
     * Obtém instância da aplicação
     */
    static getInstance() {
        if (!window.colportorApp) {
            window.colportorApp = new ColportorApp();
        }
        return window.colportorApp;
    }
}

// Inicializar aplicação quando DOM estiver pronto
document.addEventListener('DOMContentLoaded', async () => {
    const app = ColportorApp.getInstance();
    await app.init();
});

// Exportar para uso global
window.ColportorApp = ColportorApp;
