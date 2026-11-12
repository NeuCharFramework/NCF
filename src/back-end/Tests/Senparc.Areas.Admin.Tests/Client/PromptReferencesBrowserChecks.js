// Run from the repository root with playwright-cli run-code --filename=<this file>.
async page => {
    const root = 'tools/NcfSimulatedSite';
    const errors = [];
    await page.setViewportSize({ width: 1280, height: 900 });
    page.on('pageerror', error => errors.push(error.message));
    await page.setContent(`<div id="test" style="max-width:760px;margin:24px">
        <el-input type="textarea" v-model="text" v-prompt-range ref="input"></el-input>
        <prompt-reference-content v-if="history.length" :content="historyText" :references="history"
            :ai-score="autoScore"></prompt-reference-content></div>`);
    await page.addStyleTag({ path: root + '/Senparc.Web/wwwroot/lib/element-ui_2.13.2/element.css' });
    await page.addStyleTag({ path: root + '/Senparc.Areas.Admin/wwwroot/css/Admin/Shared/PromptRangeSelector.css' });
    await page.addScriptTag({ path: root + '/Senparc.Web/wwwroot/lib/vue/vue.js' });
    await page.addScriptTag({ path: root + '/Senparc.Web/wwwroot/lib/element-ui_2.13.2/element.js' });
    await page.evaluate(() => {
        window.ncfT = key => key.split('.').pop();
        window.requests = [];
        window.infos = {
            1: { promptItemId: 1, promptCode: '2026.11.04.1-T1-A1', currentScore: 8, humanScore: 8, robotScore: -1, hasBenchmark: true },
            2: { promptItemId: 2, promptCode: '2026.11.04.1-T2-A1', currentScore: -1, humanScore: -1, robotScore: -1, hasBenchmark: true }
        };
        window.items = [
            { id: 1, fullVersion: infos[1].promptCode, rangeId: 1, rangeName: '2026.11.04.1', rangeAlias: 'Test range', tactic: '1', content: 'Alpha prompt', currentScore: 8 },
            { id: 2, fullVersion: infos[2].promptCode, rangeId: 1, rangeName: '2026.11.04.1', rangeAlias: 'Test range', tactic: '2', content: 'Beta prompt', currentScore: -1 }
        ];
        window.service = {
            async get(url, config) {
                const method = url.split('.').pop();
                requests.push({ method, params: config.params });
                const data = method === 'GetSelectorOptionsAsync' ? []
                    : method === 'SearchSelectorAsync' ? { items, totalCount: 2 }
                    : method === 'GetSelectorRangeScoresAsync' ? items.map(item => ({ ...item, currentScore: infos[item.id].currentScore }))
                    : method === 'GetReferenceInfoAsync' ? infos[config.params.id]
                    : items.find(item => item.id === config.params.id);
                return { data: { success: true, data } };
            },
            async post(url, body) {
                requests.push({ method: url.split('.').pop(), body });
                const data = infos[body.id];
                data.humanScore = data.currentScore = body.score;
                return { data: { success: true, data: { ...data } } };
            }
        };
    });
    await page.addScriptTag({ path: root + '/Senparc.Areas.Admin/wwwroot/js/Admin/Shared/PromptReferences.js' });
    await page.addScriptTag({ path: root + '/Senparc.Areas.Admin/wwwroot/js/Admin/Shared/PromptRangeSelector.js' });
    await page.evaluate(() => {
        window.app = new Vue({
            el: '#test', data: { text: 'before ', history: [], historyText: '' },
            methods: {
                async autoScore(id) {
                    requests.push({ method: 'ai-score', id });
                    infos[id].robotScore = infos[id].currentScore = 3.1;
                    return { ...infos[id] };
                }
            }
        });
    });
    const textarea = page.locator('#test textarea');
    const insert = async index => {
        await textarea.evaluate(input => { input.focus(); input.setSelectionRange(input.value.length, input.value.length); });
        await page.locator('.ncf-prompt-selector-trigger').click();
        await page.locator('.ncf-prompt-selector-item').nth(index).click();
        await page.getByRole('button', { name: 'Insert', exact: true }).click();
        await page.locator('.ncf-prompt-selector').waitFor({ state: 'hidden' });
    };
    await insert(0);
    await insert(1);
    const captured = await page.evaluate(() => NcfPromptReferences.capture(document.querySelector('#test textarea')));
    if (captured.length !== 2 || captured[0].start !== 7 || captured[1].start !== 19) throw new Error('Multi-reference insertion failed');
    if (await textarea.inputValue() !== 'before Alpha promptBeta prompt') throw new Error('Annotations leaked into input');
    await page.evaluate(() => {
        app.historyText = app.text;
        app.history = NcfPromptReferences.capture(document.querySelector('#test textarea'));
    });
    await page.locator('.ncf-prompt-reference-content .ncf-prompt-reference-pill').first().click();
    await page.locator('.el-dropdown-menu:visible .el-dropdown-menu__item').filter({ hasText: /^Score$/ }).click();
    await page.locator('.ncf-prompt-score-column').first().waitFor();
    await page.waitForFunction(() => document.querySelector('.ncf-prompt-score-preview pre')?.textContent === 'Alpha prompt');
    await page.locator('.ncf-prompt-score-dialog').evaluate(async element => {
        await Promise.all(element.getAnimations().map(animation => animation.finished));
    });
    await page.locator('.ncf-prompt-score-editor .el-slider__button-wrapper').hover();
    const runway = await page.locator('.ncf-prompt-score-editor .el-slider__runway').boundingBox();
    const handle = await page.locator('.ncf-prompt-score-editor .el-slider__button-wrapper').boundingBox();
    await page.mouse.move(handle.x + handle.width / 2, handle.y + handle.height / 2);
    await page.mouse.down();
    await page.mouse.move(runway.x + runway.width * 0.64, runway.y + runway.height / 2, { steps: 6 });
    await page.mouse.up();
    await page.waitForFunction(() => document.querySelector('.ncf-prompt-score-column.is-target .ncf-prompt-score-bar').style.height === '64%');
    if (await page.evaluate(() => infos[1].currentScore) !== 8) throw new Error('Dragging saved a score before confirmation');
    await page.locator('.ncf-prompt-score-column[data-prompt-id="2"]').click();
    await page.waitForFunction(() => document.querySelector('.ncf-prompt-score-preview pre')?.textContent === 'Beta prompt');
    if (await page.locator('.el-input-number input').inputValue() !== '6.4') throw new Error('Previewing another prompt changed the draft score');
    if (await page.locator('.ncf-prompt-score-column.is-target').getAttribute('data-prompt-id') !== '1') throw new Error('Preview changed the rating target');
    if (await page.evaluate(() => requests.some(request => request.method === 'ScoreReferenceAsync'))) throw new Error('Preview sent a score write');
    await page.getByRole('button', { name: 'Save', exact: true }).click();
    await page.waitForFunction(() => NcfPromptReferences.scores[1].currentScore === 6.4);
    if (await page.evaluate(() => requests.find(request => request.method === 'ScoreReferenceAsync').body.id) !== 1) throw new Error('Save rated the preview instead of the referenced prompt');
    const matching = await page.locator('.ncf-prompt-reference-pill').allTextContents();
    if (matching.filter(text => text.includes('T1-A1') && text.includes('6.4')).length !== 2) throw new Error('Shared score not reflected in composer and history');
    await page.locator('.ncf-prompt-reference-content .ncf-prompt-reference-pill').first().click();
    await page.locator('.el-dropdown-menu:visible .el-dropdown-menu__item').filter({ hasText: /^Score$/ }).click();
    await page.getByRole('button', { name: 'AIScore', exact: true }).click();
    await page.locator('.el-message-box__btns .el-button--primary').click();
    await page.waitForFunction(() => NcfPromptReferences.scores[1].currentScore === 3.1);
    if (await page.evaluate(() => NcfPromptReferences.scores[1].humanScore) !== 6.4) throw new Error('AI erased manual source score');
    await page.getByRole('button', { name: 'Cancel', exact: true }).click();
    await page.locator('.ncf-prompt-reference-content .ncf-prompt-reference-pill').first().click();
    await page.locator('.el-dropdown-menu:visible .el-dropdown-menu__item').filter({ hasText: /^Score$/ }).click();
    await page.locator('.ncf-prompt-score-column').first().waitFor();
    await page.locator('.el-input-number input').fill('1.2');
    await page.locator('.el-input-number input').press('Tab');
    await page.waitForFunction(() => document.querySelector('.ncf-prompt-score-column.is-target .ncf-prompt-score-bar').style.height === '12%');
    await page.setViewportSize({ width: 390, height: 844 });
    const dialog = await page.locator('.ncf-prompt-score-dialog').boundingBox();
    if (dialog.x < 0 || dialog.x + dialog.width > 391) throw new Error('Rating dialog overflows mobile');
    await page.getByRole('button', { name: 'Cancel', exact: true }).click();
    if (await page.evaluate(() => infos[1].currentScore) !== 3.1) throw new Error('Cancel saved the draft');
    await textarea.evaluate(input => { input.focus(); input.setSelectionRange(10, 10); });
    await page.keyboard.insertText('x');
    const edited = await page.evaluate(() => NcfPromptReferences.capture(document.querySelector('#test textarea')));
    if (edited.length !== 1 || edited[0].promptItemId !== 2 || edited[0].start !== 20) throw new Error('Editing one reference invalidated the wrong ranges');
    if (await page.locator('.ncf-prompt-reference-content .ncf-prompt-reference-pill').count() !== 2) throw new Error('Editing composer altered persisted history');
    await page.locator('.ncf-prompt-reference-content .ncf-prompt-reference-pill').first().click();
    await page.locator('.el-dropdown-menu:visible .el-dropdown-menu__item').filter({ hasText: /^View$/ }).click();
    await page.waitForFunction(() => document.querySelector('.ncf-prompt-selector-preview').textContent.includes('Alpha prompt'));
    if (await page.getByRole('button', { name: 'Insert', exact: true }).count()) throw new Error('View-only picker could overwrite composer');
    await page.getByRole('button', { name: 'Cancel', exact: true }).click();
    await page.locator('.ncf-prompt-reference-labels .ncf-prompt-reference-pill').click();
    await page.locator('.el-dropdown-menu:visible .el-dropdown-menu__item').filter({ hasText: /^Remove$/ }).click();
    if (await page.evaluate(() => NcfPromptReferences.capture(document.querySelector('#test textarea')).length)) throw new Error('Remove reference failed');
    if (!(await textarea.inputValue()).includes('Beta prompt')) throw new Error('Removing annotation removed text');
    await page.setViewportSize({ width: 390, height: 844 });
    const history = await page.locator('.ncf-prompt-reference-content').boundingBox();
    if (history.x + history.width > 391) throw new Error('History annotation overflows mobile');
    if (errors.length) throw new Error(errors.join('\n'));
    return { passed: true, requests: await page.evaluate(() => requests.length) };
}
