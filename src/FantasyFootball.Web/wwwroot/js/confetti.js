// Thin JS interop wrapper around the canvas-confetti library loaded via CDN in index.html.
// Called from CompetitionDetail when a competition's IsFinished flips true during the visit.
window.ffConfetti = (() => {
    function celebrate() {
        if (typeof confetti !== 'function') { return; }
        // Two volleys from the bottom corners — condensed version of the canvas-confetti "cannons" README example.
        const burst = (originX) => confetti({
            particleCount: 90,
            spread: 70,
            startVelocity: 45,
            origin: { x: originX, y: 0.85 },
            disableForReducedMotion: true,
        });
        burst(0.15);
        burst(0.85);
    }
    function reset() {
        if (typeof confetti !== 'function' || typeof confetti.reset !== 'function') { return; }
        confetti.reset();
    }
    return { celebrate, reset };
})();
