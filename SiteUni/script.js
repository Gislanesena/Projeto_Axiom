const elemSlides = document.querySelector(".slides");
const elemBotãoEsquerdo = document.querySelector(".btn-prev");
const elemBotãoDireita = document.querySelector(".btn-next");
const elemCarrossel = document.querySelector(".carrossel");

const elemsImagem = document.querySelectorAll(".slides img");
const totalOriginal = elemsImagem.length;

// Clona as imagens e adiciona ao final para efeito contínuo (só vai para um lado)
elemsImagem.forEach((img) => {
    const clone = img.cloneNode(true);
    elemSlides.appendChild(clone);
});

const totalSlides = elemSlides.children.length;
const percentualPorSlide = 100 / totalSlides;

// Largura total do container e tamanho de cada slide
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
    // Quando chega na primeira “cópia” (índice = totalOriginal), reseta sem animação
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

// Pausa com o mouse em cima do carrossel
elemCarrossel.addEventListener("mouseenter", () => {
    pararAutoPlay();
});

elemCarrossel.addEventListener("mouseleave", () => {
    iniciarAutoPlay();
});

// Inicia o carrossel
iniciarAutoPlay();
atualizarCarrossel();
