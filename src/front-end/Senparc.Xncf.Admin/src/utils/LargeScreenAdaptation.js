// Large screen adaptation : 屏幕适配-主用于大屏项目-minxin注入使用

/**
 * Large screen adaptation
 * @param {baseWidth} Width
 * @param {baseHeight} Height
 * @param {HTMLElement} appRef
 */

// 默认缩放比例
const scale = {
    width: "1",
    height: "1",
}

// 设计稿尺寸(px)
const baseWidth = 1920;
const baseHeight = 1080;

// * 需保持的比例（默认1.77778）
//  parseFloat() 函数解析字符串并返回浮点数-表达范围/精度提高
const baseProportion = parseFloat((baseWidth / baseHeight).toFixed(5))

export default {
    data() {
        return {
            // * 定时函数
            drawTiming: null
        }
    },
    mounted() {
        this.calcRate()
        window.addEventListener('resize', this.resize)
    },
    beforeDestroy() {
        window.removeEventListener('resize', this.resize)
    },
    methods: {
        calcRate() {
            const appRef = this.$refs["appRef"]
            if (!appRef) return
            // 当前宽高比
            const currentRate = parseFloat((window.innerWidth / window.innerHeight).toFixed(5))
            if (appRef) {
                if (currentRate > baseProportion) {
                    // 表示更宽
                    scale.width = ((window.innerHeight * baseProportion) / baseWidth).toFixed(5)
                    scale.height = (window.innerHeight / baseHeight).toFixed(5)
                    appRef.style.transform = `scale(${scale.width}, ${scale.height}) translate(-50%, -50%)`
                } else {
                    // 表示更高
                    scale.height = ((window.innerWidth / baseProportion) / baseHeight).toFixed(5)
                    scale.width = (window.innerWidth / baseWidth).toFixed(5)
                    appRef.style.transform = `scale(${scale.width}, ${scale.height}) translate(-50%, -50%)`
                }
            }
        },
        resize() {
            clearTimeout(this.drawTiming)
            this.drawTiming = setTimeout(() => {
                this.calcRate()
                this.signInCalcRate()
            }, 200)
        }
    },
}