
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
})

function unopen() {
    alert('版块暂未开放，敬请期待~');
}

function start(docOpened, xncfName) {
    if (docOpened) {
        return true;
    }

    if (confirm('您尚未安装离线文档模块，要立即安装吗？')) {
        let openDocs = true;
        $.ajax({
            url: 'Admin/XncfModule/Index?handler=InstallModule&xncfName=' + xncfName,
            method: 'GET',
            async: false,
            success: function (json) {
                let installSuccess = json.success;
                if (!installSuccess) {
                    alert(json.message);
                } else {
                    openDocs = confirm(json.message + '，刷新此页面可看到顶部文档入口，需要立即查看文档吗？');
                    if (!openDocs) {
                        location.reload();
                    }
                }
            }
        });
        return openDocs;
    }
    return false;
}