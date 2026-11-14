var app = new Vue({
    el: "#app",
    data() {
        var validateCode = (rule, value, callback) => {
            if (this.dialog.data.menuType === 3) {
                if (!value) {
                    callback(new Error(ncfT('Menu.ButtonRequired')));
                } else {
                    callback();
                }
            } else {
                callback();
            }
        };
        return {
            // 表格数据
            tableData: [],
            // 配置模式（整棵树“菜单/页面”的拖拽/数字排序，保存后真实更新 Sort 数值）
            configMode: false,
            configMenus: [],
            dragId: null,
            dragOverId: null,
            configSaving: false,
            dialog: {
                title: ncfT('Menu.AddTitle'),
                visible: false,
                data: {
                    id: '', menuName: '', parentId: [], url: '', icon: '', sort: '', visible: true,
                    resourceCode: '', isLocked: false, menuType: ''
                },
                rules: {
                    menuName: [
                        { required: true, message: ncfT('Menu.NameRequired'), trigger: "blur" }
                    ],
                    menuType: [{ required: true, message: ncfT('Menu.TypeRequired'), trigger: "blur" }],
                    resourceCode: [{ validator: validateCode, trigger: "blur" }]
                },
                updateLoading: false,
                disabled: false,
                checkStrictly: true // 是否严格的遵守父子节点不互相关联	
            },
            dialogIcon: {
                visible: false,
                elementIcons: [
                    'fa-adjust',
                    'fa-anchor',
                    'fa-archive',
                    'fa-area-chart',
                    'fa-arrows',
                    'fa-arrows-h',
                    'fa-arrows-v',
                    'fa-asterisk',
                    'fa-at',
                    'fa-automobile',
                    'fa-ban',
                    'fa-bank',
                    'fa-bar-chart',
                    'fa-bar-chart-o',
                    'fa-barcode',
                    'fa-bars',
                    'fa-beer',
                    'fa-bell',
                    'fa-bell-o',
                    'fa-bell-slash',
                    'fa-bell-slash-o',
                    'fa-bicycle',
                    'fa-binoculars',
                    'fa-birthday-cake',
                    'fa-bolt',
                    'fa-bomb',
                    'fa-book',
                    'fa-bookmark',
                    'fa-bookmark-o',
                    'fa-briefcase',
                    'fa-bug',
                    'fa-building',
                    'fa-building-o',
                    'fa-bullhorn',
                    'fa-bullseye',
                    'fa-bus',
                    'fa-cab',
                    'fa-calculator',
                    'fa-calendar',
                    'fa-calendar-o',
                    'fa-camera',
                    'fa-camera-retro',
                    'fa-car',
                    'fa-caret-square-o-down',
                    'fa-caret-square-o-left',
                    'fa-caret-square-o-right',
                    'fa-caret-square-o-up',
                    'fa-cc',
                    'fa-certificate',
                    'fa-check',
                    'fa-check-circle',
                    'fa-check-circle-o',
                    'fa-check-square',
                    'fa-check-square-o',
                    'fa-child',
                    'fa-circle',
                    'fa-circle-o',
                    'fa-circle-o-notch',
                    'fa-circle-thin',
                    'fa-clock-o',
                    'fa-close',
                    'fa-cloud',
                    'fa-cloud-download',
                    'fa-cloud-upload',
                    'fa-code',
                    'fa-code-fork',
                    'fa-coffee',
                    'fa-cog',
                    'fa-cogs',
                    'fa-comment',
                    'fa-comment-o',
                    'fa-comments',
                    'fa-comments-o',
                    'fa-compass',
                    'fa-copyright',
                    'fa-credit-card',
                    'fa-crop',
                    'fa-crosshairs',
                    'fa-cube',
                    'fa-cubes',
                    'fa-cutlery',
                    'fa-dashboard',
                    'fa-database',
                    'fa-desktop',
                    'fa-dot-circle-o',
                    'fa-download',
                    'fa-edit',
                    'fa-ellipsis-h',
                    'fa-ellipsis-v',
                    'fa-envelope',
                    'fa-envelope-o',
                    'fa-envelope-square',
                    'fa-eraser',
                    'fa-exchange',
                    'fa-exclamation',
                    'fa-exclamation-circle',
                    'fa-exclamation-triangle',
                    'fa-external-link',
                    'fa-external-link-square',
                    'fa-eye',
                    'fa-eye-slash',
                    'fa-eyedropper',
                    'fa-fax',
                    'fa-female',
                    'fa-fighter-jet',
                    'fa-file-archive-o',
                    'fa-file-audio-o',
                    'fa-file-code-o',
                    'fa-file-excel-o',
                    'fa-file-image-o',
                    'fa-file-movie-o',
                    'fa-file-pdf-o',
                    'fa-file-photo-o',
                    'fa-file-picture-o',
                    'fa-file-powerpoint-o',
                    'fa-file-sound-o',
                    'fa-file-video-o',
                    'fa-file-word-o',
                    'fa-file-zip-o',
                    'fa-film',
                    'fa-filter',
                    'fa-fire',
                    'fa-fire-extinguisher',
                    'fa-flag',
                    'fa-flag-checkered',
                    'fa-flag-o',
                    'fa-flash',
                    'fa-flask',
                    'fa-folder',
                    'fa-folder-o',
                    'fa-folder-open',
                    'fa-folder-open-o',
                    'fa-frown-o',
                    'fa-futbol-o',
                    'fa-gamepad',
                    'fa-gavel',
                    'fa-gear',
                    'fa-gears',
                    'fa-gift',
                    'fa-glass',
                    'fa-globe',
                    'fa-graduation-cap',
                    'fa-group',
                    'fa-hdd-o',
                    'fa-headphones',
                    'fa-heart',
                    'fa-heart-o',
                    'fa-history',
                    'fa-home',
                    'fa-image',
                    'fa-inbox',
                    'fa-info',
                    'fa-info-circle',
                    'fa-institution',
                    'fa-key',
                    'fa-keyboard-o',
                    'fa-language',
                    'fa-laptop',
                    'fa-leaf',
                    'fa-legal',
                    'fa-lemon-o',
                    'fa-level-down',
                    'fa-level-up',
                    'fa-life-bouy',
                    'fa-life-buoy',
                    'fa-life-ring',
                    'fa-life-saver',
                    'fa-lightbulb-o',
                    'fa-line-chart',
                    'fa-location-arrow',
                    'fa-lock',
                    'fa-magic',
                    'fa-magnet',
                    'fa-mail-forward',
                    'fa-mail-reply',
                    'fa-mail-reply-all',
                    'fa-male',
                    'fa-map-marker',
                    'fa-meh-o',
                    'fa-microphone',
                    'fa-microphone-slash',
                    'fa-minus',
                    'fa-minus-circle',
                    'fa-minus-square',
                    'fa-minus-square-o',
                    'fa-mobile',
                    'fa-mobile-phone',
                    'fa-money',
                    'fa-moon-o',
                    'fa-mortar-board',
                    'fa-music',
                    'fa-navicon',
                    'fa-newspaper-o',
                    'fa-paint-brush',
                    'fa-paper-plane',
                    'fa-paper-plane-o',
                    'fa-paw',
                    'fa-pencil',
                    'fa-pencil-square',
                    'fa-pencil-square-o',
                    'fa-phone',
                    'fa-phone-square',
                    'fa-photo',
                    'fa-picture-o',
                    'fa-pie-chart',
                    'fa-plane',
                    'fa-plug',
                    'fa-plus',
                    'fa-plus-circle',
                    'fa-plus-square',
                    'fa-plus-square-o',
                    'fa-power-off',
                    'fa-print',
                    'fa-puzzle-piece',
                    'fa-qrcode',
                    'fa-question',
                    'fa-question-circle',
                    'fa-quote-left',
                    'fa-quote-right',
                    'fa-random',
                    'fa-recycle',
                    'fa-refresh',
                    'fa-remove',
                    'fa-reorder',
                    'fa-reply',
                    'fa-reply-all',
                    'fa-retweet',
                    'fa-road',
                    'fa-rocket',
                    'fa-rss',
                    'fa-rss-square',
                    'fa-search',
                    'fa-search-minus',
                    'fa-search-plus',
                    'fa-send',
                    'fa-send-o',
                    'fa-share',
                    'fa-share-alt',
                    'fa-share-alt-square',
                    'fa-share-square',
                    'fa-share-square-o',
                    'fa-shield',
                    'fa-shopping-cart',
                    'fa-sign-in',
                    'fa-sign-out',
                    'fa-signal',
                    'fa-sitemap',
                    'fa-sliders',
                    'fa-smile-o',
                    'fa-soccer-ball-o',
                    'fa-sort',
                    'fa-sort-alpha-asc',
                    'fa-sort-alpha-desc',
                    'fa-sort-amount-asc',
                    'fa-sort-amount-desc',
                    'fa-sort-asc',
                    'fa-sort-desc',
                    'fa-sort-down',
                    'fa-sort-numeric-asc',
                    'fa-sort-numeric-desc',
                    'fa-sort-up',
                    'fa-space-shuttle',
                    'fa-spinner',
                    'fa-spoon',
                    'fa-square',
                    'fa-square-o',
                    'fa-star',
                    'fa-star-half',
                    'fa-star-half-empty',
                    'fa-star-half-full',
                    'fa-star-half-o',
                    'fa-star-o',
                    'fa-suitcase',
                    'fa-sun-o',
                    'fa-support',
                    'fa-tablet',
                    'fa-tachometer',
                    'fa-tag',
                    'fa-tags',
                    'fa-tasks',
                    'fa-taxi',
                    'fa-terminal',
                    'fa-thumb-tack',
                    'fa-thumbs-down',
                    'fa-thumbs-o-down',
                    'fa-thumbs-o-up',
                    'fa-thumbs-up',
                    'fa-ticket',
                    'fa-times',
                    'fa-times-circle',
                    'fa-times-circle-o',
                    'fa-tint',
                    'fa-toggle-down',
                    'fa-toggle-left',
                    'fa-toggle-off',
                    'fa-toggle-on',
                    'fa-toggle-right',
                    'fa-toggle-up',
                    'fa-trash',
                    'fa-trash-o',
                    'fa-tree',
                    'fa-trophy',
                    'fa-truck',
                    'fa-tty',
                    'fa-umbrella',
                    'fa-university',
                    'fa-unlock',
                    'fa-unlock-alt',
                    'fa-unsorted',
                    'fa-upload',
                    'fa-user',
                    'fa-users',
                    'fa-video-camera',
                    'fa-volume-down',
                    'fa-volume-off',
                    'fa-volume-up',
                    'fa-warning',
                    'fa-wheelchair',
                    'fa-wifi',
                    'fa-wrench',
                    'fa-file',
                    'fa-file-archive-o',
                    'fa-file-audio-o',
                    'fa-file-code-o',
                    'fa-file-excel-o',
                    'fa-file-image-o',
                    'fa-file-movie-o',
                    'fa-file-o',
                    'fa-file-pdf-o',
                    'fa-file-photo-o',
                    'fa-file-picture-o',
                    'fa-file-powerpoint-o',
                    'fa-file-sound-o',
                    'fa-file-text',
                    'fa-file-text-o',
                    'fa-file-video-o',
                    'fa-file-word-o',
                    'fa-file-zip-o',
                    'fa-circle-o-notch',
                    'fa-cog',
                    'fa-gear',
                    'fa-refresh',
                    'fa-spinner',
                    'fa-check-square',
                    'fa-check-square-o',
                    'fa-circle',
                    'fa-circle-o',
                    'fa-dot-circle-o',
                    'fa-minus-square',
                    'fa-minus-square-o',
                    'fa-plus-square',
                    'fa-plus-square-o',
                    'fa-square',
                    'fa-square-o',
                    'fa-cc-amex',
                    'fa-cc-discover',
                    'fa-cc-mastercard',
                    'fa-cc-paypal',
                    'fa-cc-stripe',
                    'fa-cc-visa',
                    'fa-credit-card',
                    'fa-google-wallet',
                    'fa-paypal',
                    'fa-area-chart',
                    'fa-bar-chart',
                    'fa-bar-chart-o',
                    'fa-line-chart',
                    'fa-pie-chart',
                    'fa-bitcoin',
                    'fa-btc',
                    'fa-cny',
                    'fa-dollar',
                    'fa-eur',
                    'fa-euro',
                    'fa-gbp',
                    'fa-ils',
                    'fa-inr',
                    'fa-jpy',
                    'fa-krw',
                    'fa-money',
                    'fa-rmb',
                    'fa-rouble',
                    'fa-rub',
                    'fa-ruble',
                    'fa-rupee',
                    'fa-shekel',
                    'fa-sheqel',
                    'fa-try',
                    'fa-turkish-lira',
                    'fa-usd',
                    'fa-won',
                    'fa-yen',
                    'fa-align-center',
                    'fa-align-justify',
                    'fa-align-left',
                    'fa-align-right',
                    'fa-bold',
                    'fa-chain',
                    'fa-chain-broken',
                    'fa-clipboard',
                    'fa-columns',
                    'fa-copy',
                    'fa-cut',
                    'fa-dedent',
                    'fa-eraser',
                    'fa-file',
                    'fa-file-o',
                    'fa-file-text',
                    'fa-file-text-o',
                    'fa-files-o',
                    'fa-floppy-o',
                    'fa-font',
                    'fa-header',
                    'fa-indent',
                    'fa-italic',
                    'fa-link',
                    'fa-list',
                    'fa-list-alt',
                    'fa-list-ol',
                    'fa-list-ul',
                    'fa-outdent',
                    'fa-paperclip',
                    'fa-paragraph',
                    'fa-paste',
                    'fa-repeat',
                    'fa-rotate-left',
                    'fa-rotate-right',
                    'fa-save',
                    'fa-scissors',
                    'fa-strikethrough',
                    'fa-subscript',
                    'fa-superscript',
                    'fa-table',
                    'fa-text-height',
                    'fa-text-width',
                    'fa-th',
                    'fa-th-large',
                    'fa-th-list',
                    'fa-underline',
                    'fa-undo',
                    'fa-unlink',
                    'fa-angle-double-down',
                    'fa-angle-double-left',
                    'fa-angle-double-right',
                    'fa-angle-double-up',
                    'fa-angle-down',
                    'fa-angle-left',
                    'fa-angle-right',
                    'fa-angle-up',
                    'fa-arrow-circle-down',
                    'fa-arrow-circle-left',
                    'fa-arrow-circle-o-down',
                    'fa-arrow-circle-o-left',
                    'fa-arrow-circle-o-right',
                    'fa-arrow-circle-o-up',
                    'fa-arrow-circle-right',
                    'fa-arrow-circle-up',
                    'fa-arrow-down',
                    'fa-arrow-left',
                    'fa-arrow-right',
                    'fa-arrow-up',
                    'fa-arrows',
                    'fa-arrows-alt',
                    'fa-arrows-h',
                    'fa-arrows-v',
                    'fa-caret-down',
                    'fa-caret-left',
                    'fa-caret-right',
                    'fa-caret-square-o-down',
                    'fa-caret-square-o-left',
                    'fa-caret-square-o-right',
                    'fa-caret-square-o-up',
                    'fa-caret-up',
                    'fa-chevron-circle-down',
                    'fa-chevron-circle-left',
                    'fa-chevron-circle-right',
                    'fa-chevron-circle-up',
                    'fa-chevron-down',
                    'fa-chevron-left',
                    'fa-chevron-right',
                    'fa-chevron-up',
                    'fa-hand-o-down',
                    'fa-hand-o-left',
                    'fa-hand-o-right',
                    'fa-hand-o-up',
                    'fa-long-arrow-down',
                    'fa-long-arrow-left',
                    'fa-long-arrow-right',
                    'fa-long-arrow-up',
                    'fa-toggle-down',
                    'fa-toggle-left',
                    'fa-toggle-right',
                    'fa-toggle-up',
                    'fa-arrows-alt',
                    'fa-backward',
                    'fa-compress',
                    'fa-eject',
                    'fa-expand',
                    'fa-fast-backward',
                    'fa-fast-forward',
                    'fa-forward',
                    'fa-pause',
                    'fa-play',
                    'fa-play-circle',
                    'fa-play-circle-o',
                    'fa-step-backward',
                    'fa-step-forward',
                    'fa-stop',
                    'fa-youtube-play',
                    'fa-adn',
                    'fa-android',
                    'fa-angellist',
                    'fa-apple',
                    'fa-behance',
                    'fa-behance-square',
                    'fa-bitbucket',
                    'fa-bitbucket-square',
                    'fa-bitcoin',
                    'fa-btc',
                    'fa-cc-amex',
                    'fa-cc-discover',
                    'fa-cc-mastercard',
                    'fa-cc-paypal',
                    'fa-cc-stripe',
                    'fa-cc-visa',
                    'fa-codepen',
                    'fa-css3',
                    'fa-delicious',
                    'fa-deviantart',
                    'fa-digg',
                    'fa-dribbble',
                    'fa-dropbox',
                    'fa-drupal',
                    'fa-empire',
                    'fa-facebook',
                    'fa-facebook-square',
                    'fa-flickr',
                    'fa-foursquare',
                    'fa-ge',
                    'fa-git',
                    'fa-git-square',
                    'fa-github',
                    'fa-github-alt',
                    'fa-github-square',
                    'fa-gittip',
                    'fa-google',
                    'fa-google-plus',
                    'fa-google-plus-square',
                    'fa-google-wallet',
                    'fa-hacker-news',
                    'fa-html5',
                    'fa-instagram',
                    'fa-ioxhost',
                    'fa-joomla',
                    'fa-jsfiddle',
                    'fa-lastfm',
                    'fa-lastfm-square',
                    'fa-linkedin',
                    'fa-linkedin-square',
                    'fa-linux',
                    'fa-maxcdn',
                    'fa-meanpath',
                    'fa-openid',
                    'fa-pagelines',
                    'fa-paypal',
                    'fa-pied-piper',
                    'fa-pied-piper-alt',
                    'fa-pinterest',
                    'fa-pinterest-square',
                    'fa-qq',
                    'fa-ra',
                    'fa-rebel',
                    'fa-reddit',
                    'fa-reddit-square',
                    'fa-renren',
                    'fa-share-alt',
                    'fa-share-alt-square',
                    'fa-skype',
                    'fa-slack',
                    'fa-slideshare',
                    'fa-soundcloud',
                    'fa-spotify',
                    'fa-stack-exchange',
                    'fa-stack-overflow',
                    'fa-steam',
                    'fa-steam-square',
                    'fa-stumbleupon',
                    'fa-stumbleupon-circle',
                    'fa-tencent-weibo',
                    'fa-trello',
                    'fa-tumblr',
                    'fa-tumblr-square',
                    'fa-twitch',
                    'fa-twitter',
                    'fa-twitter-square',
                    'fa-vimeo-square',
                    'fa-vine',
                    'fa-vk',
                    'fa-wechat',
                    'fa-weibo',
                    'fa-weixin',
                    'fa-windows',
                    'fa-wordpress',
                    'fa-xing',
                    'fa-xing-square',
                    'fa-yahoo',
                    'fa-yelp',
                    'fa-youtube',
                    'fa-youtube-play',
                    'fa-youtube-square',
                    'fa-ambulance',
                    'fa-h-square',
                    'fa-hospital-o',
                    'fa-medkit',
                    'fa-plus-square',
                    'fa-stethoscope',
                    'fa-user-md',
                    'fa-wheelchair'
                ]
            }
        };
    },
    created: function () {
        this.getList();
    },
    mounted() {
        window.addEventListener('scroll', this.handleScroll);
        window.addEventListener('resize', this.handleResize);
        this.$nextTick(() => {
            this.initStickyParents();
        });
    },
    beforeDestroy() {
        window.removeEventListener('scroll', this.handleScroll);
        window.removeEventListener('resize', this.handleResize);
    },
    watch: {
        'dialog.visible': function (val, old) {
            // 关闭dialog，清空
            if (!val) {
                this.dialog.data = {
                    id: '', menuName: '', parentId: [], url: '', icon: '', sort: '', visible: false,
                    resourceCode: '', isLocked: false, menuType: ''
                };
                this.dialog.updateLoading = false;
                this.dialog.disabled = false;
            }
        }
    },
    methods: {
        // 选取图标
        pickIcon(item) {
            this.dialogIcon.visible = false;
            this.dialog.data.icon = item;
        },
        // 切换配置模式（整棵树“菜单/页面”拖拽/数字排序）
        toggleConfigMode() {
            this.configMode = !this.configMode;
            if (this.configMode) {
                this.buildConfigMenus();
            }
        },
        // 构建配置模式列表：递归整棵菜单树，仅保留“菜单(1)/页面(2)”，按钮(3)不参与排序；
        // 同级按 Sort 降序排列（与左侧菜单一致），最终保存为深度优先顺序的扁平数组
        buildConfigMenus() {
            var rows = [];
            var walk = function (nodes, level) {
                if (!nodes || !nodes.length) {
                    return;
                }
                nodes.slice().sort(function (a, b) {
                    var sa = Number(a.sort) || 0;
                    var sb = Number(b.sort) || 0;
                    if (sb !== sa) {
                        return sb - sa;
                    }
                    return (a.menuName || '') < (b.menuName || '') ? -1 : ((a.menuName || '') > (b.menuName || '') ? 1 : 0);
                }).forEach(function (node) {
                    var type = Number(node.menuType);
                    if (type === 1 || type === 2) {
                        rows.push({
                            id: node.id,
                            parentId: node.parentId || null,
                            menuName: node.menuName,
                            icon: node.icon || '',
                            url: node.url || '',
                            sort: Number(node.sort) || 0,
                            menuType: type,
                            level: level
                        });
                    }
                    walk(node.children, level + 1);
                });
            };
            walk(this.tableData || [], 0);
            this.configMenus = rows;
        },
        // 按 id 查找配置行
        findConfigRow(id) {
            for (var i = 0; i < this.configMenus.length; i++) {
                if (this.configMenus[i].id === id) {
                    return this.configMenus[i];
                }
            }
            return null;
        },
        // 计算以 rows[startIndex] 为根的子树在扁平数组中的结束下标（扁平数组为深度优先顺序，子树连续）
        subtreeEndIndex(rows, startIndex) {
            if (startIndex >= rows.length) {
                return startIndex;
            }
            var rootId = rows[startIndex].id;
            var parentMap = {};
            for (var i = startIndex + 1; i < rows.length; i++) {
                parentMap[rows[i].id] = rows[i].parentId || null;
            }
            var isDescendant = function (pid) {
                var checked = {};
                while (pid) {
                    if (pid === rootId) {
                        return true;
                    }
                    if (checked[pid]) {
                        return false;
                    }
                    checked[pid] = true;
                    pid = parentMap[pid] || null;
                }
                return false;
            };
            var end = startIndex;
            for (var j = startIndex + 1; j < rows.length; j++) {
                if (isDescendant(rows[j].parentId || null)) {
                    end = j;
                } else {
                    break;
                }
            }
            return end;
        },
        // 将 dragId 所在项（连同其全部子项）移动到 targetId 所在项“之前(before)/之后(after)”，仅限同级
        moveTo(dragId, targetId, position) {
            var rows = this.configMenus.slice();
            var dragIdx = -1;
            var targetIdx = -1;
            for (var i = 0; i < rows.length; i++) {
                if (rows[i].id === dragId) {
                    dragIdx = i;
                }
                if (rows[i].id === targetId) {
                    targetIdx = i;
                }
            }
            if (dragIdx < 0 || targetIdx < 0 || dragIdx === targetIdx) {
                return;
            }
            var dragRow = rows[dragIdx];
            var targetRow = rows[targetIdx];
            if ((dragRow.parentId || null) !== (targetRow.parentId || null)) {
                return; // 只允许同级之间拖动
            }
            var blockEnd = this.subtreeEndIndex(rows, dragIdx);
            var block = rows.slice(dragIdx, blockEnd + 1);
            var remaining = rows.slice(0, dragIdx).concat(rows.slice(blockEnd + 1));
            var tIdx = -1;
            for (var k = 0; k < remaining.length; k++) {
                if (remaining[k].id === targetId) {
                    tIdx = k;
                    break;
                }
            }
            if (tIdx < 0) {
                return;
            }
            var tEnd = this.subtreeEndIndex(remaining, tIdx);
            var insertAt = (position === 'after') ? tEnd + 1 : tIdx;
            this.configMenus = remaining.slice(0, insertAt).concat(block).concat(remaining.slice(insertAt));
            this.renumberGroup(dragRow.parentId || null);
        },
        // 同级重排后重新分配 Sort 数值：自上而下递减、间隔 10（与左侧菜单降序渲染一致）
        renumberGroup(parentId) {
            var key = parentId || null;
            var group = [];
            for (var i = 0; i < this.configMenus.length; i++) {
                if ((this.configMenus[i].parentId || null) === key) {
                    group.push(this.configMenus[i]);
                }
            }
            var total = group.length;
            for (var j = 0; j < group.length; j++) {
                group[j].sort = (total - j) * 10;
            }
        },
        // 判断拖放目标是否合法（仅同级可接受拖放）
        canDropTo(dragId, targetId) {
            if (!dragId || dragId === targetId) {
                return false;
            }
            var dragRow = this.findConfigRow(dragId);
            var targetRow = this.findConfigRow(targetId);
            if (!dragRow || !targetRow) {
                return false;
            }
            return (dragRow.parentId || null) === (targetRow.parentId || null);
        },
        // 拖拽开始（数字输入框与按钮上不触发整行拖拽）
        onConfigDragStart(id, ev) {
            if (ev && ev.target) {
                var t = ev.target;
                var node = (typeof t.closest === 'function') ? t.closest('input, button, .el-button, a') : null;
                if (node || t.tagName === 'INPUT' || t.tagName === 'BUTTON' || t.tagName === 'A') {
                    ev.preventDefault();
                    return;
                }
            }
            this.dragId = id;
            this.dragOverId = null;
        },
        // 拖拽悬停（仅高亮可投放的同级目标）
        onConfigDragOver(id) {
            if (this.canDropTo(this.dragId, id)) {
                this.dragOverId = id;
            } else if (this.dragOverId === id) {
                this.dragOverId = null;
            }
        },
        // 拖拽结束（落点）
        onConfigDrop(id) {
            if (!this.canDropTo(this.dragId, id)) {
                this.onConfigDragEnd();
                return;
            }
            var fromIndex = -1;
            var toIndex = -1;
            for (var i = 0; i < this.configMenus.length; i++) {
                if (this.configMenus[i].id === this.dragId) {
                    fromIndex = i;
                }
                if (this.configMenus[i].id === id) {
                    toIndex = i;
                }
            }
            this.moveTo(this.dragId, id, fromIndex < toIndex ? 'after' : 'before');
            this.onConfigDragEnd();
        },
        onConfigDragEnd() {
            this.dragId = null;
            this.dragOverId = null;
        },
        // 获取某行的同级行列表
        siblingRows(row) {
            var key = (row.parentId || null);
            var list = [];
            for (var i = 0; i < this.configMenus.length; i++) {
                if ((this.configMenus[i].parentId || null) === key) {
                    list.push(this.configMenus[i]);
                }
            }
            return list;
        },
        canMoveUp(row) {
            return this.siblingRows(row).indexOf(row) > 0;
        },
        canMoveDown(row) {
            var list = this.siblingRows(row);
            var idx = list.indexOf(row);
            return idx >= 0 && idx < list.length - 1;
        },
        // 上移/下移按钮（连同子项一起移动，辅助键盘/鼠标用户）
        moveConfigItem(row, delta) {
            if (!row) {
                return;
            }
            var siblings = this.siblingRows(row);
            var from = siblings.indexOf(row);
            var to = from + delta;
            if (to < 0 || to >= siblings.length) {
                return;
            }
            this.moveTo(row.id, siblings[to].id, delta > 0 ? 'after' : 'before');
        },
        // 直接在“排序”输入框中输入数字并回车/失焦：按 Sort 降序把该项移动到相应位置（可先试数字再微调）
        applyNumber(id) {
            var row = this.findConfigRow(id);
            if (!row) {
                return;
            }
            var val = parseInt(row.sort, 10);
            if (isNaN(val)) {
                this.renumberGroup(row.parentId || null);
                return;
            }
            var siblings = [];
            for (var i = 0; i < this.configMenus.length; i++) {
                var r = this.configMenus[i];
                if (r.id !== id && (r.parentId || null) === (row.parentId || null)) {
                    siblings.push(r);
                }
            }
            var insertPos = 0;
            for (var j = 0; j < siblings.length; j++) {
                var s = parseInt(siblings[j].sort, 10) || 0;
                if (s >= val) {
                    insertPos++;
                }
            }
            var target = null;
            var position = 'before';
            if (insertPos >= siblings.length) {
                target = siblings.length ? siblings[siblings.length - 1] : null;
                position = 'after';
            } else {
                target = siblings[insertPos];
            }
            if (!target) {
                this.renumberGroup(row.parentId || null);
                return;
            }
            this.moveTo(id, target.id, position);
        },
        // 保存配置模式排序：按当前（深度优先）顺序提交全部“菜单/页面”Id，
        // 服务端按父级分组后真实更新各级 Sort 数值
        saveConfigOrder() {
            if (!this.configMenus.length) {
                this.$message.warning(ncfT('Menu.ConfigEmpty'));
                return;
            }
            var self = this;
            self.configSaving = true;
            service.post('/Admin/Menu/Index?handler=Reorder', {
                ids: self.configMenus.map(function (m) { return m.id; })
            }).then(function (res) {
                if (res.data.success) {
                    self.$notify({
                        title: ncfT('AdminUserInfo.Success'),
                        message: ncfT('Menu.ConfigSaveSuccess'),
                        type: 'success',
                        duration: 2000
                    });
                    self.getList();
                } else {
                    self.$message.error(res.data.msg || ncfT('Admin.Common.Error'));
                }
            }).finally(function () {
                self.configSaving = false;
            });
        },
        // 更新授权
        async  auUpdateData() {
            this.au.updateLoading = true;
            const checkNodes = this.$refs.tree.getCheckedNodes(false, true);
            let array = [];
            checkNodes.map((ele) => {
                array.push({
                    PermissionId: ele.id,
                    roleId: this.au.temp.id,
                    isMenu: ele.isMenu,
                    roleCode: ele.resourceCode
                });
            });
            const respnseData = await service.post('/Admin/Role/Permission', array);
            if (respnseData.data.success) {
                this.getList();
                this.$notify({
                    title: ncfT('AdminUserInfo.Success'),
                    message: ncfT('Menu.AuthorizationSuccess'),
                    type: "success",
                    duration: 2000
                });
                this.au.visible = false;
                this.au.updateLoading = false;
            }
        },
        // 获取所有菜单
        async  getList() {
            const a = await service.get('/Admin/Menu/Edit?handler=Menu');
            const b = a.data.data;
            let allMenu = [];
            this.ddd(b, null, allMenu);
            this.tableData = allMenu;
            
            // 数据加载完成后初始化固定效果
            this.$nextTick(() => {
                this.initStickyParents();
            });
        },
        // 数据处理
        ddd(source, parentId, dest) {
            var array = source.filter(_ => _.parentId === parentId);
            for (var i in array) {
                var ele = array[i];
                ele.children = [];
                dest.unshift(ele);
                this.ddd(source, ele.id, ele.children);
            }
        },
        // 编辑 // 新增菜单 // 增加下一级
        handleEdit(index, row, flag) {
            this.dialog.visible = true;
            if (flag === 'add') {
                // 新增
                this.dialog.title = ncfT('Menu.AddTitle');
                return;
            }
            // 编辑
            let { id, menuName, parentId, url, icon, sort, visible,
                resourceCode, isLocked, menuType } = row;
            this.dialog.data = {
                id, menuName, parentId: [parentId], url, icon, sort, visible,
                resourceCode, isLocked, menuType
            };
            // dialog中父级菜单 做递归显示
            let x = [];
            this.recursionFunc(row, this.tableData, x);
            this.dialog.data.parentId = x;
            //////////////////////////////

            if (flag === 'edit') {
                this.dialog.title = ncfT('Menu.EditTitle');
                if (row.isLocked) {
                    this.dialog.disabled = true;
                }
            } else if (flag === 'addNext') {
                this.dialog.data.id = '';
                this.dialog.title = ncfT('Menu.AddChildTitle');
                this.dialog.data.menuName = '';
                this.dialog.data.parentId.push(row.id);
            }
        },
        // 设置父级菜单默认显示 递归
        recursionFunc(row, source, dest) {
            if (row.parentId === null) {
                return;
            }
            for (let i in source) {
                let ele = source[i];
                if (row.parentId === ele.id) {
                    this.recursionFunc(ele, this.tableData, dest);
                    dest.push(ele.id);
                } else {
                    this.recursionFunc(row, ele.children, dest);
                }
            }
        },
        // 更新新增、编辑
        updateData() {
            this.$refs['dataForm'].validate(valid => {
                // 表单校验
                if (valid) {
                    this.dialog.updateLoading = true;
                    let data = {
                        Id: this.dialog.data.id,
                        MenuName: this.dialog.data.menuName,
                        ParentId: this.dialog.data.parentId[this.dialog.data.parentId.length - 1],
                        Url: this.dialog.data.url,
                        Icon: this.dialog.data.icon.includes("fa ") ? this.dialog.data.icon : "fa " + this.dialog.data.icon,
                        Sort: this.dialog.data.sort * 1,
                        Visible: this.dialog.data.visible,
                        ResourceCode: this.dialog.data.resourceCode,
                        IsLocked: this.dialog.data.isLocked,
                        MenuType: this.dialog.data.menuType
                    };
                    service.post("/Admin/Menu/Edit", data).then(res => {
                        if (res.data.success) {
                            this.getList();
                            this.$notify({
                                title: ncfT('AdminUserInfo.Success'),
                                message: ncfT('AdminUserInfo.Success'),
                                type: "success",
                                duration: 2000
                            });
                            this.dialog.visible = false;
                        }
                    });
                }
            });
        },
        // 删除
        handleDelete(index, row) {
            let ids = [row.id];
            service.post("/Admin/Menu/edit?handler=Delete", ids).then(res => {
                if (res.data.success) {
                    this.getList();
                    this.$notify({
                        title: ncfT('AdminUserInfo.Success'),
                        message: ncfT('AdminChat.DeleteSessionSuccess'),
                        type: "success",
                        duration: 2000
                    });
                }
            });
        },
        // 初始化父节点固定效果
        initStickyParents() {
            const rows = document.querySelectorAll('.el-table__row');
            const table = document.querySelector('.el-table');
            
            rows.forEach(row => {
                const expandIcon = row.querySelector('.el-table__expand-icon');
                if (expandIcon && !expandIcon.classList.contains('el-table__expand-icon--leaf')) {
                    row.classList.add('sticky-parent');
                    
                    // 创建克隆行并保持列宽
                    const clone = row.cloneNode(true);
                    clone.classList.add('sticky-clone');
                    clone.style.display = 'none';
                    
                    // 复制每列的宽度
                    const originalCells = row.querySelectorAll('td');
                    const cloneCells = clone.querySelectorAll('td');
                    originalCells.forEach((cell, index) => {
                        const width = window.getComputedStyle(cell).width;
                        cloneCells[index].style.width = width;
                        cloneCells[index].style.minWidth = width;
                        cloneCells[index].style.maxWidth = width;
                    });
                    
                    // 设置克隆行的总宽度
                    clone.style.width = window.getComputedStyle(row).width;
                    
                    row.parentNode.insertBefore(clone, row.nextSibling);
                }
            });
        },
        // 处理滚动事件
        handleScroll() {
            const stickyRows = document.querySelectorAll('.sticky-parent');
            const headerHeight = document.querySelector('.el-table__header-wrapper').offsetHeight;
            const table = document.querySelector('.el-table');
            const tableRect = table.getBoundingClientRect();

            stickyRows.forEach(row => {
                const clone = row.nextElementSibling;
                if (!clone || !clone.classList.contains('sticky-clone')) return;

                const rect = row.getBoundingClientRect();
                
                if (rect.top <= headerHeight) {
                    clone.style.display = 'table-row';
                    clone.style.position = 'fixed';
                    clone.style.top = `${headerHeight}px`;
                    clone.style.left = `${tableRect.left}px`;
                    
                    // 确保克隆行的列宽与原行保持一致
                    const originalCells = row.querySelectorAll('td');
                    const cloneCells = clone.querySelectorAll('td');
                    originalCells.forEach((cell, index) => {
                        const width = window.getComputedStyle(cell).width;
                        cloneCells[index].style.width = width;
                    });
                } else {
                    clone.style.display = 'none';
                }
            });
        },
        // 在窗口大小改变时重新计算列宽
        handleResize() {
            this.initStickyParents();
        }
    }

});
