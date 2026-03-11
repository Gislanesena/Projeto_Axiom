const elemSlides = document.querySelector(".slides");
const elemBotãoEsquerdo = document.querySelector(".btn-prev");
const elemBotãoDireita = document.querySelector(".btn-next");
const elemsImagem = document.querySelectorAll(".slides img");

const primeiroClone = elemsImagem[0].cloneNode(true);
elemSlides.appendChild(primeiroClone);

let index = 0;

elemBotãoEsquerdo.addEventListener('click', () =>{
    index--;
    if (index < 0) index = elemsImagem.length -1
    atualizarCarrossel();
    // console.log(index);
})

elemBotãoDireita.addEventListener('click', () =>{
    incrementarIndex();
    atualizarCarrossel();
 //   console.log(index);
});

// const incrementarIndex = () => {
//     index++;
//     if (index > elemsImagem.length -1) index = 0
// }

const incrementarIndex = () => {
    index++;
}

// const atualizarCarrossel = () => {
//     elemSlides.style.transform = `translateX(-${index * 100}%)`;
// }

const atualizarCarrossel = () => {

    elemSlides.style.transition = "transform 0.5s ease";
    elemSlides.style.transform = `translateX(-${index * 100}%)`;

    if(index === elemsImagem.length){
        setTimeout(() => {
            elemSlides.style.transition = "none";
            index = 0;
            elemSlides.style.transform = `translateX(0%)`;
        }, 500);
    }

}

const proximoSlide = () => {
    incrementarIndex();
    atualizarCarrossel();
}

let intervalo = setInterval(proximoSlide, 3000);

elemSlides.addEventListener("mouseenter", () => {
    clearInterval(intervalo);
});

elemSlides.addEventListener("mouseleave", () => {
    intervalo = setInterval(proximoSlide, 4000);
});

const carrossel = document.querySelector(".carrossel");

carrossel.addEventListener("mousemove", (e) => {

    const rect = carrossel.getBoundingClientRect();

    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;

    carrossel.style.setProperty("--x", x + "px");
    carrossel.style.setProperty("--y", y + "px");

});