window.chart = {
  animatePath: (selector) => {
    const p = document.querySelector(selector);
    if (!p) return;
    const len = p.getTotalLength();
    p.style.transition = "none";
    p.style.strokeDasharray = `${len} ${len}`;
    p.style.strokeDashoffset = len;
    requestAnimationFrame(() => {
      p.style.transition = "stroke-dashoffset 800ms ease";
      p.style.strokeDashoffset = "0";
    });
  },
};
