async page => {
    const admin = 'tools/NcfSimulatedSite/Senparc.Areas.Admin';
    const web = 'tools/NcfSimulatedSite/Senparc.Web/wwwroot';
    const origin = 'http://ncf-memory-review.test';
    const check = (condition, message) => { if (!condition) throw new Error(message); };
    const failures = [];
    page.on('pageerror', error => failures.push(error.message));
    await page.route('**/*', async route => {
        const url = route.request().url();
        if (url === origin + '/') {
            await route.fulfill({ contentType: 'text/html', body: '<!doctype html><html><body></body></html>' });
        } else if (url === origin + '/template') {
            await route.fulfill({ path: admin + '/Areas/Admin/Pages/AdminChat/Chat.cshtml', contentType: 'text/plain' });
        } else if (url.startsWith(origin + '/resources/')) {
            await route.fulfill({ path: admin + '/Resources/AdminResource.' + url.split('/').pop() + '.resx' });
        } else if (url.startsWith(origin + '/fonts/')) {
            await route.fulfill({ path: web + '/lib/element-ui_2.13.2/fonts/' + url.split('/').pop() });
        } else {
            failures.push('Unexpected request: ' + url);
            await route.abort();
        }
    });
    const results = [];
    for (const culture of ['zh-CN', 'en']) {
        await page.setViewportSize({ width: 1280, height: 900 });
        await page.goto(origin);
        const fixture = await page.evaluate(async culture => {
            const [raw, resource] = await Promise.all([
                fetch('/template').then(response => response.text()),
                fetch('/resources/' + culture).then(response => response.text())
            ]);
            const xml = new DOMParser().parseFromString(resource, 'text/xml');
            const labels = Object.fromEntries(Array.from(xml.querySelectorAll('data'))
                .map(node => [node.getAttribute('name'), node.querySelector('value').textContent]));
            return { raw, labels };
        }, culture);
        const raw = fixture.raw;
        const localize = text => text.replace(/@AR\["([^"]+)"\]/g, (_, key) =>
            (fixture.labels[key] || key).replace(/&/g, '&amp;').replace(/"/g, '&quot;').replace(/</g, '&lt;').replace(/>/g, '&gt;'))
            .replace(/@@/g, '@');
        const alertStart = raw.indexOf('<el-alert v-if="memoryUpdateResult');
        const dialogStart = raw.indexOf('<el-dialog :title="ncfT(\'AdminChat.Memory.Settings\')"');
        check(alertStart >= 0 && dialogStart >= 0, 'Actual receipt and dialog must exist');
        await page.setContent('<div id="app">' +
            localize(raw.slice(alertStart, raw.indexOf('</el-alert>', alertStart) + '</el-alert>'.length)) +
            localize(raw.slice(dialogStart, raw.indexOf('</el-dialog>', dialogStart) + '</el-dialog>'.length)) +
            '</div>');
        await page.addStyleTag({ path: web + '/lib/element-ui_2.13.2/element.css' });
        await page.addStyleTag({ path: admin + '/wwwroot/css/Admin/Pages/AdminChat/Chat.css' });
        await page.addStyleTag({ path: admin + '/wwwroot/css/Admin/Shared/NcfDocHelp.css' });
        await page.addScriptTag({ path: web + '/lib/vue/vue.js' });
        await page.addScriptTag({ path: web + '/lib/element-ui_2.13.2/element.js' });
        await page.evaluate(({ labels, culture }) => {
            document.documentElement.lang = culture;
            window.ncfT = key => labels[key] || key;
            Vue.prototype.ncfT = ncfT;
            window.NativeVue = Vue;
            window.ChatLauncherMixin = {};
            window.Vue = function(options) { window.memoryOptions = options; };
        }, { labels: fixture.labels, culture });
        await page.addScriptTag({ path: admin + '/wwwroot/js/Admin/Pages/AdminChat/Chat.js' });
        await page.evaluate(() => { window.Vue = window.NativeVue; });
        await page.addScriptTag({ path: admin + '/wwwroot/js/Admin/Shared/NcfDocHelpCatalog.js' });
        await page.addScriptTag({ path: admin + '/wwwroot/js/Admin/Shared/NcfDocHelp.js' });
        await page.evaluate(() => {
            window.memoryWrites = [];
            window.fixtureProfile = { revision: 'r1', entries: [], history: [], markdown: '# Memory\n', systemMessage: '' };
            window.service = {
                async get(url) {
                    if (!url.endsWith('GetMemoryAsync?sessionId=42')) throw new Error('Unexpected API read');
                    return { data: { success: true, data: fixtureProfile } };
                },
                async post(url, data) {
                    if (!url.endsWith('EditMemoryEntryAsync') || data.sessionId !== 42
                        || data.revision !== fixtureProfile.revision) throw new Error('Unexpected save or revision');
                    memoryWrites.push(data);
                    const entry = { ...data.entry, id: 'entry-1', version: 1, workspaceId: '' };
                    fixtureProfile = { ...fixtureProfile, revision: 'r' + (memoryWrites.length + 1),
                        entries: data.entry.remove ? [] : [entry] };
                    return { data: { success: true, data: fixtureProfile } };
                }
            };
            window.reviewApp = new Vue({
                el: '#app', computed: memoryOptions.computed, methods: memoryOptions.methods,
                data() { return { ...memoryOptions.data(), currentSessionId: 42, memoryMode: 2 }; }
            });
            window.propose = (entry, remove = false) => reviewApp.reportMemoryUpdate('pending-review', null, 42, {
                status: 'pending-review', reason: 'pending-review', requested: true,
                revision: fixtureProfile.revision, globalChanges: 0, workspaceChanges: 0,
                proposals: [{ entry, remove }]
            });
            propose({ id: '', version: 0, text: 'Fictional Alpha is also Little A', topic: 'Alpha',
                kind: 'reference', tier: 'topic', pinned: true, workspaceId: '', expiresAt: null,
                evidence: 'Fictional Alpha is also Little A <b>unverified</b>' });
        });
        const proposal = page.locator('.chat-memory-proposal');
        check(await proposal.count() === 1, 'Candidate must be visible in the receipt');
        await proposal.locator('button').click();
        await page.locator('.chat-memory-entry-editor').waitFor();
        check(await page.evaluate(() => memoryWrites.length) === 0, 'Review is not a write');
        const evidence = page.locator('.chat-memory-entry-editor details');
        await evidence.locator('summary').click();
        check((await evidence.innerText()).includes('<b>unverified</b>'), 'Evidence must be text, not markup');
        check(await evidence.locator('b').count() === 0, 'Do not render model evidence as HTML');
        await page.locator('.chat-memory-dialog .el-dialog__headerbtn').click();
        await page.locator('.chat-memory-dialog').waitFor({ state: 'hidden' });
        check(await page.evaluate(() => memoryWrites.length) === 0, 'Closing review must not save');
        await proposal.locator('button').click();
        await page.locator('.chat-memory-entry-editor textarea').fill('Fictional Alpha is also Little A (checked)');
        await page.locator('.chat-memory-editor-actions > .el-button--primary').click();
        await page.waitForFunction(() => memoryWrites.length === 1 && reviewApp.memoryEntryDraft === null);
        check(await page.evaluate(() => memoryWrites[0].entry.text.endsWith('(checked)')), 'Save exact reviewed text');
        check(await page.locator('.chat-memory-update-result').getAttribute('class').then(value => value.includes('el-alert--success')),
            'Only successful confirmation may show success');
        await page.locator('.chat-memory-dialog .el-dialog__headerbtn').click();
        await page.locator('.chat-memory-dialog').waitFor({ state: 'hidden' });
        await page.evaluate(() => propose(fixtureProfile.entries[0], true));
        await page.setViewportSize({ width: 390, height: 844 });
        const candidateBox = await proposal.boundingBox();
        check(candidateBox.x >= 0 && candidateBox.x + candidateBox.width <= 390, 'Mobile candidate must fit');
        await proposal.locator('button').click();
        check(await page.locator('.chat-memory-editor-actions > .el-button--primary').count() === 0,
            'Deletion review must not offer a misleading Save entry action');
        await page.locator('.chat-memory-editor-actions .el-button--danger').click();
        check(await page.evaluate(() => memoryWrites.length) === 1, 'Opening delete confirmation must not delete');
        await page.locator('.el-popconfirm__action .el-button--primary').click();
        await page.waitForFunction(() => memoryWrites.length === 2 && reviewApp.memoryEntryDraft === null);
        check(await page.evaluate(() => memoryWrites[1].entry.remove === true), 'Delete must use the existing authorized editing API');
        await page.locator('.chat-memory-dialog .el-dialog__headerbtn').click();
        await page.locator('.chat-memory-dialog').waitFor({ state: 'hidden' });
        for (const reason of ['intent-deferred', 'no-save-intent']) {
            await page.evaluate(reason => reviewApp.reportMemoryUpdate('unchanged', null, 42, {
                status: 'unchanged', reason, requested: true, globalChanges: 0, workspaceChanges: 0, proposals: []
            }), reason);
            const receipt = page.locator('.chat-memory-update-result');
            check((await receipt.getAttribute('class')).includes('el-alert--info'), 'Non-saving intent is informational');
            check((await receipt.innerText()).includes(fixture.labels['AdminChat.Memory.UpdateReason.' + reason]),
                'Non-saving intent must use the current locale and never claim persistence');
            check(await proposal.count() === 0, 'Deferred intent must not retain a stale save candidate');
            check(await page.evaluate(() => memoryWrites.length) === 2, 'Intent receipt must not write Memory');
        }
        results.push(culture + ': review, cancel, save, mobile, confirmed deletion and non-saving intent receipts passed');
    }
    check(failures.length === 0, failures.join('\n'));
    return results;
}
