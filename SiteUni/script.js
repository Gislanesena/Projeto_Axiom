function initCarousel() {
    const elemSlides = document.querySelector(".slides");
    const elemBotaoEsquerdo = document.querySelector(".btn-prev");
    const elemBotaoDireita = document.querySelector(".btn-next");
    const elemCarrossel = document.querySelector(".carrossel");
    if (!elemSlides || !elemBotaoEsquerdo || !elemBotaoDireita || !elemCarrossel) return;

    const elemsImagem = document.querySelectorAll(".slides img");
    const totalOriginal = elemsImagem.length;
    if (!totalOriginal) return;

    elemsImagem.forEach((img) => {
        const clone = img.cloneNode(true);
        elemSlides.appendChild(clone);
    });

    const totalSlides = elemSlides.children.length;
    const percentualPorSlide = 100 / totalSlides;

    elemSlides.style.width = totalSlides * 100 + "%";
    elemSlides.querySelectorAll("img").forEach((img) => {
        img.style.flex = `0 0 ${percentualPorSlide}%`;
    });

    let index = 0;
    let intervalo;
    const INTERVALO_MS = 4000;

    function atualizarCarrossel(semTransicao = false) {
        if (semTransicao) elemSlides.style.transition = "none";
        const percentual = (index / totalSlides) * 100;
        elemSlides.style.transform = `translateX(-${percentual}%)`;
        if (semTransicao) {
            requestAnimationFrame(() => {
                requestAnimationFrame(() => {
                    elemSlides.style.transition = "";
                });
            });
        }
    }

    function avancar() {
        index++;
        if (index >= totalSlides) index = 0;
        atualizarCarrossel();
    }

    function voltar() {
        index--;
        if (index < 0) index = totalSlides - 1;
        atualizarCarrossel();
    }

    function aoFimDaTransicao(e) {
        if (e.propertyName !== "transform") return;
        if (index === totalOriginal) {
            index = 0;
            atualizarCarrossel(true);
        }
    }

    function iniciarAutoPlay() {
        intervalo = setInterval(avancar, INTERVALO_MS);
    }

    function pararAutoPlay() {
        if (!intervalo) return;
        clearInterval(intervalo);
        intervalo = null;
    }

    elemSlides.addEventListener("transitionend", aoFimDaTransicao);
    elemBotaoEsquerdo.addEventListener("click", voltar);
    elemBotaoDireita.addEventListener("click", avancar);
    elemCarrossel.addEventListener("mouseenter", pararAutoPlay);
    elemCarrossel.addEventListener("mouseleave", iniciarAutoPlay);

    iniciarAutoPlay();
    atualizarCarrossel();
}

function formatarData(dataIso) {
    const data = new Date(`${dataIso}T00:00:00`);
    return data.toLocaleDateString("pt-BR");
}

function mostrar(id, texto) {
    const elem = document.getElementById(id);
    if (!elem) return;
    elem.textContent = texto;
    elem.style.display = "block";
}

function esconder(id) {
    const elem = document.getElementById(id);
    if (!elem) return;
    elem.style.display = "none";
}

async function initEventos() {
    const lista = document.getElementById("lista-eventos");
    if (!lista) return;

    esconder("msg-evento");
    esconder("msg-evento-erro");

    let auth = { logado: false };
    try {
        const authResp = await fetch("/api/auth/status");
        if (authResp.ok) auth = await authResp.json();
    } catch (_) {}

    const resp = await fetch("/api/eventos");
    if (!resp.ok) {
        mostrar("msg-evento-erro", "Nao foi possivel carregar os eventos.");
        return;
    }

    const eventos = await resp.json();
    if (!eventos.length) {
        lista.innerHTML = "<p>Nenhum evento cadastrado no momento.</p>";
        return;
    }

    lista.innerHTML = eventos.map((evento) => `
        <article class="evento-card">
            <img src="${evento.fotoUrl || "https://via.placeholder.com/900x500?text=Evento"}" alt="Imagem do evento">
            <div class="evento-info">
                <h3>${evento.titulo}</h3>
                <p><strong>Data:</strong> ${formatarData(evento.dataEvento)} as ${evento.horario}</p>
                <p><strong>Endereco:</strong> ${evento.endereco}</p>
                <p>${evento.descricao || ""}</p>
                <button class="btn-inscrever" data-evento-id="${evento.id}">
                    Inscrever-se
                </button>
            </div>
        </article>
    `).join("");

    lista.querySelectorAll(".btn-inscrever").forEach((botao) => {
        botao.addEventListener("click", async () => {
            esconder("msg-evento");
            esconder("msg-evento-erro");

            if (!auth.logado) {
                mostrar("msg-evento-erro", "Faca login para se inscrever em um evento.");
                window.location.href = "login.html";
                return;
            }

            const eventoId = Number(botao.getAttribute("data-evento-id"));
            const insResp = await fetch("/api/eventos/inscrever", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ eventoId })
            });

            if (insResp.ok) {
                mostrar("msg-evento", "Inscricao realizada com sucesso.");
            } else if (insResp.status === 401) {
                mostrar("msg-evento-erro", "Sua sessao expirou. Faca login novamente.");
            } else {
                mostrar("msg-evento-erro", "Nao foi possivel concluir a inscricao.");
            }
        });
    });
}

function initContato() {
    const form = document.getElementById("form-contato");
    if (!form) return;

    form.addEventListener("submit", async (e) => {
        e.preventDefault();
        esconder("msg-ok");
        esconder("msg-erro");

        const dados = new FormData(form);
        const payload = {
            nome: String(dados.get("nome") || "").trim(),
            telefone: String(dados.get("telefone") || "").trim(),
            email: String(dados.get("email") || "").trim(),
            assunto: String(dados.get("assunto") || "").trim(),
            mensagem: String(dados.get("mensagem") || "").trim()
        };

        const resp = await fetch("/api/contato", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        if (resp.ok) {
            mostrar("msg-ok", "Mensagem enviada com sucesso. Obrigado pelo contato!");
            form.reset();
        } else {
            mostrar("msg-erro", "Preencha todos os campos obrigatorios.");
        }
    });
}

function initLogin() {
    const form = document.getElementById("form-login");
    if (!form) return;

    form.addEventListener("submit", async (e) => {
        e.preventDefault();
        esconder("msg-login-ok");
        esconder("msg-login-erro");

        const dados = new FormData(form);
        const payload = {
            nomeUsuario: String(dados.get("nome_usuario") || "").trim(),
            senha: String(dados.get("senha") || "")
        };

        const resp = await fetch("/api/login", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        if (resp.ok) {
            mostrar("msg-login-ok", "Login realizado com sucesso. Redirecionando para eventos...");
            setTimeout(() => {
                window.location.href = "eventos.html";
            }, 700);
            return;
        }

        mostrar("msg-login-erro", "Usuario ou senha invalidos.");
    });
}

initCarousel();
initEventos();
initContato();
initLogin();
