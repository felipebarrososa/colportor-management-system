/**
 * Módulo do Calendário
 * Gerencia funcionalidades do calendário PAC
 */
export class CalendarModule {
    constructor(apiModule) {
        this.api = apiModule;
        this.currentYear = new Date().getFullYear();
        this.currentMonth = new Date().getMonth() + 1;
        this.calendarData = {};
    }

    /**
     * Carrega dados do calendário mensal
     */
    async loadCalendar(year = this.currentYear, month = this.currentMonth) {
        try {
            console.log(`Carregando calendário para ${year}-${month}`);
            
            const response = await this.api.get('/calendar/monthly', { year, month });
            
            if (response.success) {
                this.currentYear = year;
                this.currentMonth = month;
                this.calendarData = response.data.calendarData || {};
                
                console.log('Dados do calendário carregados:', this.calendarData);
                return { success: true, data: response.data };
            } else {
                console.error('Erro ao carregar calendário:', response.message);
                return { success: false, message: response.message };
            }
        } catch (error) {
            console.error('Erro ao carregar calendário:', error);
            return { success: false, message: 'Erro ao carregar calendário' };
        }
    }

    /**
     * Gera HTML do calendário
     */
    generateCalendarHTML(year, month, monthName) {
        const firstDay = new Date(year, month - 1, 1);
        const lastDay = new Date(year, month, 0);
        const startDate = new Date(firstDay);
        startDate.setDate(startDate.getDate() - firstDay.getDay());

        let html = `
            <div class="calendar-header">
                <button class="btn btn-sm" onclick="calendarModule.previousMonth()">‹</button>
                <h3>${monthName} ${year}</h3>
                <button class="btn btn-sm" onclick="calendarModule.nextMonth()">›</button>
            </div>
            <div class="calendar-grid">
                <div class="calendar-weekdays">
                    <div class="weekday">Dom</div>
                    <div class="weekday">Seg</div>
                    <div class="weekday">Ter</div>
                    <div class="weekday">Qua</div>
                    <div class="weekday">Qui</div>
                    <div class="weekday">Sex</div>
                    <div class="weekday">Sáb</div>
                </div>
                <div class="calendar-days">
        `;

        for (let i = 0; i < 42; i++) {
            const currentDate = new Date(startDate);
            currentDate.setDate(startDate.getDate() + i);
            
            const dateKey = currentDate.toISOString().split('T')[0];
            const dayData = this.calendarData[dateKey];
            const isCurrentMonth = currentDate.getMonth() === month - 1;
            const isToday = currentDate.toDateString() === new Date().toDateString();

            let dayClass = 'calendar-day';
            if (!isCurrentMonth) dayClass += ' other-month';
            if (isToday) dayClass += ' today';

            html += `
                <div class="${dayClass}" onclick="calendarModule.openDayDetails('${dateKey}')">
                    <div class="day-number">${currentDate.getDate()}</div>
                    ${this.generateDayStatsHTML(dayData)}
                </div>
            `;
        }

        html += `
                </div>
            </div>
        `;

        return html;
    }

    /**
     * Gera HTML das estatísticas do dia
     */
    generateDayStatsHTML(dayData) {
        if (!dayData || dayData.total === 0) {
            return '<div class="day-stats empty">Sem dados</div>';
        }

        return `
            <div class="day-stats">
                <div class="day-stat">
                    <span class="day-stat-label">H</span>
                    <span class="day-stat-value">${dayData.males || 0}</span>
                </div>
                <div class="day-stat">
                    <span class="day-stat-label">M</span>
                    <span class="day-stat-value">${dayData.females || 0}</span>
                </div>
                <div class="day-stat">
                    <span class="day-stat-label">T</span>
                    <span class="day-stat-value">${dayData.total || 0}</span>
                </div>
            </div>
        `;
    }

    /**
     * Abre modal com detalhes do dia
     */
    async openDayDetails(dateKey) {
        const dayData = this.calendarData[dateKey];
        if (!dayData || dayData.total === 0) {
            this.showNoDataModal(dateKey);
            return;
        }

        const date = new Date(dateKey);
        const dayName = this.getDayName(date.getDay());
        const monthName = this.getMonthName(date.getMonth());
        const formattedDate = `${dayName}, ${date.getDate()} de ${monthName} de ${date.getFullYear()}`;

        let html = `
            <div class="modal-header">
                <h3>Colportores Confirmados</h3>
                <button class="icon close" onclick="calendarModule.closeDayDetails()">✕</button>
            </div>
            <div class="modal-body">
                <div class="day-summary">
                    <h4>${formattedDate}</h4>
                    <div class="summary-stats">
                        <div class="stat">
                            <span class="stat-label">Homens:</span>
                            <span class="stat-value">${dayData.males || 0}</span>
                        </div>
                        <div class="stat">
                            <span class="stat-label">Mulheres:</span>
                            <span class="stat-value">${dayData.females || 0}</span>
                        </div>
                        <div class="stat">
                            <span class="stat-label">Total:</span>
                            <span class="stat-value">${dayData.total || 0}</span>
                        </div>
                    </div>
                </div>
                <div class="regions-list">
                    <h5>Por Região</h5>
        `;

        const regions = dayData.regions || dayData.Regions || [];
        regions.forEach(region => {
            html += `
                <div class="region-card" onclick="calendarModule.openRegionDetails('${dateKey}', ${region.regionId || region.RegionId}, '${region.regionName || region.RegionName}')">
                    <div class="region-info">
                        <h6>${region.regionName || region.RegionName}</h6>
                        <div class="region-stats">
                            <span>H: ${region.males || region.Males || 0}</span>
                            <span>M: ${region.females || region.Females || 0}</span>
                            <span>T: ${region.total || region.Total || 0}</span>
                        </div>
                    </div>
                </div>
            `;
        });

        html += `
                </div>
            </div>
        `;

        this.showModal('dayDetailsModal', html);
    }

