<!-- open首页 -->
<template>
    <div class="openai">
        <!-- 首页-功能描述页 -->
        <main>
            <el-container>
                <el-header class="module-header">
                    <span class="start-title">
                        <span class="module-header-v">Senparc.Xncf.OpenAI</span>
                    </span>
                </el-header>
            </el-container>
            <div class="textArea">
                <span>功能说明</span>
                <!-- 获取appkey和organziation -->
                <div class="obtain">
                    <span>若要使用 OpenAI，需要验证 API Key 和 Organization ID。没有这两项内容？传送门：</span>
                    <a href="https://platform.openai.com/account/api-keys">获取 API Key</a>
                    <a href="https://platform.openai.com/account/org-settings">获取 Organization ID</a>
                    <!-- <span>。单个获取太麻烦？一键获取：</span> -->
                    <!-- <span style="cursor: pointer; color: #5a738e;" @click="getkeyid">获取appkey，Organization</span> -->
                </div>
            </div>
            <!-- 功能模块 -->
            <el-row>
                <el-card class="box-card mw-100">
                    <div class="nav" v-if="yesno">
                        <h3>请单击需要使用的模块 </h3>
                        <el-button el-button type=" primary" size="mini" @click="dialogVisible = true">更换信息</el-button>
                    </div>
                    <div class="nav" v-else>
                        <h3>使用该模块需验证登录 </h3>
                        <el-button el-button type=" primary" size="mini" @click="dialogVisible = true">设置信息</el-button>
                    </div>

                    <!-- 搜索-预留 -->
                    <el-input class="box-ipt" v-model="modelValue" placeholder="输入内容按下Enter" clearable
                              @keyup="searchModular"></el-input>
                    <!-- 已有Api key展示数据  -->
                    <div id="xncf-modules-area" v-if="yesno">
                        <el-col :span="6" :xs="24" :sm="12" :md="12" :lg="8" :xl="6" v-for="item in xncfOpenAIList"
                                :key="item.uid">
                            <el-card class="moduleItem">
                                <div slot="header" class="xncf-item-top svgimg greencolor" @click="goOpenAIdetail">
                                    <span class="moudelName">{{ item.menuName }}</span>
                                    <small class="version">v{{ item.version }}</small>
                                </div>
                                <span class="detail" @click="goOpenAIdetail">描述描述描述</span>
                            </el-card>
                        </el-col>
                    </div>
                    <!-- 没有Api key展示数据 -->
                    <div id="xncf-modules-area-else" v-else>
                        <el-col :span="6" :xs="24" :sm="12" :md="12" :lg="8" :xl="6" v-for="item in xncfOpenAIList"
                                :key="item.uid">
                            <el-card class="moduleItem">
                                <div slot="header" class="xncf-item-top svgimg greencolor">
                                    <span class="moudelName">{{ item.menuName }}</span>
                                    <small class="version">v{{ item.version }}</small>
                                </div>
                                <div class="detail">描述描述描述描述描述</div>
                            </el-card>
                        </el-col>
                    </div>
                </el-card>
            </el-row>
        </main>
        <el-dialog title="设置" :visible.sync="dialogVisible" width="50%" :before-close="handleClose">
            <div class="dialogArea">
                <el-form :model="ruleForm" :rules="rules" ref="ruleForm" label-width="100px" class="demo-ruleForm">
                    <el-form-item label="API KEY" prop="appkey">
                        <el-input v-model="ruleForm.appkey" show-password></el-input>
                        <a href="https://platform.openai.com/account/api-keys">获取 API Key</a>
                    </el-form-item>
                    <el-form-item label="Organization ID">
                        <el-input v-model="ruleForm.Organization"></el-input>
                        <a href="https://platform.openai.com/account/org-settings">获取 Organization ID</a>

                    </el-form-item>
                </el-form>
            </div>
            <span slot="footer" class="dialog-footer">
                <el-button @click="dialogVisible = false">取 消</el-button>
                <el-button type="primary" @click="putForm('ruleForm')">确 定</el-button>
            </span>
        </el-dialog>
    </div>
