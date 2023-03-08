<!-- 腾讯云上传视频-组件 -->
<template>
    <div>
        <el-upload ref="upload" v-loading="loading && !progress.show" style="width: 100%" element-loading-text="读取并上传文件..."
                   :class="{ inline: inline }" :accept="accept" :action="realAction" :with-credentials="true"
                   :auto-upload="autoUpload" :on-change="onChange" :before-upload="beforeupload"
                   :http-request="uploadHttpRequest" :limit="limit" :multiple="limit >= 1" :file-list="fileList"
                   :before-remove="beforeRemove" :on-success="onSuccess" :on-exceed="handleExceed"
                   :show-file-list="showFileList">
            <slot />
        </el-upload>
        <el-dialog :visible.sync="progress.show" width="550px" :modal="true" :show-close="false" :destroy-on-close="true"
                   :center="true" :close-on-press-escape="false" :close-on-click-modal="false" :append-to-body="true"
                   @before-close="handlerClose">
            <div style="text-align: center">
                <div v-for="(v, k, i) in progressBars" :key="i">
                    <el-progress :percentage="v.percent" :text-inside="true" :stroke-width="20" />
                    <h4>{{ k }} 上传速度：{{ v.speed }} Mb/s</h4>
                </div>
            </div>
        </el-dialog>
    </div>