    /**
     * Abre modal com detalhes da região
     */
    async openRegionDetails(dateKey, regionId, regionName) {
        const dayData = this.calendarData[dateKey];
        const regions = dayData.regions || dayData.Regions || [];
        const region = regions.find(r => (r.regionId || r.RegionId) == regionId);

        if (!region) {
            this.showError('Região não encontrada');
            return;
        }

        const date = new Date(dateKey);
        const formattedDate = date.toLocaleDateString('pt-BR');

        let html = `
            <div class="modal-header">
                <h3>Colportores da Região</h3>
                <div class="modal-actions">
                    <button class="btn subtle" onclick="calendarModule.backToDayDetails()">← Voltar</button>
                    <button class="icon close" onclick="calendarModule.closeRegionDetails()">✕</button>
                </div>
            </div>
            <div class="modal-body">
                <div class="region-summary">
                    <h4>${regionName}</h4>
                    <p>${formattedDate}</p>
                    <div class="summary-stats">
                        <div class="stat">
                            <span class="stat-label">Homens:</span>
                            <span class="stat-value">${region.males || region.Males || 0}</span>
                        </div>
                        <div class="stat">
                            <span class="stat-label">Mulheres:</span>
                            <span class="stat-value">${region.females || region.Females || 0}</span>
                        </div>
                        <div class="stat">
                            <span class="stat-label">Total:</span>
                            <span class="stat-value">${region.total || region.Total || 0}</span>
                        </div>
                    </div>
                </div>
                <div class="colportors-list">
                    <h5>Colportores Confirmados</h5>
        `;

        const colportors = region.colportors || region.Colportors || [];
        colportors.forEach(colportor => {
            html += `
                <div class="colportor-item">
                    <div class="colportor-info">
                        <h6>${colportor.fullName || colportor.FullName}</h6>
                        <p>CPF: ${colportor.cpf || colportor.CPF}</p>
                        <p>Gênero: ${colportor.gender || colportor.Gender}</p>
                        <p>Líder: ${colportor.leaderName || colportor.LeaderName}</p>
                    </div>
                </div>
            `;
        });

        html += `
                </div>
            </div>
        `;

        this.showModal('regionDetailsModal', html);
    }

    /**
     * Mês anterior
     */
    async previousMonth() {
        let newMonth = this.currentMonth - 1;
        let newYear = this.currentYear;

        if (newMonth < 1) {
            newMonth = 12;
            newYear--;
        }

        await this.loadCalendar(newYear, newMonth);
        this.renderCalendar();
    }

    /**
     * Próximo mês
     */
    async nextMonth() {
        let newMonth = this.currentMonth + 1;
        let newYear = this.currentYear;

        if (newMonth > 12) {
            newMonth = 1;
            newYear++;
        }

        await this.loadCalendar(newYear, newMonth);
        this.renderCalendar();
    }

    /**
     * Renderiza o calendário
     */
    renderCalendar() {
        const monthNames = [
            "Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho",
            "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro"
        ];

        const calendarElement = document.getElementById('calendar');
        if (calendarElement) {
            calendarElement.innerHTML = this.generateCalendarHTML(
                this.currentYear,
                this.currentMonth,
                monthNames[this.currentMonth - 1]
            );
        }
    }

    /**
     * Utilitários
     */
    getDayName(dayIndex) {
        const days = ['Domingo', 'Segunda', 'Terça', 'Quarta', 'Quinta', 'Sexta', 'Sábado'];
        return days[dayIndex];
    }

    getMonthName(monthIndex) {
        const months = ['Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho',
                       'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro'];
        return months[monthIndex];
    }

    showModal(modalId, content) {
        const modal = document.getElementById(modalId);
        if (modal) {
            modal.innerHTML = content;
            modal.setAttribute('aria-hidden', 'false');
        }
    }

    closeDayDetails() {
        const modal = document.getElementById('dayDetailsModal');
        if (modal) {
            modal.setAttribute('aria-hidden', 'true');
        }
    }

    closeRegionDetails() {
        const modal = document.getElementById('regionDetailsModal');
        if (modal) {
            modal.setAttribute('aria-hidden', 'true');
        }
    }

    backToDayDetails() {
        this.closeRegionDetails();
        const dayModal = document.getElementById('dayDetailsModal');
        if (dayModal) {
            dayModal.setAttribute('aria-hidden', 'false');
        }
    }

    showNoDataModal(dateKey) {
        const date = new Date(dateKey);
        const formattedDate = date.toLocaleDateString('pt-BR');
        
        const html = `
            <div class="modal-header">
                <h3>Nenhum Colportor Confirmado</h3>
                <button class="icon close" onclick="calendarModule.closeDayDetails()">✕</button>
            </div>
            <div class="modal-body">
                <p>Não há colportores confirmados para <strong>${formattedDate}</strong>.</p>
            </div>
        `;

        this.showModal('dayDetailsModal', html);
    }

    showError(message) {
        console.error(message);
        // Implementar notificação de erro
    }
}
