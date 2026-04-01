const elemSlides = document.querySelector(".slides");
const elemBotãoEsquerdo = document.querySelector(".btn-prev");
const elemBotãoDireita = document.querySelector(".btn-next");
const elemCarrossel = document.querySelector(".carrossel");

if (elemSlides && elemBotãoEsquerdo && elemBotãoDireita && elemCarrossel) {
    const elemsImagem = document.querySelectorAll(".slides img");
    const totalOriginal = elemsImagem.length;
    if (totalOriginal > 0) {
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
        if (semTransicao) {
            elemSlides.style.transition = "none";
        }
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
        if (index >= totalSlides) {
            index = 0;
        }
        atualizarCarrossel();
    }

    function voltar() {
        index--;
        if (index < 0) {
            index = totalSlides - 1;
        }
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
        intervalo = setInterval(() => {
            avancar();
        }, INTERVALO_MS);
    }

    function pararAutoPlay() {
        if (intervalo) {
            clearInterval(intervalo);
            intervalo = null;
        }
    }

    elemSlides.addEventListener("transitionend", aoFimDaTransicao);

    elemBotãoEsquerdo.addEventListener("click", () => {
        voltar();
    });

    elemBotãoDireita.addEventListener("click", () => {
        avancar();
    });

    elemCarrossel.addEventListener("mouseenter", () => {
        pararAutoPlay();
    });

    elemCarrossel.addEventListener("mouseleave", () => {
        iniciarAutoPlay();
    });

    iniciarAutoPlay();
    atualizarCarrossel();
    }
}

function mostrarMsgEventos(texto, isErro) {
    const ok = document.getElementById("msg-eventos");
    const er = document.getElementById("msg-eventos-erro");
    if (!ok || !er) return;
    ok.style.display = "none";
    er.style.display = "none";
    if (!texto) return;
    const alvo = isErro ? er : ok;
    alvo.textContent = texto;
    alvo.style.display = "block";
}

function escapeHtml(texto) {
    if (texto == null || texto === undefined) return "";
    return String(texto)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#39;");
}

/** Só http(s) para evitar javascript: em &lt;img src&gt; */
function urlImagemSegura(url) {
    if (!url || typeof url !== "string") return null;
    try {
        const u = new URL(url.trim());
        if (u.protocol !== "http:" && u.protocol !== "https:") return null;
        return u.href;
    } catch {
        return null;
    }
}

function criarBotaoCertificado(eventoId) {
    const a = document.createElement("a");
    a.className = "btn-certificado";
    a.href = `/api/eventos/${eventoId}/certificado`;
    a.textContent = "Baixar certificado";
    a.setAttribute("download", "");
    return a;
}

async function initPaginaEventos() {
    const container = document.getElementById("lista-eventos");
    if (!container) return;

    const params = new URLSearchParams(window.location.search);
    if (params.get("erro_inscricao") === "1") {
        mostrarMsgEventos("Não foi possível concluir a inscrição. Verifique o evento e tente novamente.", true);
    } else if (params.get("inscrito") === "1") {
        mostrarMsgEventos("Inscrição registrada com sucesso.", false);
    }

    let me = { loggedIn: false };
    try {
        const meRes = await fetch("/api/auth/me", { credentials: "include" });
        if (meRes.ok) {
            const raw = await meRes.text();
            try {
                me = JSON.parse(raw);
            } catch {
                me = { loggedIn: false };
            }
        }
    } catch (_) {}

    let data;
    try {
        const evRes = await fetch("/api/public/eventos", { credentials: "include" });
        if (!evRes.ok) {
            container.innerHTML = "<p>Não foi possível carregar os eventos.</p>";
            return;
        }
        const raw = await evRes.text();
        try {
            data = JSON.parse(raw);
        } catch {
            container.innerHTML = "<p>Resposta inválida do servidor.</p>";
            return;
        }
    } catch (_) {
        container.innerHTML = "<p>Não foi possível carregar os eventos.</p>";
        return;
    }

    if (!data.ok || !Array.isArray(data.eventos)) {
        container.innerHTML = "<p>Não foi possível carregar os eventos.</p>";
        return;
    }

    if (data.eventos.length === 0) {
        container.innerHTML = "<p>Nenhum evento cadastrado no momento.</p>";
        return;
    }

    container.innerHTML = data.eventos
        .map((ev) => {
            const id = String(ev.id).replace(/[^\d]/g, "");
            if (!id) return "";
            const inscrito = ev.inscrito === "true";
            let acoes = "";
            if (!me.loggedIn) {
                acoes =
                    '<p class="evento-acoes"><a href="index.html#login">Faça login</a> para se inscrever e obter o certificado.</p>';
            } else if (inscrito) {
                acoes = `<a class="btn-certificado" href="/api/eventos/${id}/certificado">Baixar certificado</a>`;
            } else {
                acoes = `<button type="button" class="btn-inscrever" data-evento-id="${id}">Inscrever-se</button>`;
            }
            const urlFoto = urlImagemSegura(ev.fotoUrl);
            const foto = urlFoto ? `<img src="${escapeHtml(urlFoto)}" alt="">` : "";
            return `
            <article class="evento-card" data-evento-id="${id}">
                ${foto}
                <div class="evento-corpo">
                    <h3>${escapeHtml(ev.titulo)}</h3>
                    <p class="evento-meta"><strong>Data:</strong> ${escapeHtml(ev.data)} às ${escapeHtml(ev.horario)}</p>
                    <p class="evento-meta"><strong>Endereço:</strong> ${escapeHtml(ev.endereco)}</p>
                    ${ev.descricao ? `<p class="evento-desc">${escapeHtml(ev.descricao)}</p>` : ""}
                    <div class="evento-botoes">${acoes}</div>
                </div>
            </article>`;
        })
        .filter(Boolean)
        .join("");

    container.querySelectorAll(".btn-inscrever").forEach((btn) => {
        btn.addEventListener("click", async () => {
            const id = btn.getAttribute("data-evento-id");
            const eventoId = parseInt(id, 10);
            if (!Number.isFinite(eventoId) || eventoId < 1) {
                mostrarMsgEventos("Identificador do evento inválido.", true);
                return;
            }
            mostrarMsgEventos("", false);
            try {
                const r = await fetch("/api/eventos/inscrever", {
                    method: "POST",
                    credentials: "include",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ evento_id: eventoId }),
                });
                const j = await r.json().catch(() => ({}));
                if (!r.ok || !j.ok) {
                    mostrarMsgEventos(j.error || "Não foi possível concluir a inscrição.", true);
                    return;
                }
                const wrap = btn.parentElement;
                btn.remove();
                wrap.appendChild(criarBotaoCertificado(String(eventoId)));
                mostrarMsgEventos("Inscrição confirmada. Você já pode baixar o certificado em PDF.", false);
            } catch (_) {
                mostrarMsgEventos("Erro de rede ao inscrever.", true);
            }
        });
    });
}

initPaginaEventos();
