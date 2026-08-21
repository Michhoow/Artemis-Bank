(function () {
    'use strict';

    var DIGITOS = 11;

    function soloDigitos(valor) {
        return (valor || '').replace(/\D/g, '').slice(0, DIGITOS);
    }

    function aplicarMascara(digitos) {
        if (digitos.length <= 3) return digitos;
        if (digitos.length <= 10) return digitos.slice(0, 3) + '-' + digitos.slice(3);
        return digitos.slice(0, 3) + '-' + digitos.slice(3, 10) + '-' + digitos.slice(10);
    }

    function posicionCursor(textoFormateado, digitosAntesDelCursor) {
        var vistos = 0;
        for (var i = 0; i < textoFormateado.length; i++) {
            if (/\d/.test(textoFormateado[i])) {
                vistos++;
                if (vistos === digitosAntesDelCursor) return i + 1;
            }
        }
        return textoFormateado.length;
    }

    function enlazar(campo) {
        campo.setAttribute('inputmode', 'numeric');
        campo.setAttribute('maxlength', String(DIGITOS + 2));
        campo.setAttribute('autocomplete', 'off');

        if (!campo.placeholder) campo.placeholder = '000-0000000-0';

        campo.value = aplicarMascara(soloDigitos(campo.value));

        campo.addEventListener('input', function () {
            var cursor = campo.selectionStart;
            var digitosAntes = soloDigitos(campo.value.slice(0, cursor)).length;

            var formateado = aplicarMascara(soloDigitos(campo.value));
            campo.value = formateado;

            var nuevaPos = posicionCursor(formateado, digitosAntes);
            campo.setSelectionRange(nuevaPos, nuevaPos);
        });

        campo.addEventListener('paste', function (evento) {
            evento.preventDefault();
            var pegado = (evento.clipboardData || window.clipboardData).getData('text');
            campo.value = aplicarMascara(soloDigitos(pegado));
        });

        campo.addEventListener('keydown', function (evento) {
            var permitidas = ['Backspace', 'Delete', 'Tab', 'Escape', 'Enter',
                              'Home', 'End', 'ArrowLeft', 'ArrowRight'];
            if (permitidas.indexOf(evento.key) !== -1) return;
            if (evento.ctrlKey || evento.metaKey) return;
            if (!/^\d$/.test(evento.key)) evento.preventDefault();
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-cedula]').forEach(enlazar);
    });
})();
