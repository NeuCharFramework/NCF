
$(function(){
    $('#qq-code').hover(function () {
        $('#qq-code-img').toggle();
    });

    $('.index-simple-notice').hover(function(){
        $(this).addClass("noticeHover");
    },function(){
        $(this).removeClass("noticeHover");
    });

    $('.start-btn').addClass('normal');

    // WebView.Avalonia 的 macOS WKWebView 未实现新窗口委托；仅在内嵌环境中
    // 将 target="_blank" 链接回退为当前页导航，普通浏览器仍保留新标签页行为。
    $(document).on('click.ncfEmbeddedWebView', 'a[target="_blank"]', function (event) {
        if (!isNcfMacEmbeddedWebView()) {
            return;
        }

        var url = this.href;
        if (url) {
            event.preventDefault();
            window.location.assign(url);
        }
    });
})

function isNcfMacEmbeddedWebView() {
    var userAgent = navigator.userAgent || '';
    return /Macintosh/i.test(userAgent) &&
        /AppleWebKit/i.test(userAgent) &&
        !/Safari\//i.test(userAgent);
}

function ncfSiteText(key, fallback) {
    if (typeof window.ncfSiteT === 'function') {
        var args = Array.prototype.slice.call(arguments, 2);
        return window.ncfSiteT.apply(window, [key].concat(args));
    }
    return fallback;
}

function showNcfDialog(options) {
    var dialogId = 'ncf-site-dialog';
    var $dialog = $('#' + dialogId);
    if ($dialog.length === 0) {
        $('body').append(
            '<div class="modal fade" id="' + dialogId + '" tabindex="-1" role="dialog" aria-modal="true" aria-labelledby="ncf-site-dialog-title">' +
            '  <div class="modal-dialog modal-dialog-centered" role="document">' +
            '    <div class="modal-content">' +
            '      <div class="modal-header">' +
            '        <h5 class="modal-title" id="ncf-site-dialog-title"></h5>' +
            '        <button type="button" class="close" data-ncf-dialog-cancel aria-label="Close"><span aria-hidden="true">&times;</span></button>' +
            '      </div>' +
            '      <div class="modal-body"><p data-ncf-dialog-message></p></div>' +
            '      <div class="modal-footer">' +
            '        <button type="button" class="btn btn-secondary" data-ncf-dialog-cancel></button>' +
            '        <button type="button" class="btn btn-primary" data-ncf-dialog-confirm></button>' +
            '      </div>' +
            '    </div>' +
            '  </div>' +
            '</div>');
        $dialog = $('#' + dialogId);
    }

    return new Promise(function (resolve) {
        var settled = false;
        var dialogResult = false;
        var complete = function () {
            if (dialogResult && typeof options.onConfirm === 'function') {
                options.onConfirm();
            } else if (!dialogResult && typeof options.onCancel === 'function') {
                options.onCancel();
            }
            resolve(dialogResult);
        };
        var settle = function (result) {
            if (settled) {
                return;
            }
            settled = true;
            dialogResult = result;
            $dialog.modal('hide');
        };

        $dialog.find('.modal-title').text(options.title || '');
        $dialog.find('[data-ncf-dialog-message]').text(options.message || '');
        $dialog.find('[data-ncf-dialog-confirm]').text(options.confirmText || ncfSiteText('Button.Confirm', 'Confirm'));
        $dialog.find('[data-ncf-dialog-cancel]').not('.close')
            .text(options.cancelText || ncfSiteText('Button.Cancel', 'Cancel'))
            .toggle(!!options.showCancel);
        $dialog.find('.close').toggle(!!options.showCancel);

        $dialog.off('.ncfDialog');
        $dialog.on('click.ncfDialog', '[data-ncf-dialog-confirm]', function () { settle(true); });
        $dialog.on('click.ncfDialog', '[data-ncf-dialog-cancel]', function () { settle(false); });
        $dialog.one('hidden.bs.modal.ncfDialog', function () {
            if (!settled) {
                settled = true;
                dialogResult = false;
            }
            complete();
        });
        $dialog.modal({ backdrop: 'static', keyboard: false, show: true });
    });
}

function unopen() {
    showNcfDialog({
        title: ncfSiteText('Message.Warning', 'Notice'),
        message: ncfSiteText('Home.ComingSoon', 'This section is not yet available. Stay tuned.'),
        confirmText: ncfSiteText('Button.Close', 'Close'),
        showCancel: false
    });
    return false;
}

function showDocsRequestError(error) {
    console.error('Failed to install the offline documentation module:', error);
    return showNcfDialog({
        title: ncfSiteText('Message.Error', 'Error'),
        message: ncfSiteText('Home.RequestFailed', 'The request failed. Please try again later.'),
        confirmText: ncfSiteText('Button.Close', 'Close'),
        showCancel: false
    });
}

function installDocsModule(xncfName) {
    try {
        $.ajax({
            url: 'Admin/XncfModule/Index?handler=InstallModule&xncfName=' + encodeURIComponent(xncfName),
            method: 'GET'
        }).then(function (json) {
            if (!json.success) {
                return showNcfDialog({
                    title: ncfSiteText('Message.Error', 'Error'),
                    message: json.message,
                    confirmText: ncfSiteText('Button.Close', 'Close'),
                    showCancel: false
                });
            }

            return showNcfDialog({
                title: ncfSiteText('Message.Success', 'Success'),
                message: ncfSiteText('Home.OpenDocsConfirm', '{0} Refresh this page to see the documentation entry. Open the documentation now?', json.message),
                confirmText: ncfSiteText('Button.Confirm', 'Confirm'),
                cancelText: ncfSiteText('Button.Cancel', 'Cancel'),
                showCancel: true,
                onCancel: function () {
                    location.reload();
                }
            });
        }).catch(showDocsRequestError);
    } catch (error) {
        showDocsRequestError(error);
    }
}

function start(docOpened, xncfName) {
    if (docOpened) {
        return true;
    }

    showNcfDialog({
        title: ncfSiteText('Message.Warning', 'Notice'),
        message: ncfSiteText('Home.InstallDocsConfirm', 'The offline documentation module is not installed. Install it now?'),
        confirmText: ncfSiteText('Button.Confirm', 'Confirm'),
        cancelText: ncfSiteText('Button.Cancel', 'Cancel'),
        showCancel: true,
        onConfirm: function () {
            installDocsModule(xncfName);
        }
    });

    // 兼容历史内联 onclick="return start(...)"：异步流程完成前阻止默认导航。
    return false;
}
