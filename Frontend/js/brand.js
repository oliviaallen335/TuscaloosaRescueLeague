(() => {
  const isInPages = location.pathname.includes("/pages/");
  const logoPath = isInPages ? "../assets/logo.png" : "assets/logo.png";
  const brandName = "Tuscaloosa Rescue League";

  document.querySelectorAll(".logo").forEach((el) => {
    el.innerHTML = `
      <img src="${logoPath}" alt="${brandName} logo">
      <span>${brandName}</span>
    `;
  });

  // Highlight current nav destination for cleaner orientation.
  const currentPath = location.pathname.replace(/\\/g, "/").toLowerCase();
  document.querySelectorAll("nav a[href]").forEach((a) => {
    const href = a.getAttribute("href");
    if (!href || href.startsWith("http")) return;
    const cleanHref = href.replace(/^\.{1,2}\//, "").toLowerCase();
    if (!cleanHref) return;
    if (currentPath.endsWith(cleanHref)) {
      a.classList.add("is-active");
      a.setAttribute("aria-current", "page");
    }
  });
})();
