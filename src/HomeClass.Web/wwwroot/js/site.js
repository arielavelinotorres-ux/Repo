/* HomeClass — interacciones. Sin jQuery: JavaScript simple y local. */
(function () {
    'use strict';

    const $ = (sel, ctx = document) => ctx.querySelector(sel);
    const $$ = (sel, ctx = document) => Array.from(ctx.querySelectorAll(sel));
    const reducirMovimiento = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const csrf = () => ($('meta[name="csrf"]') || {}).content || '';
    const normalizar = s => (s || '').toString().toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');

    /* ---------- Avisos ---------- */
    function aviso(mensaje, tipo) {
        let pila = $('.toast-stack');
        if (!pila) { pila = document.createElement('div'); pila.className = 'toast-stack'; pila.setAttribute('role', 'status'); pila.setAttribute('aria-live', 'polite'); document.body.appendChild(pila); }
        const el = document.createElement('div');
        el.className = 'aviso' + (tipo === 'error' ? ' error' : '');
        const icono = document.createElement('i');
        icono.className = 'bi ' + (tipo === 'error' ? 'bi-exclamation-octagon-fill' : 'bi-check-circle-fill');
        const texto = document.createElement('div');
        texto.textContent = mensaje;
        const cerrar = document.createElement('button');
        cerrar.type = 'button'; cerrar.setAttribute('aria-label', 'Cerrar aviso'); cerrar.innerHTML = '<i class="bi bi-x-lg"></i>';
        cerrar.addEventListener('click', () => el.remove());
        el.append(icono, texto, cerrar);
        pila.appendChild(el);
        setTimeout(() => el.remove(), tipo === 'error' ? 8000 : 4500);
    }
    $$('.toast-data').forEach(d => aviso(d.dataset.msg, d.dataset.tipo));

    /* ---------- Tema claro / oscuro ---------- */
    const temaBtn = $('#tema-toggle');
    function pintarTema() {
        const oscuro = document.documentElement.getAttribute('data-bs-theme') === 'dark';
        if (temaBtn) temaBtn.innerHTML = '<i class="bi ' + (oscuro ? 'bi-sun' : 'bi-moon-stars') + '"></i>';
    }
    pintarTema();
    if (temaBtn) temaBtn.addEventListener('click', () => {
        const nuevo = document.documentElement.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark';
        document.documentElement.setAttribute('data-bs-theme', nuevo);
        try { localStorage.setItem('homeclass-tema', nuevo); } catch (e) { }
        pintarTema();
        construirGraficos(); // los gráficos leen los colores del tema
    });

    /* ---------- Barra lateral en móvil ---------- */
    $$('[data-sidebar-toggle]').forEach(b => b.addEventListener('click', () => document.body.classList.toggle('sidebar-open')));
    const overlay = $('.sidebar-overlay');
    if (overlay) overlay.addEventListener('click', () => document.body.classList.remove('sidebar-open'));
    document.addEventListener('keydown', e => { if (e.key === 'Escape') document.body.classList.remove('sidebar-open'); });

    /* ---------- Confirmación antes de acciones destructivas ---------- */
    const modalEl = $('#confirmModal');
    if (modalEl && window.bootstrap) {
        const modal = new bootstrap.Modal(modalEl);
        let pendiente = null;
        document.addEventListener('submit', e => {
            const form = e.target;
            if (!(form instanceof HTMLFormElement) || !form.dataset.confirm || form.dataset.confirmado) return;
            e.preventDefault();
            pendiente = form;
            $('#confirmTitulo').textContent = form.dataset.confirmTitulo || 'Confirmar acción';
            $('#confirmMensaje').textContent = form.dataset.confirm;
            const btn = $('#confirmOk');
            btn.textContent = form.dataset.confirmBoton || 'Confirmar';
            btn.className = 'btn ' + (form.dataset.peligro === 'false' ? 'btn-primary' : 'btn-danger');
            modal.show();
        });
        $('#confirmOk').addEventListener('click', () => {
            if (!pendiente) return;
            pendiente.dataset.confirmado = '1';
            modal.hide();
            pendiente.requestSubmit ? pendiente.requestSubmit() : pendiente.submit();
        });
    }

    /* ---------- Buscar y filtrar listas (tablas, tarjetas, filas) ---------- */
    $$('[data-filtrable]').forEach(cont => {
        const filas = $$('[data-fila]', cont);
        const buscador = $('[data-buscar]', cont);
        const chips = $$('[data-chip]', cont);
        const contador = $('[data-contador]', cont);
        const vacio = $('[data-vacio]', cont);
        let grupo = '';

        function aplicar() {
            const q = normalizar(buscador ? buscador.value : '');
            let visibles = 0;
            filas.forEach(f => {
                const okTexto = !q || normalizar(f.dataset.texto).includes(q);
                const okGrupo = !grupo || f.dataset.grupo === grupo;
                const mostrar = okTexto && okGrupo;
                f.hidden = !mostrar;
                if (mostrar) visibles++;
            });
            if (contador) contador.textContent = visibles;
            if (vacio) vacio.hidden = visibles !== 0;
        }
        if (buscador) buscador.addEventListener('input', aplicar);
        chips.forEach(c => c.addEventListener('click', () => {
            chips.forEach(x => x.classList.remove('on'));
            c.classList.add('on');
            grupo = c.dataset.chip;
            aplicar();
        }));
        aplicar();
    });

    /* ---------- Ordenar tablas al hacer clic en el encabezado ---------- */
    $$('.tabla th[data-sort]').forEach(th => th.addEventListener('click', () => {
        const tabla = th.closest('table');
        const idx = Array.from(th.parentElement.children).indexOf(th);
        const asc = !th.classList.contains('asc');
        $$('th', tabla).forEach(x => x.classList.remove('asc', 'desc'));
        th.classList.add(asc ? 'asc' : 'desc');
        const tbody = tabla.tBodies[0];
        const valor = tr => { const td = tr.children[idx]; return td.dataset.valor !== undefined ? td.dataset.valor : td.textContent.trim(); };
        const numerico = th.dataset.sort === 'num';
        Array.from(tbody.rows).sort((a, b) => {
            let x = valor(a), y = valor(b);
            if (numerico) { x = parseFloat(x); y = parseFloat(y); x = isNaN(x) ? -1 : x; y = isNaN(y) ? -1 : y; return asc ? x - y : y - x; }
            return asc ? x.localeCompare(y, 'es') : y.localeCompare(x, 'es');
        }).forEach(tr => tbody.appendChild(tr));
    }));

    /* ---------- Números que suben hasta su valor ---------- */
    if (!reducirMovimiento) {
        $$('.stat-value').forEach(el => {
            const m = el.textContent.trim().match(/^(\d+(?:\.\d+)?)(%?)$/);
            if (!m) return;
            const meta = parseFloat(m[1]), dec = (m[1].split('.')[1] || '').length, sufijo = m[2];
            const inicio = performance.now(), dur = 650;
            (function paso(t) {
                const p = Math.min((t - inicio) / dur, 1), e = 1 - Math.pow(1 - p, 3);
                el.textContent = (meta * e).toFixed(dec) + sufijo;
                if (p < 1) requestAnimationFrame(paso);
            })(inicio);
        });
    }

    /* ---------- Gráficos (Chart.js) ---------- */
    const graficos = [];
    function construirGraficos() {
        if (!window.Chart) return;
        graficos.splice(0).forEach(g => g.destroy());
        const css = getComputedStyle(document.documentElement);
        const muted = css.getPropertyValue('--muted').trim() || '#5d6e68';
        const linea = css.getPropertyValue('--line').trim() || '#dde5e1';
        const acento = getComputedStyle(document.body).getPropertyValue('--accent').trim() || '#3b5bdb';
        Chart.defaults.font.family = "'Figtree', system-ui, sans-serif";
        Chart.defaults.color = muted;

        $$('canvas[data-chart]').forEach(canvas => {
            const cfg = JSON.parse(canvas.dataset.chart);
            const datasets = cfg.datasets.map(d => {
                const color = d.color || acento;
                if (cfg.type === 'doughnut') return { data: d.data, backgroundColor: d.colors, borderWidth: 2, borderColor: css.getPropertyValue('--surface').trim() };
                if (cfg.type === 'line') return { label: d.label, data: d.data, borderColor: color, backgroundColor: color, tension: .3, pointRadius: 4, pointHoverRadius: 6, borderWidth: 2.5, spanGaps: true, borderDash: d.discontinua ? [6, 5] : [] };
                return { label: d.label, data: d.data, backgroundColor: d.colors || color, borderRadius: 6, maxBarThickness: 46 };
            });
            const opciones = { responsive: true, maintainAspectRatio: false, animation: reducirMovimiento ? false : { duration: 600 }, plugins: { legend: { display: cfg.leyenda === true, position: 'bottom', labels: { usePointStyle: true, boxWidth: 8 } } } };
            if (cfg.type === 'doughnut') {
                opciones.cutout = '66%';
                opciones.plugins.legend = { display: true, position: 'bottom', labels: { usePointStyle: true, boxWidth: 8, padding: 14 } };
            } else {
                opciones.indexAxis = cfg.horizontal ? 'y' : 'x';
                const valor = { beginAtZero: true, suggestedMax: cfg.max || undefined, grid: { color: linea }, border: { display: false }, ticks: { precision: 0 } };
                if (cfg.max) valor.max = cfg.max;
                const categoria = { grid: { display: false }, ticks: { maxRotation: 0, autoSkip: false, callback(v) { const t = this.getLabelForValue(v); return t.length > 16 ? t.slice(0, 15) + '…' : t; } } };
                opciones.scales = cfg.horizontal ? { x: valor, y: categoria } : { x: categoria, y: valor };
            }
            graficos.push(new Chart(canvas, { type: cfg.type, data: { labels: cfg.labels, datasets }, options: opciones }));
        });
    }
    construirGraficos();

    /* ---------- Libro de calificaciones: guarda cada nota al salir de la celda ---------- */
    const claseNota = v => v === null || v === undefined ? 'sin' : v < 70 ? 'mal' : v < 80 ? 'medio' : 'bien';
    function marcar(inp, estado) {
        inp.classList.remove('guardando', 'guardada', 'error');
        if (estado) inp.classList.add(estado);
        if (estado === 'guardada') setTimeout(() => inp.classList.remove('guardada'), 1400);
    }
    $$('.nota-input').forEach(inp => {
        inp.dataset.orig = inp.value;
        inp.addEventListener('keydown', e => {
            if (e.key !== 'Enter' && e.key !== 'ArrowDown' && e.key !== 'ArrowUp') return;
            e.preventDefault();
            const fila = inp.closest('tr');
            const destino = (e.key === 'ArrowUp' ? fila.previousElementSibling : fila.nextElementSibling);
            const sig = destino && destino.querySelector('.nota-input[data-eval="' + inp.dataset.eval + '"]');
            if (sig) { sig.focus(); sig.select(); } else inp.blur();
        });
        inp.addEventListener('focus', () => inp.select());
        inp.addEventListener('change', async () => {
            const texto = inp.value.trim().replace(',', '.');
            let nota = null;
            if (texto !== '') {
                nota = parseFloat(texto);
                if (isNaN(nota) || nota < 0 || nota > 100) { marcar(inp, 'error'); aviso('La nota debe estar entre 0 y 100.', 'error'); return; }
            }
            marcar(inp, 'guardando');
            try {
                const r = await fetch(inp.dataset.url, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': csrf() },
                    body: JSON.stringify({ evaluacionId: +inp.dataset.eval, estudianteId: +inp.dataset.est, nota })
                });
                if (!r.ok) { const j = await r.json().catch(() => ({})); throw new Error(j.error || 'No se pudo guardar la nota.'); }
                const j = await r.json();
                inp.dataset.orig = inp.value;
                marcar(inp, 'guardada');
                const prom = document.getElementById('prom-' + inp.dataset.est);
                if (prom) { prom.textContent = j.promedioEstudiante === null ? '—' : j.promedioEstudiante.toFixed(1); prom.className = 'prom nota ' + claseNota(j.promedioEstudiante); }
                const grupo = document.getElementById('grupo-' + inp.dataset.eval);
                if (grupo) grupo.textContent = j.promedioGrupo === null ? '—' : j.promedioGrupo.toFixed(1);
            } catch (err) {
                marcar(inp, 'error');
                inp.value = inp.dataset.orig;
                aviso(err.message, 'error');
            }
        });
    });

    /* ---------- Editar una evaluación desde el libro ---------- */
    const evalModal = $('#evaluacionModal');
    if (evalModal) {
        evalModal.addEventListener('show.bs.modal', e => {
            const b = e.relatedTarget, d = (b && b.dataset.evalId) ? b.dataset : null;
            $('#evalTituloModal').textContent = d ? 'Editar evaluación' : 'Nueva evaluación';
            $('#ev-id').value = d ? d.evalId : '';
            $('#ev-titulo').value = d ? d.titulo : '';
            $('#ev-tipo').value = d ? d.tipo : '3';
            $('#ev-peso').value = d ? d.peso : '10';
            $('#ev-fecha').value = d ? d.fecha : new Date().toISOString().slice(0, 10);
        });
    }

    /* ---------- Restablecer contraseña (usuarios) ---------- */
    const resetModal = $('#resetModal');
    if (resetModal) resetModal.addEventListener('show.bs.modal', e => {
        $('#reset-id').value = e.relatedTarget.dataset.id;
        $('#reset-nombre').textContent = e.relatedTarget.dataset.nombre;
        $('#reset-pass').value = '';
    });

    /* ---------- Asistencia: marcar a todos y contadores en vivo ---------- */
    $$('[data-asistencia]').forEach(cont => {
        const contar = () => {
            [1, 2, 3, 4].forEach(v => {
                const out = $('[data-cuenta="' + v + '"]', cont);
                if (out) out.textContent = $$('input[type=radio][value="' + v + '"]:checked', cont).length;
            });
        };
        $$('[data-marcar-todos]', cont).forEach(b => b.addEventListener('click', () => {
            $$('input[type=radio][value="' + b.dataset.marcarTodos + '"]', cont).forEach(r => { r.checked = true; });
            contar();
        }));
        cont.addEventListener('change', contar);
        contar();
    });

    /* ---------- Matrícula: buscar, seleccionar visibles y contador ---------- */
    $$('[data-matricula]').forEach(cont => {
        const filas = $$('.fila-check', cont);
        const total = $('[data-total]', cont);
        const buscador = $('[data-buscar]', cont);
        const contar = () => { if (total) total.textContent = $$('input[type=checkbox]:checked', cont).length; };
        if (buscador) buscador.addEventListener('input', () => {
            const q = normalizar(buscador.value);
            filas.forEach(f => { f.hidden = !!q && !normalizar(f.dataset.texto).includes(q); });
        });
        const visibles = () => filas.filter(f => !f.hidden).map(f => $('input', f));
        const marcarTodos = $('[data-seleccionar-visibles]', cont), limpiar = $('[data-limpiar]', cont);
        if (marcarTodos) marcarTodos.addEventListener('click', () => { visibles().forEach(i => { i.checked = true; }); contar(); });
        if (limpiar) limpiar.addEventListener('click', () => { visibles().forEach(i => { i.checked = false; }); contar(); });
        cont.addEventListener('change', contar);
        contar();
    });

    /* ---------- Formulario de usuario: muestra solo los campos del rol elegido ---------- */
    const radiosRol = $$('input[name="Rol"][type="radio"]');
    if (radiosRol.length) {
        const actualizar = () => {
            const rol = (radiosRol.find(r => r.checked) || {}).value;
            $$('[data-solo-rol]').forEach(sec => {
                const visible = sec.dataset.soloRol.split(' ').includes(rol);
                sec.hidden = !visible;
                $$('input, select, textarea', sec).forEach(i => { i.disabled = !visible; });
            });
        };
        radiosRol.forEach(r => r.addEventListener('change', actualizar));
        actualizar();
    }

    /* ---------- Mostrar / ocultar contraseña ---------- */
    $$('[data-toggle-password]').forEach(b => b.addEventListener('click', () => {
        const input = $(b.dataset.togglePassword);
        const ver = input.type === 'password';
        input.type = ver ? 'text' : 'password';
        b.innerHTML = '<i class="bi ' + (ver ? 'bi-eye-slash' : 'bi-eye') + '"></i>';
        b.setAttribute('aria-label', ver ? 'Ocultar contraseña' : 'Mostrar contraseña');
    }));

    /* ---------- Cuentas de demostración en la pantalla de ingreso ---------- */
    $$('[data-demo-email]').forEach(b => b.addEventListener('click', () => {
        $('#Email').value = b.dataset.demoEmail;
        $('#Password').value = b.dataset.demoPass;
        $('#Password').focus();
    }));
})();
