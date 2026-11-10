(function () {
    "use strict";

    var formulario = document.getElementById("form-plano");
    if (!formulario) {
        return;
    }

    var campoValor = formulario.querySelector("[data-preco-campo]");
    var textoPreco = formulario.querySelector("[data-preco-texto]");
    var periodo = formulario.querySelector("[data-preco-periodo]");
    var alertaBasico = formulario.querySelector("[data-alerta-basico]");
    var nome = formulario.querySelector("[name='Nome']");

    function atualizarPreco() {
        if (!campoValor || !textoPreco) {
            return;
        }

        var texto = campoValor.value.trim();
        if (texto === "") {
            textoPreco.textContent = "—";
            if (periodo) {
                periodo.hidden = true;
            }
            return;
        }

        var valor = Number(texto);
        if (!Number.isFinite(valor)) {
            return;
        }

        if (valor === 0) {
            textoPreco.textContent = "Grátis";
            if (periodo) {
                periodo.hidden = true;
            }
            return;
        }

        textoPreco.textContent = valor.toLocaleString("pt-BR", {
            style: "currency",
            currency: "BRL"
        });
        if (periodo) {
            periodo.hidden = false;
        }
    }

    if (campoValor) {
        campoValor.addEventListener("input", atualizarPreco);
    }

    formulario.addEventListener("submit", function (evento) {
        if (!nome || nome.value !== "Básico" || !campoValor) {
            return;
        }

        if (evento.submitter && evento.submitter.getAttribute("formaction")) {
            return;
        }

        if (Number(campoValor.value) === 0) {
            if (alertaBasico) {
                alertaBasico.hidden = true;
            }
            return;
        }

        evento.preventDefault();
        evento.stopImmediatePropagation();
        if (alertaBasico) {
            alertaBasico.hidden = false;
        }
        campoValor.focus();
    }, true);

    var editor = formulario.querySelector("[data-beneficios]");
    var campo = editor && editor.querySelector("textarea");
    var lista = editor && editor.querySelector("[data-beneficios-lista]");
    var entrada = editor && editor.querySelector("[data-beneficio-novo]");
    var adicionar = editor && editor.querySelector("[data-beneficio-adicionar]");

    if (!editor || !campo || !lista || !entrada || !adicionar) {
        return;
    }

    function ler() {
        return campo.value
            .split(/\r?\n/)
            .map(function (item) { return item.trim(); })
            .filter(Boolean);
    }

    function gravar(itens) {
        campo.value = itens.join("\n");
    }

    function criarIcone() {
        var svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
        svg.setAttribute("viewBox", "0 0 24 24");
        svg.setAttribute("fill", "none");
        svg.setAttribute("stroke", "currentColor");
        svg.setAttribute("stroke-width", "2.25");
        svg.setAttribute("stroke-linecap", "round");
        svg.setAttribute("stroke-linejoin", "round");
        svg.setAttribute("aria-hidden", "true");
        var marca = document.createElementNS("http://www.w3.org/2000/svg", "polyline");
        marca.setAttribute("points", "20 6 9 17 4 12");
        svg.appendChild(marca);
        return svg;
    }

    function desenhar() {
        lista.replaceChildren();
        ler().forEach(function (texto, indice) {
            var item = document.createElement("li");
            item.className = "beneficio-item";

            item.appendChild(criarIcone());

            var descricao = document.createElement("span");
            descricao.className = "beneficio-item__texto";
            descricao.textContent = texto;
            item.appendChild(descricao);

            var remover = document.createElement("button");
            remover.type = "button";
            remover.className = "beneficio-item__remover";
            remover.setAttribute("aria-label", "Remover benefício " + texto);
            remover.textContent = "×";
            remover.addEventListener("click", function () {
                var itens = ler();
                itens.splice(indice, 1);
                gravar(itens);
                desenhar();
            });
            item.appendChild(remover);
            lista.appendChild(item);
        });
    }

    function incluir() {
        var texto = entrada.value.trim();
        if (!texto) {
            entrada.focus();
            return;
        }

        var itens = ler();
        itens.push(texto);
        gravar(itens);
        entrada.value = "";
        desenhar();
        entrada.focus();
    }

    adicionar.addEventListener("click", incluir);
    entrada.addEventListener("keydown", function (evento) {
        if (evento.key === "Enter") {
            evento.preventDefault();
            incluir();
        }
    });

    editor.classList.add("beneficios-editor--ativo");
    desenhar();
})();
