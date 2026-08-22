'use strict';

const assert = require('assert');
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const commonPath = path.resolve(__dirname,
    '../../../Senparc.Areas.Admin/wwwroot/js/Admin/Pages/NeuCharPivot/common.js');
const aggregatePath = path.resolve(__dirname,
    '../../../Senparc.Areas.Admin/wwwroot/js/Admin/Pages/NeuCharPivot/Aggregate.js');
const pagePath = path.resolve(__dirname,
    '../../../Senparc.Areas.Admin/Areas/Admin/Pages/NeuCharPivot/Aggregate.cshtml');

let receivedValue = null;
let receivedOptions = null;
const sandbox = {
    window: {
        DOMPurify: {
            sanitize(value, options) {
                receivedValue = value;
                receivedOptions = options;
                return '<p>safe</p>';
            }
        }
    },
    console
};
vm.createContext(sandbox);
vm.runInContext(fs.readFileSync(commonPath, 'utf8'), sandbox, { filename: commonPath });

const html = sandbox.window.NeuCharPivotUi.sanitizeHtml(
    '<p onclick="attack()">safe</p><script>attack()</script>');
assert.strictEqual(html, '<p>safe</p>');
assert.ok(receivedValue.includes('<script>'), 'The complete Function result must be sent to the sanitizer.');
assert.ok(receivedOptions.ALLOWED_TAGS.includes('table'), 'Safe rich-text tables should remain available.');
assert.ok(!receivedOptions.ALLOWED_TAGS.includes('script'), 'Executable elements must not be allowed.');
assert.deepStrictEqual(Array.from(receivedOptions.ALLOWED_ATTR), ['href', 'title']);
assert.strictEqual(receivedOptions.ALLOW_ARIA_ATTR, false);
assert.strictEqual(receivedOptions.ALLOW_DATA_ATTR, false);
assert.strictEqual(receivedOptions.ALLOWED_URI_REGEXP.test('javascript:alert(1)'), false);
assert.strictEqual(receivedOptions.ALLOWED_URI_REGEXP.test('https://www.senparc.com/'), true);

const aggregateScript = fs.readFileSync(aggregatePath, 'utf8');
const page = fs.readFileSync(pagePath, 'utf8');
assert.ok(aggregateScript.includes('NeuCharPivotUi.sanitizeHtml(data.data)'),
    'String Function results must pass through the shared sanitizer.');
assert.ok(page.includes('v-html="result.html"'), 'Sanitized Function HTML should be rendered as HTML.');
assert.ok(page.indexOf('dompurify.min.js') < page.indexOf('common.js'),
    'DOMPurify must load before the shared NeuCharPivot helpers.');
assert.ok(page.includes('v-else class="aggregate-result aggregate-result-text"'),
    'Structured results and errors must retain a text-only rendering path.');

console.log('NeuCharPivot Aggregate safe HTML tests passed.');
