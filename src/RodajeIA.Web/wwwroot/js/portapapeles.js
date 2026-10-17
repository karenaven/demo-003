// Copia texto al portapapeles; devuelve true o false y nunca lanza (FR-028a).
window.rodajeia = window.rodajeia || {};
window.rodajeia.copiar = async function (texto) {
    try {
        if (!navigator.clipboard) {
            return false;
        }
        await navigator.clipboard.writeText(texto);
        return true;
    } catch {
        return false;
    }
};
