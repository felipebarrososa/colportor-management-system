/**
 * Módulo de API
 * Gerencia requisições HTTP com tratamento de erros
 */
export class ApiModule {
    constructor(authModule) {
        this.auth = authModule;
        this.baseUrl = '/api';
    }

    /**
     * Faz requisição GET
     */
    async get(endpoint, params = {}) {
        const url = new URL(this.baseUrl + endpoint, window.location.origin);
        Object.keys(params).forEach(key => url.searchParams.append(key, params[key]));

        return this.request(url.toString(), {
            method: 'GET',
            headers: this.auth.getAuthHeaders()
        });
    }

    /**
     * Faz requisição POST
     */
    async post(endpoint, data = {}) {
        return this.request(this.baseUrl + endpoint, {
            method: 'POST',
            headers: this.auth.getAuthHeaders(),
            body: JSON.stringify(data)
        });
    }

    /**
     * Faz requisição PUT
     */
    async put(endpoint, data = {}) {
        return this.request(this.baseUrl + endpoint, {
            method: 'PUT',
            headers: this.auth.getAuthHeaders(),
            body: JSON.stringify(data)
        });
    }

    /**
     * Faz requisição DELETE
     */
    async delete(endpoint) {
        return this.request(this.baseUrl + endpoint, {
            method: 'DELETE',
            headers: this.auth.getAuthHeaders()
        });
    }

    /**
     * Executa requisição HTTP
     */
    async request(url, options = {}) {
        try {
            const response = await fetch(url, options);

            // Se não autorizado, tentar refresh token
            if (response.status === 401) {
                const refreshed = await this.refreshToken();
                if (refreshed) {
                    // Tentar novamente com novo token
                    options.headers = this.auth.getAuthHeaders();
                    return fetch(url, options);
                } else {
                    // Redirecionar para login
                    this.redirectToLogin();
                    return { success: false, message: 'Sessão expirada' };
                }
            }

            const data = await response.json();

            return {
                success: response.ok,
                data: data,
                status: response.status,
                message: data.message || (response.ok ? 'Sucesso' : 'Erro na requisição')
            };
        } catch (error) {
            console.error('Erro na requisição:', error);
            return {
                success: false,
                message: 'Erro de conexão',
                error: error
            };
        }
    }

    /**
     * Tenta renovar token
     */
    async refreshToken() {
        try {
            const response = await fetch('/api/auth/refresh', {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${this.auth.getToken()}`
                }
            });

            if (response.ok) {
                const data = await response.json();
                if (data.success) {
                    this.auth.setAuthData(data.data);
                    return true;
                }
            }
            return false;
        } catch (error) {
            console.error('Erro ao renovar token:', error);
            return false;
        }
    }

    /**
     * Redireciona para login
     */
    redirectToLogin() {
        this.auth.clearAuthData();
        window.location.href = '/admin/login.html';
    }

    /**
     * Trata erros de forma padronizada
     */
    handleError(error, context = '') {
        console.error(`Erro ${context}:`, error);
        
        const message = error.message || 'Erro inesperado';
        
        // Emitir evento de erro para UI
        document.dispatchEvent(new CustomEvent('api:error', {
            detail: { message, context }
        }));

        return message;
    }

    /**
     * Mostra notificação de sucesso
     */
    showSuccess(message) {
        document.dispatchEvent(new CustomEvent('api:success', {
            detail: { message }
        }));
    }

    /**
     * Mostra notificação de erro
     */
    showError(message) {
        document.dispatchEvent(new CustomEvent('api:error', {
            detail: { message }
        }));
    }
}
