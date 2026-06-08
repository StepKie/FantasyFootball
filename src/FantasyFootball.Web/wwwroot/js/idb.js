// IndexedDB-backed store for simulated competitions. Two object stores keyed by
// repo id: 'competitions' holds the full JSON, 'summaries' holds a tiny list
// projection so the competitions list never ships or parses every full season.
// Competitions outgrew localStorage's ~5 MB per-origin cap; IndexedDB draws from
// the browser's disk-based quota instead.
window.ffIdb = (() => {
    const DB_NAME = 'fantasy-football';
    const FULL = 'competitions';
    const SUMMARY = 'summaries';
    let dbPromise;

    function open() {
        return dbPromise ??= new Promise((resolve, reject) => {
            const req = indexedDB.open(DB_NAME, 2);
            req.onupgradeneeded = () => {
                const db = req.result;
                if (!db.objectStoreNames.contains(FULL)) { db.createObjectStore(FULL); }
                if (!db.objectStoreNames.contains(SUMMARY)) { db.createObjectStore(SUMMARY); }
            };
            req.onsuccess = () => resolve(req.result);
            req.onerror = () => reject(req.error);
        });
    }

    function read(store, op) {
        return open().then(db => new Promise((resolve, reject) => {
            const req = op(db.transaction(store, 'readonly').objectStore(store));
            req.onsuccess = () => resolve(req.result);
            req.onerror = () => reject(req.error);
        }));
    }

    function write(stores, body) {
        return open().then(db => new Promise((resolve, reject) => {
            const tx = db.transaction(stores, 'readwrite');
            body(tx);
            tx.oncomplete = () => resolve();
            tx.onerror = () => reject(tx.error);
            tx.onabort = () => reject(tx.error);
        }));
    }

    return {
        get: (key) => read(FULL, s => s.get(key)),
        keys: () => read(FULL, s => s.getAllKeys()),
        values: () => read(FULL, s => s.getAll()),
        summaries: () => read(SUMMARY, s => s.getAll()),
        summaryKeys: () => read(SUMMARY, s => s.getAllKeys()),
        // Full payload and its summary commit together so the list store never drifts from the full store.
        save: (key, full, summary) => write([FULL, SUMMARY], tx => {
            tx.objectStore(FULL).put(full, key);
            tx.objectStore(SUMMARY).put(summary, key);
        }),
        setSummary: (key, summary) => write(SUMMARY, tx => { tx.objectStore(SUMMARY).put(summary, key); }),
        remove: (key) => write([FULL, SUMMARY], tx => {
            tx.objectStore(FULL).delete(key);
            tx.objectStore(SUMMARY).delete(key);
        }),
        clear: () => write([FULL, SUMMARY], tx => {
            tx.objectStore(FULL).clear();
            tx.objectStore(SUMMARY).clear();
        }),
    };
})();
