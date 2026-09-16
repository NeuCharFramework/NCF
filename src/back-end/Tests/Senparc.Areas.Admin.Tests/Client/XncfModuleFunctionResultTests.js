'use strict';

const assert = require('assert');
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const scriptPath = path.resolve(
    __dirname,
    '../../../Senparc.Areas.Admin/wwwroot/js/Admin/Pages/XncfModule/start.js');
const pagePath = path.resolve(
    __dirname,
    '../../../Senparc.Areas.Admin/Areas/Admin/Pages/XncfModule/Start.cshtml');
const script = fs.readFileSync(scriptPath, 'utf8');
const page = fs.readFileSync(pagePath, 'utf8');

let capturedOptions = null;
const sanitizeCalls = [];

function decodeEntities(value) {
    return String(value)
        .replace(/&lt;/g, '<')
        .replace(/&gt;/g, '>')
        .replace(/&quot;/g, '"')
        .replace(/&#39;|&apos;/g, "'")
        .replace(/&amp;/g, '&');
}

const document = {
    title: '',
    createElement(tagName) {
        assert.strictEqual(tagName, 'textarea');
        let value = '';
        return {
            set innerHTML(input) {
                value = decodeEntities(input);
            },
            get value() {
                return value;
            }
        };
    }
};

const DOMPurify = {
    sanitize(value, config) {
        sanitizeCalls.push({ value, config });
        return value;
    }
};

function Vue(options) {
    capturedOptions = options;
    return options;
}

const context = vm.createContext({
    Vue,
    DOMPurify,
    document,
    window: { location: { origin: 'https://example.test' } },
    service: {},
    ncfT(key) { return key; },
    resizeUrl() { return { uid: 'test' }; },
    getNavMenu() { },
    URL,
    String,
    Array,
    Object,
    Promise,
    console
});

vm.runInContext(script, context, { filename: scriptPath });
assert.ok(capturedOptions, 'Vue page options should be captured.');

const viewModel = Object.assign({}, capturedOptions.data());
Object.keys(capturedOptions.methods).forEach(name => {
    viewModel[name] = capturedOptions.methods[name].bind(viewModel);
});

const rendered = viewModel.safeFunctionResultHtml(
    'SessionId: abc&lt;br/&gt;Template: python-exec&lt;br /&gt;Status: Running');
assert.strictEqual(
    rendered,
    'SessionId: abc<br/>Template: python-exec<br />Status: Running');

const xssPayload = '&lt;script&gt;alert(1)&lt;/script&gt;'
    + '&lt;img src=x onerror=alert(2)&gt;'
    + '&lt;a href="javascript:alert(3)" style="color:red"&gt;bad&lt;/a&gt;';
viewModel.safeFunctionResultHtml(xssPayload);

const securityCall = sanitizeCalls[sanitizeCalls.length - 1];
assert.ok(securityCall.value.includes('<script>alert(1)</script>'));
assert.ok(!securityCall.config.ALLOWED_TAGS.includes('script'));
assert.ok(!securityCall.config.ALLOWED_TAGS.includes('img'));
assert.ok(!securityCall.config.ALLOWED_ATTR.includes('onerror'));
assert.ok(!securityCall.config.ALLOWED_ATTR.includes('style'));
assert.strictEqual(securityCall.config.ALLOW_ARIA_ATTR, false);
assert.strictEqual(securityCall.config.ALLOW_DATA_ATTR, false);
assert.strictEqual(securityCall.config.ALLOWED_URI_REGEXP.test('javascript:alert(3)'), false);
assert.strictEqual(securityCall.config.ALLOWED_URI_REGEXP.test('https://example.test/result'), true);

assert.strictEqual(viewModel.decodeFunctionResult('line1\\r\\nline2\\nline3'), 'line1\nline2\nline3');
assert.ok(page.includes('v-html="safeFunctionResultHtml(runResult.msg)"'));
assert.ok(!page.includes('v-html="safeHtml(runResult.msg)"'));
assert.ok(script.includes('functionKeyMatches'),
    'XNCF Function deep links must accept the short method-name alias.');
assert.ok(page.includes('target="_blank" rel="noopener noreferrer"'));
assert.ok(page.indexOf('dompurify.min.js') < page.indexOf('Pages/XncfModule/Start.js'));

process.stdout.write('XNCF Function result HTML tests passed.\n');
