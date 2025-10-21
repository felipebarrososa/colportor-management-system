(function(){
  const token = localStorage.getItem('token')||'';
  const addBtn = document.createElement('button');
  addBtn.textContent = 'Quero ser missionário';
  addBtn.className = 'btn-primary';
  const topbar = document.querySelector('.wallet-topbar');
  if (topbar) topbar.appendChild(addBtn);

  const modal = document.createElement('div');
  modal.className = 'modal';
  modal.innerHTML = `
    <div class="dialog">
      <header class="dialog-head">
        <h2>Cadastro de Contato Missionário</h2>
        <button id="mcwClose" class="icon-btn">✕</button>
      </header>
      <div class="dialog-body">
        <form id="mcwForm" class="form grid-2">
          <input id="wFullName" placeholder="Nome completo" required />
          <select id="wGender"><option value="">Sexo</option><option>Masculino</option><option>Feminino</option></select>
          <div class="field"><label>Nascimento</label><input id="wBirth" type="date"/></div>
          <input id="wMarital" placeholder="Estado civil" />
          <input id="wNation" placeholder="Nacionalidade" />
          <input id="wCity" placeholder="Cidade" />
          <input id="wState" placeholder="Estado" />
          <input id="wPhone" placeholder="Celular" />
          <input id="wEmail" type="email" placeholder="E-mail" />
          <input id="wProfession" placeholder="Profissão" />
          <input id="wLanguages" placeholder="Outros idiomas" />
          <select id="wFluency"><option value="">Fluência</option><option>Básico</option><option>Avançado</option></select>
          <input id="wChurch" placeholder="Igreja" />
          <input id="wConversion" placeholder="Tempo de conversão" />
          <input id="wPlan" placeholder="Tempo que planeja dedicar às missões" />
          <div class="field"><label>Possui passaporte?</label>
            <select id="wPassport"><option value="false">Não</option><option value="true">Sim</option></select>
          </div>
          <div class="field"><label>Data disponível</label><input id="wAvailable" type="date"/></div>
          <div class="field"><label>Região</label><input id="wRegion" type="number" placeholder="RegionId" required/></div>
          <div class="field"><label>Líder (opcional)</label><input id="wLeader" type="number" placeholder="LeaderId"/></div>
          <button id="wSubmit" class="btn-primary full" type="submit">Enviar</button>
          <p id="wError" class="error full" hidden>Erro ao enviar</p>
        </form>
      </div>
    </div>
    <div class="modal-backdrop"></div>`;
  document.body.appendChild(modal);

  function open(){ modal.classList.add('show'); }
  function close(){ modal.classList.remove('show'); }
  addBtn.addEventListener('click', open);
  modal.querySelector('#mcwClose')?.addEventListener('click', close);
  modal.addEventListener('click', (e)=>{ if(e.target===modal) close(); });

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
      const res = await fetch('/wallet/mission-contacts', {
        method: 'POST',
        headers: { 'Content-Type':'application/json', 'Authorization':'Bearer '+ token },
        body: JSON.stringify(body)
      });
      if (!res.ok){ document.getElementById('wError').hidden=false; return; }
      close();
      alert('Enviado com sucesso!');
    }catch(err){ console.error(err); document.getElementById('wError').hidden=false; }
  });
})();



