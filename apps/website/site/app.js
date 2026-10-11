// VYRA site: Partikel-Hero, Scroll-Reveal, Release-Tabelle, GitHub-Link. Respektiert Reduced Motion.
(function () {
  "use strict";
  var reduced = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  var burger = document.getElementById("burger");
  if (burger) burger.addEventListener("click", function () {
    var n = document.getElementById("navlinks");
    if (n) n.classList.toggle("open");
  });

  var els = document.querySelectorAll(".reveal");
  if ("IntersectionObserver" in window && !reduced) {
    var io = new IntersectionObserver(function (es) {
      es.forEach(function (e) { if (e.isIntersecting) { e.target.classList.add("vis"); io.unobserve(e.target); } });
    }, { threshold: 0.12 });
    els.forEach(function (el) { io.observe(el); });
  } else {
    els.forEach(function (el) { el.classList.add("vis"); });
  }

  var cv = document.getElementById("bg");
  if (cv && !reduced) {
    var ctx = cv.getContext("2d"), ps = [], run = true;
    function size() { cv.width = innerWidth; cv.height = innerHeight; }
    size(); addEventListener("resize", size);
    for (var i = 0; i < 60; i++) ps.push({ x: Math.random(), y: Math.random(), s: 0.3 + Math.random(), v: 0.0002 + Math.random() * 0.0006 });
    document.addEventListener("visibilitychange", function () { run = !document.hidden; if (run) tick(); });
    (function tick() {
      if (!run) return;
      ctx.clearRect(0, 0, cv.width, cv.height);
      for (var k = 0; k < ps.length; k++) {
        var p = ps[k]; p.y -= p.v; if (p.y < 0) p.y = 1;
        var g = ctx.createRadialGradient(p.x * cv.width, p.y * cv.height, 0, p.x * cv.width, p.y * cv.height, 40 * p.s);
        g.addColorStop(0, "rgba(139,123,255,.5)"); g.addColorStop(1, "rgba(139,123,255,0)");
        ctx.fillStyle = g; ctx.fillRect(p.x * cv.width - 40, p.y * cv.height - 40, 80, 80);
      }
      requestAnimationFrame(tick);
    })();
  }

  var hf = document.getElementById("hero-fps"), last = performance.now(), acc = 0, n = 0;
  if (hf && !reduced) {
    (function loop(t) {
      var dt = t - last; last = t; acc += dt; n++;
      if (acc >= 1000) { hf.textContent = Math.round(1000 / (acc / n)) + " FPS (Demo)"; acc = 0; n = 0; }
      requestAnimationFrame(loop);
    })(last);
  }

  // Release-Daten: lokale Builds + optionaler GitHub-Release-Link (release.json: github, downloadUrl)
  function renderRelease(d) {
    var tbl = document.getElementById("release-table");
    if (tbl) {
      var html = "";
      (d.files || []).forEach(function (f) {
        html += "<tr><td><code>" + f.name + "</code></td><td>" + d.version + "</td><td>" + d.date + "</td><td>Windows</td><td>" + f.size + "</td><td><code>" + f.sha256.substring(0, 16) + "…</code></td></tr>";
      });
      tbl.innerHTML = html || "<tr><td colspan='6'>Keine Dateien.</td></tr>";
      var full = document.getElementById("release-sha");
      if (full && d.files) full.textContent = d.files.map(function (f) { return f.sha256 + "  " + f.name; }).join("\n");
    }
    var gl = document.getElementById("github-link");
    if (gl && d.github) {
      var a = document.createElement("a");
      a.href = d.downloadUrl || d.github;
      a.textContent = d.downloadUrl ? "Installer aus GitHub Release laden" : "Quellcode auf GitHub";
      gl.appendChild(a);
    }
    var gd = document.getElementById("github-download");
    if (gd && d.downloadUrl) {
      gd.href = d.downloadUrl;
      gd.style.display = "";
      var gv = document.getElementById("github-version");
      if (gv) gv.textContent = d.version + " · " + d.date;
    }
  }
  if (document.getElementById("release-table") || document.getElementById("github-link") || document.getElementById("github-download")) {
    fetch("release.json").then(function (r) { return r.json(); }).then(renderRelease).catch(function () {
      var tbl = document.getElementById("release-table");
      if (tbl) tbl.innerHTML = "<tr><td colspan='6'>Release-Daten noch nicht generiert (scripts/sync-website).</td></tr>";
    });
  }
})();
