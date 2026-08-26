// Comportamientos mínimos de la app (sin dependencias externas).
(function () {
    "use strict";

    // Confirmación en formularios destructivos: <form data-confirm="¿Seguro?">
    document.addEventListener("submit", function (e) {
        var form = e.target.closest("form[data-confirm]");
        if (form && !window.confirm(form.getAttribute("data-confirm"))) {
            e.preventDefault();
        }
    });

    // Autocierre de los avisos de éxito a los 6 segundos.
    document.querySelectorAll(".alert[data-autodismiss]").forEach(function (el) {
        setTimeout(function () {
            el.style.transition = "opacity .4s";
            el.style.opacity = "0";
            setTimeout(function () { el.remove(); }, 400);
        }, 6000);
    });

    // Al elegir archivo en las tarjetas de carga, resaltar el botón y avisar si
    // supera el límite del servidor (20 MB) antes de perder tiempo subiéndolo.
    var MAX_UPLOAD = 20 * 1000 * 1000;
    document.querySelectorAll('input[type="file"]').forEach(function (input) {
        input.addEventListener("change", function () {
            var form = input.closest("form");
            var btn = form ? form.querySelector('button[type="submit"]') : null;
            if (!btn || input.files.length === 0) return;
            if (input.files[0].size > MAX_UPLOAD) {
                btn.classList.remove("btn-ready");
                btn.disabled = true;
                window.alert("El archivo supera el límite de 20 MB. Exporta un listado más acotado.");
                input.value = "";
                btn.disabled = false;
                return;
            }
            btn.classList.add("btn-ready");
        });
    });

    // Evitar dobles envíos (doble clic en "Cargar" o en las acciones de fila):
    // al enviar, el botón se deshabilita hasta que la página navegue.
    document.addEventListener("submit", function (e) {
        var form = e.target;
        if (e.defaultPrevented || !(form instanceof HTMLFormElement)) return;
        var btn = form.querySelector('button[type="submit"]');
        if (btn) setTimeout(function () { btn.disabled = true; }, 0);
    });
})();
