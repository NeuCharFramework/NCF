async page => {
    const admin = 'tools/NcfSimulatedSite/Senparc.Areas.Admin';
    const web = 'tools/NcfSimulatedSite/Senparc.Web/wwwroot';
    const origin = 'http://ncf-message-rail.test';
    const failures = [];
    const check = (condition, message) => { if (!condition) throw new Error(message); };
    page.on('pageerror', error => failures.push(error.message));
    await page.route(origin + '/**', async route => {
        const pathname = route.request().url().slice(origin.length).split('?')[0];
        if (pathname === '/template') {
            await route.fulfill({ path: admin + '/Areas/Admin/Pages/AdminChat/Chat.cshtml', contentType: 'text/plain' });
        } else {
            await route.fulfill({ contentType: 'text/html', body: '<!doctype html><html><body></body></html>' });
        }
    });
    await page.setViewportSize({ width: 1280, height: 800 });
    await page.goto(origin);
    const raw = await page.evaluate(() => fetch('/template').then(response => response.text()));
    const stage = raw.slice(raw.indexOf('<div class="messages-stage"'), raw.indexOf('<div class="input-container"'))
        .replace(/@AR\["([^"]+)"\]/g, '$1').replace(/@@/g, '@');
    await page.setContent(`<div id="app"><div class="admin-chat-container"><div class="chat-content" style="height:640px;display:flex;flex-direction:column;">${stage}</div></div></div>`);
    await page.addStyleTag({ path: web + '/lib/element-ui_2.13.2/element.css' });
    await page.addStyleTag({ path: admin + '/wwwroot/css/Admin/Pages/AdminChat/Chat.css' });
    await page.addStyleTag({ content: '.messages-stage{height:420px;flex:none}.message-item{min-height:160px}' });
    await page.addScriptTag({ path: web + '/lib/vue/vue.js' });
    await page.addScriptTag({ path: web + '/lib/element-ui_2.13.2/element.js' });
    await page.evaluate(() => {
        window.NativeVue = Vue;
        window.ChatLauncherMixin = {};
        window.marked = { parse: text => text };
        window.DOMPurify = { sanitize: html => html };
        window.ncfT = key => key;
        Vue.prototype.ncfT = window.ncfT;
        window.Vue = function (options) { window.railOptions = options; };
    });
    await page.addScriptTag({ path: admin + '/wwwroot/js/Admin/Pages/AdminChat/Chat.js' });
    await page.evaluate(async () => {
        window.Vue = window.NativeVue;
        const options = window.railOptions;
        window.railTestApp = new Vue({
            el: '#app',
            data: options.data,
            computed: options.computed,
            watch: options.watch,
            methods: options.methods
        });
        railTestApp.currentSessionId = 7;
        const messages = [];
        for (let index = 1; index <= 14; index++) {
            messages.push({
                id: index,
                roleType: 0,
                content: `请帮我检查第 ${index} 项：Sandbox 上传限制与 FunctionRender 的配置需要更严格`
            });
            messages.push({ id: 100 + index, roleType: 1, content: `assistant reply ${index}` });
        }
        messages.push({ id: 99, roleType: 0, content: '   ' });
        railTestApp.messageList = messages;
        await railTestApp.$nextTick();
        await railTestApp.$nextTick();
        await railTestApp.$nextTick();
    });
    const dots = page.locator('.chat-message-rail-dot');
    check(await dots.count() === 15, 'One dot per human message, including an empty one');
    check(await page.locator('.message-assistant').count() === 14, 'Assistant replies stay in the transcript');
    const segments = await page.evaluate(() => railTestApp.messageRail);
    check(segments[0].top === 0 && segments.every(segment => segment.height >= 2), 'Rail segments cover the conversation from the top');
    check(segments.every((segment, index) => index === 0 || Math.abs(segment.top - (segments[index - 1].top + segments[index - 1].height)) <= 1), 'Adjacent segments meet so the pointer can travel between them');
    const varied = await page.evaluate(() => {
        const item = document.querySelector('[data-message-id="8"]');
        item.style.minHeight = '420px';
        railTestApp.refreshMessageRail();
        const rail = railTestApp.messageRail;
        const eighth = rail.find(segment => segment.id === 8);
        const neighbor = rail.find(segment => segment.id === 7);
        return eighth.height > neighbor.height + 8;
    });
    check(varied, 'A longer turn gets a longer rail segment');
    await dots.nth(3).hover();
    const menu = page.locator('.chat-message-rail-menu');
    await menu.waitFor({ state: 'visible' });
    const previews = await menu.locator('button span').allTextContents();
    check(previews[3].includes('第 4 项'), 'Hover preview shows the human message');
    check(previews[3].length < 60, 'Preview stays brief');
    check(previews[14] === 'AdminChat.Rail.EmptyMessage', 'Empty human messages remain selectable');
    check(await menu.evaluate(el => el.scrollHeight > el.clientHeight + 4), 'Long conversations scroll inside the preview menu');
    const before = await page.locator('.messages-container').evaluate(el => el.scrollTop);
    await menu.locator('button').nth(12).hover();
    check(await menu.isVisible(), 'The preview stays open when the pointer moves onto it');
    check(await page.evaluate(() => railTestApp.messageRailActiveId) === 13, 'Hovering a preview row highlights the matching rail segment');
    await menu.locator('button').nth(12).click();
    await page.waitForFunction(() => {
        const message = document.querySelector('[data-message-id="13"]');
        const container = document.querySelector('.messages-container');
        return message && Math.abs(container.scrollTop - (message.offsetTop - 12)) < 4;
    });
    const after = await page.locator('.messages-container').evaluate(el => el.scrollTop);
    check(after > before, 'Choosing a preview jumps to that human message');
    check(await page.evaluate(() => railTestApp.messageRailActiveId) === 13, 'The selected human message stays current');
    check(failures.length === 0, 'Browser errors: ' + failures.join('; '));
    return 'Message rail checks passed: one dot per human message, hover preview, scrollbar, and jump-to-message.';
}
