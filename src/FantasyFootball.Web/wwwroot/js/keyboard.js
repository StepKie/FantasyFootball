// preventDefault for keys the competition-detail page handles itself so the
// browser doesn't also scroll the page underneath us.
window.ffKeyboard = {
  registerPreventScroll(el) {
    if (!el) { return; }
    el.addEventListener('keydown', (e) => {
      if (e.key === ' ' || e.key === 'ArrowUp' || e.key === 'ArrowDown' || e.key === 'ArrowLeft' || e.key === 'ArrowRight') {
        e.preventDefault();
      }
      // Ctrl+Z would otherwise trigger the browser's text-editing undo on focusable elements.
      if (e.ctrlKey && (e.key === 'z' || e.key === 'Z')) {
        e.preventDefault();
      }
    });
  }
};
