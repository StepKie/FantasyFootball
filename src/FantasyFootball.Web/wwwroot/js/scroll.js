// Tiny scroll helper for the variant-B whole-stage games panel.
// Round chips invoke this to jump to a round's <section id="round-N"> anchor.
window.ffScroll = {
    toElement: (id) => {
        const el = document.getElementById(id);
        if (el) { el.scrollIntoView({ behavior: 'smooth', block: 'start' }); }
    }
};