</template>
<script>
// 请求
import { getAppkeyOrganizationID, postAppkeyOrganizationID } from "@/api/openIndex"
import { MessageBox, Message } from "element-ui";
export default {
    data() {
        return {
            // 模块数据-待定
            xncfOpenAIList: [
                {
                    menuName: "模块名称",//名称
                    version: "0.1",//版本
                },
                {
                    menuName: "模块名称",//名称
                    version: "0.1",//版本
                },
                {
                    menuName: "模块名称",//名称
                    version: "0.1",//版本
                },
                {
                    menuName: "模块名称",//名称
                    version: "0.1",//版本
                },
                {
                    menuName: "模块名称",//名称
                    version: "0.1",//版本
                },
                {
                    menuName: "模块名称",//名称
                    version: "0.1",//版本
                },
                {
                    menuName: "模块名称",//名称
                    version: "0.1",//版本
                },
                {
                    menuName: "模块名称",//名称
                    version: "0.1",//版本
                },
                {
                    menuName: "模块名称",//名称
                    version: "0.1",//版本
                },
                {
                    menuName: "模块名称",//名称
                    version: "0.1",//版本
                },
            ],
            modelValue: "",//搜索框内容-待定
            // 模块数据搜索筛选-待定
            xncfOpenAIsearch: [],
            yesno: false,//是否可点击
            passwordState: false,//输入框是否显示password
            dialogVisible: false,//弹出层状态

            // dialog-iptValue
            ruleForm: {
                appkey: '',
                Organization: '',
            },
            // dialog-iptValue-验证
            rules: {
                appkey: [
                    { required: true, message: 'appkey为必填项', trigger: 'blur' },
                ],
            },
            // 存储的key，id数据
            saveData: {
                apiKey: "",
                organizationID: ""
            }
        }
    },
    created() {
        this.getToken();
        // this.getkeyid();
    },
    methods: {
        //关闭弹出层
        handleClose(done) {
            done();
        },
        // 搜索模块内容-待定
        searchModular() {
            // console.log(this.xncfOpenAIsearch);
        },
        //获取Token
        getToken() {
            if (this.$router.currentRoute.query) {
                this.passwordState = true;//token显示password状态
                // 赋值存储appkey，id
                this.saveData.apiKey = this.$router.currentRoute.query.appKey;
                this.saveData.organizationID = this.$router.currentRoute.query.organizationID;
                console.log('spikey,id', this.saveData);
            }
        },
        // get
        async getkeyid() {
            await getAppkeyOrganizationID().then((res) => {
                if (res != null) {
                    Message({
                        message: "请求成功",
                        type: "success",
                        duration: 5 * 1000,
                    });
                    console.log('请求结果', res);
                    this.saveData['apiKey'] = res.apiKey;
                    this.saveData['organizationID'] = res.organizationID;
                    console.log(this.saveData['apiKey'], this.saveData['organizationID']);

                    this.$message({
                        message: '成功',
                        type: 'success'
                    });
                }
            }).catch(() => {
                Message({
                    message: "获取失败了",
                    type: "error",
                    duration: 5 * 1000,
                });
            })
        },
        // post-apikey
        submitForm(formName) {
            this.$refs[formName].validate((valid) => {
                if (valid) {
                    // 保存数据
                    this.saveData['apiKey'] = this.ruleForm.appkey;
                    this.saveData['organizationID'] = this.ruleForm.Organization;
                    let keyId = {
                        appKey: this.saveData['apiKey'],
                        organizationID: this.saveData['organizationID']
                    }
                    // 请求
                    postAppkeyOrganizationID(keyId).then((res) => {
                        if (res != null) {
                            Message({
                                message: "修改信息成功",
                                type: "success",
                                duration: 5 * 1000,
                            });
                            // this.$notify({
                            //     title: '提示',
                            //     message: `您的apiKey是:${this.saveData['apiKey']},您的organizationID是:${this.saveData['organizationID']}`,
                            //     duration: 0
                            // });
                        }
                    }).catch(() => {
                        Message({
                            message: "修改信息失败了",
                            type: "error",
                            duration: 5 * 1000,
                        });
                    })
                    this.yesno = true;//模块可操作
                    this.dialogVisible = false;//关闭弹出层
                    this.passwordState = true;//token显示password状态
                } else {
                    console.log('error submit!!');
                    return false;
                }
            });
        },
        // post-apikey-doing
        putForm(formName) {
            this.$refs[formName].validate((valid) => {
                if (valid) {
                    // 验证appkey修改数值是否有变化
                    // if (this.ruleForm.appkey == this.saveData['apiKey']) {
                    //     this.saveData['apiKey'] = null;
                    //     this.saveData['organizationID'] = null;
                    // } else {
                    //     this.saveData['apiKey'] = this.ruleForm.appkey;
                    //     this.saveData['organizationID'] = this.ruleForm.Organization
                    // }
                    this.saveData['apiKey'] = this.ruleForm.appkey;
                    this.saveData['organizationID'] = this.ruleForm.Organization
                    let keyId = {
                        appKey: this.saveData['apiKey'],
                        organizationID: this.saveData['organizationID']
                    }
                    console.log('appkey', this.saveData['apiKey']);
                    // 请求
                    postAppkeyOrganizationID(keyId).then((res) => {
                        if (res != null) {
                            Message({
                                message: "修改信息成功",
                                type: "success",
                                duration: 5 * 1000,
                            });
                            this.$message({
                                message: '成功',
                                type: 'success'
                            });
                        }
                    }).catch(() => {
                        Message({
                            message: "修改信息失败了",
                            type: "error",
                            duration: 5 * 1000,
                        });
                    })
                    this.yesno = true;//可操作模块
                    this.dialogVisible = false;//关闭弹出层
                    this.passwordState = true;//token显示password状态
                    // this.getkeyid();//自动调用获取appkey
                } else {
                    console.log('error submit!!');
                    return false;
                }
            });
        },
        // 去到详情页
        goOpenAIdetail() {
            this.$router.push({
                // admin路径
                //path: '/OpenAI/detail',
                // 分布式的路径
                 path: '/Module/b/opendetail',
                query: {
                    appKey: this.saveData['apiKey'],
                    organizaionID: this.saveData['organizationID']
                }
            })
        },
    }
}
</script>
<style scopen lang="scss">
// 滚动条-共用样式
::-webkit-scrollbar {
    width: 11px;
    height: 11px;
}

