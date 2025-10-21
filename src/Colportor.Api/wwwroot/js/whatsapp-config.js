(function(){
  const token = sessionStorage.getItem("token") || localStorage.getItem("token") || "";
  
  if (!token || token.split(".").length !== 3) {
    window.location.href = "/admin/login.html";
    throw new Error("No auth token");
  }

  let whatsappStatus = {
    connected: false,
    phoneNumber: null,
    lastConnection: null,
    qrCode: null
  };

  let stats = {
    messagesSent: 0,
    messagesDelivered: 0,
    messagesRead: 0,
    messagesFailed: 0
  };

  function esc(s){return String(s??'').replaceAll('&','&amp;').replaceAll('<','&lt;').replaceAll('>','&gt;');}

  // Função para requisições autenticadas
  async function authFetch(path, init = {}) {
    const res = await fetch(path, {
      ...init,
      headers: {
        "Content-Type": "application/json",
        Authorization: "Bearer " + token,
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

  // Função para mostrar notificações
  function showNotification(message, type = 'info') {
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
    
    setTimeout(() => {
      if (notification.parentElement) {
        notification.style.animation = 'slideInRight 0.3s ease reverse';
        setTimeout(() => notification.remove(), 300);
      }
    }, 5000);
  }

  // Carregar status do WhatsApp
  async function loadWhatsAppStatus() {
    try {
      const res = await authFetch('/api/whatsapp/status');
      if (res.ok) {
        const data = await res.json();
        whatsappStatus = data;
        updateStatusDisplay();
      } else {
        console.error('Erro ao carregar status:', res.status);
        showNotification('Erro ao carregar status do WhatsApp', 'error');
      }
    } catch (err) {
      console.error('Erro ao carregar status do WhatsApp:', err);
      showNotification('Erro de conexão ao carregar status', 'error');
    }
  }

  // Carregar estatísticas
  async function loadStats() {
    try {
      const res = await authFetch('/api/whatsapp/stats');
      if (res.ok) {
        const data = await res.json();
        stats = data;
        updateStatsDisplay();
      } else {
        console.error('Erro ao carregar estatísticas:', res.status);
        showNotification('Erro ao carregar estatísticas', 'error');
      }
    } catch (err) {
      console.error('Erro ao carregar estatísticas:', err);
      showNotification('Erro de conexão ao carregar estatísticas', 'error');
    }
  }

  // Atualizar exibição do status
  function updateStatusDisplay() {
    const statusIndicator = document.getElementById('statusIndicator');
    const statusText = document.getElementById('statusText');
    const qrContainer = document.getElementById('qrContainer');
    const connectionInfo = document.getElementById('connectionInfo');
    const connectBtn = document.getElementById('connectBtn');
    const disconnectBtn = document.getElementById('disconnectBtn');

    if (whatsappStatus.connected) {
      statusIndicator.classList.add('connected');
      statusText.textContent = 'Conectado';
      
      qrContainer.style.display = 'none';
      connectionInfo.style.display = 'block';
      
      document.getElementById('connectedNumber').textContent = whatsappStatus.phoneNumber || 'Número não informado';
      document.getElementById('lastConnection').textContent = whatsappStatus.lastConnection || 'Agora';
      document.getElementById('connectionStatus').textContent = 'Online';
      
      connectBtn.style.display = 'none';
      disconnectBtn.style.display = 'inline-block';
    } else {
      statusIndicator.classList.remove('connected');
      statusText.textContent = 'Desconectado';
      
      qrContainer.style.display = 'block';
      connectionInfo.style.display = 'none';
      
      connectBtn.style.display = 'inline-block';
      disconnectBtn.style.display = 'none';
    }
  }

  // Atualizar exibição das estatísticas
  function updateStatsDisplay() {
    document.getElementById('messagesSent').textContent = stats.messagesSent.toLocaleString();
    document.getElementById('messagesDelivered').textContent = stats.messagesDelivered.toLocaleString();
    document.getElementById('messagesRead').textContent = stats.messagesRead.toLocaleString();
    document.getElementById('messagesFailed').textContent = stats.messagesFailed.toLocaleString();
  }

  // Conectar WhatsApp
  async function connectWhatsApp() {
    try {
      showNotification('Iniciando conexão com WhatsApp...', 'info');
      
      const res = await authFetch('/api/whatsapp/connect', {
        method: 'POST'
      });
      
      if (res.ok) {
        const data = await res.json();
        whatsappStatus.qrCode = data.qrCode;
        
        // Mostrar QR Code
        const qrCodeElement = document.getElementById('qrCode');
        qrCodeElement.innerHTML = `
          <img src="${data.qrCode}" alt="QR Code" style="width: 100%; height: 100%; object-fit: contain;" />
        `;
        
        showNotification('QR Code gerado! Escaneie com seu WhatsApp', 'success');
        
        // Verificar status periodicamente
        startStatusPolling();
      } else {
        throw new Error('Erro ao conectar');
      }
    } catch (err) {
      console.error('Erro ao conectar WhatsApp:', err);
      showNotification('Erro ao conectar WhatsApp: ' + err.message, 'error');
    }
  }

  // Desconectar WhatsApp
  async function disconnectWhatsApp() {
    try {
      const res = await authFetch('/api/whatsapp/disconnect', {
        method: 'POST'
      });
      
      if (res.ok) {
        whatsappStatus.connected = false;
        whatsappStatus.phoneNumber = null;
        whatsappStatus.lastConnection = null;
        whatsappStatus.qrCode = null;
        
        updateStatusDisplay();
        showNotification('WhatsApp desconectado com sucesso', 'success');
        
        stopStatusPolling();
      } else {
        throw new Error('Erro ao desconectar');
      }
    } catch (err) {
      console.error('Erro ao desconectar WhatsApp:', err);
      showNotification('Erro ao desconectar WhatsApp: ' + err.message, 'error');
    }
  }

  // Polling do status
  let statusPollingInterval = null;

  function startStatusPolling() {
    statusPollingInterval = setInterval(async () => {
      try {
        const res = await authFetch('/api/whatsapp/status');
        if (res.ok) {
          const data = await res.json();
          if (data.connected && !whatsappStatus.connected) {
            whatsappStatus = data;
            updateStatusDisplay();
            showNotification('WhatsApp conectado com sucesso!', 'success');
            stopStatusPolling();
            loadStats(); // Carregar estatísticas após conectar
          }
        }
      } catch (err) {
        console.error('Erro ao verificar status:', err);
      }
    }, 2000); // Verificar a cada 2 segundos
  }

  function stopStatusPolling() {
    if (statusPollingInterval) {
      clearInterval(statusPollingInterval);
      statusPollingInterval = null;
    }
  }

  // Event listeners
  document.getElementById('backBtn')?.addEventListener('click', () => {
    window.location.href = '/admin/mission-contacts.html';
  });

  document.getElementById('connectBtn')?.addEventListener('click', connectWhatsApp);
  document.getElementById('disconnectBtn')?.addEventListener('click', disconnectWhatsApp);
  document.getElementById('refreshBtn')?.addEventListener('click', () => {
    loadWhatsAppStatus();
    loadStats();
  });
  document.getElementById('refreshStatsBtn')?.addEventListener('click', loadStats);

  // Inicializar
  loadWhatsAppStatus();
  loadStats();
})();
