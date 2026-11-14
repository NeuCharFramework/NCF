'use strict';

const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const { test } = require('node:test');

const file = path.resolve(__dirname, '../../../Senparc.Areas.Admin/wwwroot/js/Admin/Shared/VoiceNavigation.js');

function load(extra) {
    const context = Object.assign({
        window: {},
        console,
        Promise,
        setTimeout,
        clearTimeout,
        document: { documentElement: { lang: 'zh-CN' } }
    }, extra);
    vm.runInNewContext(fs.readFileSync(file, 'utf8'), context);
    return context.window.NcfVoiceNavigation;
}

function viewModel() {
    return {
        voiceNavListening: false,
        voiceNavPanelVisible: false,
        voiceNavMessage: '',
        voiceNavChoices: [],
        voiceNavDraft: ''
    };
}

const menus = [
    {
        menuName: '系统管理', url: '', children: [
            { menuName: '用户管理', url: '/Admin/AdminUserInfo/Index', id: 'sys-users', children: [] },
            { menuName: '菜单管理', url: '/Admin/Menu/Index', id: 'sys-menu', children: [] }
        ]
    },
    {
        menuName: '扩展模块', url: '', children: [
            { menuName: '用户管理', url: '/Admin/Xncf/Users', id: 'xncf-users', children: [] },
            { menuName: '知识库', url: '/Admin/KnowledgeBase/Index', id: 'kb', children: [] },
            { menuName: '首页', url: '/Admin/Index', id: 'home', children: [] }
        ]
    },
    { menuName: 'Knowledge Base', url: '/Admin/KnowledgeBase/En', id: 'kb-en', children: [] },
    { menuName: 'User Management', url: '/Admin/Users/En', id: 'users-en', children: [] },
    {
        menuName: 'System Management', url: '', children: [
            { menuName: 'User Management', url: '/Admin/System/Users', id: 'sys-users-en', children: [] }
        ]
    },
    { menuName: '知識ベース', url: '/Admin/KnowledgeBase/Ja', id: 'kb-ja', children: [] },
    { menuName: 'Base de connaissances', url: '/Admin/KnowledgeBase/Fr', id: 'kb-fr', children: [] },
    { menuName: 'Base de conocimiento', url: '/Admin/KnowledgeBase/Es', id: 'kb-es', children: [] },
    { menuName: 'База знаний', url: '/Admin/KnowledgeBase/Ru', id: 'kb-ru', children: [] },
    { menuName: '打开记录', url: '/Admin/OpenLog/Index', id: 'open-log', children: [] },
    { menuName: '危险入口', url: 'javascript:alert(1)', id: 'bad', children: [] }
];

function urls(result) {
    return Array.from(result.choices || [], item => item.url).sort();
}

test('command wrappers are removed and an exact page opens', () => {
    const api = load();
    const entries = api.indexMenus(menus);
    assert.equal(api.stripCommand('请帮我打开一下知识库页面'), '知识库');
    const result = api.match('请打开知识库页面', entries);
    assert.equal(result.status, 'navigate');
    assert.equal(result.target.url, '/Admin/KnowledgeBase/Index');
    assert.equal(api.match('打开首页', entries).target.url, '/Admin/Index');
    assert.equal(entries.some(entry => entry.url.indexOf('javascript:') >= 0), false);
});

test('parent qualifier selects one of two menus with the same name', () => {
    const api = load();
    const entries = api.indexMenus(menus);
    const ambiguous = api.match('打开用户管理', entries);
    assert.equal(ambiguous.status, 'choose');
    assert.deepEqual(urls(ambiguous), ['/Admin/AdminUserInfo/Index', '/Admin/Xncf/Users']);

    const qualified = api.match('打开系统管理的用户管理页面', entries);
    assert.equal(qualified.status, 'navigate');
    assert.equal(qualified.target.url, '/Admin/AdminUserInfo/Index');
    assert.equal(qualified.target.label, '系统管理 / 用户管理');

    const nested = api.match('扩展模块的用户管理', entries);
    assert.equal(nested.status, 'navigate');
    assert.equal(nested.target.url, '/Admin/Xncf/Users');
});

test('english, japanese, french, spanish and russian commands keep the page keyword', () => {
    const api = load();
    const entries = api.indexMenus(menus);
    assert.equal(api.match('please open the knowledge base page', entries).target.url, '/Admin/KnowledgeBase/En');
    assert.equal(api.match("open system management's user management", entries).target.url, '/Admin/System/Users');
    assert.equal(api.match('user management of system management', entries).target.url, '/Admin/System/Users');

    const englishUsers = api.match('can you open the user management page', entries);
    assert.equal(englishUsers.status, 'choose');
    assert.deepEqual(urls(englishUsers), ['/Admin/System/Users', '/Admin/Users/En']);

    assert.equal(api.match('知識ベースを開いて', entries).target.url, '/Admin/KnowledgeBase/Ja');
    assert.equal(api.match('ouvre la page base de connaissances', entries).target.url, '/Admin/KnowledgeBase/Fr');
    assert.equal(api.match('abre la página base de conocimiento', entries).target.url, '/Admin/KnowledgeBase/Es');
    assert.equal(api.match('пожалуйста, открой страницу база знаний', entries).target.url, '/Admin/KnowledgeBase/Ru');
});

