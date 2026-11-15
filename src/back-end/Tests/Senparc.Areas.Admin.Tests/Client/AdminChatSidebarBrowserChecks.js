async page => {
    const admin = 'tools/NcfSimulatedSite/Senparc.Areas.Admin';
    const web = 'tools/NcfSimulatedSite/Senparc.Web/wwwroot';
    const origin = 'http://ncf-sidebar.test';
    const failures = [];
    const check = (condition, message) => { if (!condition) throw new Error(message); };
    page.on('pageerror', error => failures.push(error.message));
    await page.route(origin + '/**', async route => {
        const pathname = route.request().url().slice(origin.length).split('?')[0];
        if (pathname === '/template') {
            await route.fulfill({ path: admin + '/Areas/Admin/Pages/AdminChat/Chat.cshtml', contentType: 'text/plain' });
        } else if (pathname.startsWith('/fonts/')) {
            await route.fulfill({ path: web + '/lib/element-ui_2.13.2' + pathname });
        } else {
            await route.fulfill({ contentType: 'text/html', body: '<!doctype html><html><body></body></html>' });
        }
    });
    await page.setViewportSize({ width: 1440, height: 900 });
    await page.goto(origin);
    const raw = await page.evaluate(() => fetch('/template').then(response => response.text()));
    const aside = raw.slice(raw.indexOf('<el-aside'), raw.indexOf('</el-aside>') + '</el-aside>'.length)
        .replace(/@AR\["([^"]+)"\]/g, '$1').replace(/@@/g, '@');
    const livePanel = raw.slice(raw.indexOf('<div v-if="isAIResponding'),
        raw.indexOf('            </div>\n            <nav v-if="messageRail.length"'))
        .replace(/@AR\["([^"]+)"\]/g, '$1').replace(/@@/g, '@');
    await page.setContent(`<div id="app"><div class="admin-chat-container">
        <el-container ref="workspaceContainer" class="chat-main-container"
            :style="{ '--chat-sidebar-width': sidebarWidth + 'px' }">
            ${aside}<el-main class="chat-content">${livePanel}</el-main>
        </el-container></div></div>`);
    await page.addStyleTag({ path: web + '/lib/element-ui_2.13.2/element.css' });
    await page.addStyleTag({ path: admin + '/wwwroot/css/Admin/Pages/AdminChat/Chat.css' });
    await page.addScriptTag({ path: web + '/lib/vue/vue.js' });
    await page.addScriptTag({ path: web + '/lib/element-ui_2.13.2/element.js' });
    await page.evaluate(() => {
        window.NativeVue = Vue;
        window.ChatLauncherMixin = {};
        window.ncfT = key => key;
        Vue.prototype.ncfT = window.ncfT;
        window.Vue = function(options) { window.sidebarOptions = options; };
    });
    await page.addScriptTag({ path: admin + '/wwwroot/js/Admin/Pages/AdminChat/Chat.js' });
    await page.evaluate(() => {
        window.Vue = window.NativeVue;
        window.sidebarRequests = [];
        window.sidebarMessages = [];
        const options = window.sidebarOptions;
        window.sidebarTestApp = new Vue({
            el: '#app', data: options.data, computed: options.computed,
            methods: {
                ...options.methods,
                switchSession(id) { this.currentSessionId = id; },
                formatMessageContent(text) { return text; }
            }
        });
        sidebarTestApp.currentUserId = 12;
        sidebarTestApp.$message = {
            success: message => sidebarMessages.push(message),
            error: message => { throw new Error(message); },
            warning: message => { throw new Error(message); }
        };
        sidebarTestApp.sessionList = [
            { id: 1, title: 'Alpha first', workspaceId: 'alpha', workspaceName: 'Alpha', sortOrder: 0 },
            { id: 2, title: 'Alpha second', workspaceId: 'alpha', workspaceName: 'Alpha', sortOrder: 1 },
            { id: 3, title: 'Alpha third', workspaceId: 'alpha', workspaceName: 'Alpha', sortOrder: 2 },
            { id: 4, title: 'Beta first', workspaceId: 'beta', workspaceName: 'Beta' }
        ];
        sidebarTestApp.currentSessionId = 1;
        window.service = {
            async post(url, data) {
                sidebarRequests.push(data);
                if (!String(url).includes('ReorderSessionAsync')) return { data: { success: true } };
                const group = sidebarTestApp.groupedSessionList.find(group => group.sessions.some(s => s.id === data.sessionId));
                const sessions = group.sessions.slice();
                const index = sessions.findIndex(s => s.id === data.sessionId);
                const moved = sessions.splice(index, 1)[0];
                sessions.splice(sessions.findIndex(s => s.id === data.targetSessionId) + (data.insertAfter ? 1 : 0), 0, moved);
                return { data: { success: true, data: sessions.map((s, sortOrder) => ({ id: s.id, sortOrder })) } };
            }
        };
    });
    const sidebar = page.locator('.chat-sidebar');
    const width = async () => Math.round((await sidebar.boundingBox()).width);
    check(await width() === 250, 'Default sidebar width must be 250px');
    await page.locator('.sidebar-toggle').click();
    check(await width() === 48, 'Collapsed sidebar must be a 48px rail');
    check(!await page.locator('.sidebar-body').isVisible(), 'Collapse must hide sidebar content');
    await page.locator('.sidebar-toggle').click();
    check(await width() === 250, 'Expand must restore sidebar width');

    const resizer = page.locator('.chat-sidebar-resizer');
    const box = await resizer.boundingBox();
    await page.mouse.move(box.x + box.width / 2, box.y + 100);
    await page.mouse.down();
    await page.mouse.move(box.x + 104, box.y + 100);
    await page.mouse.up();
    check(await width() === 350, 'Pointer resize must track the actual splitter delta');
    await resizer.focus();
    await page.keyboard.press('ArrowLeft');
    check(await width() === 330, 'Keyboard resize must use 20px steps');

    const group = page.locator('.session-group').first();
    await group.locator('.session-group-title').click();
    check(await group.locator('.session-group-title').getAttribute('aria-expanded') === 'false', 'Group collapse must expose state');
    check(!await group.locator('.session-item').first().isVisible(), 'Group collapse must hide its sessions');
    await group.locator('.session-group-title').click();
    const titles = () => group.locator('.session-title').allTextContents();
    await page.mouse.move(0, 0);
    const idleHandle = group.locator('.session-drag-handle').first();
    check(await idleHandle.evaluate(el => getComputedStyle(el).opacity) === '0', 'Reorder handle stays hidden until hover');
    const third = group.locator('.session-item').nth(2);
    await third.hover();
    check(await third.locator('.session-drag-handle').evaluate(el => getComputedStyle(el).opacity) === '1', 'Reorder handle appears on hover');
    await third.locator('.session-drag-handle').dragTo(group.locator('.session-item').first(), { targetPosition: { x: 80, y: 3 } });
    check(JSON.stringify(await titles()) === JSON.stringify(['Alpha third', 'Alpha first', 'Alpha second']), 'Real browser dragging must reorder within group');
    check(await page.evaluate(() => sidebarRequests.length) === 1, 'Drop must save exactly once');
    await group.locator('.session-item').first().hover();
    await group.locator('.session-drag-handle').first().dragTo(page.locator('.session-group').nth(1).locator('.session-item'));
    check(await page.evaluate(() => sidebarRequests.length) === 1, 'Cross-Workspace drops must not call the server');
    await group.locator('.session-drag-handle').first().focus();
    await page.keyboard.press('Alt+ArrowDown');
    check(JSON.stringify(await titles()) === JSON.stringify(['Alpha first', 'Alpha third', 'Alpha second']), 'Keyboard reorder must match drag behavior');

    await page.evaluate(() => {
        sidebarTestApp.runningSessionId = 3;
        sidebarTestApp.isSending = true;
        sidebarTestApp.isAIResponding = true;
        sidebarTestApp.currentSessionId = 1;
        sidebarTestApp.handleLiveStreamEvent('assistant-phase', {
            text: 'Only session three reasoning',
            trajectoryEvent: { eventType: 'TextReasoningContent', sequence: 1, content: 'Only session three reasoning' }
        }, 'temp', 3);
        sidebarTestApp.handleLiveStreamEvent('trajectory-event', {
            trajectoryEvent: { eventType: 'tool.call', sequence: 2, content: 'Only session three tool trace' }
        }, 'temp', 3);
    });
    check(await page.locator('.live-run-panel').count() === 0, 'Other sessions must not render the running session thinking or trajectory panel');
    await group.locator('.session-item').filter({ hasText: 'Alpha third' }).click();
    check(await page.locator('.live-run-panel').isVisible(), 'Returning to the running session must restore its live panel');
    check((await page.locator('.live-thinking-content').textContent()) === 'Only session three reasoning', 'Live reasoning must survive navigation');
    await page.locator('.live-trajectory-collapse .el-collapse-item__header').click();
    check((await page.locator('.live-trajectory-event-content').allTextContents()).includes('Only session three tool trace'), 'Live trajectory must remain available only in the owning session');
    await group.locator('.session-item').filter({ hasText: 'Alpha first' }).click();
    check(await page.locator('.live-run-panel').count() === 0, 'Navigating away must hide live state again');
    const running = group.locator('.session-item.is-running');
    check((await running.locator('.session-title').textContent()) === 'Alpha third', 'Run indicator must stay with the originating session');
    check(!await running.evaluate(el => el.classList.contains('active')), 'Running is distinct from selected');
    check(await running.evaluate(el => getComputedStyle(el, '::before').animationName) === 'chat-session-flow', 'Running rail must animate');
    check((await running.evaluate(el => getComputedStyle(el, '::before').backgroundImage)).includes('rgb(255, 61, 138)'), 'Running rail uses a distinct neon color');
    check((await running.evaluate(el => getComputedStyle(el, '::before').boxShadow)) !== 'none', 'Running rail keeps a visible glow');
    await running.locator('.session-actions').click();
    check((await page.locator('.el-dropdown-menu__item:visible').filter({ hasText: 'AdminChat.Archive.Action' }).getAttribute('class')).includes('is-disabled'), 'Running session cannot be archived from the menu');
    await page.keyboard.press('Escape');
    await page.emulateMedia({ reducedMotion: 'reduce' });
    check(await running.evaluate(el => getComputedStyle(el, '::before').animationName) === 'none', 'Respect reduced motion');
    await page.emulateMedia({ reducedMotion: 'no-preference' });
    await group.locator('.session-group-title').click();
    check(await group.locator('.sidebar-running-dot').isVisible(), 'Collapsed groups must retain activity indication');
    await page.locator('.sidebar-toggle').click();
    check(await page.locator('.sidebar-heading .sidebar-running-dot').isVisible(), 'Collapsed rail must retain activity indication');
    await page.setViewportSize({ width: 600, height: 800 });
    check(await width() === 48, 'Mobile rules must not override collapsed width');
    await page.locator('.sidebar-toggle').click();
    check(await width() === 330, 'Expansion must preserve custom width at narrow viewports');
    await page.evaluate(() => { sidebarTestApp.isSending = false; });
    check(await page.locator('.is-running').count() === 0, 'Completed tasks must return to idle styling');
    if (await group.locator('.session-group-title').getAttribute('aria-expanded') === 'false') {
        await group.locator('.session-group-title').click();
    }
    await page.evaluate(async () => {
        sidebarTestApp.currentSessionId = 1;
        await sidebarTestApp.markSessionUnread(1);
        await sidebarTestApp.markSessionUnread(3);
    });
    const unread = group.locator('.session-item.is-unread');
    check(await unread.count() === 1, 'Only a background session becomes unread');
    check((await unread.locator('.session-title').textContent()) === 'Alpha third', 'Unread marker stays on the finished session');
    check((await unread.locator('.session-unread-status').textContent()).trim() === 'AdminChat.Sidebar.Unread', 'Unread session shows a readable label');
    check(await unread.evaluate(el => getComputedStyle(el, '::before').animationName) === 'chat-session-unread', 'Unread rail breathes');
    check((await unread.evaluate(el => getComputedStyle(el, '::before').backgroundColor)).includes('34, 197, 94'), 'Unread rail is green');
    check(await page.evaluate(() => sidebarRequests.some(item => item.sessionId === 3 && item.unread === true)
        && !sidebarRequests.some(item => item.sessionId === 1 && Object.prototype.hasOwnProperty.call(item, 'unread'))),
        'Unread state is saved for the background session');
    await page.evaluate(() => sidebarTestApp.clearSessionUnread(3));
    check(await page.locator('.is-unread').count() === 0, 'Opening the finished session clears the unread reminder');
    await page.evaluate(() => {
        const reorder = window.service.post.bind(window.service);
        window.service.post = async (url, data) => {
            if (String(url).includes('ArchiveSessionAsync')) {
                sidebarRequests.push({ archive: data.sessionId });
                return { data: { success: true } };
            }
            if (String(url).includes('RestoreSessionAsync')) {
                sidebarRequests.push({ restore: data.sessionId });
                return { data: { success: true } };
            }
            return reorder(url, data);
        };
    });
    const beta = page.locator('.session-item').filter({ hasText: 'Beta first' });
    await beta.hover();
    check(await page.locator('.session-archive-button').count() === 0, 'Archive is kept inside the session menu');
    check(await beta.locator('.el-dropdown').count() === 1, 'Active sessions keep the original menu');
    await beta.locator('.session-actions').click();
    await page.locator('.el-dropdown-menu__item:visible').filter({ hasText: 'AdminChat.Archive.Action' }).click();
    check(await page.locator('.session-title', { hasText: 'Beta first' }).count() === 0, 'Archived session leaves the sidebar');
    check(await page.evaluate(() => sidebarTestApp.archivedSessionList.some(session => session.title === 'Beta first' && session.status === 1)), 'Archived session stays available in the sidebar menu');
    await page.locator('.sidebar-tools-trigger').click();
    check(await page.locator('.sidebar-tools-action').filter({ hasText: 'Memory' }).isVisible(), 'More menu keeps Memory beside New conversation');
    const archivedItem = page.locator('.chat-archive-open').filter({ hasText: 'Beta first' });
    check(await archivedItem.isVisible(), 'More menu lists the archived session');
    check(await page.locator('.chat-archive-item').filter({ hasText: 'Beta first' }).locator('.chat-archive-delete').isVisible(), 'More menu can delete an archived session');
    await page.locator('.chat-archive-item').filter({ hasText: 'Beta first' }).locator('.chat-archive-restore').click();
    check(await page.locator('.session-title', { hasText: 'Beta first' }).count() === 1, 'Restore returns the archived session to the active list');
    check(await page.locator('.chat-archive-open', { hasText: 'Beta first' }).count() === 0, 'Restored session leaves the archive menu');
    check(await page.locator('.chat-archive-trigger').count() === 0, 'Archived conversations stay out of the chat header');
    check(failures.length === 0, 'Browser errors: ' + failures.join('; '));
    return 'Sidebar browser checks passed: collapse, resize, ordering, session-isolated thinking/trajectory panels, preserved live progress, activity and reduced motion.';
}
