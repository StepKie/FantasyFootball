// Tiny scroll helper for the variant-B whole-stage games panel.
// 'start' aligns the element top with the viewport top (round headers); 'center' puts it mid-viewport so context shows above + below (just-finished / current game).
window.ffScroll = {
    toElement: (id, block) => {
        const el = document.getElementById(id);
        if (el) { el.scrollIntoView({ behavior: 'smooth', block: block || 'start' }); }
    }
};
