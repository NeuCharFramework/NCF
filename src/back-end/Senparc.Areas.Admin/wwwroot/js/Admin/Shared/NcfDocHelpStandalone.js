(function (window, document) {
    'use strict';

    function script(src) {
        return new Promise((resolve, reject) => {
            const element = document.createElement('script');
            element.src = src;
            element.onload = resolve;
            element.onerror = () => reject(new Error('Cannot load documentation help asset: ' + src));
            document.head.appendChild(element);
        });
    }

    async function mount() {
        for (const host of document.querySelectorAll('[data-ncf-doc-help-root]:not([data-mounted])')) {
            host.dataset.mounted = 'true';
            if (document.querySelector('button.ncf-doc-help-header')) {
                host.remove();
                continue;
            }
            try {
                const labels = JSON.parse(host.dataset.labels);
                window.ncfI18n = Object.assign(window.ncfI18n || {}, labels);
                window.ncfT = window.ncfT || (key => window.ncfI18n[key] || key);
                if (!window.Vue) await script(host.dataset.vue);
                if (!window.ELEMENT) await script(host.dataset.element);
                if (!window.NcfDocHelpCatalog) await script(host.dataset.catalog);
                if (!window.NcfDocHelp) await script(host.dataset.help);
                new window.Vue({
                    el: host,
                    render: h => h('div', { class: 'ncf-doc-help-standalone' }, [
                        h('ncf-doc-help', { props: { topic: host.dataset.topic, culture: host.dataset.culture } })
                    ])
                });
            } catch (error) {
                console.error('[NcfDocHelp]', error);
                host.setAttribute('role', 'alert');
                host.textContent = window.ncfT ? window.ncfT('DocHelp.LoadFailed') : error.message;
            }
        }
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', mount, { once: true });
    else mount();
})(window, document);
