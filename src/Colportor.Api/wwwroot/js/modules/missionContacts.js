import { ApiModule } from './api.js';
import { AuthModule } from './auth.js';

const auth = new AuthModule();
const api = new ApiModule(auth);

export const MissionContactsApi = {
    async list(params = {}, isWallet = false) {
        const endpoint = isWallet ? '/wallet/mission-contacts' : '/mission-contacts';
        return api.get(endpoint, params);
    },
    async getById(id, isWallet = false) {
        const endpoint = isWallet ? `/wallet/mission-contacts/${id}` : `/mission-contacts/${id}`;
        return api.get(endpoint);
    },
    async create(payload, isWallet = false) {
        const endpoint = isWallet ? '/wallet/mission-contacts' : '/mission-contacts';
        return api.post(endpoint, payload);
    },
    async update(id, payload) {
        return api.put(`/mission-contacts/${id}`, payload);
    },
    async updateStatus(id, payload) {
        return api.put(`/mission-contacts/${id}/status`, payload);
    },
    async assign(id, leaderId) {
        return api.put(`/mission-contacts/${id}/assign`, { leaderId });
    },
    async remove(id) {
        return api.delete(`/mission-contacts/${id}`);
    }
};



