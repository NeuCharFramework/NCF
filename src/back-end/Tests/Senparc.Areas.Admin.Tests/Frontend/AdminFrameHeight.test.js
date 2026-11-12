const assert = require('node:assert/strict');
const test = require('node:test');
const frame = require('../../../Senparc.Areas.Admin/wwwroot/js/Admin/Shared/AdminFrameHeight.js');

test('标签页只补偿从视口高度里扣掉的部分', function () {
    assert.equal(frame.TAB_CHROME, '96px');
    assert.equal(
        frame.boostTabFrameLength('calc(100vh - 150px)'),
        'calc(100vh + var(--ncf-tab-chrome) - 150px)'
    );
    assert.equal(
        frame.boostTabFrameLength('calc(100vh - 300px)'),
        'calc(100vh + var(--ncf-tab-chrome) - 300px)'
    );
    assert.equal(
        frame.boostTabFrameLength('max-height: calc(100vh - 200px); height: 100vh;'),
        'max-height: calc(100vh + var(--ncf-tab-chrome) - 200px); height: 100vh;'
    );
});

test('补偿可以重复执行，全屏遮罩保持原来的视口高度', function () {
    const once = frame.boostTabFrameLength('calc(100vh - 458px)');
    assert.equal(frame.boostTabFrameLength(once), once);
    assert.equal(frame.boostTabFrameLength('100vh'), '100vh');
    assert.equal(frame.boostTabFrameLength('height: 90vh;'), 'height: 90vh;');
    assert.equal(frame.boostTabFrameLength(''), '');
});
