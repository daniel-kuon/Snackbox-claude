// Achievements earned with a scan: each one slams into the middle of the stage, then shrinks
// into a row at the bottom and the next one follows. Plays inside the element AchievementSlam
// renders over the kiosk's QR area, so the purchase on the left stays visible.
window.snackboxAchievementSlam = (() => {
    const runs = new WeakMap(); // stage -> run number; a newer play or stop ends an older run
    const wait = ms => new Promise(r => setTimeout(r, ms));
    const restart = (el, cls) => { el.classList.remove(cls); void el.offsetWidth; el.classList.add(cls); };

    function sparks(stage) {
        const colors = ['#00BCD4', '#0082CD', '#FFD700', '#fff'];
        for (let i = 0; i < 16; i++) {
            const s = document.createElement('div');
            s.className = 'ach-slam-spark';
            s.style.background = colors[i % colors.length];
            stage.appendChild(s);
            const angle = Math.random() * Math.PI * 2, dist = 120 + Math.random() * 120;
            s.animate([{ transform: 'translate(-50%,-50%)', opacity: 1 },
                       { transform: `translate(${Math.cos(angle) * dist}px, ${Math.sin(angle) * dist}px) rotate(${Math.random() * 360}deg)`, opacity: 0 }],
                      { duration: 700, easing: 'cubic-bezier(.1,.8,.3,1)' }).onfinish = () => s.remove();
        }
    }

    function el(cls, html) {
        const e = document.createElement('div');
        e.className = cls;
        if (html !== undefined) e.innerHTML = html;
        return e;
    }

    // Names and descriptions come from the database as text; the icon is the stored SVG markup,
    // rendered the same way the rest of the app renders it
    function text(cls, value) {
        const e = el(cls);
        e.textContent = value;
        return e;
    }

    async function play(stage, items, dotnet) {
        const run = (runs.get(stage) || 0) + 1;
        runs.set(stage, run);
        const alive = () => runs.get(stage) === run && stage.isConnected;

        stage.replaceChildren();
        const headline = el('ach-slam-headline');
        const count = text('ach-slam-count', '0');
        const word = text('ach-slam-word', items.length === 1 ? ' Achievement Unlocked!' : ' Achievements Unlocked!');
        headline.append(count, word);
        const ring = el('ach-slam-ring'), flash = el('ach-slam-flash'), shelf = el('ach-slam-shelf');
        stage.append(headline, ring, flash, shelf);

        // The last one - the highest level - stays large in the middle; only the others move to the row
        const minis = items.slice(0, -1).map(a => {
            const m = el('ach-slam-mini');
            m.append(el('ach-slam-mini-box', a.svg || '🏆'), text('ach-slam-mini-name', a.name));
            shelf.appendChild(m);
            return m;
        });

        await wait(300);
        for (let i = 0; i < items.length; i++) {
            if (!alive()) return;
            const a = items[i];
            const card = el('ach-slam-card ach-slam-in');
            card.append(text('ach-slam-tier', items.length > 1 ? `${i + 1} / ${items.length}` : ''),
                        el('ach-slam-icon', a.svg || '🏆'), text('ach-slam-name', a.name), text('ach-slam-desc', a.description));
            stage.appendChild(card);

            await wait(270); if (!alive()) return;
            restart(stage, 'ach-slam-shake'); restart(flash, 'go'); restart(ring, 'go'); sparks(stage);
            count.textContent = String(i + 1);
            restart(count, 'bump');

            await wait(1500); if (!alive()) return;
            if (i === items.length - 1) break;

            // Into its slot in the row
            const from = card.getBoundingClientRect(), to = minis[i].getBoundingClientRect(), base = stage.getBoundingClientRect();
            card.classList.remove('ach-slam-in');
            card.classList.add('ach-slam-flying');
            card.style.left = (from.left - base.left) + 'px';
            card.style.top = (from.top - base.top) + 'px';
            card.style.transform = 'none';
            void card.offsetWidth;
            card.style.left = (to.left - base.left) + 'px';
            card.style.top = (to.top - base.top) + 'px';
            card.style.transform = `scale(${to.width / from.width})`;
            card.style.opacity = '.4';
            await wait(560); if (!alive()) return;
            card.remove();
            minis[i].classList.add('landed');
            await wait(150);
        }
        if (!alive()) return;
        await wait(200);
        stage.classList.add('ach-slam-finale');
        await wait(2500);
        if (!alive()) return;
        stage.classList.remove('ach-slam-finale');
        await dotnet.invokeMethodAsync('SlamFinished');
    }

    function stop(stage) {
        runs.set(stage, (runs.get(stage) || 0) + 1);
        stage.replaceChildren();
        stage.classList.remove('ach-slam-finale');
    }

    return { play, stop };
})();
