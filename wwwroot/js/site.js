(() => {
  const toggle = document.getElementById('lpxChatToggle');
  const chat = document.getElementById('lpxChat');
  const close = document.getElementById('lpxChatClose');
  const body = document.getElementById('lpxChatBody');
  const form = document.getElementById('lpxChatForm');
  const input = document.getElementById('lpxChatInput');
  const modeLabel = document.getElementById('lpxChatMode');
  const humanBtn = document.getElementById('lpxHumanSupport');
  const badge = document.getElementById('cartBadge');
  let humanMode = false;
  let connection = null;

  const add = (text, cls, html = false) => {
    const d = document.createElement('div');
    d.className = 'chat-bubble ' + cls;
    if (html) d.innerHTML = text; else d.textContent = text;
    body?.appendChild(d);
    if (body) body.scrollTop = body.scrollHeight;
  };

  const refreshCart = async () => {
    try {
      const r = await fetch('/Cart/Count');
      if (!r.ok) return;
      const x = await r.json();
      if (badge) badge.textContent = x.count;
    } catch (_) { }
  };

  const askAi = async (message) => {
    const fd = new FormData();
    fd.append('Message', message);
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    if (token) fd.append('__RequestVerificationToken', token);
    const r = await fetch('/Chat/Ask', { method: 'POST', body: fd });
    const x = await r.json();
    add(x.reply || 'Mình chưa hiểu yêu cầu.', 'bot');
    if (x.actionUrl) add(`<a href="${x.actionUrl}">Mở chức năng →</a>`, 'bot', true);
  };

  const startHuman = async () => {
    if (!window.signalR) {
      add('Chức năng CSKH người thật cần SignalR client. Vui lòng refresh hoặc kiểm tra mạng.', 'bot');
      return;
    }
    if (!connection) {
      connection = new signalR.HubConnectionBuilder().withUrl('/hubs/support').withAutomaticReconnect().build();
      connection.on('ReceiveMessage', (sender, message, time) => add(`${sender} • ${time}\n${message}`, sender === 'Bạn' ? 'user' : 'bot'));
      await connection.start();
      await connection.invoke('JoinSupport');
    }
    humanMode = true;
    if (modeLabel) modeLabel.textContent = 'Đang kết nối CSKH người thật';
    if (humanBtn) humanBtn.textContent = 'AI tư vấn';
    add('Mình đã chuyển sang chế độ CSKH người thật. Hãy gửi tin nhắn của bạn.', 'bot');
  };

  humanBtn?.addEventListener('click', async () => {
    if (humanMode) {
      humanMode = false;
      if (modeLabel) modeLabel.textContent = 'AI + CSKH SignalR';
      humanBtn.textContent = 'Gặp CSKH';
      add('Đã chuyển về trợ lý AI.', 'bot');
      return;
    }
    await startHuman();
  });

  toggle?.addEventListener('click', () => chat?.classList.toggle('open'));
  close?.addEventListener('click', () => chat?.classList.remove('open'));
  document.querySelectorAll('.lpx-quick button').forEach(b => b.addEventListener('click', () => ask(b.dataset.msg)));

  async function ask(message) {
    if (!message) return;
    add(message, 'user');
    input.value = '';
    try {
      if (humanMode && connection) await connection.invoke('SendMessage', message);
      else await askAi(message);
      await refreshCart();
    } catch (_) {
      add('Hệ thống đang bận. Bạn thử lại hoặc chuyển sang CSKH nhé.', 'bot');
    }
  }

  form?.addEventListener('submit', e => { e.preventDefault(); ask(input.value.trim()); });

  document.querySelectorAll('form[action$="/Cart/Add"], form[action="/Cart/Add"]').forEach(f => {
    f.addEventListener('submit', async e => {
      if (!window.fetch) return;
      e.preventDefault();
      try {
        const r = await fetch(f.action, { method: 'POST', body: new FormData(f), headers: { 'X-Requested-With': 'XMLHttpRequest' } });
        const x = await r.json();
        if (x.ok) {
          await refreshCart();
          const toast = document.createElement('div'); toast.className = 'lpx-mini-toast'; toast.textContent = x.message; document.body.appendChild(toast);
          setTimeout(() => toast.remove(), 2200);
        } else add(x.message || 'Không thể thêm vào giỏ.', 'bot');
      } catch (_) { f.submit(); }
    });
  });

  refreshCart();
})();
