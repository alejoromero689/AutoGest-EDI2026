window.autogest = {
    descargarCsv(nombre, filas) {
        const celda = valor => {
            let texto = String(valor ?? "");
            // Evita interpretar datos ingresados por usuarios como fórmulas de Excel.
            if (/^[\s]*[=+\-@]/.test(texto) || /^[\t\r\n]/.test(texto)) texto = "'" + texto;
            return '"' + texto.replaceAll('"', '""') + '"';
        };
        const csv = "\uFEFF" + filas.map(f => f.map(celda).join(";")).join("\r\n");
        const url = URL.createObjectURL(new Blob([csv], { type: "text/csv;charset=utf-8;" }));
        const enlace = document.createElement("a");
        enlace.href = url; enlace.download = nombre;
        document.body.appendChild(enlace); enlace.click(); enlace.remove();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
    }
};
