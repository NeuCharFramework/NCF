async page => {
    const admin = 'tools/NcfSimulatedSite/Senparc.Areas.Admin';
    const web = 'tools/NcfSimulatedSite/Senparc.Web/wwwroot';
    const origin = 'http://ncf-memory-help.test';
    const docsDist = await page.evaluate(() => window.NCF_DOC_HELP_TEST_DIST || '');
    const check = (condition, message) => { if (!condition) throw new Error(message); };
    const failures = [];
    const missingAssets = new Set();
    page.on('pageerror', error => failures.push(error.message));
    if (docsDist) await page.context().route('https://doc.ncf.pub/**', async route => {
        const pathname = route.request().url().slice('https://doc.ncf.pub/'.length).split('?')[0];
        try {
            await route.fulfill({ path: docsDist + '/' + (pathname.endsWith('/') ? pathname + 'index.html' : pathname) });
        } catch (error) {
            if (error.code !== 'ENOENT' || !pathname.startsWith('assets/')) throw error;
            missingAssets.add(pathname);
            await route.fulfill({ status: 404, body: 'Missing build asset: ' + pathname });
        }
    });
    await page.route(origin + '/**', async route => {
        const pathname = route.request().url().slice(origin.length).split('?')[0];
        if (pathname === '/template') {
            await route.fulfill({ path: admin + '/Areas/Admin/Pages/AdminChat/Chat.cshtml', contentType: 'text/plain' });
        } else if (pathname.startsWith('/resources/')) {
            const locale = pathname.split('/').pop();
            await route.fulfill({ path: admin + '/Resources/AdminResource' + (locale === 'base' ? '' : '.' + locale) + '.resx' });
        } else if (pathname.startsWith('/fonts/')) {
            await route.fulfill({ path: web + '/lib/element-ui_2.13.2' + pathname });
        } else if (pathname === '/') {
            await route.fulfill({ contentType: 'text/html', body: '<!doctype html><html><body></body></html>' });
        } else {
            failures.push('Unexpected request: ' + pathname);
            await route.abort();
        }
    });

    const results = [];
    for (const culture of ['zh-CN', 'en', 'ja']) {
        await page.setViewportSize({ width: 1280, height: 900 });
        await page.goto(origin);
        const raw = await page.evaluate(() => fetch('/template').then(response => response.text()));
        const labels = await page.evaluate(async culture => {
            const resources = await Promise.all(['base', culture].map(locale =>
                fetch('/resources/' + locale).then(response => response.text())));
            return Object.assign({}, ...resources.map(resource => {
                const xml = new DOMParser().parseFromString(resource, 'text/xml');
                return Object.fromEntries(Array.from(xml.querySelectorAll('data'))
                    .map(node => [node.getAttribute('name'), node.querySelector('value').textContent]));
            }));
        }, culture);
        const localize = source => source.replace(/@AR\["([^"]+)"\]/g, (_, key) =>
            (labels[key] || key).replace(/&/g, '&amp;').replace(/"/g, '&quot;').replace(/</g, '&lt;').replace(/>/g, '&gt;'))
            .replace(/@@/g, '@');
        const dialogStart = raw.indexOf('<el-dialog :title="ncfT(\'AdminChat.Memory.Settings\')"');
        check(dialogStart >= 0, 'Actual Memory dialog must exist');
        const dialog = raw.slice(dialogStart, raw.indexOf('</el-dialog>', dialogStart) + '</el-dialog>'.length);
        const composers = [...raw.matchAll(/<el-dropdown trigger="click" @@command="handleMemoryCommand[\s\S]+?<\/el-dropdown>\s*<ncf-doc-help topic="admin.memory.modes"><\/ncf-doc-help>/g)];
        check(composers.length === 2, 'Both real Memory mode selectors must have adjacent help');
        const sidebar = raw.slice(raw.indexOf('<el-aside'), raw.indexOf('</el-aside>') + '</el-aside>'.length);
        const launcher = raw.slice(raw.indexOf('<div class="chat-empty-launcher"'), raw.indexOf('</el-main>'))
            .replace(' v-else', '').replace(/@await Html\.PartialAsync\([^\n]+/g, '');
        await page.setContent(`<div id="app">
            <div v-if="!checkLayout" class="admin-chat-container">
                <form @submit.prevent="helpActions++" @click="helpActions++">
                    ${composers.map((match, index) => `<div id="composer-${index}">${localize(match[0])}</div>`).join('')}
                </form>
            </div>
            <div v-if="checkLayout" class="admin-chat-container">
                <el-container class="chat-main-container" :style="{ '--chat-sidebar-width': sidebarWidth + 'px' }">
                    ${localize(sidebar)}<el-main class="chat-content">${localize(launcher)}</el-main>
                </el-container>
            </div>${localize(dialog)}</div>`);
        await page.addStyleTag({ path: web + '/lib/element-ui_2.13.2/element.css' });
        await page.addStyleTag({ path: admin + '/wwwroot/css/Admin/Pages/AdminChat/ChatLauncher.css' });
        await page.addStyleTag({ path: admin + '/wwwroot/css/Admin/Pages/AdminChat/Chat.css' });
        await page.addStyleTag({ path: admin + '/wwwroot/css/Admin/Shared/NcfDocHelp.css' });
        await page.route(origin + '/font-awesome.css', route => route.fulfill({
            path: web + '/lib/font-awesome/css/font-awesome.min.css', contentType: 'text/css'
        }));
        await page.route(origin + '/fonts/fontawesome-webfont*', route => route.fulfill({
            path: web + '/lib/font-awesome/fonts/' + route.request().url().split('/').pop().split('?')[0]
        }));
        await page.addStyleTag({ url: origin + '/font-awesome.css' });
        await page.addScriptTag({ path: web + '/lib/vue/vue.js' });
        await page.addScriptTag({ path: web + '/lib/element-ui_2.13.2/element.js' });
        await page.evaluate(({ culture, labels }) => {
            document.documentElement.lang = culture;
            window.ncfT = key => labels[key] || key;
            Vue.prototype.ncfT = window.ncfT;
            Vue.directive('prompt-range', {});
            window.NativeVue = Vue;
            window.Vue = function(options) { window.memoryHelpOptions = options; };
        }, { culture, labels });
        await page.addScriptTag({ path: admin + '/wwwroot/js/Admin/Pages/AdminChat/ChatLauncherMixin.js' });
        await page.addScriptTag({ path: admin + '/wwwroot/js/Admin/Pages/AdminChat/Chat.js' });
        await page.evaluate(() => { window.Vue = window.NativeVue; });
        await page.addScriptTag({ path: admin + '/wwwroot/js/Admin/Shared/NcfDocHelpCatalog.js' });
        await page.addScriptTag({ path: admin + '/wwwroot/js/Admin/Shared/NcfDocHelp.js' });
        await page.evaluate(() => {
            const options = window.memoryHelpOptions;
            window.helpRequests = [];
            const rejectRequest = (...args) => { helpRequests.push(args); throw new Error('Help called a business API'); };
            window.service = { get: rejectRequest, post: rejectRequest };
            window.memoryHelpApp = new Vue({
                el: '#app',
                computed: { ...ChatLauncherMixin.computed, ...options.computed },
                methods: { ...ChatLauncherMixin.methods, ...options.methods },
                data() {
                    return {
                        ...ChatLauncherMixin.data(), ...options.data(), checkLayout: false,
                        helpActions: 0, launcherMemoryMode: 2, isCreatingSession: false,
                        isSuperAdmin: true,
                        memoryReady: true, memoryMode: 1, memoryMarkdown: '# Memory\n\n## Core\n- Unsaved draft.',
                        memorySavedMarkdown: '# Memory\n\n## Core\n', memorySystemMessage: 'Unsaved system draft'
                    };
                }
            });
        });
        check(await page.locator('iframe').count() === 0, 'Help must remain lazy');

        async function openAndClose(icon, topic, nested, escape = false) {
            await icon.click();
            const expected = await page.evaluate(topic => NcfDocHelp.resolve(topic, document.documentElement.lang), topic);
            check(await page.locator('.ncf-doc-help-frame').getAttribute('src') === expected, 'Incorrect Memory document URL');
            const frame = page.frameLocator('.ncf-doc-help-frame');
            await frame.locator('h1').first().waitFor({ timeout: 30000 });
            const anchor = expected.split('#')[1];
            if (anchor) await frame.locator('[id="' + anchor + '"]').waitFor({ state: 'attached' });
            check(await page.locator('.ncf-doc-help-external').getAttribute('href') === expected, 'External URL must match');
            if (topic === 'admin.memory') {
                const popupPromise = page.waitForEvent('popup');
                await page.locator('.ncf-doc-help-external').click();
                const popup = await popupPromise;
                await popup.locator('h1').first().waitFor({ timeout: 30000 });
                check(popup.url() === expected, 'New window must open the correct Memory locale');
                check(await popup.evaluate(() => window.opener === null), 'New window must not access the Admin opener');
                await popup.close();
            }
            if (nested) {
                check(await page.locator('.chat-memory-dialog').isVisible(), 'Memory dialog must remain open');
                const above = await page.evaluate(() => {
                    const z = selector => Number(getComputedStyle(document.querySelector(selector).parentElement).zIndex);
                    return z('.ncf-doc-help-dialog') > z('.chat-memory-dialog');
                });
                check(above, 'Documentation dialog must stack above Memory settings');
            }
            if (escape) {
                await page.locator('.ncf-doc-help-dialog .el-dialog__footer button').focus();
                await page.keyboard.press('Escape');
            } else {
                await page.locator('.ncf-doc-help-dialog .el-dialog__footer button').click();
            }
            await page.locator('.ncf-doc-help-dialog').waitFor({ state: 'hidden' });
            check(await page.locator('iframe').count() === 0, 'Close must remove the iframe');
            check(await icon.evaluate(element => document.activeElement === element), 'Focus must return to the exact opener');
            if (nested) check(await page.locator('.chat-memory-dialog').isVisible(), 'Closing help must not close Memory settings');
        }

        for (let index = 0; index < 2; index++) {
            await openAndClose(page.locator('#composer-' + index + ' .ncf-doc-help-button'), 'admin.memory.modes', false);
        }
        await page.evaluate(() => { memoryHelpApp.memoryDialogVisible = true; });
        await openAndClose(page.locator('.chat-memory-heading .ncf-doc-help-button'), 'admin.memory', true, true);
        for (const [tab, topic] of [
            ['entries', 'entries'], ['loaded', 'loaded'], ['edit', 'markdown'], ['system', 'system']
        ]) {
            await page.locator('#tab-' + tab).click();
            await openAndClose(page.locator('#pane-' + tab + ' .ncf-doc-help-button'), 'admin.memory.' + topic, true);
        }
        await page.setViewportSize({ width: 390, height: 844 });
        await page.locator('.chat-memory-heading .ncf-doc-help-button').click();
        const box = await page.locator('.ncf-doc-help-dialog').boundingBox();
        check(box.x >= 0 && box.x + box.width <= 391 && box.height <= 844, 'Nested help must fit mobile viewport');
        await page.locator('.ncf-doc-help-dialog .el-dialog__footer button').click();
        await page.locator('.ncf-doc-help-dialog').waitFor({ state: 'hidden' });
        await page.setViewportSize({ width: 1280, height: 900 });
        const state = await page.evaluate(() => ({
            requests: helpRequests.length, actions: memoryHelpApp.helpActions,
            mode: memoryHelpApp.memoryMode, launcher: memoryHelpApp.launcherMemoryMode,
            draft: memoryHelpApp.memoryMarkdown, system: memoryHelpApp.memorySystemMessage
        }));
        check(state.requests === 0 && state.actions === 0, 'Help must not request, save, send or submit');
        check(state.mode === 1 && state.launcher === 2, 'Help must not alter either Memory mode');
        check(state.draft === '# Memory\n\n## Core\n- Unsaved draft.' && state.system === 'Unsaved system draft',
            'Help must preserve unsaved Memory and SystemMessage drafts');
        await page.evaluate(() => {
            memoryHelpApp.memoryDialogVisible = false;
            memoryHelpApp.checkLayout = true;
        });
        await page.locator('.chat-memory-dialog').waitFor({ state: 'hidden' });
        for (const width of [1440, 1100, 768, 390]) {
            await page.setViewportSize({ width, height: 900 });
            await page.evaluate(width => {
                memoryHelpApp.sidebarCollapsed = width < 800;
                memoryHelpApp.sidebarWidth = width === 1100 ? 215 : 250;
            }, width);
            await page.locator('.chat-entrance-composer-footer').waitFor();
            const geometry = await page.evaluate(() => {
                const sidebar = document.querySelector('.sidebar-actions');
                const sidebarButtons = Array.from(sidebar.querySelectorAll('.el-button:not(.user-stats-button)'));
                const groups = Array.from(document.querySelectorAll('.chat-entrance-composer-footer .ncf-doc-help-group, .chat-launcher-toolbar .ncf-doc-help-group, .launcher-toolbar-mode'));
                return {
                    sidebarHelp: sidebar.querySelectorAll('.ncf-doc-help-button').length,
                    clipped: sidebarButtons.filter(button => button.clientWidth > 0 && button.scrollWidth > button.clientWidth).length,
                    groups: groups.map(group => {
                        const help = group.querySelector('.ncf-doc-help-button').getBoundingClientRect();
                        const control = group.querySelector('.el-button, .el-dropdown, .el-radio-group').getBoundingClientRect();
                        const outer = group.getBoundingClientRect();
                        return {
                            gap: help.left - control.right,
                            centered: Math.abs((help.top + help.height / 2) - (control.top + control.height / 2)),
                            contained: help.right <= outer.right + 1 && help.left >= outer.left - 1
                        };
                    })
                };
            });
            check(geometry.sidebarHelp === 0 && geometry.clipped === 0,
                culture + ' sidebar buttons must remain readable at width ' + width);
            check(geometry.groups.length === 3, 'Attachment, Harness and Memory must be grouped');
            for (const group of geometry.groups) {
                check(group.gap >= -1 && group.gap <= 6 && group.centered <= 2 && group.contained,
                    culture + ' detached or wrapped help at width ' + width + ': ' + JSON.stringify(group));
            }
        }
        results.push(culture + ': composers, chapters, new window, stacking, Escape, focus, mobile, drafts and 4 layout widths passed');
    }
    check(failures.length === 0, failures.join('\n'));
    return { results, missingBuildAssets: Array.from(missingAssets) };
}