// 滚动条-共用样式
::-webkit-scrollbar-thumb {
    border-radius: 10px;
    background-color: #4b5563;
    background-color: #37bc9b;
    border: 2px solid transparent;
    background-clip: padding-box;
}

.openai {
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
    margin: 10px;
    background-color: #fff;

    main {
        width: 100%;
        min-height: 80px;
        padding: 15px;

        .nav {
            display: flex;
            align-items: center;
            justify-content: center;
            width: 100%;
            height: 40px;
            padding: 10px;
            margin-bottom: 10px;

            h3 {
                display: inline-block;
                margin: 10px;
                padding: 10px;
                white-space: nowrap;
            }
        }

        .textArea {
            width: 100%;
            max-height: 100%;
            min-height: 40px;
            padding: 10px;
            margin: 10px 0px;
            white-space: pre-wrap;
            box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);

            .obtain {
                margin-top: 10px;

                a {
                    padding-right: 5px;
                }
            }
        }

        .box-ipt {
            max-width: 300px;
        }

        .clearfix {
            display: flex;
            align-items: center;
            justify-content: space-between;
        }

        ::v-deep .el-card__header {
            padding: 16px;
            font-weight: bold;
        }

        ::v-deep .el-card__body {
            padding: 16px;
        }

        // 已登录
        #xncf-modules-area {
            display: flex;
            flex-wrap: wrap;
            display: flex;

            overflow: hidden;

            .moduleItem {
                margin-top: 10px;
                cursor: pointer;
            }

            .detail {
                white-space: pre-wrap;
                font-size: 14px;
            }

            .xncf-item-top {
                display: flex;
                flex-direction: row;
                font-weight: bold !important;
                flex-wrap: nowrap;
                gap: 10px;
            }

            .moudelName {
                flex: 1;
                word-break: break-all;
            }

            .version {
                white-space: nowrap;
            }
        }

        // 未登录
        #xncf-modules-area-else {
            display: flex;
            flex-wrap: wrap;
            display: flex;
            overflow: hidden;

            .moduleItem {
                cursor: not-allowed;
                // width: 300px;
                margin: 10px 10px 0px 0px;
                background-color: #c0c4cc11;
            }

            .detail {
                white-space: pre-wrap;
                font-size: 14px;
            }

            .xncf-item-top {
                display: flex;
                flex-direction: row;
                font-weight: bold !important;
                flex-wrap: nowrap;
                gap: 10px;
            }

            .moudelName {
                flex: 1;
                word-break: break-all;
            }

            .version {
                white-space: nowrap;
            }
        }
    }

    .dialogArea {
        span {
            line-height: 40px;
        }
    }


}
</style>
<style scoped>
.main>>>.el-card__body {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
}
</style>