test('a menu whose name starts with a command word is not stripped away', () => {
    const api = load();
    const entries = api.indexMenus(menus);
    const result = api.match('打开记录', entries);
    assert.equal(result.status, 'navigate');
    assert.equal(result.target.url, '/Admin/OpenLog/Index');
});

test('menu names come from the current menu response', () => {
    const api = load();
    const source = fs.readFileSync(file, 'utf8');
    assert.equal(source.includes('menuName:'), false);
    const list = api.menuListFromResponse({
        data: {
            success: true,
            data: {
                menuList: [
                    {
                        menuName: '新模块', url: '', children: [
                            { menuName: '新页面', url: '/Admin/New/Index', children: [] }
                        ]
                    }
                ]
            }
        }
    });
    const result = api.match('打开新模块的新页面', api.indexMenus(list));
    assert.equal(result.status, 'navigate');
    assert.equal(result.target.url, '/Admin/New/Index');
});

test('an unfinished recognition result is still usable', () => {
    const api = load();
    const interimOnly = api.pickTranscripts({ finals: [], interim: '打开知识库。' });
    assert.equal(interimOnly.length, 1);
    assert.equal(interimOnly[0], '打开知识库');

    const finalWins = api.pickTranscripts({ finals: ['打开菜单管理'], interim: '打开' });
    assert.equal(finalWins.length, 1);
    assert.equal(finalWins[0], '打开菜单管理');

    const blankFinal = api.pickTranscripts({ finals: ['。'], interim: '用户管理' });
    assert.equal(blankFinal.length, 1);
    assert.equal(blankFinal[0], '用户管理');
});

test('a module name opens the first page after its setup entry', () => {
    const api = load();
    const modules = [
        {
            menuName: '扩展模块', url: '', children: [
                {
                    menuName: '微信管理', url: '', id: 'weixin', children: [
                        { menuName: '设置/执行', url: '/Admin/XncfModule/Start?uid=weixin', id: 'weixin-setup', children: [] },
                        { menuName: '首页', url: '/Admin/WeixinManager/Index?uid=weixin', id: 'weixin-home', children: [] },
                        { menuName: '公众号管理', url: '/Admin/WeixinManager/MpAccount?uid=weixin', id: 'weixin-mp', children: [] }
                    ]
                },
                {
                    menuName: '文件管理', url: '', id: 'files', children: [
                        { menuName: '设置/执行', url: '/Admin/XncfModule/Start?uid=files', id: 'files-setup', children: [] },
                        { menuName: '文件资源管理', url: '/Admin/FileManager/Index?uid=files', id: 'files-home', children: [] }
                    ]
                }
            ]
        }
    ];
    const entries = api.indexMenus(modules);
    const landing = api.match('请打开微信管理', entries);
    assert.equal(landing.status, 'navigate');
    assert.equal(landing.target.url, '/Admin/WeixinManager/Index?uid=weixin');

    const child = api.match('打开公众号管理', entries);
    assert.equal(child.status, 'navigate');
    assert.equal(child.target.url, '/Admin/WeixinManager/MpAccount?uid=weixin');

    const setup = api.match('打开微信管理的设置/执行', entries);
    assert.equal(setup.status, 'navigate');
    assert.equal(setup.target.url, '/Admin/XncfModule/Start?uid=weixin');
});

test('dismiss closes the voice panel without keeping the draft', () => {
    const api = load();
    const vm = {
        voiceNavPanelVisible: true,
        voiceNavListening: true,
        voiceNavChoices: [{ url: '/Admin/Index', label: '首页' }],
        voiceNavDraft: '菜单'
    };
    api.dismiss(vm);
    assert.equal(vm.voiceNavPanelVisible, false);
    assert.equal(vm.voiceNavListening, false);
    assert.equal(vm.voiceNavDraft, '');
    assert.equal(vm.voiceNavChoices.length, 0);
    assert.equal(api.text('close'), '关闭');
});

