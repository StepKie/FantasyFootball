// Document-level keyboard handling for the competition detail page.
// Fires regardless of which element has focus (Space simming the next game shouldn't require
// clicking on the page first), and skips when the user is typing into an input.
window.ffKeyboard = {
  _dotNetRef: null,
  _handler: null,

  registerDocumentHandler(dotNetRef) {
    this.unregisterDocumentHandler();
    this._dotNetRef = dotNetRef;
    this._handler = (e) => {
      // Don't intercept when focus is on a form control or content-editable surface.
      const t = e.target;
      if (t && (t.tagName === 'INPUT' || t.tagName === 'TEXTAREA' || t.tagName === 'SELECT' || t.isContentEditable)) {
        return;
      }

      // Suppress browser defaults for keys we own — scroll on Space + arrows, browser undo on Ctrl+Z.
      if (e.key === ' ' || e.key === 'ArrowUp' || e.key === 'ArrowDown' || e.key === 'ArrowLeft' || e.key === 'ArrowRight') {
        e.preventDefault();
      }
      if (e.ctrlKey && (e.key === 'z' || e.key === 'Z' || e.key === 'y' || e.key === 'Y')) {
        e.preventDefault();
      }

      this._dotNetRef.invokeMethodAsync('OnKeyDown', e.key, e.ctrlKey, e.shiftKey);
    };
    document.addEventListener('keydown', this._handler);
  },

  unregisterDocumentHandler() {
    if (this._handler) {
      document.removeEventListener('keydown', this._handler);
      this._handler = null;
    }
    this._dotNetRef = null;
  }
};
