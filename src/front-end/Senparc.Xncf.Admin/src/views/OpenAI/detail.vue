<!-- open详情页 -->
<!-- 此页面目前还剩对话部分，接口拿不到回复 -->
<template>
    <div class="aisearch">
        <div class="showText" ref="showText">
            <!-- 空状态 -->
            <div v-if="this.showState == false">
                <el-empty description="请在下方输入框中输入您想要了解的内容"></el-empty>
            </div>
            <!-- 请求数据区域 -->
            <div v-else id="demo">
                <main class="mian" v-for="item in dataState" :key="item.id">
                    <!-- 提问 -->
                    <div class="messageList">
                        <div class="messageListitem">
                            <div class="headPortrait"></div>
                            <div class="QA">
                                {{ item.quesition }}
                            </div>
                        </div>
                    </div>
                    <!-- 正常回复 -->
                    <div class="messageList1" v-if="item.anserState == 1">
                        <div class="messageList1item">
                            <div class="headPortrait"></div>
                            <div class="QA">
                                {{ item.anserData }}
                            </div>
                        </div>
                    </div>
                    <!-- 回复报错 -->
                    <div class="messageList2" v-if="item.anserState == 2">
                        <div class="messageList2item">
                            <div class="headPortrait"></div>
                            <div class="QA">
                                无法获取到该数据
                            </div>
                        </div>
                    </div>
                </main>
            </div>
        </div>
        <!-- 输入框区域 -->
        <div class="quesitionArea">
            <div class="iptArea">
                <el-input placeholder="请输入内容" v-model="input">
                    <el-button slot="append" icon="el-icon-position" @click="findContent"
                               v-loading.fullscreen.lock="fullscreenLoading"></el-button>
                </el-input>
                <el-button type="primary" @click="goOpenAImian">返回</el-button>
        </div>
        <!-- <div class="iptArea" style="font-size: 14px;">
                                                                                                                                                                                                        <span>您可以在这里输入想要查找的内容。</span>
                                                                                                                                                                                                                                                                                                                                                                                                                            </div> -->
            <div class="iptArea" style="font-size: 14px;">
                <span @click="addLocalData">您可以在这里输入想要查找的内容。</span>
                <span style="color:gray;cursor: pointer;" @click="LocalData">显示历史数据</span>
            </div>
        </div>
    </div>
</template>
<script>
// 请求
import { postNews } from "@/api/openDetail"
export default {
    data() {
        return {
            input: "",
            idNum: 1,
            dataState: [],//数据
            showState: false,//展示状态
            fullscreenLoading: false,//loading
            locastoreDatas: null,//本地存储历史shuju
            firstsay: true,//第一次输入
            textArea: '', // 所有输入过的文本

            // save请求到的数据
            saveData: {
                apiKey: "",
                organizationID: ""
            }
        }
    },
    created() {
        this.getToken();
    },
    methods: {
        addLocalData() {
            // 此函数是后期对话接口更新后的保存本地数据逻辑
            let dataState = {
                question: "你好",
                text: '张三\n\n李四\n\n王五\n\n赵六\n\n宋七\n\n路八',
            }
            var datas = dataState.text.split('\n')
            //去重
            function arrayUnique(arr) {
                var len = arr.length;
                var res = [];
                for (var i = 0; i < len; i++) {
                    if (res.indexOf(arr[i]) === -1) {  //如果该数之前没有出现过
                        res.push(arr[i]);  //就把该元素push进去
                    }
                }
                return res;
            }
            // console.log(arrayUnique(datas));
            dataState.text = arrayUnique(datas)[arrayUnique(datas).length - 1];
            // console.log(dataState);
            localStorage.setItem('takeNotes', JSON.stringify(dataState));
            console.log(JSON.parse(localStorage.getItem('takeNotes')));
        },
        LocalData() {

        },
        // 获取路由传值参数
        getToken() {
            if (this.$router.currentRoute.query) {
                this.saveData.apiKey = this.$router.currentRoute.query.appKey;
                this.saveData.organizationID = this.$router.currentRoute.query.organizationID;
                console.log('spikey,id', this.saveData);
            }
        },
        // 回到首页
        goOpenAImian() {
            this.$router.push({
                // path: '/OpenAI/index',
                // 分布式的跳转路径
                path: '/Module/b/openindex',
                query: {
                    appKey: this.saveData['apiKey'],
                    organizaionID: this.saveData['organizationID']
                }
            })
        },
        // 新增消息
        // 调用的时候\n去掉(展示时)，存储的时候把test保存至本地。下一次发送消息的时候不 把上次的问话放进去，以及回复。另外一种情况。
        async findContent() {
            this.showState = true;//显示信息列表
            this.idNum = this.idNum + 1;//id

            this.fullscreenLoading = true;//lodaing

            this.textArea = this.firstsay ? this.input : this.textArea + '\n\n' + this.input

            // this.firstsay ? this.textArea = this.input : this.textArea = this.textArea + '\n\n' + this.input
            // console.log('1', this.textArea);
            var requestData = {
                prompt: this.textArea,
                model: null,//暂时不做，后期选模型
                maxTokens: 20,//稍等，最大消费
            }
            this.textArea = requestData.prompt;//所有文字
            console.log('调用接口的传参', requestData);

            await postNews(requestData).then((res) => {
                console.log('请求成功', res);
                if (res.data.finish_reason == 'length') {
                    this.$message({
                        message: 'maxToken不够用啦,要设长点哦',
                        type: 'warning'
                    });
                    this.dataState.push({
                        id: this.idNum,
                        quesition: this.input,
                        anserData: res.data.text,
                        anserState: 1,

                    })

                    this.showHistory();
                    this.fullscreenLoading = false;//lodaing
                } else {
                    // 展示json新增
                    this.dataState.push({
                        id: this.idNum,
                        quesition: this.input,
                        anserData: res.data.text,
                        anserState: 1,
                    })
                    this.fullscreenLoading = false;//lodaing
                }
                // 数据存储本地
                localStorage.setItem('takeNotes', JSON.stringify(this.dataState));
                this.showHistory();
                this.firstsay = false;
                this.fullscreenLoading = false;//lodaing
            }).catch(() => {
                console.log('请求失败');
                this.fullscreenLoading = false;//lodaing
            })
            // 滚动最下面
            setTimeout(() => {
                this.$refs.showText.scrollTop = this.$refs.showText.scrollHeight;//滚动
            }, 100)

            this.input = null;// 清空输入框内容
        },
        // 显示历史数据内容
        showHistory() {
            var arr = JSON.parse(localStorage.getItem('takeNotes'));

            // 这里需要写一个数组去重
            for (let i = 0; i < arr.length; i++) {
                if (res.indexOf(arr[i]) == -1) {
                    this.locastoreDatas.push(arr[i]);
                }
            }
            console.log('local', this.locastoreDatas);
            this.locastoreDatas.forEach(item => {
                // item.
                this.dataState.unshift(item);
            });
            // console.log('a', this.dataState);
        }
    },
}
</script>

