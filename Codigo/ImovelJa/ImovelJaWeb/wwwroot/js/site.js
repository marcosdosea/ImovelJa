(function () {
    "use strict";

    /* Envio único: nos formulários marcados com data-envio-unico, desativa os
       botões depois do primeiro clique válido. Sem isso, um clique duplo em
       "Aprovar" manda dois pedidos, e o segundo falha porque o anúncio já foi
       validado. Se a validação recusar o envio, os botões continuam ativos. */
    function ligarEnvioUnico(formulario) {
        formulario.addEventListener("submit", function (evento) {
            if (evento.defaultPrevented || (typeof formulario.checkValidity === "function" && !formulario.checkValidity())) {
                return;
            }

            var botao = evento.submitter;

            /* Botão desativado não é enviado: o valor dele (aprovado=true ou
               false) vai num campo escondido antes de desativar. */
            if (botao && botao.name) {
                var campo = document.createElement("input");
                campo.type = "hidden";
                campo.name = botao.name;
                campo.value = botao.value;
                campo.setAttribute("data-envio-unico-copia", "");
                formulario.appendChild(campo);
            }

            formulario.querySelectorAll("button[type='submit']").forEach(function (item) {
                item.disabled = true;
            });

            if (botao) {
                botao.setAttribute("aria-busy", "true");
            }
        });
    }

    /* Ao voltar pelo histórico do navegador, a página pode vir da memória
       com os botões ainda desativados: reativa e tira o campo escondido. */
    function reativarFormularios(evento) {
        if (!evento.persisted) {
            return;
        }
        document.querySelectorAll("form[data-envio-unico]").forEach(function (formulario) {
            formulario.querySelectorAll("button[type='submit']").forEach(function (item) {
                item.disabled = false;
                item.removeAttribute("aria-busy");
            });
            formulario.querySelectorAll("input[data-envio-unico-copia]").forEach(function (item) {
                item.remove();
            });
        });
    }

    /* Cadastro em duas etapas, no mesmo formulário: a etapa 2 fica oculta
       até os campos visíveis da etapa 1 serem aceitos. Se o servidor devolver
       erro, abre a etapa que contém o campo inválido. */
    function ligarEtapas(formulario) {
        var etapas = formulario.querySelectorAll("[data-etapa]");
        var indicadores = formulario.querySelectorAll("[data-etapa-indicador]");
        if (!etapas.length) {
            return;
        }

        function etapaComErro(numero) {
            var etapa = formulario.querySelector("[data-etapa='" + numero + "']");
            if (!etapa) {
                return false;
            }

            return etapa.querySelector(".input-validation-error, .field-validation-error:not(:empty)") !== null;
        }

        function mostrar(numero, focar) {
            etapas.forEach(function (etapa) {
                etapa.hidden = etapa.getAttribute("data-etapa") !== String(numero);
            });

            indicadores.forEach(function (item) {
                var indice = Number(item.getAttribute("data-etapa-indicador"));
                var atual = indice === numero;
                item.classList.toggle("etapas__item--atual", atual);
                item.classList.toggle("etapas__item--feita", indice < numero);
                if (atual) {
                    item.setAttribute("aria-current", "step");
                } else {
                    item.removeAttribute("aria-current");
                }
            });

            if (focar) {
                var foco = formulario.querySelector("[data-etapa='" + numero + "'] input, [data-etapa='" + numero + "'] select, [data-etapa='" + numero + "'] textarea");
                if (foco) {
                    foco.focus();
                }
            }
        }

        mostrar(etapaComErro(2) && !etapaComErro(1) ? 2 : 1, false);

        var proxima = formulario.querySelector("[data-etapa-proxima]");
        if (proxima) {
            proxima.addEventListener("click", function () {
                var campos = formulario.querySelectorAll("[data-etapa='1'] input, [data-etapa='1'] select, [data-etapa='1'] textarea");
                var validador = window.jQuery ? window.jQuery(formulario).data("validator") : null;
                var primeiroInvalido = null;

                for (var i = 0; i < campos.length; i++) {
                    var valido = validador ? validador.element(campos[i]) !== false : campos[i].checkValidity();
                    if (!valido && !primeiroInvalido) {
                        primeiroInvalido = campos[i];
                    }
                }

                if (primeiroInvalido) {
                    primeiroInvalido.focus();
                    return;
                }

                mostrar(2, true);
            });
        }

        var anterior = formulario.querySelector("[data-etapa-anterior]");
        if (anterior) {
            anterior.addEventListener("click", function () {
                mostrar(1, true);
            });
        }
    }

    function iniciar() {
        document.querySelectorAll("form[data-envio-unico]").forEach(ligarEnvioUnico);
        document.querySelectorAll("form[data-etapas]").forEach(ligarEtapas);
    }

    function agendar() {
        window.setTimeout(iniciar, 0);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", agendar);
    } else {
        agendar();
    }
      /* Confirmação: nos formulários marcados com data-confirm-nome, pede
    confirmação antes de bloquear. O nome vem de um atributo, lido como
    texto puro, e nunca é montado dentro de código JavaScript. */
    function ligarConfirmacao(formulario) {
        formulario.addEventListener("submit", function (evento) {
            var nome = formulario.getAttribute("data-confirm-nome");

            if (!window.confirm("Deseja realmente bloquear o usuário " + nome + "?")) {
                evento.preventDefault();
            }
        });
    }
    window.addEventListener("pageshow", reativarFormularios);

    /* A conversa abre já no fim do histórico, na mensagem mais recente. */
    document.querySelectorAll("[data-rolar-fim]").forEach(function (painel) {
        painel.scrollTop = painel.scrollHeight;
    });
    document.querySelectorAll("form[data-confirm-nome]").forEach(ligarConfirmacao);

    /* Filtro de lista: o campo com data-filtro-lista="id da lista" mostra só
       os itens cujo data-filtro-texto contém o que foi digitado, sem diferença
       de maiúsculas e acentos. Para CPF/CNPJ, a pontuação digitada é ignorada.
       Se nenhum item combinar, aparece o aviso data-filtro-vazio da lista. */
    function normalizar(texto) {
        return texto.normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLowerCase();
    }

    function ligarFiltro(campo) {
        var lista = document.getElementById(campo.getAttribute("data-filtro-lista"));
        if (!lista) {
            return;
        }

        var itens = lista.querySelectorAll("[data-filtro-texto]");
        var aviso = document.querySelector("[data-filtro-vazio='" + lista.id + "']");

        campo.addEventListener("input", function () {
            var termo = normalizar(campo.value.trim());
            var soDocumento = /^[0-9.\-\/\s]+$/.test(termo);
            var digitos = termo.replace(/[^0-9]/g, "");
            var visiveis = 0;

            itens.forEach(function (item) {
                var texto = normalizar(item.getAttribute("data-filtro-texto"));
                var combina = termo === "" ||
                    (soDocumento ? texto.indexOf(digitos) !== -1 : texto.indexOf(termo) !== -1);

                item.hidden = !combina;
                if (combina) {
                    visiveis++;
                }
            });

            if (aviso) {
                aviso.hidden = visiveis !== 0;
            }
        });
    }

    document.querySelectorAll("[data-filtro-lista]").forEach(ligarFiltro);

    /* Valor faltante do pagamento (Tela_012.2): acompanha o valor pago digitado.
       O campo traz o valor do aluguel em data-aluguel e o id do campo do valor
       pago em data-valor-faltante. */
    function ligarValorFaltante(faltante) {
        var valorPago = document.getElementById(faltante.getAttribute("data-valor-faltante"));
        var aluguel = parseFloat(faltante.getAttribute("data-aluguel") || "0");
        if (!valorPago || isNaN(aluguel)) {
            return;
        }

        valorPago.addEventListener("input", function () {
            var pago = parseFloat(valorPago.value);
            var resta = Math.max(0, aluguel - (isNaN(pago) ? 0 : pago));
            faltante.value = resta.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
        });
    }

    document.querySelectorAll("[data-valor-faltante]").forEach(ligarValorFaltante);

    /* Janela que abre sozinha ao carregar a página, como a confirmação de
       plano ativado (Tela_005.1.1). O componente de janela é o do Bootstrap. */
    document.querySelectorAll(".modal[data-abrir-ao-carregar]").forEach(function (janela) {
        if (window.bootstrap) {
            window.bootstrap.Modal.getOrCreateInstance(janela).show();
        }
    });

})();
