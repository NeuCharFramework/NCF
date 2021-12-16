module.exports = {
  publicPath: './', // 这个值被设置为空字符串 ('') 或是相对路径 ('./')，这样所有的资源都会被链接为相对路径，这样打出来的包可以被部署在任意路径。
  configureWebpack: {
    resolve: {
      symlinks: false
    }
  }
}