</template>
<script>
import { computeMD5, uuid } from '@/utils/index'
import COS from 'cos-js-sdk-v5'
import { tencentCosCredential } from '@/api/common'
const chunkSizeMB = 1024000 // 分片2MB
const chunkSize = chunkSizeMB * 1024 * 1024 // 分片2MB
export default {
    name: 'CommonFileUpload',
    props: {
        action: {
            type: String,
            required: false,
            default: 'api/Senparc.Xncf.TestModular/ApiAppService/Xncf.TestModular_ApiAppService.MyCustomApi'
        },
        accept: {
            type: String,
            required: false,
            default: 'image/gif, image/jpeg, image/jpg, image/png, image/svg, video/mpeg ,audio/mp4, video/mp4'
        },
        bucketName: {
            type: String,
            required: false,
            default: 'UploadFiles'
        },
        limit: {
            type: Number,
            required: false,
            default: NaN
        },
        isSignalFile: {
            type: Boolean,
            required: false,
            default: false
        },
        inline: {
            type: Boolean,
            required: false,
            default: false
        },
        autoUpload: {
            type: Boolean,
            required: false,
            default: true
        },
        showFileList: {
            type: Boolean,
            required: false,
            default: true
        }
    },
    data() {
        return {
            loading: false,
            progress: {
                current: [],
                total: 1.0,
                show: false
            },
            fileList: [],
            successFiles: [],
            progressBars: {},
            successList: {}
        }
    },
    computed: {
        realAction() {
            return process.env.VUE_APP_BASE_API + this.action
        },
        getProgress() {
            return 0
            // const p = (this.progress.current.length / this.progress.total * 1.0) * 100;
            // return p.toFixed(2) * 1.0;
        }
    },
    methods: {
        beforeRemove(file, fileList) {
            return this.$confirm(`确定移除 ${file.name}？`)
        },
        addSingleFile(singleFile) {
            this.fileList.push(singleFile)
        },
        onSuccess(response, file, fileList) {
            this.$emit('on-success', { response, file, fileList })
        },
        handlerClose(done) {
            //
        },
        handleExceed(files, fileList) {
            this.$message.warning(`当前限制选择 ${this.limit} 个文件。`)
            // this.$message.warning(`最多一次性上传 ${this.limit} 个文件`);
        },
        onChange(file, fileList) {
            this.fileList = fileList

            // console.info('change....');
            if (file.status === 'ready') {
                const i = this.fileList.filter(_ => _.name === file.name)
                if (i.length > 1) {
                    this.fileList.pop()
                    // this.removeSingleFile(file);
                } else {
                    this.progressBars[file.name] = { percent: 0, speed: 0 }
                }
            }
            this.$emit('on-change', { file, fileList })
            // console.info(this.fileList);
        },
        // beforeRemove(file, fileList) {
        //   this.fileList = fileList;
        //   // console.info(this.fileList);
        //   this.$emit('on-remove', { file, fileList });
        // },
        beforeupload(e) {
            this.$emit('before-upload', e)
        },
        async uploadHttpRequest(param) {
            this.loading = true
            this.$emit('before-upload-http-request', param)
            try {
                const sizeMB = param.file.size / 1024.0 / 1024.0
                const md5Hash =
                    sizeMB > chunkSizeMB
                        ? 'uuid_' + uuid()
                        : await computeMD5(param.file)
                console.info('md5Hash', md5Hash);
                let filepath = ''
                const index = param.file.name.lastIndexOf('.')
                const extensionName = param.file.name.substr(
                    index,
                    param.file.name.length
                )
                const newName =
                    process.env.VUE_APP_COS_FILEPREFIX + 'b' + md5Hash + extensionName
                if (param.file.type.lastIndexOf('image') >= 0) {
                    filepath = 'images/' + newName
                } else if (param.file.type.lastIndexOf('video') >= 0) {
                    filepath = 'medias/' + newName
                } else if (param.file.type.lastIndexOf('audio') >= 0) {
                    filepath = 'medias/' + newName
                } else {
                    filepath = 'medias/' + newName
                    this.$message.error(
                        `不支持此文件名上传，文件：${param.file.name}, 类型:${param.file.type
                        }`
                    )
                    return
                }
                const credentialResponse = await tencentCosCredential({
                    bucket: process.env.VUE_APP_COS_BUKET,
                    region: process.env.VUE_APP_COS_REGION,
                    allowActions:
                        sizeMB > chunkSizeMB
                            ? [
                                'name/cos:InitiateMultipartUpload',
                                'name/cos:ListMultipartUploads',
                                'name/cos:ListParts',
                                'name/cos:UploadPart',
                                'name/cos:CompleteMultipartUpload'
                            ]
                            : ['name/cos:PutObject', 'name/cos:PostObject'],
                    AllowPrefixs: [filepath]
                })
                console.log(credentialResponse);
                var cos = this.getCosInstance(
                    credentialResponse.data.credentials,
                    parseInt(new Date().getTime() / 1000),
                    credentialResponse.data.expiredTime
                )
                if (!this.progress.show) {
                    this.progress.show = true
                }
                if (sizeMB > chunkSizeMB) {
                    // this.progress.current = [];
                    // this.progress.total = 1;
                    if (!this.progress.show) {
                        this.progress.show = true
                    }
                    // 分片上传
                    await this.multipartUpload(cos, param, filepath)
                } else {
                    await this.normalUpload(cos, param, filepath)
                }
                this.$message.success(param.file.name + ' 上传成功!')
                // normalUpload
            } catch (e) {
                console.log('eeeeee', e)
                // this.progress.show = false;
                this.$message.error('上传失败！')
                this.$emit('file-upload-error', e)
            } finally {
                this.loading = false
            }
        },
        normalUpload(cos, param, filepath) {
            return this.cosPutObject(cos, {
                Key: filepath,
                Body: param.file
            })
                .then(data => {
                    const array = data.Location.split('/')
                    array.splice(0, 1)

                    this.progress.show = !Object.values(
                        JSON.parse(JSON.stringify(this.progressBars))
                    ).every(_ => _.percent >= 100)
                    this.successList[param.file.uid] = {
                        file: '/' + array.join('/'),
                        fullFilePath: data.Location
                    }
                    console.info('Object.values(this.successList).length', Object.values(this.successList).length)
                    console.info('this.fileList.length', this.fileList.length)
                    console.info('this.limit', this.limit)
                    if (Object.values(this.successList).length === this.fileList.length || this.isSignalFile) {
                        // 全部完成
                        // this.$emit('file-upload-success', {
                        //   file: '/' + array.join('/'),
                        //   fullFilePath: data.Location
                        // });
                        this.progressBars = [] // 清空进度条
                        this.$emit('file-upload-success', {
                            file: '/' + array.join('/'),
                            fullFilePath: data.Location,
                            extra: JSON.parse(JSON.stringify(this.successList))
                        })
                    }
                    // this.successFiles.push(param.file);
                })
                .finally(() => {
                    this.loading = false
                })
        },
        multipartUpload(cos, param, filepath) {
            return this.cosMultipartInit(cos, param, filepath)
                .then(uploadId => {
                    const parts = new Array(Math.ceil(param.file.size / chunkSize))
                        .fill(0)
                        .map(function (item, index) {
                            return { PartNumber: index + 1 }
                        })
                    // this.progress.total = parts.length;
                    const promises = []
                    promises.push(Promise.resolve(uploadId))
                    parts.forEach(partItem => {
                        const partNumber = partItem.PartNumber
                        const start = (partNumber - 1) * chunkSize
                        const end = Math.min(start + chunkSize)
                        const blob = param.file.slice(start, end)
                        const args = {
                            key: filepath,
                            uploadId: uploadId,
                            partNumber: partNumber,
                            blob: blob,
                            file: param.file
                        }
                        const p = this.cosMultipartUpload(cos, args)
                        promises.push(p)
                    })
                    return Promise.all(promises)
                })
                .then(promises => {
                    const uploadId = promises[0]
                    promises.shift()
                    return this.cosMultipartComplete(cos, {
                        key: filepath,
                        uploadId: uploadId,
                        parts: promises
                    })
                })
                .then(data => {
                    // console.info(data);
                    const array = data.Location.split('/')
                    array.splice(0, 1)
                    this.$emit('file-upload-success', {
                        file: '/' + array.join('/'),
                        fullFilePath: data.Location
                    })
                    // console.info(this.progressBars);
                    this.progress.show = !Object.values(
                        JSON.parse(JSON.stringify(this.progressBars))
                    ).every(_ => _.percent >= 100)
                    // console.info()
                    // this.progressData.every(_ => _ > 0)
                })
        },
        getCosInstance(credentials, startTime, expiredTime, cosOptions) {
            const _options = {}
            _options.getAuthorization = function (options, callback) {
                callback({
                    TmpSecretId: credentials.tmpSecretId,
                    TmpSecretKey: credentials.tmpSecretKey,
                    XCosSecurityToken: credentials.token,
                    StartTime: startTime,
                    ExpiredTime: expiredTime,
                    ScopeLimit: true
                })
            }
            Object.assign(_options, cosOptions || {})
            const cos = new COS(_options)
            return cos
        },
        cosMultipartInit(cos, param, filepath) {
            return new Promise((resolve, reject) => {
                cos.multipartInit(
                    {
                        Key: filepath,
                        Bucket: process.env.VUE_APP_COS_BUKET,
                        Region: process.env.VUE_APP_COS_REGION
                    },
                    (err, data) => {
                        if (err) {
                            reject({ err, type: 'multipartInit' })
                        } else if (data) {
                            const uploadId = data.UploadId
                            resolve(uploadId)
                        }
                    }
                )
            })
        },
        cosMultipartUpload(cos, param) {
            const that = this
            return new Promise((resolve, reject) => {
                cos.multipartUpload(
                    {
                        Bucket: process.env.VUE_APP_COS_BUKET,
                        Region: process.env.VUE_APP_COS_REGION,
                        Key: param.key,
                        UploadId: param.uploadId,
                        PartNumber: param.partNumber,
                        Body: param.blob,
                        onProgress: progressData => {
                            const current = (progressData.loaded * 1.0) / 1024 / 1024
                            const total = (progressData.total * 1.0) / 1024 / 1024
                            that.progressBars[param.file.name] =
                                ((current / total) * 100).toFixed(2) * 1
                            // console.info('forceUpdate before', that.progressBars);
                            that.$forceUpdate()
                            // console.info('forceUpdate after', that.progressBars);
                            // console.log(JSON.stringify(progressData));
                        }
                    },
                    function (err, data) {
                        if (err) {
                            reject(err)
                        } else if (data) {
                            that.progress.current.push(1)
                            resolve({
                                PartNumber: param.partNumber,
                                ETag: data.headers.etag
                            })
                        }
                    }
                )
            })
        },
        cosMultipartComplete(cos, options) {
            return new Promise((resolve, reject) => {
                cos.multipartComplete(
                    {
                        Bucket: process.env.VUE_APP_COS_BUKET,
                        Region: process.env.VUE_APP_COS_REGION,
                        Key: options.key,
                        UploadId: options.uploadId,
                        Parts: options.parts
                    },
                    function (err, data) {
                        if (err) {
                            reject(err)
                        } else if (data) {
                            resolve(data)
                        }
                    }
                )
            })
        },
        cosPutObject(cos, options) {
            const that = this
            return new Promise((resolve, reject) => {
                cos.putObject(
                    {
                        Bucket: process.env.VUE_APP_COS_BUKET,
                        Region: process.env.VUE_APP_COS_REGION,
                        Key: options.Key,
                        Body: options.Body,
                        onProgress: function (progressData) {
                            const current = (progressData.loaded * 1.0) / 1024 / 1024
                            const total = (progressData.total * 1.0) / 1024 / 1024
                            that.progressBars[options.Body.name] = {
                                percent: ((current / total) * 100).toFixed(2) * 1.0, // progressData.percent * 100.0,
                                speed: (progressData.speed / 1024.0 / 1024.0).toFixed(2) * 1.0
                            }
                            that.$forceUpdate()
                        }
                    },
                    function (err, data) {
                        if (data) {
                            resolve(data)
                        } else if (err) {
                            reject(err)
                        }
                    }
                )
            })
        },
        submit() {
            this.$refs['upload'].submit()
        },
        removeSingleFile(file) {
            const index = this.fileList.findIndex((value, index, arr) => {
                return value.uid === file.uid
            })
            const removeFiles = this.fileList.splice(index, 1)
            // debugger
            // this.progressBars[removeFiles[0].name] = undefined;
            console.info('this.successList', this.successList)
            delete this.progressBars[removeFiles[0].name]
            delete this.successList[removeFiles[0].uid]
            // this.$refs['upload'].clearFiles();
        },
        anyFile() {
            return this.fileList.length > 0
        }
    }
}
</script>
<style lang="scss" scoped>
.inline {
    display: inline;
}

.el-button {
    margin-left: 0px;
}

.el-dialog--center .el-dialog__body {
    text-align: center;
}
</style>
  