test('a blocked microphone tells the user the browser will not ask again', () => {
    const api = load();
    assert.match(api.describeSpeechError('not-allowed', 'denied'), /不会再弹出/);
    assert.match(api.describeSpeechError('not-allowed', 'prompt'), /需要允许麦克风/);
    assert.match(api.describeSpeechError('not-allowed', 'granted'), /系统拦住了浏览器/);
    assert.match(api.describeSpeechError('service-not-allowed', ''), /没有开放语音识别/);
    assert.equal(api.speechPromptIsSeparate({ userAgent: 'Mozilla/5.0 Chrome/130.0.0.0 Safari/537.36' }), true);
    assert.equal(api.speechPromptIsSeparate({ userAgent: 'Mozilla/5.0 Version/17.6 Safari/605.1.15' }), false);
    assert.equal(api.speechPromptIsSeparate({ userAgent: 'Mozilla/5.0 (iPhone) CriOS/130.0.0.0 Mobile/15E148 Safari/604.1' }), false);
});

test('chrome asks for the microphone before starting recognition', async () => {
    const calls = [];
    const browser = {
        navigator: {
            userAgent: 'Mozilla/5.0 Chrome/130.0.0.0 Safari/537.36',
            permissions: {
                query() {
                    return Promise.resolve({ state: 'prompt' });
                }
            },
            mediaDevices: {
                getUserMedia(constraints) {
                    calls.push(constraints);
                    return Promise.resolve({
                        getTracks() {
                            return [{ stop() { calls.push('stop'); } }];
                        }
                    });
                }
            }
        },
        SpeechRecognition: function SpeechRecognition() {
            this.start = function () { calls.push('start'); };
            this.abort = function () { calls.push('abort'); };
            this.stop = function () { calls.push('recognition-stop'); };
        }
    };
    const api = load({ window: browser });
    const vm = viewModel();
    api.toggle(vm);
    assert.match(vm.voiceNavMessage, /弹出的提示/);
    await new Promise((resolve) => setImmediate(resolve));
    assert.equal(calls[0].audio, true);
    assert.equal(calls[1], 'stop');
    assert.equal(calls[2].audio, true);
    assert.equal(calls[3], 'start');
    assert.equal(vm.voiceNavListening, true);
    assert.match(vm.voiceNavMessage, /请说要打开的页面名称/);
});

test('chrome does not start recognition when the microphone is already blocked', async () => {
    const calls = [];
    const browser = {
        navigator: {
            userAgent: 'Mozilla/5.0 Chrome/130.0.0.0 Safari/537.36 Edg/130.0.0.0',
            permissions: {
                query() {
                    return Promise.resolve({ state: 'denied' });
                }
            },
            mediaDevices: {
                getUserMedia() {
                    calls.push('ask');
                    const error = new Error('Permission denied');
                    error.name = 'NotAllowedError';
                    return Promise.reject(error);
                }
            }
        },
        SpeechRecognition: function SpeechRecognition() {
            this.start = function () { calls.push('start'); };
        }
    };
    const api = load({ window: browser });
    const vm = viewModel();
    api.toggle(vm);
    await new Promise((resolve) => setImmediate(resolve));
    assert.deepEqual(calls, ['ask']);
    assert.match(vm.voiceNavMessage, /不会再弹出/);
    assert.equal(vm.voiceNavListening, false);
    assert.equal(vm.voiceNavPanelVisible, true);
});

test('on-device speech prefers dictation and falls back to command', async () => {
    const api = load();
    const seen = [];
    function Recognition() {}
    Recognition.available = function (options) {
        seen.push('available:' + options.quality);
        return Promise.resolve(options.quality === 'command' ? 'available' : 'downloadable');
    };
    Recognition.install = function (options) {
        seen.push('install:' + options.quality);
        return Promise.resolve(false);
    };
    const quality = await api.prepareLocalSpeech(Recognition, 'zh-CN');
    assert.equal(quality, 'command');
    assert.deepEqual(seen, ['available:dictation', 'install:dictation', 'available:command']);
});

function chromiumWindow(SpeechRecognition, getUserMedia) {
    return {
        navigator: {
            userAgent: 'Mozilla/5.0 Chrome/142.0.0.0 Safari/537.36',
            permissions: {
                query() {
                    return Promise.resolve({ state: 'granted' });
                }
            },
            mediaDevices: {
                getUserMedia: getUserMedia || function () {
                    return Promise.resolve({ getTracks() { return [{ stop() {} }]; } });
                }
            }
        },
        SpeechRecognition: SpeechRecognition
    };
}

