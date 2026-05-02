window.cmosAsmEditor = {
    textarea: null,

    create: function (elementId, initialValue) {
        this.textarea = document.getElementById(elementId);
        if (!this.textarea) {
            console.error('ASM editor element not found:', elementId);
            return;
        }
        if (initialValue !== undefined && initialValue !== null) {
            this.textarea.value = initialValue;
        }
        // Future: replace with CodeMirror 6
        // TODO: Add CodeMirror 6 syntax highlighting for ASM
    },

    getValue: function (elementId) {
        const el = document.getElementById(elementId);
        return el ? el.value : '';
    },

    setValue: function (elementId, value) {
        const el = document.getElementById(elementId);
        if (el) el.value = value;
    }
};
