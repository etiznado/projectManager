const controllers = new WeakMap();

function controller(root) {
    let state = controllers.get(root);
    if (state) return state;
    const syncFullscreen = () => {
        const button = root.querySelector('.fullscreen-toggle');
        if (!button) return;
        const active = Boolean(document.fullscreenElement);
        const label = active ? 'Salir de pantalla completa' : 'Entrar en pantalla completa';
        button.dataset.fullscreen = String(active);
        button.setAttribute('aria-pressed', String(active));
        button.setAttribute('aria-label', label);
        button.title = label;
        button.disabled = !document.fullscreenEnabled || typeof document.documentElement.requestFullscreen !== 'function';
    };
    const measure = () => {
        syncFullscreen();
        const reference = root.querySelector('.delivery-size-reference');
        if (state.reference !== reference) {
            if (state.reference) state.observer.unobserve(state.reference);
            state.reference = reference;
            if (reference) state.observer.observe(reference);
        }
        if (reference) {
            const height = `${Math.ceil(reference.getBoundingClientRect().height)}px`;
            if (root.style.getPropertyValue('--two-project-height') !== height)
                root.style.setProperty('--two-project-height', height);
        } else if (root.style.getPropertyValue('--two-project-height')) {
            root.style.removeProperty('--two-project-height');
        }
        const track = root.querySelector('.initiative-track');
        const card = root.querySelector('.initiative-card.selected');
        const detail = root.querySelector('#initiative-detail');
        if (!track || !card || !detail) { root.dataset.pointerVisible = 'false'; return; }
        const bounds = root.getBoundingClientRect();
        const rail = track.getBoundingClientRect();
        const selected = card.getBoundingClientRect();
        const center = selected.left + selected.width / 2;
        root.dataset.pointerVisible = String(center > rail.left + 12 && center < rail.right - 12);
        root.style.setProperty('--pointer-x', `${center - bounds.left}px`);
        root.style.setProperty('--pointer-top', `${selected.bottom - bounds.top - 1}px`);
        root.style.setProperty('--pointer-gap', `${Math.max(0, detail.getBoundingClientRect().top - selected.bottom + 2)}px`);
        const previous = root.querySelector('.previous');
        const next = root.querySelector('.next');
        previous.disabled = track.scrollLeft <= 1;
        next.disabled = track.scrollLeft + track.clientWidth >= track.scrollWidth - 1;
    };
    const schedule = () => { cancelAnimationFrame(state.frame); state.frame = requestAnimationFrame(measure); };
    const toggleFullscreen = event => {
        if (!(event.target instanceof Element) || !event.target.closest('.fullscreen-toggle')) return;
        // Call synchronously from the native click to retain browser user activation.
        try {
            const operation = document.fullscreenElement ? document.exitFullscreen() : document.documentElement.requestFullscreen();
            Promise.resolve(operation).catch(syncFullscreen);
        } catch { syncFullscreen(); }
    };
    const fullscreenChanged = () => { syncFullscreen(); schedule(); };
    const trap = event => {
        if (event.key !== 'Tab') return;
        const dialog = root.querySelector('[role="dialog"]');
        if (!dialog) return;
        const focusable = [...dialog.querySelectorAll('button:not(:disabled), input:not(:disabled):not([type="hidden"]), [tabindex="0"]')];
        if (!focusable.length) { event.preventDefault(); return; }
        const first = focusable[0], last = focusable.at(-1);
        if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
        else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
    };
    state = { frame: 0, schedule, trap, toggleFullscreen, fullscreenChanged, reference: null, observer: new ResizeObserver(schedule) };
    state.observer.observe(root);
    root.addEventListener('scroll', schedule, true);
    root.addEventListener('keydown', trap);
    root.addEventListener('click', toggleFullscreen);
    document.addEventListener('fullscreenchange', fullscreenChanged);
    controllers.set(root, state);
    return state;
}

export function update(root) { controller(root).schedule(); }
export function scroll(root, direction) {
    const track = root.querySelector('.initiative-track');
    if (track) track.scrollBy({ left: direction * track.clientWidth * .85, behavior: matchMedia('(prefers-reduced-motion: reduce)').matches ? 'instant' : 'smooth' });
}
export function openModal(root) {
    controller(root);
    // EditForm includes hidden handler/antiforgery inputs before its visible controls.
    root.querySelector('[role="dialog"] input[type="date"]:not(:disabled)')?.focus({ preventScroll: true });
}
export function restoreFocus(root, controlId) {
    (document.getElementById(controlId) ?? root.querySelector('h1'))?.focus();
}
export function dispose(root) {
    const state = controllers.get(root);
    if (!state) return;
    cancelAnimationFrame(state.frame);
    state.observer.disconnect();
    root.removeEventListener('scroll', state.schedule, true);
    root.removeEventListener('keydown', state.trap);
    root.removeEventListener('click', state.toggleFullscreen);
    document.removeEventListener('fullscreenchange', state.fullscreenChanged);
    controllers.delete(root);
}