<style scoped lang="scss">
// 滚动条-公共样式
::-webkit-scrollbar {
    width: 11px;
    height: 11px;
}

// 滚动条-公共样式
::-webkit-scrollbar-thumb {
    border-radius: 10px;
    background-color: #4b5563;
    background-color: #37bc9b;
    border: 2px solid transparent;
    background-clip: padding-box;
}

.aisearch {
    height: 90vh;
    display: flex;
    flex-direction: column;
    justify-content: space-between;

    .showText {
        height: 700px;
        width: 100%;
        background-color: #fff;
        overflow: hidden;
        overflow-y: auto;
        scrollbar-width: none;
        /* firefox */
        -ms-overflow-style: none;

        .mian {
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: flex-start;
            padding: 2px 15px;
            background-color: #fff;
            // height: 80vh;
            overflow: hidden;
            overflow: auto;

            .messageList {
                padding: 20px 5px;
                background-color: #fff;
                border-top: 1px solid rgba(0, 0, 0, 0.31);
                border-bottom: 1px solid rgba(0, 0, 0, 0.20);

                .messageListitem {
                    width: 70%;
                    display: flex;
                    align-items: flex-start;
                    justify-content: flex-start;

                    .QA {
                        padding: 10px;
                        white-space: pre-wrap;
                        font-size: 14px;
                    }
                }

            }

            .messageList1 {

                padding: 20px 5px;
                background-color: rgba(217, 217, 227, .8);
                border-bottom: 1px solid rgba(0, 0, 0, 0.31);

                .messageList1item {
                    width: 70%;
                    display: flex;
                    align-items: flex-start;
                    justify-content: flex-start;

                    .QA {
                        padding: 10px;
                        white-space: pre-wrap;
                        font-size: 14px;
                    }
                }
            }

            .messageList2 {

                padding: 20px 5px;
                background-color: rgba(217, 217, 227, .8);
                border-bottom: 1px solid rgba(0, 0, 0, 0.31);

                .messageList2item {
                    width: 70%;
                    display: flex;
                    align-items: flex-start;
                    justify-content: flex-start;

                    .QA {
                        padding: 10px;
                        white-space: pre-wrap;
                        font-size: 14px;
                        background-color: rgba(239, 68, 68, .1);
                        border-radius: 4px;
                        border: 1px solid rgba(239, 68, 68, 1);
                    }
                }
            }

            .messageList,
            .messageList1,
            .messageList2 {
                display: flex;
                align-items: center;
                justify-content: center;
                border-color: rgba(0, 0, 0, .1);
                width: 100%;

                .headPortrait {
                    width: 30px;
                    height: 30px;
                    border-radius: 4px;
                    background-color: aqua;
                    margin-right: 10px;
                }
            }
        }
    }

    .quesitionArea {
        height: 200px;
        background-color: #fff;
        box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;


        .iptArea {
            display: flex;
            align-items: center;
            justify-content: center;
            width: 100%;
            padding: 15px;
            margin: 10px;

            ::v-deep .el-input {
                max-width: 800px;
                margin-right: 10px;
            }
        }
    }
}
</style>
<style scoped>
.el-empty {
    height: 70vh;
}
</style>
