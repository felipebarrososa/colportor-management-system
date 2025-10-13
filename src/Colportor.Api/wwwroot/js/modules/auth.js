/**
 * Módulo de Autenticação
 * Gerencia login, logout e estado de autenticação
 */
export class AuthModule {
    constructor() {
        this.token = localStorage.getItem('authToken');
        this.user = JSON.parse(localStorage.getItem('user') || 'null');
        this.isAuthenticated = !!this.token;
    }

    /**
     * Realiza login
     */
    async login(email, password) {
        try {
            const response = await fetch('/api/auth/login', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ email, password })
            });

            const data = await response.json();

            if (response.ok && data.success) {
                this.setAuthData(data.data);
                return { success: true, data: data.data };
            } else {
                return { success: false, message: data.message || 'Erro no login' };
            }
        } catch (error) {
            console.error('Erro no login:', error);
            return { success: false, message: 'Erro de conexão' };
        }
    }

    /**
     * Realiza logout
     */
    async logout() {
        try {
            if (this.token) {
                await fetch('/api/auth/logout', {
                    method: 'POST',
                    headers: {
                        'Authorization': `Bearer ${this.token}`
                    }
                });
            }
        } catch (error) {
            console.error('Erro no logout:', error);
        } finally {
            this.clearAuthData();
        }
    }

    /**
     * Verifica se o usuário está autenticado
     */
    isLoggedIn() {
        return this.isAuthenticated && !!this.token;
    }

    /**
     * Obtém o token de autenticação
     */
    getToken() {
        return this.token;
    }

    /**
     * Obtém dados do usuário
     */
    getUser() {
        return this.user;
    }

    /**
     * Verifica se o usuário tem um role específico
     */
    hasRole(role) {
        return this.user?.role === role;
    }

    /**
     * Verifica se é admin
     */
    isAdmin() {
        return this.hasRole('Admin');
    }

    /**
     * Verifica se é líder
     */
    isLeader() {
        return this.hasRole('Leader');
    }

    /**
     * Define dados de autenticação
     */
    setAuthData(authData) {
        this.token = authData.token;
        this.user = authData.user;
        this.isAuthenticated = true;

        localStorage.setItem('authToken', this.token);
        localStorage.setItem('user', JSON.stringify(this.user));
    }

    /**
     * Limpa dados de autenticação
     */
    clearAuthData() {
        this.token = null;
        this.user = null;
        this.isAuthenticated = false;

        localStorage.removeItem('authToken');
        localStorage.removeItem('user');
    }

    /**
     * Obtém headers com autenticação
     */
    getAuthHeaders() {
        return {
            'Authorization': `Bearer ${this.token}`,
            'Content-Type': 'application/json'
        };
    }
}
