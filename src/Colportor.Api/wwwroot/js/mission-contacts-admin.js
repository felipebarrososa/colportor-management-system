(function(){
  const main = document.querySelector('main.page');
  if (!main) return;

  function esc(s){return String(s??'').replaceAll('&','&amp;').replaceAll('<','&lt;').replaceAll('>','&gt;');}
  const token = localStorage.getItem('token')||'';

  // Toolbar + filtros
  const toolbar = document.createElement('section');
  toolbar.className = 'card glass';
  toolbar.innerHTML = `
    <div class="panel-header">
      <h3 class="panel-title">👥 Contatos Missionários</h3>
      <div class="panel-actions">
        <button id="mcOpenCreate" class="btn primary">Novo contato</button>
      </div>
    </div>
    <div class="grid-4">
      <div class="field"><label>Busca</label><input id="mcSearch" placeholder="nome, e-mail, telefone, cidade"/></div>
      <div class="field"><label>Status</label>
        <select id="mcStatus">
          <option value="">Todos</option>
          <option>Novo</option>
          <option>Contato iniciado</option>
          <option>Interessado</option>
          <option>Agendado</option>
          <option>Inscrito</option>
          <option>Não interessado</option>
          <option>Arquivado</option>
        </select>
      </div>
      <div class="field"><label>De</label><input id="mcFrom" type="date"/></div>
      <div class="field"><label>Até</label><input id="mcTo" type="date"/></div>
    </div>
    <div class="actions right">
      <button id="mcApply" class="btn">Filtrar</button>
      <button id="mcClear" class="btn subtle">Limpar</button>
    </div>`;

  // Tabela
  const panel = document.createElement('section');
  panel.className = 'card glass';
  panel.innerHTML = `
    <div class="table-wrap">
      <table class="table table-fixed">
        <colgroup>
          <col style="width:64px"/>
          <col/>
          <col style="width:180px"/>
          <col style="width:180px"/>
          <col style="width:160px"/>
          <col style="width:160px"/>
          <col style="width:120px"/>
          <col style="width:160px"/>
        </colgroup>
        <thead>
          <tr>
            <th>ID</th>
            <th>Nome</th>
            <th>Telefone</th>
            <th>E-mail</th>
            <th>Região</th>
            <th>Líder</th>
            <th>Status</th>
            <th style="text-align:right">Ações</th>
          </tr>
        </thead>
        <tbody id="mcRows"></tbody>
      </table>
    </div>
    <div class="pagination">
      <button id="mcPrev" class="btn">‹</button>
      <span id="mcPageInfo">1</span>
      <button id="mcNext" class="btn">›</button>
    </div>`;

  main.appendChild(toolbar);
  main.appendChild(panel);

  // Modal de criação
  const createModal = document.createElement('div');
  createModal.className = 'modal';
  createModal.innerHTML = `
    <div class="modal-dialog">
      <header class="modal-header"><h3>Novo Contato Missionário</h3><button class="icon close" id="mcClose">✕</button></header>
      <section class="modal-body">
        <form id="mcForm" class="form grid-2">
          <input id="mcFullName" placeholder="Nome completo" required />
          <select id="mcGender"><option value="">Sexo</option><option>Masculino</option><option>Feminino</option></select>
          <div class="field"><label>Nascimento</label><input id="mcBirth" type="date"/></div>
          <input id="mcMarital" placeholder="Estado civil" />
          <input id="mcNation" placeholder="Nacionalidade" />
          <input id="mcCity" placeholder="Cidade" />
          <input id="mcState" placeholder="Estado" />
          <input id="mcPhone" placeholder="Celular" />
          <input id="mcEmail" type="email" placeholder="E-mail" />
          <input id="mcProfession" placeholder="Profissão" />
          <input id="mcLanguages" placeholder="Outros idiomas" />
          <select id="mcFluency"><option value="">Fluência</option><option>Básico</option><option>Avançado</option></select>
          <input id="mcChurch" placeholder="Igreja" />
          <input id="mcConversion" placeholder="Tempo de conversão" />
          <input id="mcPlan" placeholder="Tempo que planeja dedicar às missões" />
          <div class="field"><label>Possui passaporte?</label>
            <select id="mcPassport"><option value="false">Não</option><option value="true">Sim</option></select>
          </div>
          <div class="field"><label>Data disponível</label><input id="mcAvailable" type="date"/></div>
          <div class="field"><label>Região</label><input id="mcRegion" type="number" placeholder="RegionId" required/></div>
          <div class="field"><label>Líder (opcional)</label><input id="mcLeader" type="number" placeholder="LeaderId"/></div>
          <button id="mcSubmit" class="btn primary full" type="submit">Salvar</button>
          <p id="mcError" class="error full" hidden>Erro ao salvar</p>
        </form>
      </section>
    </div>
    <div class="modal-backdrop"></div>`;
  document.body.appendChild(createModal);

  function openCreate(){ createModal.classList.add('show'); }
  function closeCreate(){ createModal.classList.remove('show'); }
  document.getElementById('mcOpenCreate')?.addEventListener('click', openCreate);
  createModal.querySelector('#mcClose')?.addEventListener('click', closeCreate);
  createModal.addEventListener('click', (e)=>{ if(e.target===createModal) closeCreate(); });

  // Estado e listagem
  const state = { page: 1, pageSize: 10 };
  async function fetchList(){
    const params = { 
      page: state.page, pageSize: state.pageSize,
      searchTerm: document.getElementById('mcSearch').value.trim() || '',
      status: document.getElementById('mcStatus').value || '',
      createdFrom: document.getElementById('mcFrom').value || '',
      createdTo: document.getElementById('mcTo').value || ''
    };
    const res = await fetch(`/api/mission-contacts?${new URLSearchParams(params).toString()}`, { headers: { 'Authorization': 'Bearer ' + token } });
    if (!res.ok) return;
    const data = await res.json();
    renderRows(data.items || [], data.totalCount || 0);
  }

  function renderRows(items,total){
    const tb = document.getElementById('mcRows');
    if (!tb) return;
    tb.innerHTML = items.map(x=>`
      <tr>
        <td>${x.id}</td>
        <td>${esc(x.fullName)}</td>
        <td>${esc(x.phone||'')}</td>
        <td>${esc(x.email||'')}</td>
        <td>${esc(x.regionName||'')}</td>
        <td>${esc(x.leaderName||'')}</td>
        <td>${esc(x.status)}</td>
        <td style="text-align:right">
          <button class="btn subtle" data-view="${x.id}">Ver</button>
        </td>
      </tr>`).join('');
    document.getElementById('mcPageInfo').textContent = String(state.page);
  }

  document.getElementById('mcApply')?.addEventListener('click', ()=>{ state.page=1; fetchList(); });
  document.getElementById('mcClear')?.addEventListener('click', ()=>{
    ['mcSearch','mcStatus','mcFrom','mcTo'].forEach(id=>{ const el=document.getElementById(id); if(el) el.value=''; });
    state.page=1; fetchList();
  });
  document.getElementById('mcPrev')?.addEventListener('click', ()=>{ if(state.page>1){ state.page--; fetchList(); } });
  document.getElementById('mcNext')?.addEventListener('click', ()=>{ state.page++; fetchList(); });

  createModal.querySelector('#mcForm')?.addEventListener('submit', async (e)=>{
    e.preventDefault();
    const body = {
      fullName: document.getElementById('mcFullName').value.trim(),
      gender: document.getElementById('mcGender').value||null,
      birthDate: document.getElementById('mcBirth').value||null,
      maritalStatus: document.getElementById('mcMarital').value||null,
      nationality: document.getElementById('mcNation').value||null,
      city: document.getElementById('mcCity').value||null,
      state: document.getElementById('mcState').value||null,
      phone: document.getElementById('mcPhone').value||null,
      email: document.getElementById('mcEmail').value||null,
      profession: document.getElementById('mcProfession').value||null,
      speaksOtherLanguages: !!(document.getElementById('mcLanguages').value||'').trim(),
      otherLanguages: document.getElementById('mcLanguages').value||null,
      fluencyLevel: document.getElementById('mcFluency').value||null,
      church: document.getElementById('mcChurch').value||null,
      conversionTime: document.getElementById('mcConversion').value||null,
      missionsDedicationPlan: document.getElementById('mcPlan').value||null,
      hasPassport: document.getElementById('mcPassport').value === 'true',
      availableDate: document.getElementById('mcAvailable').value||null,
      regionId: parseInt(document.getElementById('mcRegion').value||'0',10),
      leaderId: (document.getElementById('mcLeader').value||'').trim()? parseInt(document.getElementById('mcLeader').value,10): null
    };
    try{
      const res = await fetch('/api/mission-contacts', {
        method: 'POST',
        headers: { 'Content-Type':'application/json', 'Authorization':'Bearer '+ token },
        body: JSON.stringify(body)
      });
      if (!res.ok){ document.getElementById('mcError').hidden=false; return; }
      closeCreate();
      fetchList();
    }catch(err){ console.error(err); document.getElementById('mcError').hidden=false; }
  });

  fetchList();
})();



