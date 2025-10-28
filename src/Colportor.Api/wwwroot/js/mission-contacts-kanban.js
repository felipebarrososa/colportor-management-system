(function(){
  const token = sessionStorage.getItem("token") || localStorage.getItem("token") || "";
  
  if (!token || token.split(".").length !== 3) {
    window.location.href = "/admin/login.html";
    throw new Error("No auth token");
  }
  
  let contacts = [];
  let regions = [];
  let leaders = [];
  let currentContactId = null;
  let observations = {}; // Armazenar observações por contato
  let isEditing = false;
  let originalContactData = null;
  let reminders = []; // Sistema de lembretes
  
  
  // Variáveis do WhatsApp melhorado
  let isTyping = false;
  let typingTimeout = null;
  let messageStatusInterval = null;
  // Variáveis globais para Server-Sent Events
  let messageEventSource = null;
  let sseHeartbeatInterval = null;
  let lastSSEMessageTime = null;

  function esc(s){return String(s??'').replaceAll('&','&amp;').replaceAll('<','&lt;').replaceAll('>','&gt;');}
  
  // Função simples para reproduzir áudio placeholder
  function playAudioPlaceholder(element) {
    const playButton = element.querySelector('div:last-child');
    if (playButton) {
      if (playButton.textContent === '▶️') {
        playButton.textContent = '⏸️';
        playButton.style.color = '#ef4444';
        setTimeout(() => {
          playButton.textContent = '▶️';
          playButton.style.color = '#10b981';
        }, 2000);
      } else {
        playButton.textContent = '▶️';
        playButton.style.color = '#10b981';
      }
    }
  }

  // Função para controlar o player de áudio moderno - GLOBAL
  window.toggleAudio = function(audioId) {
    const audio = document.getElementById(audioId);
    const playIcon = document.getElementById('playIcon_' + audioId);
    const waveform = document.querySelector(`#${audioId}`)?.closest('.whatsapp-audio-player')?.querySelector('.audio-waveform');
    
    if (!audio) {
      console.error('Áudio não encontrado:', audioId);
      return;
    }
    
    if (audio.paused) {
      // Pausar outros áudios
      document.querySelectorAll('audio').forEach(a => {
        if (a.id !== audioId && !a.paused) {
          a.pause();
          const otherId = a.id;
          const otherIcon = document.getElementById('playIcon_' + otherId);
          const otherWaveform = document.querySelector(`#${otherId}`)?.closest('.whatsapp-audio-player')?.querySelector('.audio-waveform');
          if (otherIcon) otherIcon.textContent = '▶️';
          if (otherWaveform) otherWaveform.style.animationPlayState = 'paused';
        }
      });
      
      // Reproduzir áudio atual
      audio.play().then(() => {
        if (playIcon) playIcon.textContent = '⏸️';
        if (waveform) waveform.style.animationPlayState = 'running';
        updateAudioProgress(audioId);
      }).catch(e => {
        console.error('Erro ao reproduzir áudio:', e);
        if (playIcon) playIcon.textContent = '▶️';
      });
    } else {
      // Pausar áudio atual
      audio.pause();
      if (playIcon) playIcon.textContent = '▶️';
      if (waveform) waveform.style.animationPlayState = 'paused';
    }
  };

  // Função para atualizar progresso do áudio - GLOBAL
  window.updateAudioProgress = function(audioId) {
    const audio = document.getElementById(audioId);
    const progress = document.getElementById('progress_' + audioId);
    const currentTime = document.getElementById('currentTime_' + audioId);
    const duration = document.getElementById('duration_' + audioId);
    
    if (!audio || !progress) return;
    
    // Atualizar duração quando carregada
    if (audio.duration && duration && duration.textContent === '0:00') {
      duration.textContent = formatTime(audio.duration);
    }
    
    // Atualizar progresso
    const percent = (audio.currentTime / audio.duration) * 100;
    progress.style.width = percent + '%';
    
    // Atualizar tempo atual
    if (currentTime) {
      currentTime.textContent = formatTime(audio.currentTime);
    }
    
    // Continuar atualizando se estiver tocando
    if (!audio.paused) {
      requestAnimationFrame(() => updateAudioProgress(audioId));
    }
  };

  // Função para buscar posição no áudio - GLOBAL
  window.seekAudio = function(audioId, event) {
    const audio = document.getElementById(audioId);
    const progressBar = event.currentTarget;
    const rect = progressBar.getBoundingClientRect();
    const percent = (event.clientX - rect.left) / rect.width;
    
    if (audio && audio.duration) {
      audio.currentTime = percent * audio.duration;
      updateAudioProgress(audioId);
    }
  };

  // Função para formatar tempo - GLOBAL
  window.formatTime = function(seconds) {
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return mins + ':' + (secs < 10 ? '0' : '') + secs;
  };
  
  console.log('🚀 MISSION-CONTACTS-KANBAN v202510212026 CARREGADO!');
  console.log('🔥 VERSÃO NOVA CARREGADA - SERVER-SENT EVENTS ATIVO!');

  // Função para obter token de autenticação
  function getToken() {
    return localStorage.getItem('token') || sessionStorage.getItem('token') || '';
  }

  // Função para mapear status para IDs de coluna
  function mapStatusToColumnId(status) {
    let columnId = status.toLowerCase().replace(/\s+/g, '-');
    
    // Correções específicas para acentos e caracteres especiais
    const statusMap = {
      'não-interessado': 'nao-interessado',
      'não-interessada': 'nao-interessado',
      'nao-interessado': 'nao-interessado',
      'nao-interessada': 'nao-interessado'
    };
    
    return statusMap[columnId] || columnId;
  }

  // Função para mostrar notificações
  function showNotification(message, type = 'info') {
    // Remover notificação anterior se existir
    const existing = document.querySelector('.notification');
    if (existing) existing.remove();
    
    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;
    notification.innerHTML = `
      <div class="notification-content">
        <span class="notification-icon">${type === 'success' ? '✅' : type === 'error' ? '❌' : 'ℹ️'}</span>
        <span class="notification-message">${message}</span>
        <button class="notification-close" onclick="this.parentElement.parentElement.remove()">×</button>
      </div>
    `;
    
    // Adicionar estilos se não existirem
    if (!document.querySelector('#notification-styles')) {
      const styles = document.createElement('style');
      styles.id = 'notification-styles';
      styles.textContent = `
        .notification {
          position: fixed;
          top: 20px;
          right: 20px;
          z-index: 10000;
          max-width: 400px;
          background: var(--card);
          border: 1px solid var(--stroke);
          border-radius: 12px;
          box-shadow: 0 8px 32px rgba(0, 0, 0, 0.3);
          backdrop-filter: blur(20px);
          animation: slideInRight 0.3s ease;
        }
        
        .notification-success {
          border-left: 4px solid var(--primary);
        }
        
        .notification-error {
          border-left: 4px solid #ef4444;
        }
        
        .notification-content {
          display: flex;
          align-items: center;
          padding: 16px;
          gap: 12px;
        }
        
        .notification-icon {
          font-size: 20px;
        }
        
        .notification-message {
          flex: 1;
          color: var(--txt);
          font-weight: 500;
        }
        
        .notification-close {
          background: none;
          border: none;
          color: var(--muted);
          font-size: 20px;
          cursor: pointer;
          padding: 4px;
          border-radius: 4px;
          transition: all 0.2s ease;
        }
        
        .notification-close:hover {
          background: var(--panel);
          color: var(--txt);
        }
        
        @keyframes slideInRight {
          from {
            transform: translateX(100%);
            opacity: 0;
          }
          to {
            transform: translateX(0);
            opacity: 1;
          }
        }
      `;
      document.head.appendChild(styles);
    }
    
    document.body.appendChild(notification);
    
    // Auto-remover após 5 segundos
    setTimeout(() => {
      if (notification.parentElement) {
        notification.style.animation = 'slideInRight 0.3s ease reverse';
        setTimeout(() => notification.remove(), 300);
      }
    }, 5000);
  }

  // Função para requisições autenticadas (igual ao admin.js)
  async function authFetch(path, init = {}) {
    // Buscar token dinamicamente
    const currentToken = sessionStorage.getItem("token") || localStorage.getItem("token") || "";
    
    const res = await fetch(path, {
      ...init,
      headers: {
        "Content-Type": "application/json",
        Authorization: "Bearer " + currentToken,
        ...(init.headers || {}),
      },
    });
    if (res.status === 401) {
      sessionStorage.removeItem("token");
      localStorage.removeItem("token");
      window.location.href = "/admin/login.html";
      throw new Error("401");
    }
    return res;
  }

  // Carregar dados iniciais
  async function loadData(){
    // Carregar observações salvas
    loadObservations();
    
    await Promise.all([
      loadContacts(),
      loadRegions(),
      loadLeaders()
    ]);
    renderKanban();
  }
  
  // Carregar observações do localStorage (fallback)
  function loadObservations() {
    try {
      const saved = localStorage.getItem('mission_observations');
      if (saved) {
        observations = JSON.parse(saved);
      }
    } catch (err) {
      console.error('Erro ao carregar observações:', err);
      observations = {};
    }
  }

  // Carregar observações de um contato específico da API
  async function loadContactObservations(contactId) {
    try {
      const res = await authFetch(`/api/mission-contacts/${contactId}/observations`);
      if (res.ok) {
        const apiObservations = await res.json();
        observations[contactId] = apiObservations.map(obs => ({
          type: obs.type,
          title: obs.title,
          content: obs.content,
          date: obs.createdAt,
          author: obs.author
        }));
      }
    } catch (err) {
      console.error('Erro ao carregar observações da API:', err);
    }
  }

  // Gravar histórico de mudança de status
  async function logStatusChange(contactId, fromStatus, toStatus) {
    try {
      // Criar observação de mudança de status
      const statusObservation = {
        type: 'status-change',
        title: 'Mudança de Status',
        content: `Status alterado de "${fromStatus}" para "${toStatus}"`,
        date: new Date().toISOString(),
        author: 'Sistema'
      };

      // Salvar no banco via API
      const res = await authFetch(`/api/mission-contacts/${contactId}/observations`, {
        method: 'POST',
        body: JSON.stringify({
          missionContactId: contactId,
          type: statusObservation.type,
          title: statusObservation.title,
          content: statusObservation.content,
          author: statusObservation.author
        })
      });

      if (res.ok) {
        // Atualizar cache local
        if (!observations[contactId]) {
          observations[contactId] = [];
        }
        observations[contactId].push(statusObservation);
      }
    } catch (err) {
      console.error('Erro ao gravar histórico de mudança de status:', err);
    }
  }

  async function loadContacts(){
    try {
      const res = await authFetch('/api/mission-contacts?pageSize=1000');
      if (!res.ok) return;
      const data = await res.json();
      contacts = data.items || [];
    } catch(err) {
      console.error('Erro ao carregar contatos:', err);
    }
  }

  async function loadRegions(){
    try {
      const res = await authFetch('/admin/regions');
      if (!res.ok) return;
      regions = await res.json();
      
      // Atualizar filtro de região
      const select = document.getElementById('regionFilter');
      if (select) {
        select.innerHTML = '<option value="">Todas as regiões</option>' + 
          regions.map(r => `<option value="${r.id}">${esc(r.name)}</option>`).join('');
      }
      
      // Atualizar select de região no formulário
      const formSelect = document.getElementById('ncRegion');
      if (formSelect) {
        formSelect.innerHTML = '<option value="">Selecione uma região...</option>' + 
          regions.map(r => `<option value="${r.id}">${esc(r.name)}</option>`).join('');
      }
    } catch(err) {
      console.error('Erro ao carregar regiões:', err);
    }
  }

  async function loadLeaders(){
    try {
      const res = await authFetch('/admin/leaders');
      if (!res.ok) return;
      leaders = await res.json();
      
      // Atualizar filtro de líder
      const select = document.getElementById('leaderFilter');
      if (select) {
        select.innerHTML = '<option value="">Todos os líderes</option>' + 
          leaders.map(l => `<option value="${l.id}">${esc(l.fullName || l.email)}</option>`).join('');
      }
      
      // Atualizar select de líder no formulário
      const formSelect = document.getElementById('ncLeader');
      if (formSelect) {
        formSelect.innerHTML = '<option value="">Selecione um líder...</option>' + 
          leaders.map(l => `<option value="${l.id}">${esc(l.fullName || l.email)}</option>`).join('');
      }
    } catch(err) {
      console.error('Erro ao carregar líderes:', err);
    }
  }

  function renderKanban(){
    console.log('Renderizando Kanban com', contacts.length, 'contatos');
    
    // Limpar todas as colunas
    document.querySelectorAll('.kanban-cards').forEach(col => col.innerHTML = '');
    
    // Agrupar contatos por status
    const grouped = {};
    contacts.forEach(contact => {
      const status = contact.status || 'Novo';
      if (!grouped[status]) grouped[status] = [];
      grouped[status].push(contact);
    });

    console.log('Contatos agrupados por status:', grouped);

    // Primeiro, zerar todos os contadores
    document.querySelectorAll('.kanban-count').forEach(counter => {
      counter.textContent = '0';
    });

    // Renderizar cada coluna
    Object.keys(grouped).forEach(status => {
      const columnId = mapStatusToColumnId(status);
      const container = document.getElementById(`cards-${columnId}`);
      const counter = document.getElementById(`count-${columnId}`);
      
      console.log(`Status: ${status} -> ColumnId: ${columnId}, Contatos: ${grouped[status].length}`);
      
      if (container) {
        container.innerHTML = grouped[status].map(contact => createContactCard(contact)).join('');
        if (counter) counter.textContent = grouped[status].length;
        console.log(`Coluna ${columnId} atualizada com ${grouped[status].length} contatos`);
      } else {
        console.warn(`Container não encontrado para status: ${status} (columnId: ${columnId})`);
      }
    });
    
    // Reconfigurar drag and drop após renderizar
    setupDragAndDrop();
  }

  function createContactCard(contact){
    const statusClass = mapStatusToColumnId(contact.status || 'novo');
    
    return `
      <div class="contact-card status-${statusClass}" data-id="${contact.id}" draggable="true">
        <div class="contact-name">${esc(contact.fullName)}</div>
        ${contact.phone ? `<div class="contact-info contact-phone">📞 ${esc(contact.phone)}</div>` : ''}
        ${contact.email ? `<div class="contact-info contact-email">✉️ ${esc(contact.email)}</div>` : ''}
        ${contact.city ? `<div class="contact-info">🏙️ ${esc(contact.city)}</div>` : ''}
        ${contact.maritalStatus ? `<div class="contact-info">💍 ${esc(contact.maritalStatus)}</div>` : ''}
        ${contact.regionName ? `<div class="contact-info">🌍 ${esc(contact.regionName)}</div>` : ''}
        <div class="contact-actions">
          <button class="btn btn-sm" onclick="viewContact(${contact.id})">Ver</button>
          <button class="btn btn-sm" onclick="sendWhatsApp(${contact.id})">📱 WhatsApp</button>
        </div>
      </div>`;
  }

  // Drag and Drop
  function setupDragAndDrop(){
    // Adicionar listeners para cards
    document.querySelectorAll('.contact-card').forEach(card => {
      card.addEventListener('dragstart', handleDragStart);
      card.addEventListener('dragend', handleDragEnd);
    });

    // Adicionar listeners para colunas (apenas uma vez)
    document.querySelectorAll('.kanban-column').forEach(column => {
      column.removeEventListener('dragover', handleDragOver);
      column.removeEventListener('drop', handleDrop);
      column.addEventListener('dragover', handleDragOver);
      column.addEventListener('drop', handleDrop);
    });
  }

  function handleDragStart(e){
    e.target.classList.add('dragging');
    e.dataTransfer.setData('text/plain', e.target.dataset.id);
  }

  function handleDragEnd(e){
    e.target.classList.remove('dragging');
  }

  function handleDragOver(e){
    e.preventDefault();
  }

  async function handleDrop(e){
    e.preventDefault();
    const contactId = e.dataTransfer.getData('text/plain');
    const newStatus = e.currentTarget.dataset.status;
    
    try {
      const res = await authFetch(`/api/mission-contacts/${contactId}/status`, {
        method: 'PUT',
        body: JSON.stringify({ status: newStatus })
      });
      
      if (res.ok) {
        // Atualizar localmente
        const contact = contacts.find(c => c.id == contactId);
        if (contact) {
          const oldStatus = contact.status;
          contact.status = newStatus;
          
          // Gravar histórico de mudança de status
          await logStatusChange(contactId, oldStatus, newStatus);
          
          // Atualizar visual
          renderKanban();
          setupDragAndDrop();
          
          // Se o modal estiver aberto para este contato, atualizar timeline
          if (currentContactId == contactId) {
            await loadContactObservations(contactId);
            renderTimeline(contact);
          }
        }
      }
    } catch(err) {
      console.error('Erro ao atualizar status:', err);
    }
  }

  // Funções globais para os botões
  window.viewContact = async function(id){
    const contact = contacts.find(c => c.id == id);
    if (!contact) return;
    
    currentContactId = id;
    isEditing = false;
    
    const modal = document.getElementById('contactModal');
    const editActions = document.getElementById('editActions');
    const editBtn = document.getElementById('editContactBtn');
    
    // Mostrar botão de editar, esconder ações de edição
    editBtn.style.display = 'block';
    editActions.style.display = 'none';
    
    // Atualizar o header do modal com dados reais do contato
    const contactTitle = document.getElementById('contactTitle');
    const contactSubtitle = document.getElementById('contactSubtitle');
    const contactStatusModern = document.getElementById('contactStatusModern');
    
    if (contactTitle) {
      contactTitle.textContent = contact.fullName || 'Sem nome';
    }
    
    if (contactSubtitle) {
      contactSubtitle.textContent = contact.phone || 'Sem telefone';
    }
    
    if (contactStatusModern) {
      // Por enquanto, sempre "online" (pode ser melhorado depois com status real)
      const statusText = contactStatusModern.querySelector('.status-text');
      if (statusText) {
        statusText.textContent = 'Online agora';
      }
    }
    
    renderContactInfo(contact, false);
    
    // Carregar observações da API e renderizar timeline
    await loadContactObservations(contact.id);
    renderTimeline(contact);
    
    modal.setAttribute('aria-hidden', 'false');
    
    // Configurar event listeners do chat quando o modal for aberto
    setTimeout(() => {
      setupChatEventListeners();
    }, 100);
  };

  // Renderizar informações do contato (modo visualização ou edição) - DESIGN MODERNO
  function renderContactInfo(contact, editing = false) {
    const contactInfoGrid = document.getElementById('contactInfoGrid');
    
    if (editing) {
      contactInfoGrid.innerHTML = `
        <div class="info-card-modern">
          <h4>👤 Dados Pessoais</h4>
          <div class="info-item-modern">
            <div class="info-label-modern">Nome completo</div>
            <div class="info-value-modern">
              <input type="text" id="editFullName" value="${esc(contact.fullName)}" style="background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1); border-radius: 8px; padding: 8px 12px; color: #fff; width: 200px;" />
            </div>
          </div>
          <div class="info-item-modern">
            <div class="info-label-modern">Gênero</div>
            <div class="info-value-modern">
              <select id="editGender" style="background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1); border-radius: 8px; padding: 8px 12px; color: #fff; width: 200px;">
                <option value="">Selecione...</option>
                <option value="Masculino" ${contact.gender === 'Masculino' ? 'selected' : ''}>Masculino</option>
                <option value="Feminino" ${contact.gender === 'Feminino' ? 'selected' : ''}>Feminino</option>
                <option value="Outro" ${contact.gender === 'Outro' ? 'selected' : ''}>Outro</option>
              </select>
            </div>
          </div>
          <div class="info-item-modern">
            <div class="info-label-modern">Data de nascimento</div>
            <div class="info-value-modern">
              <input type="date" id="editBirthDate" value="${contact.birthDate ? contact.birthDate.split('T')[0] : ''}" style="background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1); border-radius: 8px; padding: 8px 12px; color: #fff; width: 200px;" />
            </div>
          </div>
          <div class="info-item-modern">
            <div class="info-label-modern">Estado civil</div>
            <div class="info-value-modern">
              <select id="editMaritalStatus" style="background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1); border-radius: 8px; padding: 8px 12px; color: #fff; width: 200px;">
                <option value="">Selecione...</option>
                <option value="Solteiro(a)" ${contact.maritalStatus === 'Solteiro(a)' ? 'selected' : ''}>Solteiro(a)</option>
                <option value="Casado(a)" ${contact.maritalStatus === 'Casado(a)' ? 'selected' : ''}>Casado(a)</option>
                <option value="Divorciado(a)" ${contact.maritalStatus === 'Divorciado(a)' ? 'selected' : ''}>Divorciado(a)</option>
                <option value="Viúvo(a)" ${contact.maritalStatus === 'Viúvo(a)' ? 'selected' : ''}>Viúvo(a)</option>
              </select>
            </div>
          </div>
        </div>
        
        <div class="info-card-modern">
          <h4>🌍 Localização</h4>
          <div class="info-item-modern">
            <div class="info-label-modern">Nacionalidade</div>
            <div class="info-value-modern">
              <input type="text" id="editNationality" value="${esc(contact.nationality || '')}" style="background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1); border-radius: 8px; padding: 8px 12px; color: #fff; width: 200px;" />
            </div>
          </div>
          <div class="info-item-modern">
            <div class="info-label-modern">Cidade</div>
            <div class="info-value-modern">
              <input type="text" id="editCity" value="${esc(contact.city || '')}" style="background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1); border-radius: 8px; padding: 8px 12px; color: #fff; width: 200px;" />
            </div>
          </div>
          <div class="info-item-modern">
            <div class="info-label-modern">Estado</div>
            <div class="info-value-modern">
              <input type="text" id="editState" value="${esc(contact.state || '')}" style="background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1); border-radius: 8px; padding: 8px 12px; color: #fff; width: 200px;" />
            </div>
          </div>
        </div>
        
        <div class="info-card-modern">
          <h4>📞 Contato</h4>
          <div class="info-item-modern">
            <div class="info-label-modern">Telefone</div>
            <div class="info-value-modern">
              <input type="tel" id="editPhone" value="${esc(contact.phone || '')}" style="background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1); border-radius: 8px; padding: 8px 12px; color: #fff; width: 200px;" />
            </div>
          </div>
          <div class="info-item-modern">
            <div class="info-label-modern">E-mail</div>
            <div class="info-value-modern">
              <input type="email" id="editEmail" value="${esc(contact.email || '')}" style="background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1); border-radius: 8px; padding: 8px 12px; color: #fff; width: 200px;" />
            </div>
          </div>
        </div>
        
        <div class="info-card-modern">
          <h4>💼 Profissional</h4>
          <div class="info-item-modern">
            <div class="info-label-modern">Profissão</div>
            <div class="info-value-modern">
              <input type="text" id="editProfession" value="${esc(contact.profession || '')}" style="background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1); border-radius: 8px; padding: 8px 12px; color: #fff; width: 200px;" />
            </div>
          </div>
          <div class="info-item-modern">
            <div class="info-label-modern">Outros idiomas</div>
            <div class="info-value-modern">
              <input type="text" id="editOtherLanguages" value="${esc(contact.otherLanguages || '')}" style="background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1); border-radius: 8px; padding: 8px 12px; color: #fff; width: 200px;" />
            </div>
          </div>
          <div class="info-item-modern">
            <div class="info-label-modern">Nível de fluência</div>
            <div class="info-value-modern">
              <select id="editFluencyLevel" style="background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1); border-radius: 8px; padding: 8px 12px; color: #fff; width: 200px;">
                <option value="">Selecione...</option>
                <option value="Básico" ${contact.fluencyLevel === 'Básico' ? 'selected' : ''}>Básico</option>
                <option value="Intermediário" ${contact.fluencyLevel === 'Intermediário' ? 'selected' : ''}>Intermediário</option>
                <option value="Avançado" ${contact.fluencyLevel === 'Avançado' ? 'selected' : ''}>Avançado</option>
                <option value="Fluente" ${contact.fluencyLevel === 'Fluente' ? 'selected' : ''}>Fluente</option>
              </select>
            </div>
          </div>
        </div>
        
        <div class="info-card-modern">
          <h4>⛪ Espiritual</h4>
          <div class="info-item-modern">
            <div class="info-label-modern">Igreja</div>
            <div class="info-value-modern">
              <input type="text" id="editChurch" value="${esc(contact.church || '')}" style="background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1); border-radius: 8px; padding: 8px 12px; color: #fff; width: 200px;" />
            </div>
          </div>
          <div class="info-item-modern">
            <div class="info-label-modern">Tempo de conversão</div>
            <div class="info-value-modern">
              <input type="text" id="editConversionTime" value="${esc(contact.conversionTime || '')}" style="background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1); border-radius: 8px; padding: 8px 12px; color: #fff; width: 200px;" />
            </div>
          </div>
          <div class="info-item-modern">
            <div class="info-label-modern">Plano de dedicação missionária</div>
            <div class="info-value-modern">
              <textarea id="editMissionsDedicationPlan" rows="3" style="background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1); border-radius: 8px; padding: 8px 12px; color: #fff; width: 100%; resize: vertical;">${esc(contact.missionsDedicationPlan || '')}</textarea>
            </div>
          </div>
        </div>
        
        <div class="info-card-modern">
          <h4>✈️ Missão</h4>
          <div class="info-item-modern">
            <div class="info-label-modern">Tem passaporte</div>
            <div class="info-value-modern">
              <select id="editHasPassport" style="background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1); border-radius: 8px; padding: 8px 12px; color: #fff; width: 200px;">
                <option value="false" ${!contact.hasPassport ? 'selected' : ''}>Não</option>
                <option value="true" ${contact.hasPassport ? 'selected' : ''}>Sim</option>
              </select>
            </div>
          </div>
          <div class="info-item-modern">
            <div class="info-label-modern">Data disponível</div>
            <div class="info-value-modern">
              <input type="date" id="editAvailableDate" value="${contact.availableDate ? contact.availableDate.split('T')[0] : ''}" style="background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1); border-radius: 8px; padding: 8px 12px; color: #fff; width: 200px;" />
            </div>
          </div>
          <div class="info-item-modern">
            <div class="info-label-modern">Região</div>
            <div class="info-value-modern">
              <select id="editRegionId" style="background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1); border-radius: 8px; padding: 8px 12px; color: #fff; width: 200px;">
                <option value="">Selecione uma região...</option>
                ${regions.map(r => `<option value="${r.id}" ${contact.regionId == r.id ? 'selected' : ''}>${esc(r.name)}</option>`).join('')}
              </select>
            </div>
          </div>
          <div class="info-item-modern">
            <div class="info-label-modern">Líder</div>
            <div class="info-value-modern">
              <select id="editLeaderId" style="background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1); border-radius: 8px; padding: 8px 12px; color: #fff; width: 200px;">
                <option value="">Selecione um líder...</option>
                ${leaders.map(l => `<option value="${l.id}" ${contact.leaderId == l.id ? 'selected' : ''}>${esc(l.fullName || l.email)}</option>`).join('')}
              </select>
            </div>
          </div>
        </div>
      `;
    } else {
      contactInfoGrid.innerHTML = `
        <div class="info-card-modern">
          <h4>👤 Dados Pessoais</h4>
          <div class="info-item-modern">
            <div class="info-label-modern">Nome completo</div>
            <div class="info-value-modern">${esc(contact.fullName)}</div>
          </div>
          <div class="info-item-modern">
            <div class="info-label-modern">Gênero</div>
            <div class="info-value-modern">${contact.gender ? esc(contact.gender) : '—'}</div>
          </div>
          <div class="info-item-modern">
            <div class="info-label-modern">Idade</div>
            <div class="info-value-modern">${contact.birthDate ? Math.floor((new Date() - new Date(contact.birthDate)) / (365.25 * 24 * 60 * 60 * 1000)) + ' anos' : '—'}</div>
          </div>
          <div class="info-item-modern">
            <div class="info-label-modern">Estado civil</div>
            <div class="info-value-modern">${contact.maritalStatus ? esc(contact.maritalStatus) : '—'}</div>
          </div>
          <div class="info-item-modern">
            <div class="info-label-modern">Data de nascimento</div>
            <div class="info-value-modern">${contact.birthDate ? new Date(contact.birthDate).toLocaleDateString('pt-BR') : '—'}</div>
          </div>
        </div>
        
        <div class="info-card-modern">
          <h4>🌍 Localização</h4>
          <div class="info-item-modern">
            <div class="info-label-modern">Nacionalidade</div>
            <div class="info-value-modern">${contact.nationality ? esc(contact.nationality) : '—'}</div>
          </div>
          <div class="info-item-modern">
            <div class="info-label-modern">Cidade</div>
            <div class="info-value-modern">${contact.city ? esc(contact.city) : '—'}</div>
          </div>
          <div class="info-item-modern">
            <div class="info-label-modern">Estado</div>
            <div class="info-value-modern">${contact.state ? esc(contact.state) : '—'}</div>
          </div>
        </div>
        
        <div class="info-card-modern">
          <h4>📞 Contato</h4>
          <div class="info-item-modern">
            <div class="info-label-modern">Telefone</div>
            <div class="info-value-modern">${contact.phone ? esc(contact.phone) : '—'}</div>
          </div>
          <div class="info-item-modern">
            <div class="info-label-modern">E-mail</div>
            <div class="info-value-modern">${contact.email ? esc(contact.email) : '—'}</div>
          </div>
        </div>
        
        <div class="info-card-modern">
          <h4>💼 Profissional</h4>
          <div class="info-item-modern">
            <div class="info-label-modern">Profissão</div>
            <div class="info-value-modern">${contact.profession ? esc(contact.profession) : '—'}</div>
          </div>
          <div class="info-item-modern">
            <div class="info-label-modern">Outros idiomas</div>
            <div class="info-value-modern">${contact.otherLanguages ? esc(contact.otherLanguages) : '—'}</div>
          </div>
          <div class="info-item-modern">
            <div class="info-label-modern">Nível de fluência</div>
            <div class="info-value-modern">${contact.fluencyLevel ? esc(contact.fluencyLevel) : '—'}</div>
          </div>
        </div>
        
        <div class="info-card-modern">
          <h4>⛪ Espiritual</h4>
          <div class="info-item-modern">
            <div class="info-label-modern">Igreja</div>
            <div class="info-value-modern">${contact.church ? esc(contact.church) : '—'}</div>
          </div>
          <div class="info-item-modern">
            <div class="info-label-modern">Tempo de conversão</div>
            <div class="info-value-modern">${contact.conversionTime ? esc(contact.conversionTime) : '—'}</div>
          </div>
          <div class="info-item-modern">
            <div class="info-label-modern">Plano de dedicação missionária</div>
            <div class="info-value-modern">${contact.missionsDedicationPlan ? esc(contact.missionsDedicationPlan) : '—'}</div>
          </div>
        </div>
        
        <div class="info-card-modern">
          <h4>✈️ Missão</h4>
          <div class="info-item-modern">
            <div class="info-label-modern">Tem passaporte</div>
            <div class="info-value-modern">${contact.hasPassport ? '✅ Sim' : '❌ Não'}</div>
          </div>
          <div class="info-item-modern">
            <div class="info-label-modern">Data disponível</div>
            <div class="info-value-modern">${contact.availableDate ? new Date(contact.availableDate).toLocaleDateString('pt-BR') : '—'}</div>
          </div>
          <div class="info-item-modern">
            <div class="info-label-modern">Região</div>
            <div class="info-value-modern">${contact.regionName ? esc(contact.regionName) : '—'}</div>
          </div>
          <div class="info-item-modern">
            <div class="info-label-modern">Líder</div>
            <div class="info-value-modern">${contact.leaderName ? esc(contact.leaderName) : '—'}</div>
          </div>
        </div>
      `;
    }
  }

  // Função para renderizar timeline
  function renderTimeline(contact) {
    const timelineContainer = document.getElementById('timelineContainer');
    
    // Dados de timeline
    const timelineData = [
      {
        type: 'system',
        title: 'Contato criado',
        content: `Contato criado em ${new Date(contact.createdAt).toLocaleDateString('pt-BR')} às ${new Date(contact.createdAt).toLocaleTimeString('pt-BR')}`,
        date: contact.createdAt,
        author: 'Sistema'
      },
      {
        type: 'status-change',
        title: 'Status atual',
        content: `Status definido como "${contact.status}"`,
        date: contact.updatedAt || contact.createdAt,
        author: 'Sistema'
      }
    ];
    
    // Adicionar observações se existirem
    if (contact.notes) {
      timelineData.push({
        type: 'observation',
        title: 'Observação inicial',
        content: contact.notes,
        date: contact.createdAt,
        author: 'Sistema'
      });
    }
    
    // Adicionar observações salvas localmente
    if (observations[contact.id]) {
      observations[contact.id].forEach(obs => {
        timelineData.push(obs);
      });
    }
    
    // Ordenar por data (mais recente primeiro)
    timelineData.sort((a, b) => new Date(b.date) - new Date(a.date));
    
    if (timelineData.length === 0) {
      timelineContainer.innerHTML = '<div class="empty-timeline">Nenhuma atividade registrada</div>';
      return;
    }
    
    timelineContainer.innerHTML = timelineData.map(item => `
      <div class="timeline-item-modern">
        <div class="timeline-icon-modern">
          ${item.type === 'system' ? '⚙️' : 
            item.type === 'status-change' ? '🔄' : 
            item.type === 'observation' ? '📝' : 
            item.type === 'call' ? '📞' : 
            item.type === 'whatsapp' ? '📱' : 
            item.type === 'meeting' ? '🤝' : '📋'}
        </div>
        <div class="timeline-content-modern">
          <h5>${esc(item.title)}</h5>
          <p>${esc(item.content)}</p>
          <div class="timeline-date-modern">
            ${esc(item.author)} • ${new Date(item.date).toLocaleDateString('pt-BR')} às ${new Date(item.date).toLocaleTimeString('pt-BR')}
          </div>
        </div>
      </div>
    `).join('');
  }
  
  function getTypeLabel(type) {
    const labels = {
      'system': 'Sistema',
      'status-change': 'Mudança de Status',
      'observation': 'Observação',
      'call': 'Ligação',
      'whatsapp': 'WhatsApp',
      'meeting': 'Reunião',
      'other': 'Outro'
    };
    return labels[type] || 'Atividade';
  }


  window.sendWhatsApp = async function(id){
    console.log('🚀 Botão WhatsApp clicado para ID:', id);
    // Abrir modal de contato e ir direto para a aba WhatsApp
    currentContactId = id;
    const contact = contacts.find(c => c.id === id);
    console.log('👤 Contato encontrado:', contact);
    
    if (contact) {
      console.log('📱 Abrindo modal de chat...');
      // Usar a função correta para abrir o modal
      await viewContact(id);
      // Aguardar um pouco para o modal carregar e depois trocar para a aba WhatsApp
      setTimeout(() => {
        console.log('🔄 Procurando aba WhatsApp...');
        const whatsappTab = document.querySelector('[data-tab="whatsapp"]');
        if (whatsappTab) {
          console.log('✅ Clicando na aba WhatsApp');
          whatsappTab.click();
        } else {
          console.log('❌ Aba WhatsApp não encontrada');
        }
      }, 200);
    } else {
      console.log('❌ Contato não encontrado');
    }
  };

  // Event listeners
  document.getElementById('backBtn')?.addEventListener('click', () => {
    window.location.href = '/admin/dashboard.html';
  });

  document.getElementById('newContactBtn')?.addEventListener('click', () => {
    document.getElementById('newContactModal').setAttribute('aria-hidden', 'false');
  });

  document.getElementById('closeContactModal')?.addEventListener('click', () => {
    document.getElementById('contactModal').setAttribute('aria-hidden', 'true');
  });

  document.getElementById('closeNewContactModal')?.addEventListener('click', () => {
    document.getElementById('newContactModal').setAttribute('aria-hidden', 'true');
  });
  

  // Event listeners para observações
  document.getElementById('addObservationBtn')?.addEventListener('click', () => {
    document.getElementById('observationModal').style.display = 'flex';
  });

  document.getElementById('closeObservationModal')?.addEventListener('click', () => {
    document.getElementById('observationModal').style.display = 'none';
  });

  // Parar polling quando o modal de contato for fechado
  document.getElementById('closeContactModal')?.addEventListener('click', () => {
    stopMessagePolling();
  });

  document.getElementById('cancelObservation')?.addEventListener('click', () => {
    document.getElementById('observationModal').style.display = 'none';
  });

  // Formulário de observação
  document.getElementById('observationForm')?.addEventListener('submit', async (e) => {
    e.preventDefault();
    
    const type = document.getElementById('observationType').value;
    const text = document.getElementById('observationText').value.trim();
    
    if (!currentContactId) {
      alert('Erro: ID do contato não encontrado');
      return;
    }
    
    if (!text) {
      alert('Por favor, descreva a observação');
      return;
    }
    
    try {
      // Salvar observação na API
      const res = await authFetch(`/api/mission-contacts/${currentContactId}/observations`, {
        method: 'POST',
        body: JSON.stringify({
          missionContactId: currentContactId,
          type: type,
          title: getTypeLabel(type),
          content: text,
          author: 'Usuário'
        })
      });
      
      if (res.ok) {
        // Fechar modal
        document.getElementById('observationModal').style.display = 'none';
        
        // Limpar formulário
        document.getElementById('observationForm').reset();
        
        // Recarregar observações e timeline
        await loadContactObservations(currentContactId);
        const contact = contacts.find(c => c.id == currentContactId);
        if (contact) {
          renderTimeline(contact);
        }
        
        // Mostrar feedback visual
        const submitBtn = document.querySelector('#observationForm button[type="submit"]');
        const originalText = submitBtn.textContent;
        submitBtn.textContent = '✓ Salvo!';
        submitBtn.style.background = 'var(--primary)';
        
        setTimeout(() => {
          submitBtn.textContent = originalText;
          submitBtn.style.background = '';
        }, 2000);
      } else {
        throw new Error('Erro ao salvar observação na API');
      }
      
    } catch (err) {
      console.error('Erro ao salvar observação:', err);
      alert('Erro ao salvar observação: ' + err.message);
    }
  });

  // Funções de edição
  window.editContact = function() {
    if (!currentContactId) return;
    
    const contact = contacts.find(c => c.id == currentContactId);
    if (!contact) return;
    
    isEditing = true;
    originalContactData = { ...contact };
    
    const editActions = document.getElementById('editActions');
    const editBtn = document.getElementById('editContactBtn');
    
    // Esconder botão de editar, mostrar ações de edição
    editBtn.style.display = 'none';
    editActions.style.display = 'flex';
    
    // Renderizar em modo de edição
    renderContactInfo(contact, true);
  };

  window.saveContact = async function() {
    if (!currentContactId) return;
    
    try {
      const contact = contacts.find(c => c.id == currentContactId);
      if (!contact) return;
      
      // Coletar dados do formulário
      const updateData = {
        fullName: document.getElementById('editFullName').value,
        gender: document.getElementById('editGender').value,
        birthDate: document.getElementById('editBirthDate').value ? new Date(document.getElementById('editBirthDate').value).toISOString() : null,
        maritalStatus: document.getElementById('editMaritalStatus').value,
        nationality: document.getElementById('editNationality').value,
        city: document.getElementById('editCity').value,
        state: document.getElementById('editState').value,
        phone: document.getElementById('editPhone').value,
        email: document.getElementById('editEmail').value,
        profession: document.getElementById('editProfession').value,
        speaksOtherLanguages: document.getElementById('editOtherLanguages').value ? true : false,
        otherLanguages: document.getElementById('editOtherLanguages').value,
        fluencyLevel: document.getElementById('editFluencyLevel').value,
        church: document.getElementById('editChurch').value,
        conversionTime: document.getElementById('editConversionTime').value,
        missionsDedicationPlan: document.getElementById('editMissionsDedicationPlan').value,
        hasPassport: document.getElementById('editHasPassport').value === 'true',
        availableDate: document.getElementById('editAvailableDate').value ? new Date(document.getElementById('editAvailableDate').value).toISOString() : null,
        regionId: document.getElementById('editRegionId').value ? parseInt(document.getElementById('editRegionId').value) : null,
        leaderId: document.getElementById('editLeaderId').value ? parseInt(document.getElementById('editLeaderId').value) : null
      };
      
      // Debug: mostrar dados que serão enviados
      console.log('Dados para atualização:', updateData);
      
      // Enviar para API
      const res = await authFetch(`/api/mission-contacts/${currentContactId}`, {
        method: 'PUT',
        body: JSON.stringify(updateData)
      });
      
      if (res.ok) {
        // Obter dados atualizados da resposta da API
        const updatedContact = await res.json();
        
        // Atualizar contato local com dados completos da API
        const contactIndex = contacts.findIndex(c => c.id == currentContactId);
        if (contactIndex !== -1) {
          contacts[contactIndex] = updatedContact;
        }
        
        // Limpar dados originais antes de sair do modo de edição
        originalContactData = null;
        
        // Sair do modo de edição
        isEditing = false;
        const editActions = document.getElementById('editActions');
        const editBtn = document.getElementById('editContactBtn');
        editBtn.style.display = 'block';
        editActions.style.display = 'none';
        
        // Atualizar Kanban com dados atualizados
        renderKanban();
        setupDragAndDrop();
        
        // Atualizar modal com dados atualizados
        renderContactInfo(updatedContact, false);
        
        // Mostrar feedback visual
        showNotification('Contato atualizado com sucesso!', 'success');
      } else {
        throw new Error('Erro ao atualizar contato');
      }
    } catch (err) {
      console.error('Erro ao salvar contato:', err);
      alert('Erro ao salvar contato: ' + err.message);
    }
  };

  window.cancelEdit = function() {
    isEditing = false;
    
    const editActions = document.getElementById('editActions');
    const editBtn = document.getElementById('editContactBtn');
    
    // Mostrar botão de editar, esconder ações de edição
    editBtn.style.display = 'block';
    editActions.style.display = 'none';
    
    // Só restaurar dados originais se não foi um salvamento
    if (originalContactData) {
      const contact = contacts.find(c => c.id == currentContactId);
      if (contact) {
        Object.assign(contact, originalContactData);
        renderContactInfo(contact, false);
      }
    }
    
    // Limpar dados originais
    originalContactData = null;
  };

  // Event listeners para edição
  document.getElementById('editContactBtn')?.addEventListener('click', editContact);
  document.getElementById('saveContactBtn')?.addEventListener('click', saveContact);
  document.getElementById('cancelEditBtn')?.addEventListener('click', cancelEdit);

  document.getElementById('refreshBtn')?.addEventListener('click', loadData);

  document.getElementById('searchBtn')?.addEventListener('click', async () => {
    const search = document.getElementById('searchInput').value;
    const region = document.getElementById('regionFilter').value;
    const leader = document.getElementById('leaderFilter').value;
    
    // Filtrar contatos localmente
    let filtered = contacts;
    if (search) {
      filtered = filtered.filter(c => 
        c.fullName?.toLowerCase().includes(search.toLowerCase()) ||
        c.phone?.includes(search) ||
        c.email?.toLowerCase().includes(search.toLowerCase())
      );
    }
    if (region) {
      filtered = filtered.filter(c => c.regionId == region);
    }
    if (leader) {
      filtered = filtered.filter(c => c.leaderId == leader);
    }
    
    contacts = filtered;
    renderKanban();
  });

  document.getElementById('newContactForm')?.addEventListener('submit', async (e) => {
    e.preventDefault();
    
    const body = {
      fullName: document.getElementById('ncFullName').value.trim(),
      gender: document.getElementById('ncGender').value||null,
      birthDate: document.getElementById('ncBirth').value||null,
      maritalStatus: document.getElementById('ncMarital').value||null,
      nationality: document.getElementById('ncNation').value||null,
      city: document.getElementById('ncCity').value||null,
      state: document.getElementById('ncState').value||null,
      phone: document.getElementById('ncPhone').value||null,
      email: document.getElementById('ncEmail').value||null,
      profession: document.getElementById('ncProfession').value||null,
      speaksOtherLanguages: !!(document.getElementById('ncLanguages').value||'').trim(),
      otherLanguages: document.getElementById('ncLanguages').value||null,
      fluencyLevel: document.getElementById('ncFluency').value||null,
      church: document.getElementById('ncChurch').value||null,
      conversionTime: document.getElementById('ncConversion').value||null,
      missionsDedicationPlan: document.getElementById('ncPlan').value||null,
      hasPassport: document.getElementById('ncPassport').value === 'true',
      availableDate: document.getElementById('ncAvailable').value||null,
      regionId: parseInt(document.getElementById('ncRegion').value||'0',10),
      leaderId: (document.getElementById('ncLeader').value||'').trim()? parseInt(document.getElementById('ncLeader').value,10): null
    };
    
    try {
      const res = await authFetch('/api/mission-contacts', {
        method: 'POST',
        body: JSON.stringify(body)
      });
      
      if (res.ok) {
        document.getElementById('newContactModal').setAttribute('aria-hidden', 'true');
        document.getElementById('newContactForm').reset();
        await loadData();
      } else {
        document.getElementById('ncError').hidden = false;
      }
    } catch(err) {
      console.error('Erro ao criar contato:', err);
      document.getElementById('ncError').hidden = false;
    }
  });

  // ===========================
  // WHATSAPP CHAT FUNCTIONALITY
  // ===========================
  
  let chatMessages = {}; // Armazenar mensagens por contato
  let currentChatContactId = null;

  // Função para alternar entre abas
  function switchTab(tabName) {
    // Remover active de todas as abas (classes modernas)
    document.querySelectorAll('.modern-tab').forEach(btn => btn.classList.remove('active'));
    document.querySelectorAll('.tab-content-modern').forEach(content => content.classList.remove('active'));
    
    // Ativar aba selecionada
    document.querySelector(`[data-tab="${tabName}"]`).classList.add('active');
    document.getElementById(`tab-${tabName}`).classList.add('active');
    
    // Se for a aba do WhatsApp, carregar chat
    if (tabName === 'whatsapp' && currentContactId) {
      loadChatMessages(currentContactId);
    }
  }

        // Função para carregar mensagens do chat
        async function loadChatMessages(contactId) {
          currentChatContactId = contactId;
          const contact = contacts.find(c => c.id == contactId);
          if (!contact) return;

          // Atualizar informações do contato no chat
          document.getElementById('chatContactName').textContent = contact.fullName;
          document.getElementById('chatContactPhone').textContent = contact.phone || 'Telefone não informado';
          document.getElementById('chatContactStatus').textContent = '🟢 Online agora';

          try {
            // Garantir que o WhatsApp esteja conectado antes de buscar mensagens
            const status = await checkWhatsAppStatus();
            if (!status || !status.connected) {
              console.log('ℹ️ WhatsApp não está conectado. Abortando carregamento de mensagens.');
              showStatusPanel('Conecte o WhatsApp para carregar as mensagens deste contato.', 'warning');
              chatMessages[contactId] = [];
              renderChatMessages(chatMessages[contactId]);
              return;
            }

            // Carregar mensagens da API usando o número de telefone
            const phoneNumber = contact.phone || contact.cellPhone;
            if (phoneNumber) {
              console.log(`🔍 Carregando mensagens reais para: ${phoneNumber}`);
              // Mostrar indicador "Carregando mensagens..."
              showLoadingMessages();
              const res = await authFetch(`/api/whatsapp/messages/${phoneNumber}?limit=20&includeMedia=true`);
              console.log(`📡 Resposta da API:`, res.status, res.ok);
              
              if (res.ok) {
                try {
                  const data = await res.json();
                  console.log(`✅ Mensagens reais carregadas:`, data);
                  
                  // A API retorna um array direto de mensagens
                  const messages = Array.isArray(data) ? data : [];
                  console.log(`📊 Total de mensagens recebidas:`, messages.length);
                  
                  chatMessages[contactId] = messages.map(msg => ({
                    id: msg.id !== undefined ? String(msg.id) : (msg.Id !== undefined ? String(msg.Id) : ''),
                    sender: msg.sender || msg.Sender || 'contact',
                    content: msg.content || msg.Content || '',
                    timestamp: new Date(msg.timestamp || msg.Timestamp),
                    status: typeof msg.status === 'number' ? ['sending', 'sent', 'delivered', 'read', 'failed'][msg.status] || 'sent' : (msg.status || msg.Status || 'sent'),
                    mediaUrl: msg.mediaUrl || msg.MediaUrl || null,
                    mediaType: msg.mediaType || msg.MediaType || null,
                    hasMedia: !!(msg.hasMedia || msg.MediaUrl || msg.mediaUrl)
                  }));
                  renderChatMessages(chatMessages[contactId]);
                } catch (jsonError) {
                  console.error('💥 Erro ao processar JSON da resposta:', jsonError);
                  console.log('📄 Conteúdo da resposta:', await res.text());
                  chatMessages[contactId] = [];
                }
              } else {
                console.log('❌ Nenhuma mensagem encontrada para este contato');
                chatMessages[contactId] = [];
              }
            } else {
              console.log('❌ Número de telefone não encontrado para este contato');
              chatMessages[contactId] = [];
            }
          } catch (err) {
            console.error('💥 Erro ao carregar mensagens:', err);
            chatMessages[contactId] = [];
          }
          hideLoadingMessages();
          renderChatMessages(chatMessages[contactId]);
          
          // Iniciar polling para mensagens em tempo real
          const currentPhoneNumber = contact.phone || contact.cellPhone;
          if (currentPhoneNumber) {
            startMessageStream(contactId, currentPhoneNumber);
          } else {
            console.log('⚠️ phoneNumber não definido, pulando startMessageStream');
          }
        }

  // Função para iniciar Server-Sent Events para mensagens em tempo real
  function startMessageStream(contactId, phoneNumber) {
    // Parar stream anterior se existir
    if (messageEventSource) {
      messageEventSource.close();
      console.log('🔄 Parando stream anterior');
    }
    
    console.log(`🚀 Iniciando Server-Sent Events para contato ${contactId} com telefone ${phoneNumber}`);
    
    // Criar conexão Server-Sent Events
    messageEventSource = new EventSource(`/api/whatsapp/messages/stream/${phoneNumber}`);
    
    messageEventSource.onopen = function(event) {
      console.log('✅ Conexão SSE estabelecida');
    };
    
        messageEventSource.onmessage = function(event) {
          const startTime = performance.now();
          lastSSEMessageTime = Date.now(); // Atualizar timestamp da última mensagem
          try {
            const data = JSON.parse(event.data);
            console.log('📨 Dados recebidos via SSE:', data);
            console.log('⏱️ Tempo de recebimento SSE:', new Date().toLocaleTimeString());
        
        if (data.type === 'messages_update' && currentChatContactId === contactId) {
          console.log(`🆕 Atualização de mensagens recebida: ${data.messageCount} mensagens`);
          
          // Verificar se há mensagens novas (não duplicar)
          if (data.messages && data.messages.length > 0) {
            const currentMessages = chatMessages[contactId] || [];
            const newMessages = data.messages.map(msg => ({
              id: msg.id !== undefined ? String(msg.id) : '',
              sender: msg.sender || 'contact',
              content: msg.content || '',
              timestamp: new Date(msg.timestamp),
              status: 'sent',
              mediaUrl: msg.mediaUrl || null,
              mediaType: msg.mediaType || null,
              hasMedia: !!(msg.hasMedia || msg.mediaUrl)
            }));
            
            // Verificar se há mensagens realmente novas
            const currentIds = currentMessages.map(m => m.id);
            const newIds = newMessages.map(m => m.id);
            
            console.log(`🔍 IDs atuais (${currentIds.length}):`, currentIds.slice(-5)); // Últimos 5 IDs
            console.log(`🔍 IDs novos (${newIds.length}):`, newIds.slice(-5)); // Últimos 5 IDs
            console.log(`🔍 Estrutura da primeira mensagem atual:`, currentMessages[0]);
            console.log(`🔍 Estrutura da primeira mensagem nova:`, newMessages[0]);
            console.log(`🔍 Dados brutos do SSE:`, data.messages[0]);
            
            // Verificar se há mensagens realmente novas baseado no conteúdo mais recente
            const currentLatestContent = currentMessages.length > 0 ? 
                currentMessages.sort((a, b) => new Date(b.timestamp) - new Date(a.timestamp))[0]?.content : '';
            const newLatestContent = newMessages.length > 0 ? 
                newMessages.sort((a, b) => new Date(b.timestamp) - new Date(a.timestamp))[0]?.content : '';
            
            const hasNewMessages = newLatestContent !== currentLatestContent || 
                                 newIds.some(id => !currentIds.includes(id));
            
            console.log(`🔍 Comparação: Atuais=${currentMessages.length}, Novas=${newMessages.length}`);
            console.log(`🔍 Conteúdo atual mais recente: "${currentLatestContent}"`);
            console.log(`🔍 Conteúdo novo mais recente: "${newLatestContent}"`);
            console.log(`🔍 TemNovas=${hasNewMessages}`);
            
            if (hasNewMessages) {
              const renderStartTime = performance.now();
              console.log(`🆕 Mensagens novas detectadas! Atualizando chat...`);
              console.log(`📊 Mensagens atuais: ${currentMessages.length} → Novas: ${newMessages.length}`);
              console.log('⏱️ Iniciando renderização:', new Date().toLocaleTimeString());
              
              // Adicionar apenas as mensagens realmente novas (não substituir todas)
              const newMessageIds = newMessages.map(m => m.id);
              const currentMessageIds = currentMessages.map(m => m.id);
              const trulyNewMessages = newMessages.filter(m => !currentMessageIds.includes(m.id));
              
              if (trulyNewMessages.length > 0) {
                console.log(`🆕 Adicionando ${trulyNewMessages.length} mensagens realmente novas`);
                chatMessages[contactId] = [...currentMessages, ...trulyNewMessages];
                
                // Renderizar apenas as novas mensagens (mais eficiente)
                trulyNewMessages.forEach(message => {
                  const messageElement = createMessageElement(message);
                  const container = document.getElementById('chatMessages');
                  if (container) {
                    container.appendChild(messageElement);
                  }
                });
                
                console.log(`✅ ${trulyNewMessages.length} mensagens adicionadas instantaneamente!`);
              } else {
                // Fallback: renderizar todas as mensagens
                chatMessages[contactId] = newMessages;
                renderChatMessages(chatMessages[contactId]);
              }
              
              const renderEndTime = performance.now();
              console.log(`⏱️ Renderização concluída em ${(renderEndTime - renderStartTime).toFixed(2)}ms`);
              
              // Scroll para a última mensagem
              setTimeout(() => {
                const container = document.getElementById('chatMessages');
                if (container) {
                  container.scrollTop = container.scrollHeight;
                }
              }, 50);
            } else {
              console.log(`✅ Nenhuma mensagem nova detectada`);
              console.log(`📊 Mensagens atuais: ${currentMessages.length}, Mensagens recebidas: ${newMessages.length}`);
            }
          }
        } else if (data.type === 'error') {
          console.error('❌ Erro no stream:', data.message);
        }
      } catch (err) {
        console.error('💥 Erro ao processar dados SSE:', err);
      }
    };
    
        messageEventSource.onerror = function(event) {
          console.error('❌ Erro na conexão SSE:', event);
          console.log('🔄 Estado da conexão:', messageEventSource.readyState);
          
          // Tentar reconectar imediatamente se a conexão foi fechada
          if (messageEventSource.readyState === EventSource.CLOSED) {
            console.log('🔄 Conexão fechada - reconectando imediatamente...');
            setTimeout(() => {
              if (currentChatContactId === contactId) {
                startMessageStream(contactId, phoneNumber);
              }
            }, 1000);
          } else {
            // Tentar reconectar após 3 segundos para outros erros
            setTimeout(() => {
              if (currentChatContactId === contactId) {
                console.log('🔄 Tentando reconectar SSE...');
                startMessageStream(contactId, phoneNumber);
              }
            }, 3000);
          }
        };
    
    console.log(`🚀 Stream SSE configurado para contato ${contactId}`);
    
    // Iniciar heartbeat para monitorar a conexão
    startSSEHeartbeat(contactId, phoneNumber);
  }
  
  // Função para monitorar a conexão SSE com heartbeat
  function startSSEHeartbeat(contactId, phoneNumber) {
    // Parar heartbeat anterior se existir
    if (sseHeartbeatInterval) {
      clearInterval(sseHeartbeatInterval);
    }
    
    lastSSEMessageTime = Date.now();
    
    sseHeartbeatInterval = setInterval(() => {
      const now = Date.now();
      const timeSinceLastMessage = now - lastSSEMessageTime;
      
      // Se não recebeu mensagem há mais de 10 segundos, reconectar
      if (timeSinceLastMessage > 10000) {
        console.log('💔 Heartbeat: Não recebeu mensagens há 10+ segundos, reconectando...');
        if (currentChatContactId === contactId) {
          startMessageStream(contactId, phoneNumber);
        }
      } else {
        console.log(`💓 Heartbeat: Conexão ativa (última mensagem há ${Math.round(timeSinceLastMessage/1000)}s)`);
      }
    }, 5000); // Verificar a cada 5 segundos
  }

  // Função para parar stream de mensagens
  function stopMessageStream() {
    if (messageEventSource) {
      messageEventSource.close();
      messageEventSource = null;
      console.log('⏹️ Stream de mensagens parado');
    }
    
    // Parar heartbeat
    if (sseHeartbeatInterval) {
      clearInterval(sseHeartbeatInterval);
      sseHeartbeatInterval = null;
      console.log('⏹️ Heartbeat parado');
    }
  }

  // Parar stream quando a página for descarregada
  window.addEventListener('beforeunload', () => {
    stopMessageStream();
  });

  // Parar stream quando o usuário sair da aba
  document.addEventListener('visibilitychange', () => {
    if (document.hidden) {
      console.log('👁️ Página oculta - parando stream');
      stopMessageStream();
    } else {
      console.log('👁️ Página visível - reiniciando stream se necessário');
      if (currentChatContactId) {
        const contact = contacts.find(c => c.id == currentChatContactId);
        if (contact) {
          const phoneNumber = contact.phone || contact.cellPhone;
          if (phoneNumber) {
            startMessageStream(currentChatContactId, phoneNumber);
          }
        }
      }
    }
  });


  // Função para renderizar mensagens do chat
  function renderChatMessages(messages) {
    const container = document.getElementById('chatMessages');
    console.log('🎨 Renderizando mensagens:', messages);
    console.log('🎨 Container encontrado:', container);
    console.log('🎨 Tipo de messages:', typeof messages);
    console.log('🎨 Length de messages:', messages?.length);
    
    if (!container) {
      console.error('❌ Container chatMessages não encontrado!');
      return;
    }
    
    if (!messages || messages.length === 0) {
      console.log('📭 Nenhuma mensagem para renderizar');
      container.innerHTML = `
        <div class="chat-empty">
          <div class="chat-empty-icon">💬</div>
          <div class="chat-empty-text">Nenhuma mensagem ainda</div>
          <div class="chat-empty-subtext">Inicie uma conversa com este contato</div>
        </div>
      `;
      return;
    }

    console.log('🎨 Processando', messages.length, 'mensagens');
    
    // Agrupar mensagens por data
    const groupedMessages = groupMessagesByDate(messages);
    console.log('🎨 Mensagens agrupadas:', groupedMessages);
    
    const html = Object.keys(groupedMessages).map(date => `
      <div class="message-group">
        <div class="message-date">${date}</div>
        ${groupedMessages[date].map(msg => createMessageHTML(msg)).join('')}
      </div>
    `).join('');
    
    console.log('🎨 HTML gerado:', html.substring(0, 200) + '...');
    container.innerHTML = html;

    // Scroll para o final
    container.scrollTop = container.scrollHeight;
    console.log('🎨 Renderização concluída');
  }

  // Função para agrupar mensagens por data
  function groupMessagesByDate(messages) {
    const groups = {};
    
    // Primeiro ordenar todas as mensagens por timestamp (mais antigas primeiro)
    const sortedMessages = [...messages].sort((a, b) => new Date(a.timestamp) - new Date(b.timestamp));
    console.log('🎨 Mensagens ordenadas por timestamp:', sortedMessages.map(m => ({ content: m.content, timestamp: m.timestamp })));
    
    sortedMessages.forEach(msg => {
      const date = msg.timestamp.toLocaleDateString('pt-BR', {
        weekday: 'long',
        year: 'numeric',
        month: 'long',
        day: 'numeric'
      });
      
      if (!groups[date]) groups[date] = [];
      groups[date].push(msg);
    });
    
    return groups;
  }

  // Função para criar HTML da mensagem
  function createMessageHTML(message) {
    console.log('🔍 PROCESSANDO MENSAGEM:', {
      id: message.id,
      content: message.content,
      mediaType: message.mediaType,
      mediaUrl: message.mediaUrl ? 'presente' : 'ausente',
      hasMedia: message.hasMedia,
      sender: message.sender
    });

    const isSent = message.sender === 'user';
    const timeStr = message.timestamp.toLocaleTimeString('pt-BR', {
      hour: '2-digit',
      minute: '2-digit'
    });
    
    const statusIcons = {
      'sending': '⏳',
      'sent': '✓',
      'delivered': '✓✓',
      'read': '✓✓',
      'failed': '❌'
    };
    
    const statusClass = message.status === 'read' ? 'read' : 
                       message.status === 'delivered' ? 'delivered' : 
                       message.status === 'failed' ? 'failed' : '';

    // Renderizar mídia se existir - VERSÃO SIMPLIFICADA
    let mediaContent = '';
    
    // DETECÇÃO SIMPLES E DIRETA DE MÍDIA
    let isAudio = false;
    let isImage = false;
    let isVideo = false;
    let isDocument = false;
    
         // Detectar áudio de forma ULTRA ROBUSTA
         if (message.mediaType === 'audio' || 
             (message.mediaUrl && message.mediaUrl.includes('audio/')) ||
             (message.content && message.content.includes('🎵')) ||
             (message.content && message.content.includes('audio-')) ||
             (message.hasMedia && !message.mediaType && !message.mediaUrl && !message.content) || // Fallback para áudios sem dados
             (message.hasMedia && message.mediaType === null && message.mediaUrl === null)) { // Fallback para áudios com hasMedia=true mas sem tipo
           isAudio = true;
         }
    // Detectar imagem
    else if (message.mediaType === 'image' || 
             (message.mediaUrl && message.mediaUrl.includes('image/'))) {
      isImage = true;
    }
    // Detectar vídeo
    else if (message.mediaType === 'video' || 
             (message.mediaUrl && message.mediaUrl.includes('video/'))) {
      isVideo = true;
    }
    // Detectar documento
    else if (message.mediaType === 'document' || 
             (message.hasMedia && !isAudio && !isImage && !isVideo)) {
      isDocument = true;
    }
    
    // RENDERIZAÇÃO SIMPLES E BONITA DE ÁUDIO
    if (isAudio) {
      if (message.mediaUrl && message.mediaUrl.startsWith('data:')) {
        // Player simples e funcional quando tem URL
        const uniqueId = 'audio_' + message.id + '_' + Date.now();
        mediaContent = `
          <div class="message-media" style="margin: 8px 0;">
            <div style="
              background: #1f2937;
              border: 1px solid #374151;
              border-radius: 8px;
              padding: 12px;
              max-width: 250px;
              display: flex;
              align-items: center;
              gap: 12px;
            ">
              <button onclick="toggleAudio('${uniqueId}')" style="
                background: #10b981;
                border: none;
                border-radius: 50%;
                width: 36px;
                height: 36px;
                display: flex;
                align-items: center;
                justify-content: center;
                cursor: pointer;
                transition: all 0.2s ease;
              " onmouseover="this.style.background='#059669'" onmouseout="this.style.background='#10b981'">
                <span id="playIcon_${uniqueId}" style="color: white; font-size: 14px;">▶️</span>
              </button>
              
              <div style="flex: 1; display: flex; align-items: center; gap: 8px;">
                <span style="color: #9ca3af; font-size: 11px;" id="currentTime_${uniqueId}">0:00</span>
                <div style="flex: 1; height: 3px; background: #4b5563; border-radius: 2px; position: relative; cursor: pointer;" onclick="seekAudio('${uniqueId}', event)">
                  <div id="progress_${uniqueId}" style="
                    height: 100%;
                    background: #10b981;
                    border-radius: 2px;
                    width: 0%;
                    transition: width 0.1s ease;
                  "></div>
                </div>
                <span style="color: #9ca3af; font-size: 11px;" id="duration_${uniqueId}">0:00</span>
              </div>
              
              <audio id="${uniqueId}" preload="metadata" style="display: none;">
                <source src="${message.mediaUrl}" type="audio/ogg">
                <source src="${message.mediaUrl}" type="audio/mpeg">
                <source src="${message.mediaUrl}" type="audio/wav">
              </audio>
            </div>
          </div>
        `;
      } else {
        // Placeholder simples quando não tem URL
        const fileName = message.content ? message.content.replace('🎵', '').trim() : 'Áudio';
        mediaContent = `
          <div class="message-media" style="margin: 8px 0;">
            <div style="
              background: #1f2937;
              border: 1px solid #374151;
              border-radius: 8px;
              padding: 12px;
              max-width: 250px;
              display: flex;
              align-items: center;
              gap: 12px;
              cursor: pointer;
            " onclick="playAudioPlaceholder(this)">
              <div style="
                background: #10b981;
                border-radius: 50%;
                width: 36px;
                height: 36px;
                display: flex;
                align-items: center;
                justify-content: center;
                font-size: 16px;
              ">🎵</div>
              <div style="flex: 1;">
                <div style="color: #f9fafb; font-weight: 500; font-size: 13px;">${fileName}</div>
                <div style="color: #9ca3af; font-size: 11px;">Arquivo de áudio</div>
              </div>
              <div style="
                background: #10b981;
                border-radius: 50%;
                width: 28px;
                height: 28px;
                display: flex;
                align-items: center;
                justify-content: center;
                font-size: 12px;
                color: white;
              ">▶️</div>
            </div>
          </div>
        `;
      }
    } else if (isImage) {
      
      if (message.mediaUrl && message.mediaUrl.startsWith('data:')) {
        mediaContent = `<div class="message-media"><img src="${message.mediaUrl}" alt="Imagem" style="max-width: 200px; max-height: 200px; border-radius: 8px; object-fit: cover;" /></div>`;
      } else {
        mediaContent = `<div class="message-media"><div style="background: #1f2937; border: 1px solid #374151; border-radius: 8px; padding: 12px; text-align: center;"><div style="font-size: 24px;">📷</div><div style="color: #f9fafb;">Imagem</div></div></div>`;
      }
    } else if (isVideo) {
      
      if (message.mediaUrl && message.mediaUrl.startsWith('data:')) {
        mediaContent = `<div class="message-media"><video src="${message.mediaUrl}" controls style="max-width: 200px; max-height: 200px; border-radius: 8px;"></video></div>`;
      } else {
        mediaContent = `<div class="message-media"><div style="background: #1f2937; border: 1px solid #374151; border-radius: 8px; padding: 12px; text-align: center;"><div style="font-size: 24px;">🎥</div><div style="color: #f9fafb;">Vídeo</div></div></div>`;
      }
    } else if (isDocument) {
      mediaContent = `<div class="message-media"><div style="background: #1f2937; border: 1px solid #374151; border-radius: 8px; padding: 12px; text-align: center;"><div style="font-size: 24px;">📎</div><div style="color: #f9fafb;">Documento</div></div></div>`;
    }

    return `
      <div class="message ${isSent ? 'sent' : 'received'}">
        ${!isSent ? `
          <div class="message-header">
            <span class="message-sender">${isSent ? 'Você' : 'Contato'}</span>
            <span class="message-time">${timeStr}</span>
          </div>
        ` : ''}
        ${mediaContent}
        ${message.content ? `<div class="message-bubble">${esc(message.content)}</div>` : ''}
        ${isSent ? `
          <div class="message-status">
            <span class="status-icon ${statusClass}">${statusIcons[message.status] || '✓'}</span>
          </div>
        ` : ''}
      </div>
    `;
  }

        // Função para enviar mensagem
        async function sendMessage() {
          const input = document.getElementById('chatMessageInput');
          const message = input.value.trim();
          
          if (!message || !currentChatContactId) return;

          // Adicionar mensagem localmente primeiro (para feedback imediato)
          const newMessage = {
            id: Date.now(),
            sender: 'user',
            content: message,
            timestamp: new Date(),
            status: 'sending'
          };

          if (!chatMessages[currentChatContactId]) {
            chatMessages[currentChatContactId] = [];
          }
          
          chatMessages[currentChatContactId].push(newMessage);
          
          // Limpar input
          input.value = '';
          input.style.height = 'auto';
          
          // Re-renderizar mensagens
          renderChatMessages(chatMessages[currentChatContactId]);
          
          try {
            // Enviar para API usando o número de telefone
            const contact = contacts.find(c => c.id == currentChatContactId);
            const phoneNumber = contact?.phone || contact?.cellPhone;
            
            if (!phoneNumber) {
              showNotification('Número de telefone não encontrado para este contato', 'error');
              return;
            }
            
            const res = await authFetch('/api/whatsapp/send', {
              method: 'POST',
              body: JSON.stringify({
                contactId: currentChatContactId,
                phoneNumber: phoneNumber,
                content: message
              })
            });
            
            if (res.ok) {
              const data = await res.json();
              // Atualizar mensagem com ID real e status
              const localMessage = chatMessages[currentChatContactId].find(m => m.id === newMessage.id);
              if (localMessage) {
                localMessage.id = data.id;
                localMessage.status = data.status;
                localMessage.timestamp = new Date(data.timestamp);
              }
              
              showNotification('Mensagem enviada com sucesso!', 'success');
            } else {
              // Marcar como falha
              const localMessage = chatMessages[currentChatContactId].find(m => m.id === newMessage.id);
              if (localMessage) {
                localMessage.status = 'failed';
              }
              showNotification('Erro ao enviar mensagem', 'error');
            }
            
            // Re-renderizar com status atualizado
            renderChatMessages(chatMessages[currentChatContactId]);
            
          } catch (err) {
            console.error('Erro ao enviar mensagem:', err);
            // Marcar como falha
            const localMessage = chatMessages[currentChatContactId].find(m => m.id === newMessage.id);
            if (localMessage) {
              localMessage.status = 'failed';
            }
            showNotification('Erro ao enviar mensagem', 'error');
            renderChatMessages(chatMessages[currentChatContactId]);
          }
        }

  // Event listeners para o chat
  document.addEventListener('DOMContentLoaded', function() {
    // Carregar lembretes salvos
    loadReminders();
    
    // Botão de notificações
    document.getElementById('notificationsBtn')?.addEventListener('click', (e) => {
      e.stopPropagation();
      toggleNotificationsBalloon();
    });
    
    // Abas
    document.querySelectorAll('.modern-tab').forEach(btn => {
      btn.addEventListener('click', () => {
        const tabName = btn.dataset.tab;
        switchTab(tabName);
      });
    });

    // Envio de mensagem
    document.getElementById('sendMessageBtn')?.addEventListener('click', sendMessage);
    
    // Ações rápidas
    const quickActionsBtn = document.getElementById('quickActionsBtn');
    console.log('🔄 Botão de ações rápidas encontrado:', !!quickActionsBtn);
    if (quickActionsBtn) {
      quickActionsBtn.addEventListener('click', (e) => {
        e.preventDefault();
        console.log('🔄 Botão de ações rápidas clicado!');
        window.toggleQuickActions();
      });
    }
    
    // Enter para enviar mensagem
    document.getElementById('chatMessageInput')?.addEventListener('keypress', (e) => {
      if (e.key === 'Enter' && !e.shiftKey) {
        e.preventDefault();
        sendMessage();
      }
    });

    // Auto-resize do textarea
    document.getElementById('chatMessageInput')?.addEventListener('input', function() {
      this.style.height = 'auto';
      this.style.height = Math.min(this.scrollHeight, 120) + 'px';
    });
    
    // Verificar status do WhatsApp ao carregar
    checkWhatsAppStatus();
  });

  // Função para configurar event listeners
  function setupChatEventListeners() {
    console.log('🔧 Configurando event listeners do chat');
    
    // Botão de Anexo
    const attachBtn = document.getElementById('attachBtn');
    if (attachBtn) {
      attachBtn.addEventListener('click', () => {
        console.log('📎 Botão de anexo clicado');
        const fileInput = document.getElementById('fileInput');
        if (fileInput) {
          fileInput.click();
        } else {
          console.error('❌ Input de arquivo não encontrado');
        }
      });
    } else {
      console.error('❌ Botão de anexo não encontrado');
    }
    
    // Processar arquivos selecionados
    const fileInput = document.getElementById('fileInput');
    if (fileInput) {
      fileInput.addEventListener('change', (e) => {
        console.log('📁 Arquivos selecionados:', e.target.files);
        const files = Array.from(e.target.files);
        if (files.length > 0) {
          handleFileUpload(files);
        }
      });
    } else {
      console.error('❌ Input de arquivo não encontrado');
    }

    // Seletor de Emojis - Versão Moderna
    const emojiBtn = document.getElementById('emojiBtn');
    const emojiPicker = document.getElementById('emojiPicker');
    const emojiPickerElement = document.getElementById('emojiPickerElement');
    const chatInput = document.getElementById('chatMessageInput');
    
    if (emojiBtn && emojiPicker && emojiPickerElement) {
      // Toggle do seletor de emojis
      emojiBtn.addEventListener('click', (e) => {
        e.stopPropagation();
        console.log('😊 Botão de emoji clicado');
        emojiPicker.classList.toggle('show');
        console.log('🎨 Seletor de emoji:', emojiPicker.classList.contains('show') ? 'visível' : 'oculto');
      });
      
      // Evento de seleção de emoji
      emojiPickerElement.addEventListener('emoji-click', (e) => {
        const emoji = e.detail.unicode;
        console.log('🎯 Emoji selecionado:', emoji);
        
        if (chatInput) {
          chatInput.value += emoji;
          chatInput.focus();
          chatInput.style.height = 'auto';
          chatInput.style.height = Math.min(chatInput.scrollHeight, 120) + 'px';
        }
        
        // Fechar seletor após seleção
        emojiPicker.classList.remove('show');
      });
      
      // Fechar seletor ao clicar fora
      document.addEventListener('click', (e) => {
        if (!emojiPicker.contains(e.target) && !emojiBtn.contains(e.target)) {
          emojiPicker.classList.remove('show');
        }
      });
    } else {
      console.error('❌ Elementos do seletor de emojis não encontrados');
    }


    // Função para processar upload de arquivos - Versão Real
    async function handleFileUpload(files) {
      console.log('🔄 Processando upload de arquivos:', files);
      console.log('📱 CurrentChatContactId:', currentChatContactId);
      
      if (!currentChatContactId) {
        showNotification('Nenhum contato selecionado', 'error');
        return;
      }
      
      for (const file of files) {
        try {
          console.log('📄 Processando arquivo:', file.name, 'Tipo:', file.type);
          
          // Verificar tipo de arquivo
          const fileType = file.type.split('/')[0];
          
          if (!['image', 'video'].includes(fileType)) {
            showNotification('Tipo de arquivo não suportado', 'error');
            continue;
          }
          
          // Criar URL temporária para preview
          const fileUrl = URL.createObjectURL(file);
          
          // Criar mensagem com mídia
          const newMessage = {
            id: Date.now() + Math.random(),
            sender: 'user',
            content: '',
            timestamp: new Date(),
            status: 'sending',
            mediaType: fileType,
            fileName: file.name,
            mediaUrl: fileUrl
          };
          
          console.log('💬 Nova mensagem criada:', newMessage);
          
          if (!chatMessages[currentChatContactId]) {
            chatMessages[currentChatContactId] = [];
          }
          
          chatMessages[currentChatContactId].push(newMessage);
          console.log('📝 Mensagem adicionada ao chat');
          renderChatMessages(chatMessages[currentChatContactId]);
          
          // Enviar arquivo via WhatsApp
          try {
            const formData = new FormData();
            formData.append('file', file);
            formData.append('contactId', currentChatContactId);
            // enviar número real do contato
            const sendContact = contacts.find(c => c.id == currentChatContactId);
            const sendPhone = sendContact?.phone || sendContact?.cellPhone || '';
            formData.append('phoneNumber', sendPhone);
            
            console.log('📤 Enviando arquivo para API...');
            const response = await fetch('/api/whatsapp/send-media', {
              method: 'POST',
              headers: {
                'Authorization': `Bearer ${getToken()}`
              },
              body: formData
            });
            
            console.log('📥 Resposta da API:', response.status, response.statusText);
            
            if (response.ok) {
              const localMessage = chatMessages[currentChatContactId].find(m => m.id === newMessage.id);
              if (localMessage) {
                localMessage.status = 'sent';
              }
              renderChatMessages(chatMessages[currentChatContactId]);
              showNotification('Arquivo enviado com sucesso!', 'success');
            } else {
              const errorText = await response.text();
              console.error('❌ Erro da API:', errorText);
              throw new Error(`Erro ao enviar arquivo: ${response.status} - ${errorText}`);
            }
          } catch (error) {
            console.error('Erro ao enviar arquivo:', error);
            // Simular envio se a API falhar
            setTimeout(() => {
              const localMessage = chatMessages[currentChatContactId].find(m => m.id === newMessage.id);
              if (localMessage) {
                localMessage.status = 'sent';
              }
              renderChatMessages(chatMessages[currentChatContactId]);
              showNotification('Arquivo enviado (simulado)', 'success');
            }, 1000);
          }
          
        } catch (err) {
          console.error('Erro ao processar arquivo:', err);
          showNotification('Erro ao enviar arquivo', 'error');
        }
      }
      
      // Limpar input
      const fileInput = document.getElementById('fileInput');
      if (fileInput) {
        fileInput.value = '';
      }
    }

  }

  // Verificar elementos na inicialização
  // Função para abrir modal de imagem
  window.openImageModal = function(imageUrl) {
    if (!imageUrl) {
      showNotification('Imagem não disponível', 'info');
      return;
    }
    
    // Criar modal de imagem
    const modal = document.createElement('div');
    modal.style.cssText = `
      position: fixed;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      background: rgba(0, 0, 0, 0.8);
      display: flex;
      justify-content: center;
      align-items: center;
      z-index: 10000;
      cursor: pointer;
    `;
    
    const img = document.createElement('img');
    img.src = imageUrl;
    img.style.cssText = `
      max-width: 90%;
      max-height: 90%;
      border-radius: 8px;
      box-shadow: 0 4px 20px rgba(0, 0, 0, 0.5);
    `;
    
    modal.appendChild(img);
    document.body.appendChild(modal);
    
    // Fechar ao clicar
    modal.addEventListener('click', () => {
      document.body.removeChild(modal);
    });
  };

  // Funções melhoradas para WhatsApp
  
  // Variáveis globais para WhatsApp
  let whatsappStatusInterval = null;
  let whatsappConnectionStatus = 'Disconnected';
  let whatsappLastError = null;

  // Função para verificar status do WhatsApp
  async function checkWhatsAppStatus() {
    try {
      const res = await authFetch('/api/whatsapp/status');
      const data = await res.json();
      
      whatsappConnectionStatus = data.status || 'Disconnected';
      whatsappLastError = data.lastError;
      
      updateWhatsAppUI(data);
      
      return data;
    } catch (error) {
      console.error('Erro ao verificar status do WhatsApp:', error);
      whatsappConnectionStatus = 'Error';
      whatsappLastError = 'Erro de conexão com o servidor';
      updateWhatsAppUI({ connected: false, status: 'Error', lastError: whatsappLastError });
      return null;
    }
  }

  // Função para atualizar a UI do WhatsApp (tolerante à ausência de elementos)
  function updateWhatsAppUI(data) {
    const statusIcon = document.getElementById('whatsappStatusIcon');
    const statusText = document.getElementById('whatsappStatusText');
    const connectBtn = document.getElementById('whatsappConnectBtn');
    const clearBtn = document.getElementById('whatsappClearBtn');
    const disconnectBtn = document.getElementById('whatsappDisconnectBtn');
    const statusPanel = document.getElementById('whatsappStatusPanel');
    const statusMessage = document.getElementById('statusMessage');

    // Se elementos essenciais não existirem, não tenta manipular DOM
    if (!statusIcon || !statusText) {
      return;
    }

    // Atualizar botões baseado no status (quando presentes)
    if (data.connected) {
      if (connectBtn) connectBtn.style.display = 'none';
      if (clearBtn) clearBtn.style.display = 'none';
      if (disconnectBtn) disconnectBtn.style.display = 'inline-flex';

      statusIcon.className = 'fas fa-wifi';
      statusText.textContent = 'Conectado';
      statusIcon.style.color = '#28a745';

      if (statusPanel) statusPanel.style.display = 'none';
    } else {
      if (connectBtn) connectBtn.style.display = 'inline-flex';
      if (clearBtn) clearBtn.style.display = 'inline-flex';
      if (disconnectBtn) disconnectBtn.style.display = 'none';

      // Atualizar status baseado no estado
      switch (data.status) {
        case 'QR_Generated':
          statusIcon.className = 'fas fa-qrcode';
          statusText.textContent = 'QR Code Gerado';
          statusIcon.style.color = '#17a2b8';
          showStatusPanel('QR Code gerado! Acesse as configurações para visualizar.', 'info');
          break;
        case 'Connecting':
        case 'Authenticated':
          statusIcon.className = 'fas fa-spinner fa-spin';
          statusText.textContent = 'Conectando...';
          statusIcon.style.color = '#ffc107';
          showStatusPanel('Conectando ao WhatsApp...', 'warning');
          break;
        case 'Auth_Failed':
          statusIcon.className = 'fas fa-exclamation-triangle';
          statusText.textContent = 'Falha na Autenticação';
          statusIcon.style.color = '#dc3545';
          showStatusPanel('Falha na autenticação. Clique em "Limpar" para tentar novamente.', 'error');
          break;
        case 'Failed':
          statusIcon.className = 'fas fa-times-circle';
          statusText.textContent = 'Falha na Conexão';
          statusIcon.style.color = '#dc3545';
          showStatusPanel('Falha na conexão. Clique em "Limpar" para tentar novamente.', 'error');
          break;
        default:
          statusIcon.className = 'fas fa-wifi';
          statusText.textContent = 'Desconectado';
          statusIcon.style.color = '#6c757d';
          if (statusPanel) statusPanel.style.display = 'none';
      }
    }
  }

  // Função para mostrar painel de status
  function showStatusPanel(message, type = 'info') {
    const statusPanel = document.getElementById('whatsappStatusPanel');
    const statusMessage = document.getElementById('statusMessage');

    // Se o painel não existir nesta página, apenas loga a mensagem
    if (!statusPanel || !statusMessage) {
      console.info(`[WhatsApp] ${type}: ${message}`);
      return;
    }

    statusMessage.textContent = message;
    statusPanel.style.display = 'block';
    
    // Atualizar cor baseada no tipo
    switch (type) {
      case 'info':
        statusPanel.style.background = 'var(--info-light)';
        statusPanel.style.borderColor = 'var(--info)';
        break;
      case 'warning':
        statusPanel.style.background = 'var(--warning-light)';
        statusPanel.style.borderColor = 'var(--warning)';
        break;
      case 'error':
        statusPanel.style.background = 'var(--danger-light)';
        statusPanel.style.borderColor = 'var(--danger)';
        break;
      case 'success':
        statusPanel.style.background = 'var(--success-light)';
        statusPanel.style.borderColor = 'var(--success)';
        break;
    }
  }

  // Função para conectar WhatsApp
  window.toggleWhatsAppStatus = async function() {
    try {
      const res = await authFetch('/api/whatsapp/connect', {
        method: 'POST'
      });
      
      if (res.ok) {
        showNotification('Iniciando conexão WhatsApp...', 'info');
        startStatusPolling();
      } else {
        const error = await res.json();
        showNotification('Erro ao conectar: ' + error.message, 'error');
      }
    } catch (error) {
      console.error('Erro ao conectar WhatsApp:', error);
      showNotification('Erro ao conectar WhatsApp', 'error');
    }
  };

  // Função para desconectar WhatsApp
  window.disconnectWhatsApp = async function() {
    try {
      const res = await authFetch('/api/whatsapp/disconnect', {
        method: 'POST'
      });
      
      if (res.ok) {
        showNotification('WhatsApp desconectado', 'info');
        stopStatusPolling();
        checkWhatsAppStatus();
      } else {
        const error = await res.json();
        showNotification('Erro ao desconectar: ' + error.message, 'error');
      }
    } catch (error) {
      console.error('Erro ao desconectar WhatsApp:', error);
      showNotification('Erro ao desconectar WhatsApp', 'error');
    }
  };

  // Função para limpar sessão WhatsApp
  window.clearWhatsAppSession = async function() {
    try {
      const res = await authFetch('/api/whatsapp/clear-session', {
        method: 'POST'
      });
      
      if (res.ok) {
        showNotification('Sessão WhatsApp limpa. Reconectando...', 'info');
        stopStatusPolling();
        setTimeout(() => {
          toggleWhatsAppStatus();
        }, 1000);
      } else {
        const error = await res.json();
        showNotification('Erro ao limpar sessão: ' + error.message, 'error');
      }
    } catch (error) {
      console.error('Erro ao limpar sessão WhatsApp:', error);
      showNotification('Erro ao limpar sessão WhatsApp', 'error');
    }
  };

  // Função para atualizar status
  window.refreshWhatsAppStatus = function() {
    checkWhatsAppStatus();
  };


  // Função para iniciar polling de status
  function startStatusPolling() {
    stopStatusPolling(); // Limpar interval anterior se existir
    
    // Verificar status imediatamente
    checkWhatsAppStatus();
    
    // Verificar a cada 3 segundos
    whatsappStatusInterval = setInterval(async () => {
      const data = await checkWhatsAppStatus();
      
      // Se conectado, parar o polling
      if (data && data.connected) {
        stopStatusPolling();
        showNotification('WhatsApp conectado com sucesso!', 'success');
      }
    }, 3000);
  }

  // Função para parar polling de status
  function stopStatusPolling() {
    if (whatsappStatusInterval) {
      clearInterval(whatsappStatusInterval);
      whatsappStatusInterval = null;
    }
  }


  // Função para enviar mensagem rápida
  window.sendQuickMessage = function(message) {
    if (currentChatContactId) {
      document.getElementById('chatMessageInput').value = message;
      sendMessage();
    }
  };
  
  // Função para alternar ações rápidas
  window.toggleQuickActions = function() {
    const quickActions = document.getElementById('quickActions');
    if (quickActions) {
      const isVisible = quickActions.style.display === 'flex';
      quickActions.style.display = isVisible ? 'none' : 'flex';
      console.log('🔄 Ações rápidas:', isVisible ? 'ocultas' : 'visíveis');
    }
  }
  
  // Função para mostrar indicador de digitação
  function showTypingIndicator() {
    if (isTyping) return;
    
    isTyping = true;
    const chatMessages = document.getElementById('chatMessages');
    const typingIndicator = document.createElement('div');
    typingIndicator.className = 'message received';
    typingIndicator.innerHTML = `
      <div class="typing-indicator">
        <span>Contato está digitando</span>
        <div class="typing-dots">
          <div class="typing-dot"></div>
          <div class="typing-dot"></div>
          <div class="typing-dot"></div>
        </div>
      </div>
    `;
    typingIndicator.id = 'typing-indicator';
    chatMessages.appendChild(typingIndicator);
    chatMessages.scrollTop = chatMessages.scrollHeight;
  }

  // Indicador de carregamento de mensagens
  function showLoadingMessages() {
    const container = document.getElementById('chatMessages');
    if (!container) return;
    const existing = document.getElementById('loading-messages');
    if (existing) existing.remove();
    const el = document.createElement('div');
    el.id = 'loading-messages';
    el.className = 'message system';
    el.style.opacity = '0.8';
    el.innerHTML = `<div class="bubble">Carregando mensagens...</div>`;
    container.appendChild(el);
  }

  function hideLoadingMessages() {
    const el = document.getElementById('loading-messages');
    if (el) el.remove();
  }
  
  // Função para esconder indicador de digitação
  function hideTypingIndicator() {
    const typingIndicator = document.getElementById('typing-indicator');
    if (typingIndicator) {
      typingIndicator.remove();
    }
    isTyping = false;
  }
  
  
  
  // Função para criar elemento de mensagem melhorado
  function createMessageElement(message) {
    const messageDiv = document.createElement('div');
    messageDiv.className = `message ${message.sender === 'user' ? 'sent' : 'received'}`;
    
    const time = new Date(message.timestamp).toLocaleTimeString('pt-BR', {
      hour: '2-digit',
      minute: '2-digit'
    });
    
    let mediaContent = '';
    if (message.mediaUrl) {
      if (message.mediaType === 'image') {
        mediaContent = `
          <div class="media-preview">
            <img src="${message.mediaUrl}" alt="Imagem" onclick="openImageModal('${message.mediaUrl}')" />
            <div class="media-overlay">
              <span class="media-play-icon">🔍</span>
            </div>
          </div>
        `;
      } else if (message.mediaType === 'video') {
        mediaContent = `
          <div class="media-preview">
            <video controls>
              <source src="${message.mediaUrl}" type="video/mp4">
            </video>
            <div class="media-overlay">
              <span class="media-play-icon">▶️</span>
            </div>
          </div>
        `;
      } else if (message.mediaType === 'sticker') {
        mediaContent = `
          <div class="media-preview">
            <img src="${message.mediaUrl}" alt="Sticker" class="message-sticker" style="max-width: 120px; max-height: 120px; border-radius: 8px; cursor: pointer; object-fit: contain;" onclick="openImageModal('${message.mediaUrl}')" />
            <div class="media-overlay">
              <span class="media-play-icon">😊</span>
            </div>
          </div>
        `;
      }
    }
    
    const statusIndicator = message.sender === 'user' ? createStatusIndicator(message.status) : '';
    
    messageDiv.innerHTML = `
      <div class="message-header">
        <span class="message-sender">${message.sender === 'user' ? 'Você' : 'Contato'}</span>
        <span class="message-time">${time}</span>
      </div>
      <div class="message-bubble">
        ${mediaContent}
        <div class="message-content">${esc(message.content)}</div>
        ${statusIndicator}
      </div>
    `;
    
    return messageDiv;
  }
  
  // Função para criar indicador de status
  function createStatusIndicator(status) {
    const statusMap = {
      'sending': { class: 'sent', text: 'Enviando' },
      'sent': { class: 'sent', text: 'Enviado' },
      'delivered': { class: 'delivered', text: 'Entregue' },
      'read': { class: 'read', text: 'Lido' },
      'failed': { class: 'failed', text: 'Falhou' }
    };
    
    const statusInfo = statusMap[status] || { class: 'sent', text: 'Enviado' };
    
    return `
      <div class="message-status-indicator">
        <div class="status-dot ${statusInfo.class}"></div>
        <span>${statusInfo.text}</span>
      </div>
    `;
  }
  
  // Função para formatar data
  function formatDate(dateString) {
    const date = new Date(dateString);
    const today = new Date();
    const yesterday = new Date(today);
    yesterday.setDate(yesterday.getDate() - 1);
    
    if (date.toDateString() === today.toDateString()) {
      return 'Hoje';
    } else if (date.toDateString() === yesterday.toDateString()) {
      return 'Ontem';
    } else {
      return date.toLocaleDateString('pt-BR', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric'
      });
    }
  }
  
  // Função para abrir modal de imagem
  window.openImageModal = function(imageUrl) {
    // Implementar modal de visualização de imagem
    console.log('Abrir imagem:', imageUrl);
  };

  // ===========================
  // SISTEMA DE TEMPLATES
  // ===========================
  
  let templates = []; // Armazenar templates
  let currentTemplateCategory = 'all';
  
  // Templates padrão
  const defaultTemplates = [
    {
      id: 1,
      name: 'Saudação Inicial',
      category: 'greeting',
      content: 'Olá {nome}! Como posso ajudá-lo hoje?'
    },
    {
      id: 2,
      name: 'Agradecimento',
      category: 'greeting',
      content: 'Obrigado pelo seu interesse, {nome}! Fico feliz em poder ajudar.'
    },
    {
      id: 3,
      name: 'Follow-up',
      category: 'followup',
      content: 'Oi {nome}, como está? Gostaria de saber se tem alguma dúvida sobre nossa conversa anterior.'
    },
    {
      id: 4,
      name: 'Agendamento',
      category: 'meeting',
      content: 'Olá {nome}! Gostaria de agendar uma conversa para discutirmos melhor suas necessidades. Quando seria um bom horário?'
    },
    {
      id: 5,
      name: 'Encerramento',
      category: 'closing',
      content: 'Foi um prazer conversar com você, {nome}! Qualquer dúvida, estarei aqui para ajudar.'
    }
  ];
  
  // Função para mostrar modal de templates
  window.showTemplatesModal = function() {
    console.log('📝 Abrindo modal de templates...');
    const modal = document.getElementById('templatesModal');
    if (modal) {
      modal.style.display = 'flex';
      loadTemplates();
    } else {
      console.error('❌ Modal de templates não encontrado');
    }
  };

  // ===========================
  // NOVAS FUNCIONALIDADES DE AÇÕES RÁPIDAS
  // ===========================

  // Função para mostrar modal de lembretes
  window.showReminderModal = function() {
    console.log('🔔 Abrindo modal de lembretes...');
    const modal = document.getElementById('reminderModal');
    if (modal) {
      modal.style.display = 'flex';
      // Definir data/hora padrão para 1 hora no futuro
      const now = new Date();
      now.setHours(now.getHours() + 1);
      document.getElementById('reminderDateTime').value = now.toISOString().slice(0, 16);
    }
  };

  // Função para fechar modal de lembretes
  window.closeReminderModal = function() {
    const modal = document.getElementById('reminderModal');
    if (modal) {
      modal.style.display = 'none';
      // Limpar formulário
      document.getElementById('reminderForm').reset();
    }
  };

  // Função para mostrar modal de agendamento
  window.showScheduleModal = function() {
    console.log('📅 Abrindo modal de agendamento...');
    const modal = document.getElementById('scheduleModal');
    if (modal) {
      modal.style.display = 'flex';
      // Definir data/hora padrão para amanhã às 9h
      const tomorrow = new Date();
      tomorrow.setDate(tomorrow.getDate() + 1);
      tomorrow.setHours(9, 0, 0, 0);
      document.getElementById('scheduleDateTime').value = tomorrow.toISOString().slice(0, 16);
    }
  };

  // Função para fechar modal de agendamento
  window.closeScheduleModal = function() {
    const modal = document.getElementById('scheduleModal');
    if (modal) {
      modal.style.display = 'none';
      // Limpar formulário
      document.getElementById('scheduleForm').reset();
    }
  };


  // ===========================
  // SISTEMA DE LEMBRETES
  // ===========================

        // Função para criar lembrete
        async function createReminder(reminderData) {
            const reminder = {
                contactId: currentContactId,
                title: reminderData.title,
                description: reminderData.description,
                dateTime: reminderData.dateTime,
                priority: reminderData.priority
            };

            try {
                // Salvar no backend
                const response = await authFetch('/api/reminders', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(reminder)
                });

                if (response.ok) {
                    const savedReminder = await response.json();
                    reminders.push(savedReminder);
                    console.log('🔔 Lembrete criado:', savedReminder);
                    
                    // Atualizar indicadores visuais
                    updateReminderIndicators();
                    
                    // Configurar notificação
                    scheduleReminderNotification(savedReminder);
                    
                    return savedReminder;
                } else {
                    const errorData = await response.json();
                    throw new Error(errorData.message || 'Erro ao salvar lembrete');
                }
            } catch (error) {
                console.error('Erro ao criar lembrete:', error);
                showNotification('Erro ao criar lembrete: ' + error.message, 'error');
                throw error;
            }
        }

  // Função para atualizar indicadores visuais de lembretes
  function updateReminderIndicators() {
    // Atualizar indicador no card do contato
    updateContactCardReminderIndicator();
    
    // Atualizar indicador na coluna
    updateColumnReminderIndicator();
    
    // Atualizar badge de notificações se estiver no chat
    if (currentChatContactId) {
      loadNotificationsForCurrentContact();
    }
  }

        // Função para atualizar indicador no card do contato
        function updateContactCardReminderIndicator() {
            // Atualizar todos os cards de contato
            const contactCards = document.querySelectorAll('[data-contact-id]');
            contactCards.forEach(contactCard => {
                const contactId = parseInt(contactCard.dataset.contactId);
                const activeReminders = reminders.filter(r => 
                    r.contactId === contactId && 
                    !r.completed
                );
                
                const overdueReminders = activeReminders.filter(r => 
                    new Date(r.dateTime) < new Date()
                );
                
                const todayReminders = activeReminders.filter(r => {
                    const today = new Date();
                    const reminderDate = new Date(r.dateTime);
                    return reminderDate.toDateString() === today.toDateString();
                });
                
                let indicator = contactCard.querySelector('.reminder-indicator');
                
                if (activeReminders.length > 0) {
                    if (!indicator) {
                        indicator = document.createElement('div');
                        indicator.className = 'reminder-indicator';
                        contactCard.style.position = 'relative';
                        contactCard.appendChild(indicator);
                    }
                    
                    // Definir cor e ícone baseado na prioridade
                    let color = '#3b82f6'; // Azul para lembretes futuros
                    let icon = '🔔';
                    
                    if (overdueReminders.length > 0) {
                        color = '#ef4444'; // Vermelho para atrasados
                        icon = '⚠️';
                    } else if (todayReminders.length > 0) {
                        color = '#f59e0b'; // Amarelo para hoje
                        icon = '⏰';
                    }
                    
                    indicator.innerHTML = icon;
                    indicator.style.cssText = `
                        position: absolute;
                        top: 8px;
                        right: 8px;
                        background: ${color};
                        color: white;
                        border-radius: 50%;
                        width: 24px;
                        height: 24px;
                        display: flex;
                        align-items: center;
                        justify-content: center;
                        font-size: 12px;
                        z-index: 10;
                        animation: pulse 2s infinite;
                        box-shadow: 0 2px 8px rgba(0,0,0,0.3);
                    `;
                    
                    // Tooltip com informações dos lembretes
                    indicator.title = `Lembretes: ${activeReminders.length} (${overdueReminders.length} atrasados, ${todayReminders.length} hoje)`;
                } else if (indicator) {
                    indicator.remove();
                }
            });
        }

        // Função para atualizar indicador na coluna
        function updateColumnReminderIndicator() {
            // Implementar indicador na coluna do Kanban
            const columns = document.querySelectorAll('.kanban-column');
            columns.forEach(column => {
                const columnContacts = contacts.filter(c => c.status === column.dataset.status);
                const columnReminders = reminders.filter(r => 
                    !r.completed && 
                    columnContacts.some(c => c.id === r.contactId)
                );
                
                const overdueReminders = columnReminders.filter(r => 
                    new Date(r.dateTime) < new Date()
                );
                
                let indicator = column.querySelector('.column-reminder-indicator');
                if (columnReminders.length > 0) {
                    if (!indicator) {
                        indicator = document.createElement('div');
                        indicator.className = 'column-reminder-indicator';
                        column.style.position = 'relative';
                        column.appendChild(indicator);
                    }
                    
                    // Definir cor baseada na prioridade
                    let color = '#3b82f6'; // Azul para lembretes futuros
                    if (overdueReminders.length > 0) {
                        color = '#ef4444'; // Vermelho para atrasados
                    }
                    
                    indicator.innerHTML = `🔔 ${columnReminders.length}`;
                    indicator.style.cssText = `
                        position: absolute;
                        top: 10px;
                        right: 10px;
                        background: ${color};
                        color: white;
                        padding: 4px 8px;
                        border-radius: 12px;
                        font-size: 12px;
                        font-weight: bold;
                        z-index: 10;
                        animation: pulse 2s infinite;
                        box-shadow: 0 2px 8px rgba(0,0,0,0.3);
                    `;
                } else if (indicator) {
                    indicator.remove();
                }
            });
        }

  // Função para agendar notificação do lembrete
  function scheduleReminderNotification(reminder) {
    const now = new Date();
    const reminderTime = new Date(reminder.dateTime);
    const timeUntilReminder = reminderTime.getTime() - now.getTime();
    
    if (timeUntilReminder > 0) {
      setTimeout(() => {
        showReminderNotification(reminder);
      }, timeUntilReminder);
    }
  }

        // Função para mostrar notificação do lembrete
        function showReminderNotification(reminder) {
            // Verificar se o lembrete ainda não foi completado
            const currentReminder = reminders.find(r => r.id === reminder.id);
            if (!currentReminder || currentReminder.completed) return;
            
            // Criar notificação
            const notification = document.createElement('div');
            notification.className = 'reminder-notification';
            notification.style.cssText = `
                position: fixed;
                top: 20px;
                right: 20px;
                background: linear-gradient(135deg, #ff6b6b 0%, #ee5a52 100%);
                color: white;
                padding: 16px 20px;
                border-radius: 12px;
                box-shadow: 0 8px 32px rgba(255, 107, 107, 0.3);
                z-index: 10000;
                max-width: 350px;
                animation: slideInRight 0.3s ease;
            `;
            
            notification.innerHTML = `
                <div style="display: flex; align-items: center; gap: 12px;">
                    <div style="font-size: 24px;">🔔</div>
                    <div style="flex: 1;">
                        <div style="font-weight: bold; margin-bottom: 4px;">${reminder.title}</div>
                        <div style="font-size: 14px; opacity: 0.9;">${reminder.contactName || 'Contato'}</div>
                        ${reminder.description ? `<div style="font-size: 12px; opacity: 0.8; margin-top: 4px;">${reminder.description}</div>` : ''}
                    </div>
                    <button onclick="markReminderComplete(${reminder.id})" style="
                        background: rgba(255,255,255,0.2);
                        border: none;
                        color: white;
                        border-radius: 6px;
                        padding: 4px 8px;
                        cursor: pointer;
                        font-size: 12px;
                    ">✓</button>
                </div>
            `;
            
            document.body.appendChild(notification);
            
            // Auto-remover após 10 segundos
            setTimeout(() => {
                if (notification.parentNode) {
                    notification.remove();
                }
            }, 10000);
        }

        // Função para marcar lembrete como completo
        window.markReminderComplete = async function(reminderId) {
            try {
                const response = await authFetch(`/api/reminders/${reminderId}/complete`, {
                    method: 'PUT'
                });

                if (response.ok) {
                    const reminder = reminders.find(r => r.id === reminderId);
                    if (reminder) {
                        reminder.completed = true;
                        reminder.completedAt = new Date();
                        updateReminderIndicators();
                        
                        // Remover notificação
                        const notification = document.querySelector('.reminder-notification');
                        if (notification) {
                            notification.remove();
                        }
                        
                        showNotification('Lembrete marcado como completo!', 'success');
                    }
                } else {
                    const errorData = await response.json();
                    showNotification('Erro ao marcar lembrete: ' + errorData.message, 'error');
                }
            } catch (error) {
                console.error('Erro ao marcar lembrete como completo:', error);
                showNotification('Erro ao marcar lembrete como completo', 'error');
            }
        };

        // Função para carregar lembretes
        async function loadReminders() {
            try {
                // Buscar do backend
                const response = await authFetch('/api/reminders/my-reminders');
                if (response.ok) {
                    const savedReminders = await response.json();
                    reminders = savedReminders.map(r => ({
                        ...r,
                        dateTime: new Date(r.dateTime),
                        createdAt: new Date(r.createdAt)
                    }));
                    updateReminderIndicators();
                    console.log('🔔 Lembretes carregados:', reminders.length);
                    return;
                } else {
                    console.warn('Erro ao carregar lembretes do backend:', response.status);
                }
            } catch (error) {
                console.error('Erro ao carregar lembretes do backend:', error);
            }

            // Fallback para localStorage
            const savedReminders = localStorage.getItem('reminders');
            if (savedReminders) {
                reminders = JSON.parse(savedReminders).map(r => ({
                    ...r,
                    dateTime: new Date(r.dateTime),
                    createdAt: new Date(r.createdAt)
                }));
                updateReminderIndicators();
                console.log('🔔 Lembretes carregados do localStorage:', reminders.length);
            }
        }

  
  // Função para carregar templates
  async function loadTemplates() {
    try {
      // Carregar templates da API (implementar depois)
      // Por enquanto, usar templates padrão
      templates = [...defaultTemplates];
      renderTemplates();
    } catch (error) {
      console.error('Erro ao carregar templates:', error);
      templates = [...defaultTemplates];
      renderTemplates();
    }
  }
  
  // Função para renderizar templates
  function renderTemplates() {
    const templatesList = document.getElementById('templatesList');
    if (!templatesList) return;
    
    const filteredTemplates = currentTemplateCategory === 'all' 
      ? templates 
      : templates.filter(t => t.category === currentTemplateCategory);
    
    if (filteredTemplates.length === 0) {
      templatesList.innerHTML = `
        <div class="empty-templates">
          <div class="empty-templates-icon">📝</div>
          <div>Nenhum template encontrado</div>
        </div>
      `;
      return;
    }
    
    templatesList.innerHTML = filteredTemplates.map(template => `
      <div class="template-item" onclick="useTemplate(${template.id})">
        <div class="template-header">
          <div class="template-name">${esc(template.name)}</div>
          <div class="template-category">${getCategoryLabel(template.category)}</div>
        </div>
        <div class="template-content">${esc(template.content)}</div>
        <div class="template-actions">
          <button class="template-action-btn primary" onclick="event.stopPropagation(); useTemplate(${template.id})">
            Usar
          </button>
          <button class="template-action-btn" onclick="event.stopPropagation(); editTemplate(${template.id})">
            Editar
          </button>
          <button class="template-action-btn" onclick="event.stopPropagation(); deleteTemplate(${template.id})">
            Excluir
          </button>
        </div>
      </div>
    `).join('');
  }
  
  // Função para obter label da categoria
  function getCategoryLabel(category) {
    const labels = {
      'greeting': 'Saudações',
      'followup': 'Follow-up',
      'meeting': 'Reuniões',
      'closing': 'Encerramento',
      'other': 'Outros'
    };
    return labels[category] || 'Outros';
  }
  
  // Função para usar template
  window.useTemplate = function(templateId) {
    const template = templates.find(t => t.id === templateId);
    if (!template) return;
    
    // Personalizar template com nome do contato
    const contact = contacts.find(c => c.id === currentChatContactId);
    let personalizedContent = template.content;
    
    if (contact && contact.fullName) {
      personalizedContent = personalizedContent.replace(/{nome}/g, contact.fullName);
    }
    
    // Preencher campo de mensagem
    document.getElementById('chatMessageInput').value = personalizedContent;
    
    // Fechar modal de templates
    document.getElementById('templatesModal').style.display = 'none';
    
    // Focar no campo de mensagem
    document.getElementById('chatMessageInput').focus();
  };
  
  // Função para mostrar modal de novo template
  window.showNewTemplateModal = function(templateId = null) {
    const modal = document.getElementById('newTemplateModal');
    const title = document.getElementById('templateModalTitle');
    const form = document.getElementById('templateForm');
    
    if (templateId) {
      // Editar template existente
      const template = templates.find(t => t.id === templateId);
      if (template) {
        title.textContent = 'Editar Template';
        document.getElementById('templateName').value = template.name;
        document.getElementById('templateCategory').value = template.category;
        document.getElementById('templateContent').value = template.content;
        form.dataset.templateId = templateId;
      }
    } else {
      // Novo template
      title.textContent = 'Novo Template';
      form.reset();
      delete form.dataset.templateId;
    }
    
    modal.style.display = 'flex';
  }
  
  // Função para editar template
  function editTemplate(templateId) {
    showNewTemplateModal(templateId);
  }
  
  // Função para excluir template
  function deleteTemplate(templateId) {
    if (confirm('Tem certeza que deseja excluir este template?')) {
      templates = templates.filter(t => t.id !== templateId);
      renderTemplates();
    }
  }
  
  // Função para salvar template
  function saveTemplate() {
    const form = document.getElementById('templateForm');
    const templateId = form.dataset.templateId;
    const name = document.getElementById('templateName').value;
    const category = document.getElementById('templateCategory').value;
    const content = document.getElementById('templateContent').value;
    
    if (!name || !content) {
      showNotification('Preencha todos os campos obrigatórios', 'error');
      return;
    }
    
    if (templateId) {
      // Editar template existente
      const template = templates.find(t => t.id == templateId);
      if (template) {
        template.name = name;
        template.category = category;
        template.content = content;
      }
    } else {
      // Criar novo template
      const newTemplate = {
        id: Date.now(), // ID simples para demonstração
        name,
        category,
        content
      };
      templates.push(newTemplate);
    }
    
    renderTemplates();
    document.getElementById('newTemplateModal').style.display = 'none';
    showNotification('Template salvo com sucesso!', 'success');
  }

  console.log('🔍 Verificando elementos:');
  console.log('📎 Botão anexo:', document.getElementById('attachBtn'));
  console.log('😊 Botão emoji:', document.getElementById('emojiBtn'));
  console.log('📁 Input arquivo:', document.getElementById('fileInput'));
  console.log('🎨 Seletor emoji:', document.getElementById('emojiPicker'));

  // Event listeners para templates
  document.getElementById('closeTemplatesModal')?.addEventListener('click', () => {
    document.getElementById('templatesModal').style.display = 'none';
  });
  
  document.getElementById('closeNewTemplateModal')?.addEventListener('click', () => {
    document.getElementById('newTemplateModal').style.display = 'none';
  });

  // Event listeners para novos modais
  document.getElementById('closeReminderModal')?.addEventListener('click', closeReminderModal);
  document.getElementById('closeScheduleModal')?.addEventListener('click', closeScheduleModal);

  // Event listeners para formulários
        document.getElementById('reminderForm')?.addEventListener('submit', async (e) => {
            e.preventDefault();
            const formData = {
                title: document.getElementById('reminderTitle').value,
                description: document.getElementById('reminderDescription').value,
                dateTime: document.getElementById('reminderDateTime').value,
                priority: document.getElementById('reminderPriority').value
            };
            
            try {
                await createReminder(formData);
                closeReminderModal();
                showNotification('Lembrete criado com sucesso!', 'success');
            } catch (error) {
                console.error('Erro ao criar lembrete:', error);
                // O erro já foi mostrado na função createReminder
            }
        });

  // Fechar modais clicando fora
  document.addEventListener('click', (e) => {
    if (e.target.classList.contains('observation-modal')) {
      e.target.style.display = 'none';
    }
  });

  document.getElementById('scheduleForm')?.addEventListener('submit', async (e) => {
    e.preventDefault();
    
    const scheduleType = document.getElementById('scheduleType').value;
    const scheduleDateTime = document.getElementById('scheduleDateTime').value;
    const scheduleDuration = document.getElementById('scheduleDuration').value;
    const scheduleNotes = document.getElementById('scheduleNotes').value;
    
    // Mapear tipo de compromisso para título
    const typeLabels = {
      'meeting': 'Reunião',
      'call': 'Ligação',
      'visit': 'Visita',
      'training': 'Treinamento',
      'other': 'Outro'
    };
    
    const title = typeLabels[scheduleType] || 'Agendamento';
    const description = scheduleNotes ? `Duração: ${scheduleDuration} min\n\n${scheduleNotes}` : `Duração: ${scheduleDuration} min`;
    
    try {
      console.log('📅 Criando agendamento...');
      
      // Por enquanto, salvar como lembrete também (backend de agendamento ainda não existe)
      // TODO: Criar endpoint separado para agendamentos quando backend estiver pronto
      await createReminder({
        contactId: currentChatContactId,
        title: title,
        description: description,
        dateTime: scheduleDateTime,
        priority: 'medium'
      });
      
      showNotification('Compromisso agendado com sucesso!', 'success');
      closeScheduleModal();
      await loadReminders();
    } catch (error) {
      console.error('Erro ao criar agendamento:', error);
      showNotification('Erro ao criar agendamento.', 'error');
    }
  });
  
  document.getElementById('cancelTemplate')?.addEventListener('click', () => {
    document.getElementById('newTemplateModal').style.display = 'none';
  });
  
  document.getElementById('templateForm')?.addEventListener('submit', (e) => {
    e.preventDefault();
    saveTemplate();
  });
  
  // Event listeners para categorias de templates
  document.querySelectorAll('.category-btn').forEach(btn => {
    btn.addEventListener('click', (e) => {
      // Remover active de todas as categorias
      document.querySelectorAll('.category-btn').forEach(b => b.classList.remove('active'));
      // Adicionar active na categoria clicada
      e.target.classList.add('active');
      // Filtrar templates
      currentTemplateCategory = e.target.dataset.category;
      renderTemplates();
    });
  });

  // ===== SISTEMA DE NOTIFICAÇÕES =====
  
  // Função para alternar o balão de notificações
  function toggleNotificationsBalloon() {
    const balloon = document.getElementById('notificationsBalloon');
    
    if (balloon) {
      const wasShowing = balloon.classList.contains('show');
      
      balloon.classList.toggle('show');
      
      const isShowing = balloon.classList.contains('show');
      
      // Se estiver fechando, remover CSS inline
      if (!isShowing) {
        balloon.style.cssText = '';
        return;
      }
      
      // Se estiver abrindo, carregar notificações
      if (isShowing) {
        // CSS de debug temporário - animação saindo de baixo do sino
        const btn = document.getElementById('notificationsBtn');
        const btnRect = btn.getBoundingClientRect();
        
        // Posição final (debaixo do sino)
        const finalTop = btnRect.bottom + 8;
        // Garantir que não corte na direita (mobile)
        const balloonWidth = 320;
        const rightEdge = window.innerWidth - btnRect.right;
        const padding = 16; // padding do mobile
        // Calcular a posição right final: se o balão ultrapassar a tela, ajustar
        let finalRight = rightEdge;
        if (rightEdge + balloonWidth > window.innerWidth - padding) {
          finalRight = window.innerWidth - balloonWidth - padding;
        }
        
        // Primeiro: posicionar pequeno e invisível na posição do sino
        balloon.style.cssText = `
          position: fixed !important;
          top: ${btnRect.top + (btnRect.height / 2)}px !important;
          right: ${finalRight}px !important;
          width: 320px !important;
          max-height: 400px !important;
          background: #1a1a1a !important;
          border: 2px solid #14b86a !important;
          border-radius: 12px !important;
          box-shadow: 0 8px 32px rgba(0, 0, 0, 0.5) !important;
          z-index: 10000 !important;
          opacity: 0 !important;
          transform: scale(0.1) translateY(-10px) !important;
          transform-origin: top center !important;
          pointer-events: all !important;
          visibility: visible !important;
          display: block !important;
          transition: all 0.4s cubic-bezier(0.34, 1.56, 0.64, 1) !important;
        `;
        
        // Animar para a posição final (crescendo de baixo do sino)
        setTimeout(() => {
          balloon.style.cssText = `
            position: fixed !important;
            top: ${finalTop}px !important;
            right: ${finalRight}px !important;
            width: 320px !important;
            max-height: 400px !important;
            background: #1a1a1a !important;
            border: 2px solid #14b86a !important;
            border-radius: 12px !important;
            box-shadow: 0 8px 32px rgba(0, 0, 0, 0.5) !important;
            z-index: 10000 !important;
            opacity: 1 !important;
            transform: scale(1) translateY(0) !important;
            transform-origin: top center !important;
            pointer-events: all !important;
            visibility: visible !important;
            display: block !important;
            transition: all 0.4s cubic-bezier(0.34, 1.56, 0.64, 1) !important;
          `;
        }, 50);
        loadNotificationsForCurrentContact();
      }
    } else {
      console.error('🔔 Balão de notificações não encontrado!');
    }
  }
  
  // Função para carregar notificações do contato atual
  async function loadNotificationsForCurrentContact() {
    if (!currentChatContactId) {
      return;
    }
    
    try {
      // Carregar lembretes do contato
      const reminders = await loadRemindersForContact(currentChatContactId);
      
      // Carregar agendamentos (filtrar por data: hoje e amanhã)
      const schedules = await loadSchedulesForContact(currentChatContactId);
      
      // Renderizar notificações
      renderNotifications(reminders, schedules);
      
      // Atualizar badge
      updateNotificationBadge(reminders.length + schedules.length);
      
    } catch (error) {
      console.error('Erro ao carregar notificações:', error);
    }
  }
  
  // Função para carregar agendamentos de um contato específico
  async function loadSchedulesForContact(contactId) {
    try {
      // Por enquanto, retornar lembretes que sejam agendamentos
      // (títulos que começam com "Reunião", "Ligação", "Visita", "Treinamento")
      const reminders = await loadRemindersForContact(contactId);
      const schedules = reminders.filter(r => {
        const title = r.title?.toLowerCase() || '';
        return title.includes('reunião') || title.includes('ligação') || 
               title.includes('visita') || title.includes('treinamento') ||
               title.includes('outro');
      });
      console.log(`📅 Agendamentos encontrados: ${schedules.length}`);
      return schedules;
    } catch (error) {
      console.error('Erro ao carregar agendamentos do contato:', error);
      return [];
    }
  }
  
  // Função para carregar lembretes de um contato específico
  async function loadRemindersForContact(contactId) {
    try {
      const response = await authFetch(`/api/reminders/contact/${contactId}`);
      
      if (response.ok) {
        const data = await response.json();
        return data.data || [];
      } else {
        console.error('Erro na API de lembretes:', response.status, response.statusText);
        return [];
      }
    } catch (error) {
      console.error('Erro ao carregar lembretes do contato:', error);
      return [];
    }
  }
  
  // Função para renderizar notificações no balão
  function renderNotifications(reminders, schedules) {
    const content = document.getElementById('notificationsContent');
    if (!content) return;
    
    // Remover duplicatas: criar um Set com IDs únicos
    const allNotificationsMap = new Map();
    
    // Adicionar lembretes
    reminders.forEach(r => {
      allNotificationsMap.set(r.id || r.Id, r);
    });
    
    // Adicionar agendamentos (que são lembretes filtrados)
    schedules.forEach(s => {
      if (!allNotificationsMap.has(s.id || s.Id)) {
        allNotificationsMap.set(s.id || s.Id, s);
      }
    });
    
    const allNotifications = Array.from(allNotificationsMap.values());
    
    if (allNotifications.length === 0) {
      content.innerHTML = `
        <div class="notifications-empty">
          <div class="notifications-empty-icon">🔔</div>
          <p>Nenhum lembrete ou agendamento para este contato</p>
        </div>
      `;
      return;
    }
    
    // Ordenar por data (mais recentes primeiro)
    allNotifications.sort((a, b) => new Date(b.DateTime || b.date) - new Date(a.DateTime || a.date));
    
    content.innerHTML = allNotifications.map(notification => {
      const isReminder = notification.title !== undefined;
      const isSchedule = ['reunião', 'ligação', 'visita', 'treinamento', 'outro'].some(
        type => (notification.title || '').toLowerCase().includes(type)
      );
      const isOverdue = isReminder && new Date(notification.dateTime) < new Date() && !notification.completed;
      
      const dateTime = notification.dateTime || notification.DateTime || notification.date;
      const formattedTime = formatNotificationTime(dateTime);
      
      // Ícones: 🔔 para lembrete, 📅 para agendamento/schedule
      const iconText = isSchedule ? '📅' : '🔔';
      
      const escapedTitle = (notification.title || notification.Title || '').replace(/'/g, "&#39;").replace(/"/g, "&quot;");
      const escapedDesc = (notification.description || '').replace(/'/g, "&#39;").replace(/"/g, "&quot;");
      const escapedDate = (notification.dateTime || notification.DateTime || notification.date || '');
      
      return `
        <div class="notification-item" onclick="window.handleNotificationClick(${notification.id || notification.Id}, '${escapedTitle}', '${escapedDesc}', '${escapedDate}')">
          <div class="notification-icon ${isReminder ? (isOverdue ? 'overdue' : 'reminder') : 'schedule'}">
            ${iconText}
          </div>
          <div class="notification-details">
            <h4 class="notification-title">${notification.title || notification.Title}</h4>
            <p class="notification-time">${formattedTime}</p>
            ${isReminder ? `<span class="notification-priority ${(notification.priority || notification.Priority || 'medium').toLowerCase()}">${notification.priority || notification.Priority || 'Média'}</span>` : ''}
          </div>
        </div>
      `;
    }).join('');
  }
  
  // Função para formatar tempo da notificação
  function formatNotificationTime(dateTime) {
    try {
      const date = new Date(dateTime);
      const now = new Date();
      
      // Validar se a data é válida
      if (isNaN(date.getTime())) {
        return 'Data inválida';
      }
      
      const diff = date - now;
      
      if (diff < 0) {
        const daysAgo = Math.floor(Math.abs(diff) / (1000 * 60 * 60 * 24));
        if (daysAgo === 0) return 'Hoje';
        if (daysAgo === 1) return 'Ontem';
        return `${daysAgo} dias atrás`;
      } else {
        const daysUntil = Math.floor(diff / (1000 * 60 * 60 * 24));
        if (daysUntil === 0) return 'Hoje';
        if (daysUntil === 1) return 'Amanhã';
        return `Em ${daysUntil} dias`;
      }
    } catch (error) {
      console.error('Erro ao formatar data:', error);
      return 'Data inválida';
    }
  }
  
  // Função para atualizar badge de notificações
  function updateNotificationBadge(count) {
    const badge = document.getElementById('notificationBadge');
    if (badge) {
      if (count > 0) {
        badge.textContent = count;
        badge.style.display = 'flex';
      } else {
        badge.style.display = 'none';
      }
    }
  }
  
  // Função para lidar com clique em notificação (global)
  window.handleNotificationClick = function(notificationId, title, description, dateTime) {
    // Fechar o balão de notificações
    const balloon = document.getElementById('notificationsBalloon');
    if (balloon) {
      balloon.classList.remove('show');
    }
    
    // Exibir detalhes da notificação
    const descriptionText = description || 'Nenhuma descrição disponível.';
    const formattedDate = formatNotificationTime(dateTime);
    
    showNotification(
      `<strong>${title}</strong><br>
       ${descriptionText}<br>
       <small style="opacity: 0.7;">${formattedDate}</small>`,
      'info'
    );
  }
  
  // Fechar balão ao clicar fora
  document.addEventListener('click', (e) => {
    const balloon = document.getElementById('notificationsBalloon');
    const btn = document.getElementById('notificationsBtn');
    
    // Não fechar se clicar no botão (o botão já tem seu próprio handler)
    if (btn && btn.contains(e.target)) {
      return;
    }
    
    // Fechar apenas se clicar fora do balão e do botão
    if (balloon && balloon.classList.contains('show') && !balloon.contains(e.target)) {
      balloon.classList.remove('show');
    }
  });

  // Inicializar
  loadData().then(() => {
    setupDragAndDrop();
  });
})();
