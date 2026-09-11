(() => {
  const input = document.getElementById('liveSearch');
  const box = document.getElementById('suggestions');
  let timer;
  input?.addEventListener('input', () => {
    clearTimeout(timer);
    const q = input.value.trim();
    if (q.length < 2) { if (box) box.innerHTML = ''; return; }
    timer = setTimeout(async () => {
      try {
        const r = await fetch('/Shop/Suggestions?term=' + encodeURIComponent(q));
        if (!r.ok) return;
        const items = await r.json();
        if (!box) return;
        box.innerHTML = '';
        for (const x of items) {
          const a = document.createElement('a');
          a.href = '/Shop/Details?slug=' + encodeURIComponent(x.slug);
          const img = document.createElement('img');
          img.src = x.thumbnailUrl || '/images/product-placeholder.svg';
          img.alt = x.name;
          img.width = 42; img.height = 42;
          img.style.cssText = 'width:42px;height:42px;object-fit:cover;border-radius:8px';
          const div = document.createElement('div');
          const b = document.createElement('b'); b.textContent = x.name; div.appendChild(b);
          const p = document.createElement('div'); p.textContent = Number(x.price).toLocaleString('vi-VN') + '₫'; div.appendChild(p);
          a.append(img, div); box.appendChild(a);
        }
      } catch (_) { }
    }, 220);
  });
})();
