async page => {
    const admin = 'tools/NcfSimulatedSite/Senparc.Areas.Admin';
    const web = 'tools/NcfSimulatedSite/Senparc.Web/wwwroot';
    const origin = 'http://ncf-doc-help.test';
    const docsDist = await page.evaluate(() => window.NCF_DOC_HELP_TEST_DIST || '');
    const missingDocAssets = new Set();
    const serveDocs = async route => {
        const pathname = route.request().url().slice('https://doc.ncf.pub/'.length).split('?')[0];
        try {
            await route.fulfill({ path: docsDist + '/' + (pathname.endsWith('/') || !pathname ? pathname + 'index.html' : pathname) });
        } catch (error) {
            if (error.code !== 'ENOENT' || !pathname.startsWith('assets/')) throw error;
            missingDocAssets.add(pathname);
            await route.fulfill({ status: 404, body: 'Missing build asset: ' + pathname });
        }
    };
    if (docsDist) await page.context().route('https://doc.ncf.pub/**', serveDocs);
    const failures = [];
    const check = (condition, message) => { if (!condition) throw new Error(message); };
    const canonical = url => url.replace(/\/index\.html(?=[?#]|$)/, '/');
    page.on('pageerror', error => failures.push(error.message));
    await page.route(origin + '/**', async route => {
        const pathname = route.request().url().slice(origin.length).split('?')[0];
        if (pathname.startsWith('/resources/')) {
            await route.fulfill({ path: admin + '/Resources/AdminResource.' + pathname.split('/').pop() + '.resx' });
        } else if (pathname.startsWith('/fonts/')) {
            await route.fulfill({ path: web + '/lib/element-ui_2.13.2' + pathname });
        } else if (pathname.startsWith('/help-assets/')) {
            const name = pathname.split('/').pop();
            const asset = name === 'vue.js' ? web + '/lib/vue/vue.js'
                : name === 'element.js' ? web + '/lib/element-ui_2.13.2/element.js'
                : admin + '/wwwroot/js/Admin/Shared/' + name;
            await route.fulfill({ path: asset });
        } else {
            await route.fulfill({ contentType: 'text/html', body: '<!doctype html><html><head></head><body></body></html>' });
        }
    });

    async function mount(culture, pagePath) {
        await page.goto(origin + pagePath);
        await page.evaluate(({ culture, pagePath }) => {
            document.documentElement.lang = culture;
            document.body.innerHTML = `
                <div id="help-test">
                    <header class="el-header" style="height:60px">
                        <div class="header-breadcrumb">NCF</div>
                        <ncf-doc-help class="ncf-doc-help-header" :page-path="pagePath"></ncf-doc-help>
                    </header>
                    <form @submit.prevent="actions++">
                        <button type="button" id="publish" @click="actions++">Publish</button>
                        <ncf-doc-help topic="modules.publish"></ncf-doc-help>
                        <span id="actions">{{ actions }}</span>
                    </form>
                </div>`;
            window.helpTestPagePath = pagePath;
        }, { culture, pagePath });
        await page.addStyleTag({ path: web + '/lib/element-ui_2.13.2/element.css' });
        await page.addStyleTag({ path: admin + '/wwwroot/css/Admin/Shared/layout.css' });
        await page.addStyleTag({ path: admin + '/wwwroot/css/Admin/Shared/NcfDocHelp.css' });
        await page.addScriptTag({ path: web + '/lib/vue/vue.js' });
        await page.addScriptTag({ path: web + '/lib/element-ui_2.13.2/element.js' });
        await page.evaluate(async culture => {
            const resource = await fetch('/resources/' + culture).then(response => response.text());
            const xml = new DOMParser().parseFromString(resource, 'text/xml');
            window.ncfI18n = Object.fromEntries(Array.from(xml.querySelectorAll('data'))
                .map(node => [node.getAttribute('name'), node.querySelector('value').textContent]));
            window.ncfT = key => window.ncfI18n[key] || key;
        }, culture);
        await page.addScriptTag({ path: admin + '/wwwroot/js/Admin/Shared/NcfDocHelpCatalog.js' });
        await page.addScriptTag({ path: admin + '/wwwroot/js/Admin/Shared/NcfDocHelp.js' });
        await page.evaluate(() => {
            window.helpTestApp = new Vue({
                el: '#help-test',
                data: { pagePath: window.helpTestPagePath, actions: 0 }
            });
        });
        check(await page.locator('iframe').count() === 0, 'Documents must not load before a help click');
    }

    async function close() {
        await page.locator('.ncf-doc-help-dialog .el-dialog__footer button').click();
        await page.locator('.ncf-doc-help-dialog').waitFor({ state: 'hidden' });
        check(await page.locator('.ncf-doc-help-frame').count() === 0, 'Closing must release the iframe');
    }

    const results = [];
    for (const culture of ['zh-CN', 'en']) {
        for (const [pagePath, topic] of [
            ['/Admin/Index', 'admin.home'],
            ['/Admin/XncfModule/', 'modules.manage'],
            ['/Admin/MCP/Index', 'mcp.home'],
            ['/Admin/PromptRange/Index', 'prompts.dashboard'],
            ['/Admin/PromptRange/Prompt', 'prompts.workbench'],
            ['/Admin/Workspace/Index', 'workspace.manage'],
            ['/Admin/Sandbox/Index', 'sandbox.manage'],
            ['/Admin/Sandbox/Setup', 'sandbox.setup'],
            ['/Admin/KnowledgeBase/Index', 'knowledge.manage'],
            ['/Admin/PromptRange/Skill', 'prompts.skills'],
            ['/Admin/AgentsManager/Index', 'agents.manage']
        ]) {
            await mount(culture, pagePath);
            const icon = page.locator('.ncf-doc-help-header');
            await icon.click();
            const expected = await page.evaluate(topic => NcfDocHelp.resolve(topic, document.documentElement.lang), topic);
            check(await page.locator('.ncf-doc-help-frame').getAttribute('src') === expected, 'Incorrect document URL');
            const frame = page.frameLocator('.ncf-doc-help-frame');
            await frame.locator('h1').first().waitFor({ timeout: 25000 });
            check((await frame.locator('h1').first().innerText()).length > 0, 'Live documentation must render inside iframe');
            const anchor = expected.split('#')[1];
            await frame.locator('[id="' + anchor + '"]').waitFor({ state: 'attached', timeout: 25000 });
            const external = page.locator('.ncf-doc-help-external');
            check(await external.getAttribute('href') === expected, 'New window link must use the selected document');
            const popupPromise = page.waitForEvent('popup');
            await external.click();
            const popup = await popupPromise;
            await popup.waitForURL(url => canonical(url.href) === canonical(expected));
            await popup.waitForLoadState('domcontentloaded');
            check(canonical(popup.url()) === canonical(expected), 'New window must open the matching locale');
            check(await popup.evaluate(() => window.opener === null), 'New window must not access the Admin opener');
            await popup.close();
            await close();
            check(await icon.evaluate(element => element === document.activeElement), 'Focus must return to help icon');
            results.push(culture + ' ' + topic + ': iframe + new window + focus passed');
        }
        await page.locator('form .ncf-doc-help-button').click();
        const frame = page.frameLocator('.ncf-doc-help-frame');
        const anchor = 'help-module-publish';
        await frame.locator('[id="' + anchor + '"]').waitFor({ state: 'attached', timeout: 25000 });
        check(await page.locator('#actions').innerText() === '0', 'Help must not execute the adjacent action or submit a form');
        check(decodeURIComponent(await page.locator('iframe').getAttribute('src')).endsWith('#' + anchor), 'Chapter anchor must match the locale');
        await page.setViewportSize({ width: 390, height: 844 });
        const box = await page.locator('.ncf-doc-help-dialog').boundingBox();
        check(box.x >= 0 && box.x + box.width <= 391 && box.height <= 844, 'Mobile dialog must fit the viewport');
        await close();
        await page.setViewportSize({ width: 1280, height: 900 });
        results.push(culture + ' modules.publish: live chapter + action isolation + mobile passed');
    }

    for (const culture of ['ja', 'fr', 'es', 'ru']) {
        await mount(culture, '/Admin/MCP');
        await page.locator('.ncf-doc-help-header').click();
        check(await page.locator('iframe').getAttribute('src') === 'https://doc.ncf.pub/NcfPackageSources/xncf/mcp-manager.html#help-mcp-manager', culture + ' must use English documentation');
        check(!(await page.locator('.ncf-doc-help-dialog').innerText()).includes('DocHelp.'), 'Missing help translation');
        await close();
    }
    await page.evaluate(() => {
        window.helpTestExistingApp = window.helpTestApp;
        const host = document.createElement('div');
        host.setAttribute('data-ncf-doc-help-root', '');
        document.body.appendChild(host);
    });
    await page.addScriptTag({ path: admin + '/wwwroot/js/Admin/Shared/NcfDocHelpStandalone.js' });
    check(await page.locator('[data-ncf-doc-help-root]').count() === 0, 'Inherited Vue layouts must not show duplicate help');
    check(await page.evaluate(() => window.helpTestApp === window.helpTestExistingApp), 'Standalone loading must preserve the page Vue root');
    if (docsDist) {
        for (const culture of ['zh-CN', 'en']) {
            await mount(culture, '/Admin/KnowledgeBase/Index');
            const topics = await page.evaluate(() => Object.keys(NcfDocHelpCatalog.topics));
            for (const topic of topics) {
                await page.evaluate(topic => NcfDocHelp.open(topic), topic);
                const url = await page.locator('.ncf-doc-help-frame').getAttribute('src');
                const fragment = url.split('#')[1];
                await page.frameLocator('.ncf-doc-help-frame').locator(fragment
                    ? '[id="' + decodeURIComponent(fragment) + '"]' : 'h1').first().waitFor({ state: 'attached', timeout: 25000 });
                await close();
            }
            results.push(culture + ': every configured topic rendered from production VuePress output');
        }
    }

    // Legacy pages must reuse the same help implementation without a page Vue root.
    for (const culture of ['zh-CN', 'en']) {
        await page.goto(origin + '/standalone');
        await page.addStyleTag({ path: web + '/lib/element-ui_2.13.2/element.css' });
        await page.addStyleTag({ path: admin + '/wwwroot/css/Admin/Shared/NcfDocHelp.css' });
        await page.evaluate(async culture => {
            const xml = new DOMParser().parseFromString(await fetch('/resources/' + culture).then(r => r.text()), 'text/xml');
            const labels = Object.fromEntries(Array.from(xml.querySelectorAll('data'))
                .filter(node => node.getAttribute('name').startsWith('DocHelp.'))
                .map(node => [node.getAttribute('name'), node.querySelector('value').textContent]));
            document.documentElement.lang = 'en';
            const host = document.createElement('div');
            host.setAttribute('data-ncf-doc-help-root', '');
            Object.assign(host.dataset, {
                topic: 'admin.login', culture, labels: JSON.stringify(labels),
                vue: '/help-assets/vue.js', element: '/help-assets/element.js',
                catalog: '/help-assets/NcfDocHelpCatalog.js', help: '/help-assets/NcfDocHelp.js'
            });
            document.body.appendChild(host);
        }, culture);
        await page.addScriptTag({ path: admin + '/wwwroot/js/Admin/Shared/NcfDocHelpStandalone.js' });
        const button = page.locator('.ncf-doc-help-standalone button');
        await button.click();
        const url = await page.locator('iframe').getAttribute('src');
        check(url.includes('/zh/') === (culture === 'zh-CN'), 'Standalone help must use server culture, not a legacy html lang');
        await page.frameLocator('.ncf-doc-help-frame').locator('h1').first().waitFor({ timeout: 25000 });
        await close();
        results.push(culture + ': standalone lazy dependencies, culture and dialog passed');
    }
    await mount('en', '/Admin/Unknown/Index');
    check(await page.locator('.ncf-doc-help-header').count() === 0, 'Unknown pages must not show an unrelated help entry');

    await page.route('https://doc.ncf.pub/**', route => route.abort());
    await page.locator('form .ncf-doc-help-button').click();
    await page.locator('.ncf-doc-help-dialog').waitFor();
    check(await page.locator('.ncf-doc-help-external').isVisible(), 'New-window fallback must remain available when framing fails');
    await close();
    await page.unroute('https://doc.ncf.pub/**');

    await page.clock.install();
    let pendingRoute;
    await page.route('https://doc.ncf.pub/**', route => { pendingRoute = route; });
    const stalledRequest = page.waitForRequest('https://doc.ncf.pub/**');
    await page.locator('form .ncf-doc-help-button').click();
    await stalledRequest;
    await page.clock.fastForward(15001);
    check(await page.locator('.ncf-doc-help-error').isVisible(), 'Timed-out documents must show a visible error');
    await pendingRoute.abort();
    await page.unroute('https://doc.ncf.pub/**');
    await page.locator('.ncf-doc-help-error button').click();
    await page.frameLocator('.ncf-doc-help-frame').locator('h1').first().waitFor({ timeout: 25000 });
    check(await page.locator('.ncf-doc-help-error').count() === 0, 'Retry must clear the timeout error');
    await page.clock.resume();
    await close();

    check(failures.length === 0, failures.join('\n'));
    await page.unroute(origin + '/**');
    if (docsDist) await page.context().unroute('https://doc.ncf.pub/**', serveDocs);
    return {
        passed: results,
        additional: 'All non-Chinese locales, unknown pages, blocked iframe, timeout and retry passed',
        missingDocAssets: Array.from(missingDocAssets)
    };
}
