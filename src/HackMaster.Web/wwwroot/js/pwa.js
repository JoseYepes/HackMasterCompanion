// Aplicación instalable y sin conexión (F013, specs/011-instalable-sin-conexion/contracts/pwa.md):
// registra el trabajador de servicio, avisa de las versiones nuevas y guarda el aviso de instalar del
// navegador para el botón «Instalar». Es un script clásico que se carga antes de Blazor.
(function () {
  var state = { updateReady: false, canInstall: false, isIos: false, isStandalone: false };
  var subscribers = {};
  var nextId = 1;
  var installEvent = null;
  var registration = null;

  var ua = navigator.userAgent || '';
  // El iPad moderno se presenta como Mac, pero con pantalla táctil.
  state.isIos = /iPhone|iPad|iPod/i.test(ua) || (/Macintosh/.test(ua) && navigator.maxTouchPoints > 1);

  function standalone() {
    return (window.matchMedia && window.matchMedia('(display-mode: standalone)').matches) || navigator.standalone === true;
  }

  state.isStandalone = standalone();

  function snapshot() {
    return { updateReady: state.updateReady, canInstall: state.canInstall, isIos: state.isIos, isStandalone: state.isStandalone };
  }

  function notify() {
    Object.keys(subscribers).forEach(function (id) {
      subscribers[id].invokeMethodAsync('OnPwaState', snapshot()).catch(function () { });
    });
  }

  function set(changes) {
    var changed = false;
    Object.keys(changes).forEach(function (key) {
      if (state[key] !== changes[key]) {
        state[key] = changes[key];
        changed = true;
      }
    });
    if (changed) notify();
  }

  window.addEventListener('beforeinstallprompt', function (event) {
    // Se guarda para el botón «Instalar» de la página de inicio (FR-010).
    event.preventDefault();
    installEvent = event;
    set({ canInstall: true });
  });

  window.addEventListener('appinstalled', function () {
    installEvent = null;
    set({ canInstall: false });
  });

  if (window.matchMedia) {
    var query = window.matchMedia('(display-mode: standalone)');
    if (query.addEventListener) {
      query.addEventListener('change', function () { set({ isStandalone: standalone() }); });
    }
  }

  // Hay una versión nueva esperando y la página ya estaba bajo una anterior (FR-008).
  function watch(worker) {
    if (!worker) return;
    function check() {
      if (worker.state === 'installed' && navigator.serviceWorker.controller) set({ updateReady: true });
    }
    check();
    worker.addEventListener('statechange', check);
  }

  if ('serviceWorker' in navigator) {
    var hadController = !!navigator.serviceWorker.controller;
    navigator.serviceWorker.addEventListener('controllerchange', function () {
      // Cambió la versión que manda: se recarga para no mezclar archivos de dos versiones (FR-007).
      if (hadController) window.location.reload();
      hadController = true;
    });

    // Tras la carga, para no retrasar la primera vista (FR-013).
    window.addEventListener('load', function () {
      navigator.serviceWorker.register('service-worker.js', { updateViaCache: 'none' }).then(function (reg) {
        registration = reg;
        watch(reg.waiting);
        reg.addEventListener('updatefound', function () { watch(reg.installing); });
      }).catch(function () {
        // Sin trabajador (modo privado, navegador sin soporte): la aplicación funciona con conexión.
      });
    });

    document.addEventListener('visibilitychange', function () {
      if (document.visibilityState === 'visible' && registration) registration.update().catch(function () { });
    });
  }

  window.hackmasterPwa = {
    subscribe: function (dotnetRef) {
      var id = nextId++;
      subscribers[id] = dotnetRef;
      dotnetRef.invokeMethodAsync('OnPwaState', snapshot()).catch(function () { });
      return id;
    },
    unsubscribe: function (id) {
      delete subscribers[id];
    },
    install: function () {
      if (!installEvent) return Promise.resolve(false);
      var event = installEvent;
      installEvent = null;
      set({ canInstall: false });
      return event.prompt().then(function () { return event.userChoice; }).then(function (choice) {
        return choice.outcome === 'accepted';
      });
    },
    applyUpdate: function () {
      if (registration && registration.waiting) registration.waiting.postMessage({ type: 'SKIP_WAITING' });
    },
    isOffline: function () {
      return navigator.onLine === false;
    }
  };
})();