test('chrome passes the live microphone track to on-device recognition', async () => {
    const started = [];
    let openedMicrophone = false;
    function SpeechRecognition() {
        this.start = function (track) {
            started.push({
                local: this.processLocally === true,
                continuous: this.continuous === true,
                track: !!(track && track.readyState !== 'ended'),
                phrases: this.phrases && this.phrases.length
            });
        };
        this.abort = function () {};
        this.stop = function () {};
    }
    SpeechRecognition.available = function (options) {
        return Promise.resolve(options.quality === 'dictation' ? 'available' : 'unavailable');
    };
    const browser = chromiumWindow(SpeechRecognition, function () {
        openedMicrophone = true;
        return Promise.resolve({
            getAudioTracks() {
                return [{ readyState: 'live', label: 'Mic', stop() {} }];
            }
        });
    });
    browser.SpeechRecognitionPhrase = function (phrase, boost) {
        this.phrase = phrase;
        this.boost = boost;
    };
    browser.service = {
        get() {
            return Promise.resolve({
                data: {
                    data: {
                        menuList: [
                            { menuName: '知识库', url: '/Admin/KnowledgeBase/Index', children: [] }
                        ]
                    }
                }
            });
        }
    };
    const api = load({ window: browser });
    const vm = viewModel();
    api.toggle(vm);
    await new Promise((resolve) => setImmediate(resolve));
    assert.equal(openedMicrophone, true);
    assert.equal(started.length, 1);
    assert.equal(started[0].local, true);
    assert.equal(started[0].continuous, false);
    assert.equal(started[0].track, true);
    assert.equal(started[0].phrases, 1);
    assert.match(vm.voiceNavMessage, /请说要打开的页面名称/);
});

test('chrome downloads the local language pack before listening', async () => {
    const steps = [];
    function SpeechRecognition() {
        this.start = function () { steps.push(this.processLocally === true ? 'local-start' : 'cloud-start'); };
        this.abort = function () {};
    }
    SpeechRecognition.available = function (options) {
        steps.push('available:' + options.quality);
        return Promise.resolve(options.quality === 'dictation' ? 'downloadable' : 'unavailable');
    };
    SpeechRecognition.install = function (options) {
        steps.push('install:' + options.quality);
        return Promise.resolve(true);
    };
    const api = load({ window: chromiumWindow(SpeechRecognition) });
    const vm = viewModel();
    api.toggle(vm);
    assert.match(vm.voiceNavMessage, /本地语音识别/);
    await new Promise((resolve) => setImmediate(resolve));
    assert.deepEqual(steps, ['install:dictation', 'available:dictation', 'local-start']);
    assert.match(vm.voiceNavMessage, /请说要打开的页面名称/);
});

test('a language-pack install that never finishes does not keep the panel waiting', async () => {
    const api = load();
    function Recognition() {}
    Recognition.available = function (options) {
        return Promise.resolve(options.quality === 'dictation' ? 'downloadable' : 'available');
    };
    Recognition.install = function () {
        return new Promise(function () {});
    };
    const quality = await api.prepareLocalSpeech(Recognition, 'zh-CN', null, {
        installPromise: Recognition.install(),
        installTimeout: 30
    });
    assert.equal(quality, '');
    assert.match(api.text('downloading'), /不会再弹窗/);
});

test('a premature no-speech does not end the session, and heard text fills the input', async () => {
    let recognition = null;
    function SpeechRecognition() {
        recognition = this;
        this.starts = 0;
        this.start = function () { this.starts += 1; };
        this.abort = function () {};
        this.stop = function () {};
    }
    SpeechRecognition.available = function (options) {
        return Promise.resolve(options.quality === 'dictation' ? 'available' : 'unavailable');
    };
    const api = load({ window: chromiumWindow(SpeechRecognition) });
    const vm = viewModel();
    api.toggle(vm);
    await new Promise((resolve) => setImmediate(resolve));
    recognition.onerror({ error: 'no-speech' });
    recognition.onend();
    assert.equal(vm.voiceNavListening, true);
    assert.doesNotMatch(vm.voiceNavMessage, /没有听清/);
    recognition.onresult({
        results: [{ isFinal: false, 0: { transcript: '打开知识库' }, length: 1 }]
    });
    assert.equal(vm.voiceNavDraft, '打开知识库');
    recognition.onnomatch({
        results: [{ isFinal: true, 0: { transcript: '知识库' }, length: 1 }]
    });
    assert.equal(vm.voiceNavDraft, '知识库');
    await new Promise((resolve) => setTimeout(resolve, 250));
    assert.equal(recognition.starts, 2);
});

test('safari starts recognition directly so the click can show its own prompt', async () => {
    const calls = [];
    const browser = {
        navigator: {
            userAgent: 'Mozilla/5.0 Version/17.6 Safari/605.1.15',
            mediaDevices: {
                getUserMedia() {
                    calls.push('ask');
                    return Promise.resolve({ getTracks() { return []; } });
                }
            }
        },
        webkitSpeechRecognition: function SpeechRecognition() {
            this.start = function () { calls.push('start'); };
        }
    };
    const api = load({ window: browser });
    api.toggle(viewModel());
    await new Promise((resolve) => setImmediate(resolve));
    assert.equal(calls.length, 1);
    assert.equal(calls[0], 'start');
});

test('unknown speech does not navigate', () => {
    const api = load();
    const result = api.match('打开一个不存在的页面', api.indexMenus(menus));
    assert.equal(result.status, 'none');
    assert.equal(result.target, null);
});
