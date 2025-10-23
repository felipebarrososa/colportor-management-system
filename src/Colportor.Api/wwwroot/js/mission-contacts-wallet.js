(function(){
  const token = localStorage.getItem('token')||'';
  const addBtn = document.createElement('button');
  addBtn.textContent = 'Quero ser missionário';
  addBtn.className = 'btn-primary';
  const topbarActions = document.querySelector('.wallet-topbar .topbar-actions');
  if (topbarActions) topbarActions.appendChild(addBtn);

  // Estilos CSS para o formulário
  const styles = `
    <style>
      .wallet-modal {
        position: fixed;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        background: rgba(0, 0, 0, 0.8);
        backdrop-filter: blur(8px);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 10000;
        opacity: 0;
        visibility: hidden;
        transition: all 0.3s ease;
      }
      .wallet-modal.show {
        opacity: 1;
        visibility: visible;
      }
      .wallet-modal-dialog {
        max-width: 600px;
        width: 90%;
        max-height: 90vh;
        background: linear-gradient(135deg, #1a1a1a 0%, #2d2d2d 100%);
        border-radius: 16px;
        box-shadow: 0 25px 50px rgba(0, 0, 0, 0.6);
        border: 1px solid #333;
        overflow: hidden;
        transform: scale(0.9);
        transition: transform 0.3s ease;
      }
      .wallet-modal.show .wallet-modal-dialog {
        transform: scale(1);
      }
      .wallet-modal-header {
        padding: 28px 32px;
        border-bottom: 2px solid #333;
        background: linear-gradient(135deg, #1a1a1a 0%, #2d2d2d 50%, #1a1a1a 100%);
        display: flex;
        justify-content: space-between;
        align-items: center;
        position: relative;
        overflow: hidden;
      }
      .wallet-modal-header::before {
        content: '';
        position: absolute;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        background: linear-gradient(135deg, rgba(74, 222, 128, 0.05) 0%, rgba(34, 197, 94, 0.1) 50%, rgba(74, 222, 128, 0.05) 100%);
        pointer-events: none;
      }
      .wallet-modal-title {
        color: #fff;
        margin: 0;
        font-size: 22px;
        font-weight: 700;
        background: linear-gradient(135deg, #4ade80 0%, #22c55e 50%, #16a34a 100%);
        -webkit-background-clip: text;
        -webkit-text-fill-color: transparent;
        background-clip: text;
        text-shadow: 0 2px 4px rgba(0, 0, 0, 0.3);
        position: relative;
        z-index: 1;
        letter-spacing: 0.5px;
      }
      .wallet-modal-close {
        background: linear-gradient(135deg, #333 0%, #444 100%);
        border: 2px solid #555;
        color: #fff;
        font-size: 18px;
        cursor: pointer;
        padding: 0;
        border-radius: 12px;
        transition: all 0.3s ease;
        width: 44px;
        height: 44px;
        display: flex;
        align-items: center;
        justify-content: center;
        position: relative;
        z-index: 1;
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
      }
      .wallet-modal-close:hover {
        background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%);
        border-color: #f87171;
        color: #fff;
        transform: scale(1.1) rotate(90deg);
        box-shadow: 0 6px 20px rgba(239, 68, 68, 0.4);
      }
      .wallet-modal-close:active {
        transform: scale(0.95) rotate(90deg);
      }
      .wallet-modal-body {
        padding: 24px;
        background: #1a1a1a;
        max-height: 70vh;
        overflow-y: auto;
        scrollbar-width: thin;
        scrollbar-color: #4ade80 #333;
      }
      .wallet-modal-body::-webkit-scrollbar {
        width: 8px;
      }
      .wallet-modal-body::-webkit-scrollbar-track {
        background: #1a1a1a;
        border-radius: 4px;
      }
      .wallet-modal-body::-webkit-scrollbar-thumb {
        background: linear-gradient(135deg, #4ade80 0%, #22c55e 100%);
        border-radius: 4px;
        border: 1px solid #333;
        transition: all 0.2s ease;
      }
      .wallet-modal-body::-webkit-scrollbar-thumb:hover {
        background: linear-gradient(135deg, #22c55e 0%, #16a34a 100%);
        box-shadow: 0 0 8px rgba(74, 222, 128, 0.3);
      }
      .wallet-modal-body::-webkit-scrollbar-thumb:active {
        background: linear-gradient(135deg, #16a34a 0%, #15803d 100%);
      }
      .wallet-modal-body::-webkit-scrollbar-corner {
        background: #1a1a1a;
      }
      .wallet-form {
        display: flex;
        flex-direction: column;
        gap: 20px;
      }
      .wallet-grid-2 {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 16px;
      }
      .wallet-field {
        display: flex;
        flex-direction: column;
        gap: 8px;
      }
      .wallet-label {
        color: #fff;
        font-weight: 500;
        font-size: 14px;
        text-transform: uppercase;
        letter-spacing: 0.5px;
      }
      .wallet-input, .wallet-select {
        padding: 14px 16px;
        background: #222;
        border: 2px solid #333;
        border-radius: 10px;
        color: #fff;
        font-size: 14px;
        transition: all 0.2s;
        font-family: inherit;
      }
      .wallet-input:focus, .wallet-select:focus {
        outline: none;
        border-color: #4ade80;
        box-shadow: 0 0 0 3px rgba(74, 222, 128, 0.1);
        background: #2a2a2a;
      }
      .wallet-input::placeholder {
        color: #666;
      }
      .wallet-select option {
        background: #222;
        color: #fff;
      }
      .wallet-actions {
        margin-top: 8px;
        display: flex;
        justify-content: center;
      }
      .wallet-btn {
        background: linear-gradient(135deg, #4ade80 0%, #22c55e 100%);
        color: #000;
        border: none;
        border-radius: 10px;
        padding: 14px 32px;
        font-size: 16px;
        font-weight: 600;
        cursor: pointer;
        transition: all 0.2s;
        width: 100%;
        position: relative;
        overflow: hidden;
      }
      .wallet-btn:hover {
        transform: translateY(-2px);
        box-shadow: 0 8px 25px rgba(74, 222, 128, 0.3);
      }
      .wallet-btn:active {
        transform: translateY(0);
      }
      .wallet-error {
        text-align: center;
        margin-top: 12px;
        color: #ef4444;
        font-size: 14px;
        padding: 8px;
        background: rgba(239, 68, 68, 0.1);
        border-radius: 8px;
        border: 1px solid rgba(239, 68, 68, 0.2);
      }
      @media (max-width: 768px) {
        .wallet-grid-2 {
          grid-template-columns: 1fr;
        }
        .wallet-modal-dialog {
          width: 95%;
          margin: 20px;
        }
      }
    </style>
  `;

  const modal = document.createElement('div');
  modal.className = 'wallet-modal';
  modal.innerHTML = `
    ${styles}
    <div class="wallet-modal-dialog">
      <header class="wallet-modal-header">
        <h3 class="wallet-modal-title">Novo Contato Missionário</h3>
        <button class="wallet-modal-close" id="mcwClose" aria-label="Fechar">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <line x1="18" y1="6" x2="6" y2="18"></line>
            <line x1="6" y1="6" x2="18" y2="18"></line>
          </svg>
        </button>
      </header>
      <section class="wallet-modal-body">
        <form id="mcwForm" class="wallet-form">
          <div class="wallet-grid-2">
            <div class="wallet-field">
              <label for="wFullName" class="wallet-label">Nome completo</label>
              <input id="wFullName" class="wallet-input" placeholder="Nome completo" required />
            </div>
            <div class="wallet-field">
              <label for="wGender" class="wallet-label">Sexo</label>
              <select id="wGender" class="wallet-select">
                <option value="">Selecione...</option>
                <option value="Masculino">Masculino</option>
                <option value="Feminino">Feminino</option>
              </select>
            </div>
          </div>
          
          <div class="wallet-grid-2">
            <div class="wallet-field">
              <label for="wBirth" class="wallet-label">Data de nascimento</label>
              <input id="wBirth" class="wallet-input" type="date"/>
            </div>
            <div class="wallet-field">
              <label for="wMarital" class="wallet-label">Estado civil</label>
              <input id="wMarital" class="wallet-input" placeholder="Estado civil" />
            </div>
          </div>
          
          <div class="wallet-grid-2">
            <div class="wallet-field">
              <label for="wNation" class="wallet-label">Nacionalidade</label>
              <input id="wNation" class="wallet-input" placeholder="Nacionalidade" />
            </div>
            <div class="wallet-field">
              <label for="wCity" class="wallet-label">Cidade</label>
              <input id="wCity" class="wallet-input" placeholder="Cidade" />
            </div>
          </div>
          
          <div class="wallet-grid-2">
            <div class="wallet-field">
              <label for="wState" class="wallet-label">Estado</label>
              <input id="wState" class="wallet-input" placeholder="Estado" />
            </div>
            <div class="wallet-field">
              <label for="wPhone" class="wallet-label">Celular</label>
              <input id="wPhone" class="wallet-input" placeholder="Celular" />
            </div>
          </div>
          
          <div class="wallet-grid-2">
            <div class="wallet-field">
              <label for="wEmail" class="wallet-label">E-mail</label>
              <input id="wEmail" class="wallet-input" type="email" placeholder="E-mail" />
            </div>
            <div class="wallet-field">
              <label for="wProfession" class="wallet-label">Profissão</label>
              <input id="wProfession" class="wallet-input" placeholder="Profissão" />
            </div>
          </div>
          
          <div class="wallet-grid-2">
            <div class="wallet-field">
              <label for="wLanguages" class="wallet-label">Outros idiomas</label>
              <input id="wLanguages" class="wallet-input" placeholder="Outros idiomas" />
            </div>
            <div class="wallet-field">
              <label for="wFluency" class="wallet-label">Fluência</label>
              <select id="wFluency" class="wallet-select">
                <option value="">Selecione...</option>
                <option value="Básico">Básico</option>
                <option value="Intermediário">Intermediário</option>
                <option value="Avançado">Avançado</option>
                <option value="Fluente">Fluente</option>
              </select>
            </div>
          </div>
          
          <div class="wallet-grid-2">
            <div class="wallet-field">
              <label for="wChurch" class="wallet-label">Igreja</label>
              <input id="wChurch" class="wallet-input" placeholder="Igreja" />
            </div>
            <div class="wallet-field">
              <label for="wConversion" class="wallet-label">Tempo de conversão</label>
              <input id="wConversion" class="wallet-input" placeholder="Ex: 2 anos" />
            </div>
          </div>
          
          <div class="wallet-grid-2">
            <div class="wallet-field">
              <label for="wPlan" class="wallet-label">Tempo que planeja dedicar às missões</label>
              <input id="wPlan" class="wallet-input" placeholder="Ex: 1 ano" />
            </div>
            <div class="wallet-field">
              <label for="wPassport" class="wallet-label">Possui passaporte?</label>
              <select id="wPassport" class="wallet-select">
                <option value="false">Não</option>
                <option value="true">Sim</option>
              </select>
            </div>
          </div>
          
          <div class="wallet-grid-2">
            <div class="wallet-field">
              <label for="wAvailable" class="wallet-label">Data disponível</label>
              <input id="wAvailable" class="wallet-input" type="date"/>
            </div>
            <div class="wallet-field">
              <label for="wRegion" class="wallet-label">Região</label>
              <select id="wRegion" class="wallet-select" required>
                <option value="">Selecione uma região...</option>
              </select>
            </div>
          </div>
          
          <div class="wallet-field">
            <label for="wLeader" class="wallet-label">Líder (opcional)</label>
            <select id="wLeader" class="wallet-select">
              <option value="">Selecione um líder...</option>
            </select>
          </div>
          
          <div class="wallet-actions">
            <button id="wSubmit" class="wallet-btn" type="submit">
              Salvar Contato
              <span class="spinner" hidden></span>
            </button>
          </div>
          
          <div id="wError" class="wallet-error" hidden>
            Erro ao salvar contato
          </div>
        </form>
      </section>
    </div>`;
  document.body.appendChild(modal);

  function open(){ 
    modal.classList.add('show'); 
    loadRegions();
  }
  function close(){ modal.classList.remove('show'); }
  addBtn.addEventListener('click', open);
  modal.querySelector('#mcwClose')?.addEventListener('click', close);
  modal.addEventListener('click', (e)=>{ if(e.target===modal) close(); });

  // Carregar regiões
  async function loadRegions() {
    try {
      console.log('🔄 Carregando regiões...');
      const res = await fetch('/geo/regions');
      console.log('📡 Resposta das regiões:', res.status, res.ok);
      if (!res.ok) {
        console.error('❌ Erro na resposta das regiões:', res.status);
        return;
      }
      const regions = await res.json();
      console.log('📋 Regiões carregadas:', regions);
      const select = document.getElementById('wRegion');
      if (!select) {
        console.error('❌ Select de região não encontrado');
        return;
      }
      select.innerHTML = '<option value="">Selecione uma região...</option>';
      regions.forEach(r => {
        const option = document.createElement('option');
        option.value = r.id;
        option.textContent = r.name;
        select.appendChild(option);
      });
      console.log('✅ Regiões carregadas com sucesso');
    } catch (err) {
      console.error('❌ Erro ao carregar regiões:', err);
    }
  }

  // Carregar líderes quando região for selecionada
  document.addEventListener('change', async (e) => {
    if (e.target.id === 'wRegion') {
      const regionId = e.target.value;
      console.log('🔄 Região selecionada:', regionId);
      const leaderSelect = document.getElementById('wLeader');
      if (!leaderSelect) {
        console.error('❌ Select de líder não encontrado');
        return;
      }
      leaderSelect.innerHTML = '<option value="">Selecione um líder...</option>';
      
      if (!regionId) {
        console.log('⚠️ Nenhuma região selecionada');
        return;
      }
      
      try {
        console.log('🔄 Carregando líderes para região:', regionId);
        const res = await fetch(`/geo/leaders?regionId=${regionId}`);
        console.log('📡 Resposta dos líderes:', res.status, res.ok);
        if (!res.ok) {
          console.error('❌ Erro na resposta dos líderes:', res.status);
          return;
        }
        const leaders = await res.json();
        console.log('👥 Líderes carregados:', leaders);
        leaders.forEach(l => {
          const option = document.createElement('option');
          option.value = l.id;
          option.textContent = l.fullName || l.name || 'Nome não informado';
          leaderSelect.appendChild(option);
        });
        console.log('✅ Líderes carregados com sucesso');
      } catch (err) {
        console.error('❌ Erro ao carregar líderes:', err);
      }
    }
  });

  modal.querySelector('#mcwForm')?.addEventListener('submit', async (e)=>{
    e.preventDefault();
    const body = {
      fullName: document.getElementById('wFullName').value.trim(),
      gender: document.getElementById('wGender').value||null,
      birthDate: document.getElementById('wBirth').value||null,
      maritalStatus: document.getElementById('wMarital').value||null,
      nationality: document.getElementById('wNation').value||null,
      city: document.getElementById('wCity').value||null,
      state: document.getElementById('wState').value||null,
      phone: document.getElementById('wPhone').value||null,
      email: document.getElementById('wEmail').value||null,
      profession: document.getElementById('wProfession').value||null,
      speaksOtherLanguages: !!(document.getElementById('wLanguages').value||'').trim(),
      otherLanguages: document.getElementById('wLanguages').value||null,
      fluencyLevel: document.getElementById('wFluency').value||null,
      church: document.getElementById('wChurch').value||null,
      conversionTime: document.getElementById('wConversion').value||null,
      missionsDedicationPlan: document.getElementById('wPlan').value||null,
      hasPassport: document.getElementById('wPassport').value === 'true',
      availableDate: document.getElementById('wAvailable').value||null,
      regionId: parseInt(document.getElementById('wRegion').value||'0',10),
      leaderId: (document.getElementById('wLeader').value||'').trim()? parseInt(document.getElementById('wLeader').value,10): null
    };
    try{
      // Obter token atualizado
      const currentToken = localStorage.getItem('token') || '';
      console.log('🔑 Token para envio:', currentToken ? currentToken.substring(0, 20) + '...' : 'VAZIO');
      
      const res = await fetch('/wallet/mission-contacts', {
        method: 'POST',
        headers: { 'Content-Type':'application/json', 'Authorization':'Bearer '+ currentToken },
        body: JSON.stringify(body)
      });
      if (!res.ok){ document.getElementById('wError').hidden=false; return; }
      close();
      alert('Enviado com sucesso!');
    }catch(err){ console.error(err); document.getElementById('wError').hidden=false; }
  });
})();



