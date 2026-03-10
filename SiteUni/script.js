const elemSlides = document.querySelector(".slides");
const elemBotãoEsquerdo = document.querySelector(".btn-prev");
const elemBotãoDireita = document.querySelector(".btn-next");
const elemsImagem = document.querySelectorAll(".slides img");

let index = 0;

elemBotãoEsquerdo.addEventListener('click', () =>{
    index--;
    if (index < 0) index = elemsImagem.length -1
    atualizarCarrossel();
    // console.log(index);
})

elemBotãoDireita.addEventListener('click', () =>{
    incrementarIndex
    atualizarCarrossel();
 //   console.log(index);
});

const incrementarIndex = () => {
    index++;
    if (index > elemsImagem.length -1) index = 0
}

const atualizarCarrossel = () => {
    elemSlides.style.transform = `translateX(-${index * 100}%)`;
}

setInterval(() =>{
    incrementarIndex();
    atualizarCarrossel();
}, 4000);