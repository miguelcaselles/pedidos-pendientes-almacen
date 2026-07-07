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

    // Al elegir archivo en las tarjetas de carga, resaltar el botón.
    document.querySelectorAll('input[type="file"]').forEach(function (input) {
        input.addEventListener("change", function () {
            var btn = input.closest("form")?.querySelector('button[type="submit"]');
            if (btn && input.files.length > 0) btn.classList.add("btn-ready");
        });
    });
})();
