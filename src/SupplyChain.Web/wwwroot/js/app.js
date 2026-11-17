(function () {
  "use strict";

  var root = document.documentElement;
  var THEME_KEY = "supplychain-theme";

  function applyTheme(theme) {
    root.setAttribute("data-theme", theme);
    document.querySelectorAll("[data-theme-toggle]").forEach(function (button) {
      button.setAttribute("aria-label", theme === "dark" ? "Switch to light mode" : "Switch to dark mode");
      button.setAttribute("aria-pressed", theme === "dark" ? "true" : "false");
      var use = button.querySelector("use");
      if (use) {
        use.setAttribute("href", theme === "dark" ? "/media/icons.svg#i-sun" : "/media/icons.svg#i-moon");
      }
    });
  }

  var stored = null;
  try { stored = localStorage.getItem(THEME_KEY); } catch (error) { stored = null; }
  applyTheme(stored === "dark" ? "dark" : "light");

  document.addEventListener("click", function (event) {
    var toggle = event.target.closest("[data-theme-toggle]");
    if (!toggle) return;
    var next = root.getAttribute("data-theme") === "dark" ? "light" : "dark";
    applyTheme(next);
    try { localStorage.setItem(THEME_KEY, next); } catch (e) { /* ignore */ }
  });

  /* ---------------------------------------------------------------------
     Header, mobile nav and sidebar
     --------------------------------------------------------------------- */

  var header = document.querySelector("[data-header]");
  function onScroll() {
    if (!header) return;
    header.classList.toggle("is-scrolled", window.scrollY > 8);
  }
  onScroll();
  window.addEventListener("scroll", onScroll, { passive: true });

  var navToggle = document.querySelector("[data-nav-toggle]");
  var navLinks = document.querySelector("[data-nav]");
  if (navToggle && navLinks) {
    navToggle.addEventListener("click", function () {
      var open = navLinks.classList.toggle("is-open");
      navToggle.setAttribute("aria-expanded", open ? "true" : "false");
      var use = navToggle.querySelector("use");
      if (use) use.setAttribute("href", open ? "/media/icons.svg#i-close" : "/media/icons.svg#i-menu");
    });
    document.addEventListener("click", function (event) {
      if (!navLinks.classList.contains("is-open")) return;
      if (navLinks.contains(event.target) || navToggle.contains(event.target)) return;
      navLinks.classList.remove("is-open");
      navToggle.setAttribute("aria-expanded", "false");
    });
  }

  var sidebar = document.querySelector("[data-sidebar]");
  var sidebarToggle = document.querySelector("[data-sidebar-toggle]");
  var scrim = document.querySelector("[data-sidebar-scrim]");
  function closeSidebar() {
    if (!sidebar) return;
    sidebar.classList.remove("is-open");
    if (scrim) scrim.classList.remove("is-open");
    if (sidebarToggle) sidebarToggle.setAttribute("aria-expanded", "false");
  }
  if (sidebarToggle && sidebar) {
    sidebarToggle.addEventListener("click", function () {
      var open = sidebar.classList.toggle("is-open");
      if (scrim) scrim.classList.toggle("is-open", open);
      sidebarToggle.setAttribute("aria-expanded", open ? "true" : "false");
    });
  }
  if (scrim) scrim.addEventListener("click", closeSidebar);
  document.addEventListener("keydown", function (event) {
    if (event.key === "Escape") closeSidebar();
  });
  if (window.matchMedia("(min-width: 901px)").matches) {
    window.addEventListener("resize", function () {
      if (window.innerWidth > 900 && sidebar && sidebar.classList.contains("is-open")) closeSidebar();
    });
  }

  /* ---------------------------------------------------------------------
     Reveal on scroll
     --------------------------------------------------------------------- */

  var revealItems = document.querySelectorAll(".reveal");
  if (revealItems.length) {
    if ("IntersectionObserver" in window) {
      var observer = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
          if (!entry.isIntersecting) return;
          entry.target.classList.add("is-visible");
          observer.unobserve(entry.target);
        });
      }, { threshold: 0.12, rootMargin: "0px 0px -40px 0px" });
      revealItems.forEach(function (item) { observer.observe(item); });
    } else {
      revealItems.forEach(function (item) { item.classList.add("is-visible"); });
    }
  }

  /* ---------------------------------------------------------------------
     Toasts
     --------------------------------------------------------------------- */

  var ICONS = {
    success: "i-check-circle",
    error: "i-alert",
    info: "i-info"
  };

  function toast(message, type) {
    if (!message) return;
    type = type || "info";
    var stack = document.querySelector(".toast-stack");
    if (!stack) {
      stack = document.createElement("div");
      stack.className = "toast-stack";
      stack.setAttribute("aria-live", "polite");
      document.body.appendChild(stack);
    }
    var el = document.createElement("div");
    el.className = "toast " + type;
    el.setAttribute("role", "status");
    el.innerHTML =
      '<svg class="icon" aria-hidden="true"><use href="/media/icons.svg#' + (ICONS[type] || ICONS.info) + '"></use></svg>' +
      '<div class="grow"></div>';
    el.querySelector(".grow").textContent = message;
    stack.appendChild(el);
    window.setTimeout(function () {
      el.classList.add("is-leaving");
      window.setTimeout(function () { el.remove(); }, 260);
    }, 4200);
  }

  window.SupplyChain = window.SupplyChain || {};
  window.SupplyChain.toast = toast;

  /* ---------------------------------------------------------------------
     AJAX add to cart
     --------------------------------------------------------------------- */

  var csrf = document.querySelector('meta[name="csrf-token"]');
  var csrfToken = csrf ? csrf.getAttribute("content") : "";

  document.addEventListener("submit", function (event) {
    var form = event.target.closest("form.js-add-to-cart");
    if (!form) return;
    event.preventDefault();

    var button = form.querySelector("[data-add-button]") || form.querySelector("button");
    if (button && button.disabled) return;
    var original = button ? button.innerHTML : "";
    if (button) {
      button.disabled = true;
      button.innerHTML = '<svg class="icon" aria-hidden="true"><use href="/media/icons.svg#i-refresh"></use></svg>';
    }

    fetch(form.getAttribute("action") || window.location.pathname, {
      method: "POST",
      body: new FormData(form),
      headers: {
        "X-Requested-With": "XMLHttpRequest",
        "X-CSRF-TOKEN": csrfToken
      },
      credentials: "same-origin"
    })
      .then(function (response) {
        if (response.status === 401) {
          window.location.href = "/login?returnUrl=" + encodeURIComponent(window.location.pathname);
          throw new Error("Please sign in first.");
        }
        if (!response.ok) throw new Error("We couldn't add that right now.");
        return response.json();
      })
      .then(function (data) {
        toast(data.message || "Added to cart", data.success ? "success" : "error");
        if (data.success) {
          document.querySelectorAll("[data-cart-count]").forEach(function (badge) {
            badge.textContent = data.count;
            badge.hidden = !data.count;
          });
          if (button) {
            button.classList.add("is-added");
            button.innerHTML = original.indexOf("Add") >= 0 ? "Added" : original;
            window.setTimeout(function () {
              button.classList.remove("is-added");
              button.innerHTML = original;
              button.disabled = false;
            }, 1600);
            return;
          }
        }
        if (button) {
          button.innerHTML = original;
          button.disabled = false;
        }
      })
      .catch(function (error) {
        toast(error.message || "Something went wrong.", "error");
        if (button) {
          button.innerHTML = original;
          button.disabled = false;
        }
      });
  });

  /* ---------------------------------------------------------------------
     Quantity steppers
     --------------------------------------------------------------------- */

  document.addEventListener("click", function (event) {
    var button = event.target.closest("[data-step]");
    if (!button) return;
    var wrap = button.closest(".stepper");
    if (!wrap) return;
    var input = wrap.querySelector("input");
    if (!input) return;
    var min = parseInt(input.getAttribute("min") || "1", 10);
    var max = parseInt(input.getAttribute("max") || "999", 10);
    var value = parseInt(input.value || "1", 10);
    value += parseInt(button.getAttribute("data-step"), 10);
    value = Math.max(min, Math.min(max, value));
    input.value = value;
    input.dispatchEvent(new Event("change", { bubbles: true }));
    if (wrap.hasAttribute("data-autosubmit")) {
      var form = wrap.closest("form");
      if (form && form.requestSubmit) form.requestSubmit();
    }
  });

  /* ---------------------------------------------------------------------
     Password reveal
     --------------------------------------------------------------------- */

  document.addEventListener("click", function (event) {
    var toggle = event.target.closest("[data-password-toggle]");
    if (!toggle) return;
    var target = document.getElementById(toggle.getAttribute("data-password-toggle"));
    if (!target) return;
    var show = target.type === "password";
    target.type = show ? "text" : "password";
    toggle.setAttribute("aria-label", show ? "Hide password" : "Show password");
    var use = toggle.querySelector("use");
    if (use) use.setAttribute("href", show ? "/media/icons.svg#i-eye-off" : "/media/icons.svg#i-eye");
  });

  /* ---------------------------------------------------------------------
     Favourites (local only)
     --------------------------------------------------------------------- */

  var FAV_KEY = "supplychain-favourites";
  var favourites = [];
  try { favourites = JSON.parse(localStorage.getItem(FAV_KEY) || "[]"); } catch (e) { favourites = []; }
  if (!Array.isArray(favourites)) favourites = [];

  function saveFavourites() {
    try { localStorage.setItem(FAV_KEY, JSON.stringify(favourites)); } catch (e) { /* ignore */ }
  }

  document.querySelectorAll("[data-favourite]").forEach(function (button) {
    var id = button.getAttribute("data-favourite");
    if (favourites.indexOf(id) >= 0) button.classList.add("is-liked");
    button.addEventListener("click", function () {
      var index = favourites.indexOf(id);
      if (index >= 0) {
        favourites.splice(index, 1);
        button.classList.remove("is-liked");
        toast("Removed from your saved produce.", "info");
      } else {
        favourites.push(id);
        button.classList.add("is-liked");
        toast("Saved for later.", "success");
      }
      saveFavourites();
    });
  });

  /* ---------------------------------------------------------------------
     Settings tabs
     --------------------------------------------------------------------- */

  document.querySelectorAll("[data-tab]").forEach(function (tab) {
    tab.addEventListener("click", function () {
      var target = tab.getAttribute("data-tab");
      var group = tab.parentElement;
      if (group) group.querySelectorAll("[data-tab]").forEach(function (item) { item.classList.remove("is-active"); });
      tab.classList.add("is-active");
      document.querySelectorAll("[data-panel]").forEach(function (panel) {
        panel.classList.toggle("is-active", panel.getAttribute("data-panel") === target);
      });
    });
  });

  /* ---------------------------------------------------------------------
     Animated charts and counters
     --------------------------------------------------------------------- */

  function animateBars() {
    document.querySelectorAll(".bar-fill[data-h]").forEach(function (bar) {
      bar.style.height = bar.getAttribute("data-h") + "%";
    });
  }

  function animateCounters() {
    document.querySelectorAll("[data-count]").forEach(function (el) {
      var target = parseFloat(el.getAttribute("data-count"));
      if (!isFinite(target)) return;
      var prefix = el.getAttribute("data-prefix") || "";
      var suffix = el.getAttribute("data-suffix") || "";
      var duration = 900;
      var start = performance.now();
      function frame(now) {
        var progress = Math.min((now - start) / duration, 1);
        var eased = 1 - Math.pow(1 - progress, 3);
        var value = Math.round(target * eased);
        el.textContent = prefix + value.toLocaleString() + suffix;
        if (progress < 1) requestAnimationFrame(frame);
      }
      requestAnimationFrame(frame);
    });
  }

  function whenVisible(elements, callback) {
    if (!elements.length) return;
    if (!("IntersectionObserver" in window)) { callback(); return; }
    var io = new IntersectionObserver(function (entries) {
      if (entries.some(function (entry) { return entry.isIntersecting; })) {
        callback();
        io.disconnect();
      }
    }, { threshold: 0.2 });
    elements.forEach(function (el) { io.observe(el); });
  }

  var bars = document.querySelectorAll(".bar-fill[data-h]");
  var counters = document.querySelectorAll("[data-count]");
  window.requestAnimationFrame(function () {
    whenVisible(bars, animateBars);
    whenVisible(counters, animateCounters);
  });

  /* ---------------------------------------------------------------------
     Auto-dismiss server toasts
     --------------------------------------------------------------------- */

  document.querySelectorAll(".toast[data-auto]").forEach(function (el) {
    window.setTimeout(function () {
      el.classList.add("is-leaving");
      window.setTimeout(function () { el.remove(); }, 260);
    }, 5200);
  });
})();
