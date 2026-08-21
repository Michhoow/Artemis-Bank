(function () {
    'use strict';

    function aNumero(texto) {
        var limpio = (texto || '').toString().replace(/[^\d.-]/g, '');
        var valor = parseFloat(limpio);
        return isNaN(valor) ? null : valor;
    }

    function formatear(valor) {
        return valor.toLocaleString('es-DO', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    }

    function enlazar(formulario) {
        var selector = formulario.querySelector('[data-tope-origen]');
        var campo = formulario.querySelector('[data-tope-monto]');
        if (!selector || !campo) return;

        var aviso = document.createElement('div');
        aviso.className = 'form-text text-danger d-none';
        campo.closest('.mb-3').appendChild(aviso);

        function topeActual() {
            var opcion = selector.options[selector.selectedIndex];
            if (!opcion || !opcion.value) return null;
            return aNumero(opcion.getAttribute('data-tope'));
        }

        function aplicarTope(avisar) {
            var tope = topeActual();

            if (tope === null || tope <= 0) {
                campo.removeAttribute('max');
                aviso.classList.add('d-none');
                return;
            }

            campo.setAttribute('max', tope.toFixed(2));

            var monto = aNumero(campo.value);
            if (monto !== null && monto > tope) {
                campo.value = tope.toFixed(2);
                if (avisar) {
                    aviso.textContent = 'El monto máximo que puede pagar es RD$' + formatear(tope) +
                                        '. Se ajustó automáticamente.';
                    aviso.classList.remove('d-none');
                }
                return;
            }

            aviso.classList.add('d-none');
        }

        selector.addEventListener('change', function () { aplicarTope(false); });
        campo.addEventListener('input', function () { aviso.classList.add('d-none'); });
        campo.addEventListener('blur', function () { aplicarTope(true); });
        formulario.addEventListener('submit', function () { aplicarTope(true); });

        aplicarTope(false);
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('form[data-tope-pago]').forEach(enlazar);
    });
})();
