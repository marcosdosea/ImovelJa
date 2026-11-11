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

    function iniciar() {
        document.querySelectorAll("form[data-envio-unico]").forEach(ligarEnvioUnico);
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
    document.querySelectorAll("form[data-envio-unico]").forEach(ligarEnvioUnico);
    document.querySelectorAll("form[data-confirm-nome]").forEach(ligarConfirmacao);

})();
