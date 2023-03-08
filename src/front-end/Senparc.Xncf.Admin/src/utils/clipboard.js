// 复制文本剪切板功能插件
import Vue from 'vue'
import Clipboard from 'clipboard'

function clipboardSuccess() {
  Vue.prototype.$message({
    message: 'Copy successfully',
    type: 'success',
    duration: 1500
  })
}

function clipboardError() {
  Vue.prototype.$message({
    message: 'Copy failed',
    type: 'error'
  })
}

export default function handleClipboard(text, event) {
  // 初始化
  const clipboard = new Clipboard(event.target, {
    text: () => text
  })
  // 复制成功
  clipboard.on('success', () => {
    clipboardSuccess()
    clipboard.destroy() //释放内存
  })
  // 复制失败
  clipboard.on('error', () => {
    clipboardError()
    clipboard.destroy() //释放内存
  })
  clipboard.onClick(event)
}
