<template>
  <!-- 应用中枢,剩余流程未动 -->
  <div class="application">
    <!-- 标题 -->
    <el-card v-if="!showAddTable">
      <nav class="nav">
        <h3 class="shoptit">NeuChar Group222应用中枢</h3>
        <div class="discover">
          <i class="el-icon-discover"></i>
          <span>使用指南</span>
        </div>
      </nav>
    </el-card>
    <el-card>
      <main class="main" v-if="!showAddTable">
        <div class="mainTitle">
          <el-button @click="addTable">新增</el-button>
          <el-button type="danger">删除</el-button>
          <el-input
            placeholder="请输入内容"
            v-model="input3"
            class="input-with-select"
          >
            <el-button
              slot="append"
              icon="el-icon-search"
              @click="search"
            ></el-button>
          </el-input>
        </div>
        <div class="mainCenter">
          <el-table
            ref="multipleTable"
            :data="tableData"
            tooltip-effect="dark"
            style="width: 100%"
            @selection-change="handleSelectionChange"
            border
          >
            <el-table-column type="selection"> </el-table-column>
            <el-table-column label="名称">
              <template slot-scope="scope">{{ scope.row.date }}</template>
            </el-table-column>
            <el-table-column prop="name" label="是否允许重复">
            </el-table-column>
            <el-table-column
              prop="address"
              label="添加事件"
              show-overflow-tooltip
            >
            </el-table-column>
            <el-table-column label="操作"> </el-table-column>
          </el-table>
          <div style="margin-top: 1.25rem">
            <el-button @click="toggleSelection([tableData[1], tableData[2]])"
              >切换第二、第三行的选中状态</el-button
            >
            <el-button @click="toggleSelection()">取消选择</el-button>
          </div>
        </div>
      </main>
      <main class="main" v-if="showAddTable">
        <div class="preservation">
          <el-button type="primary">保存</el-button>
        </div>
        <el-collapse v-model="activeNames" @change="handleChange">
          <el-collapse-item title="应用中枢基础设置" name="1">
            <div class="predetail">
              <el-tabs
                v-model="activeName"
                type="card"
                @tab-click="handleClick"
              >
                <el-tab-pane label="基础信息" name="first">
                  <div class="iptArea">
                    <span>应用中枢名称</span>
                    <el-input
                      v-model="appNameIpt"
                      placeholder="应用中枢名称"
                    ></el-input>
                  </div>
                  <div class="iptArea">
                    <span>应用中枢简介</span>
                    <el-input
                      v-model="appNameIpt"
                      type="textarea"
                      :rows="2"
                      placeholder="应用中枢简介"
                    ></el-input>
                  </div>
                  <div class="repeatArea">
                    <span>重复流程:</span>
                    <el-radio v-model="radio" label="1">允许</el-radio>
                  </div>
                  <div class="graycolor">
                    说明：最后一个流程完成后，扫码重新回到第一个流程中
                  </div>
                  <div class="repeatArea">
                    <span>流程进入码:</span>
                    <el-button type="success" plain>查看</el-button>
                  </div>
                  <div class="graycolor">说明：扫码进入应用中枢。</div>
                </el-tab-pane>
                <el-tab-pane label="全局变量设置" name="second">
                  <div class="graycolor">
                    说明：最后一个流程完成后，扫码重新回到第一个流程中
                  </div>
                  <el-button type="success" plain>添加</el-button>
                </el-tab-pane>
              </el-tabs>
            </div>
          </el-collapse-item>
        </el-collapse>
        <el-collapse v-model="activeNames" @change="handleChange">
          <el-collapse-item title="搜索App名称" name="2">
            <!-- <el-button @click="antvX6Check(false)">模拟点击空白图表</el-button>
            <el-button @click="antvX6Check(true)">模拟点击有数据图表</el-button> -->
            <div class="prepri">
              <!-- antv区域 -->
              <el-card>
                <!-- 添加区块 -->
                <div
                  v-if="prostate == true && whetherData == true"
                  class="addArea"
                >
                  <div
                    class="addAreain"
                    @drag="menuDrag('defaultSquare')"
                    draggable="true"
                  >
                    <i class="icon-square"></i><strong>矩形模块</strong>
                  </div>
                  <div
                    class="addAreain"
                    draggable="true"
                    @drag="menuDrag('defaultYSquare')"
                  >
                    <i class="icon-square"></i><strong>圆角矩形</strong>
                  </div>
                  <div
                    class="addAreain"
                    draggable="true"
                    @drag="menuDrag('output')"
                  >
                    <i class="icon-square"></i><strong>Dag</strong>
                  </div>
                  <el-button @click="actionStore">触发运行</el-button>
                </div>
                <!-- 绘制区域 -->
                <div class="antv-wrapper">
                  <div
                    class="wrapper-canvas"
                    :style="{ height: height }"
                    id="wrapper"
                    @drop="drop($event)"
                    @dragover.prevent
                  ></div>
                  <div class="wrapper-tips">
                    <div class="wrapper-tips-item">
                      <el-switch
                        v-model="isPortsShow"
                        @change="changePortsShow"
                      ></el-switch>
                      <span>链接桩常显</span>
                    </div>
                  </div>
                </div>
              </el-card>
              <!-- 详情添加，首页添加 -->
              <el-card v-if="editDrawer == true">
                <div class="process">
                  <!-- 添加首页 -->
                  <div v-if="prostate == false" class="editsmall">
                    <h4>添加流程</h4>
                    <span>选择App</span>
                    <el-select
                      v-model="ruleForm.appname"
                      placeholder="请选择"
                      @input="changeNode('labelText', ruleForm.appname)"
                    >
                      <el-option
                        v-for="item in options"
                        :key="item.value"
                        :label="item.label"
                        :value="item.value"
                      >
                      </el-option>
                    </el-select>
                    <el-button @click="changepro">开始配置</el-button>
                    <div class="shopping">应用商店</div>
                  </div>
                  <!-- 详情添加 -->
                  <div v-if="prostate == true" class="editbig">
                    <h2>编辑流程</h2>
                    <div class="check">
                      <div class="checkl">
                        <span> 已选 </span>
                        <p>APP ：</p>
                      </div>
                      <div class="checkr">
                        <span>{{ this.ruleForm.appname }}</span>
                        <p>设置</p>
                      </div>
                    </div>
                    <h2>流程配置</h2>
                    <div class="cessout">
                      <el-radio v-model="ruleForm.yresource" label="1">
                        <div class="cess">
                          <span>隐藏流程</span>
                          <p>(跳过该流程，且参数不可回复)</p>
                        </div>
                      </el-radio>
                      <el-radio v-model="ruleForm.eresource" label="2">
                        <div class="cess">
                          <span>自动进入</span>
                          <p>(跳过该流程，且参数不可回复)</p>
                        </div>
                      </el-radio>
                    </div>
                    <el-form
                      :model="ruleForm"
                      :rules="rules"
                      mn
                      ref="ruleForm"
                      label-width="6.25rem"
                      class="demo-ruleForm"
                    >
                      <div class="elform">
                        <div class="itemtit">流程名称</div>
                        <el-form-item prop="name">
                          <el-input
                            v-model="ruleForm.name"
                            placeholder="请输入流程名称"
                          >
                          </el-input>
                        </el-form-item>
                        <div class="itemtit">流程描述</div>
                        <el-form-item prop="name">
                          <el-input v-model="ruleForm.desc" type="textarea">
                          </el-input>
                        </el-form-item>
                        <div class="itemtit">选择版本</div>
                        <el-select
                          v-model="ruleForm.value"
                          placeholder="请选择"
                        >
                          <el-option
                            v-for="item in ruleForm.options"
                            :key="item.value"
                            :label="item.label"
                            :value="item.value"
                          >
                          </el-option>
                        </el-select>
                        <div class="check">
                          <div class="checkl">
                            <p>appId ：</p>
                            <span>描述：{{ ruleForm.appidDescribe }}</span>
                          </div>
                        </div>
                        <div>
                          <el-radio v-model="ruleForm.appIdradio" label="1"
                            >填写</el-radio
                          >
                          <el-radio v-model="ruleForm.appIdradio" label="2"
                            >选择上级输出</el-radio
                          >
                          <el-radio v-model="ruleForm.appIdradio" label="3"
                            >全局变量</el-radio
                          >
                          <el-input
                            v-if="ruleForm.appIdradio == '1'"
                            v-model="ruleForm.appIdinput"
                            placeholder="请输入内容"
                          ></el-input>
                          <el-select
                            v-if="ruleForm.appIdradio == '2'"
                            v-model="ruleForm.appIdselect"
                            placeholder="请选择"
                          >
                            <el-option
                              v-for="item in ruleForm.options"
                              :key="item.value"
                              :label="item.label"
                              :value="item.value"
                            >
                            </el-option>
                          </el-select>
                          <el-select
                            v-if="ruleForm.appIdradio == '3'"
                            v-model="ruleForm.appIdselectoverall"
                            placeholder="请选择"
                          >
                            <el-option
                              v-for="item in ruleForm.overalloptions"
                              :key="item.value"
                              :label="item.label"
                              :value="item.value"
                            >
                            </el-option>
                          </el-select>
                        </div>
                        <div class="check">
                          <div class="checkl">
                            <p>id ：</p>
                            <span>描述：{{ ruleForm.idDescribe }}</span>
                          </div>
                        </div>
                        <div>
                          <el-radio v-model="ruleForm.lineIdradio" label="1"
                            >填写</el-radio
                          >
                          <el-radio v-model="ruleForm.lineIdradio" label="2"
                            >选择上级输出</el-radio
                          >
                          <el-radio v-model="ruleForm.lineIdradio" label="3"
                            >全局变量</el-radio
                          >
                          <!-- 先注释状态，交互未添加 -->
                          <el-input
                            v-if="ruleForm.lineIdradio == '1'"
                            v-model="ruleForm.Idinput"
                            placeholder="请输入内容"
                          ></el-input>
                          <el-select
                            v-if="ruleForm.lineIdradio == '2'"
                            v-model="ruleForm.Idselect"
                            placeholder="请选择"
                          >
                            <el-option
                              v-for="item in ruleForm.options"
                              :key="item.value"
                              :label="item.label"
                              :value="item.value"
                            >
                            </el-option>
                          </el-select>
                          <el-select
                            v-if="ruleForm.lineIdradio == '3'"
                            v-model="ruleForm.Idselectoverall"
                            placeholder="请选择"
                          >
                            <el-option
                              v-for="item in ruleForm.overalloptions"
                              :key="item.value"
                              :label="item.label"
                              :value="item.value"
                            >
                            </el-option>
                          </el-select>
                          <!-- <el-input
                            v-model="ruleForm.input"
                            placeholder="请输入内容"
                          ></el-input>
                          <el-select
                            v-model="ruleForm.value"
                            placeholder="请选择"
                          >
                            <el-option
                              v-for="item in ruleForm.options"
                              :key="item.value"
                              :label="item.label"
                              :value="item.value"
                            >
                            </el-option>
                          </el-select> -->
                        </div>
                        <div style="margin: 0.9375rem 0">
                          <el-button @click="handlerSend('ruleForm')"
                            >确认/导出</el-button
                          >
                          <el-button @click="handlerDel"
                            >删除此{{
                              editTitle === "编辑节点" ? "节点" : "连线"
                            }}</el-button
                          >
                        </div>
                      </div>
                    </el-form>
                  </div>
                  <!-- 编辑线路 -->
                  <div v-if="prostate == 'line'" class="editbig">
                    <div class="check">
                      <div class="checkl">
                        <p>appId ：</p>
                        <span>{{ ruleForm.appidline }}</span>
                      </div>
                    </div>
                    <!-- 线的基本配置项，暂时注释 -->
                    <!-- <div>
                      <el-radio v-model="ruleForm.lineradio" label="1"
                        >填写</el-radio
                      >
                      <el-radio v-model="ruleForm.lineradio" label="2"
                        >选择上级输出</el-radio
                      >
                      <el-radio v-model="ruleForm.lineradio" label="3"
                        >全局变量</el-radio
                      >
                      <el-input
                        v-if="this.ruleForm.lineradio == '1'"
                        v-model="ruleForm.appidline"
                        placeholder="请输入内容"
                      >
                      </el-input>
                      <el-select
                        v-if="ruleForm.lineradio == '2'"
                        v-model="ruleForm.lineselect"
                        placeholder="请选择"
                      >
                        <el-option
                          v-for="item in ruleForm.lineselectoptions"
                          :key="item.value"
                          :label="item.label"
                          :value="item.value"
                        >
                        </el-option>
                      </el-select>
                      <el-select
                        v-if="ruleForm.lineradio == '3'"
                        v-model="ruleForm.lineoverall"
                        placeholder="请选择"
                      >
                        <el-option
                          v-for="item in ruleForm.lineoveralloption"
                          :key="item.value"
                          :label="item.label"
                          :value="item.value"
                        >
                        </el-option>
                      </el-select>
                    </div> -->
                    <div style="margin: 0.9375rem 0">
                      <el-button @click="handlerDel"
                        >删除此{{
                          editTitle === "编辑节点" ? "节点" : "连线"
                        }}</el-button
                      >
                    </div>
                  </div>
                </div>
              </el-card>
            </div>
          </el-collapse-item>
        </el-collapse>
      </main>
    </el-card>
    <!-- 页脚组件 -->
    <footerinfo />
    <!-- 反馈组件 -->
    <feedback />
  </div>
</template>
<script>
import footerinfo from "@/components/FooterInfo";
import feedback from "@/components/Feedback";
// 引入antv/x6
import { Graph, Shape, Path } from "@antv/x6";
import "@antv/x6-vue-shape";
import database from "./components/nodeTheme/database.vue";
import condition from "./components/nodeTheme/condition.vue";
import onlyout from "./components/nodeTheme/onlyOut.vue";
import onlyin from "./components/nodeTheme/onlyIn.vue";
import {
  configSetting,
  configNodeShape,
  configNodePorts,
  configEdgeLabel,
  graphBindKey,
} from "@/utils/antvSetting";

export default {
  components: {
    footerinfo,
    feedback,
  },
  props: {
    height: {
      type: String,
      default: "100vh", //'45rem'
    },
    value: {
      type: String,
      default: "",
    },
  },
  data() {
    return {
      input3: "", //搜索的输入框
      //   表单数据
      tableData: [
        {
          date: "2016-05-03",
          name: "王小虎",
          address: "上海市普陀区金沙江路 1518 弄",
        },
        {
          date: "2016-05-02",
          name: "王小虎",
          address: "上海市普陀区金沙江路 1518 弄",
        },
        {
          date: "2016-05-04",
          name: "王小虎",
          address: "上海市普陀区金沙江路 1518 弄",
        },
        {
          date: "2016-05-01",
          name: "王小虎",
          address: "上海市普陀区金沙江路 1518 弄",
        },
        {
          date: "2016-05-08",
          name: "王小虎",
          address: "上海市普陀区金沙江路 1518 弄",
        },
        {
          date: "2016-05-06",
          name: "王小虎",
          address: "上海市普陀区金沙江路 1518 弄",
        },
        {
          date: "2016-05-07",
          name: "王小虎",
          address: "上海市普陀区金沙江路 1518 弄",
        },
      ],
      multipleSelection: [],
      // 切换新增详情首页状态,为方便现在改为true
      showAddTable: true,
      // 折叠面板的展示状态
      activeNames: ["2"],
      //
      activeName: "first",
      // 应用中枢名称ipt
      radio: "",
      appNameIpt: "",
      // 应用中枢配置-添加流程显示状态
      showProcess: false,
      // 显示添加流程
      options: [
        {
          value: "app名称111",
          label: "app名称111",
        },
        {
          value: "app名称222",
          label: "app名称222",
        },
      ],
      // 当前app的配置数据
      ruleForm: {
        appname: "微微嗨云互动", //下拉框app名称选中内容,标题内容
        // 流程配置
        yresource: "", //隐藏流程
        eresource: "", //自动进入
        name: "流程名称11", //流程名称
        desc: "", //流程描述
        value: "", //版本选择值
        // 版本选择项
        options: [
          {
            value: "0.1.0",
            label: "0.1.0",
          },
          {
            value: "2.1.0",
            label: "2.1.0",
          },
          {
            value: "3.1.0",
            label: "3.1.0",
          },
        ],
        appidDescribe: "appid描述", //appid描述
        idDescribe: "id描述", //appid描述
        appIdradio: "1", //选择输入appid的方式 1.填写 2.选择上级输出 3.全局变量
        appIdinput: "", //appid输入方式- 1.填写input
        appIdselect: "", //appid输入方式-2.选择上级输出select
        appIdselectoptions: [
          {
            value: "value0.1.0",
            label: "value0.1.0",
          },
          {
            value: "value2.1.0",
            label: "value2.1.0",
          },
          {
            value: "value3.1.0",
            label: "value3.1.0",
          },
        ],
        appIdselectoverall: "", //appid输入方式-3.全局变量select
        overalloptions: [
          {
            value: "111value",
            label: "111value",
          },
          {
            value: "222value",
            label: "222value",
          },
          {
            value: "333value",
            label: "333value",
          },
        ],
        Idradio: "2", //选择输入id的方式 1.填写 2.选择上级输出 3.全局变量
        Idinput: "", //id输入方式- 1.填写input
        Idselect: "", //id输入方式-2.选择上级输出select
        Idselectoptions: [
          {
            value: "value0.1.0",
            label: "value0.1.0",
          },
          {
            value: "value2.1.0",
            label: "value2.1.0",
          },
          {
            value: "value3.1.0",
            label: "value3.1.0",
          },
        ],
        Idselectoverall: "", //id输入方式-3.全局变量select
        overalloptions: [
          {
            value: "111value",
            label: "111value",
          },
          {
            value: "222value",
            label: "222value",
          },
          {
            value: "333value",
            label: "333value",
          },
        ],
        // 连线部分
        // appid
        appidline: "", //id输入方式- 1.填写input  线appid
        /*
        //线的基础配置项，暂时注释
        //lineradio: "", //线单选状态
        //lineselect: "", //id输入方式-2.选择上级输出select
        //lineselectoptions: [
        // {
        //   value: "value0.1.0",
        //   label: "value0.1.0",
        // },
        //  {
        //    value: "value2.1.0",
        //    label: "value2.1.0",
        //  },
        //  {
        //    value: "value3.1.0",
        //    label: "value3.1.0",
        // },
        //],
        */
        lineoverall: "", //id输入方式-3.全局变量select
        lineoveralloption: [
          {
            value: "111value",
            label: "111value",
          },
          {
            value: "222value",
            label: "222value",
          },
          {
            value: "333value",
            label: "333value",
          },
        ],
      },
      // 表单验证
      rules: {
        name: [{ required: true, message: "不可为空", trigger: "blur" }],
      },
      // antv
      menuItem: "", //添加的区块
      isPortsShow: false,
      graph: null,
      editTitle: "", //用做判断节点或连线
      // 选中节点数据
      form: {},
      // 历史图表数据,回显使用
      graphJson: [
        {
          shape: "edge",
          attrs: {
            line: {
              stroke: "#A2B1C3",
              targetMarker: { name: "block", width: 12, height: 8 },
            },
          },
          id: "3b9deb09-070e-4eb2-89a7-be4b804f86ec",
          zIndex: 0,
          source: {
            cell: "65b7edf1-7749-4922-af0b-853e8df02d07",
            port: "e92d7d86-eeb6-4996-a3da-a201b849323e",
          },
          target: {
            cell: "452cefc5-84c7-412e-9dce-97aafb7149c4",
            port: "f7659f47-670d-4cf0-860d-147b4fbc1df9",
          },
        },
        {
          shape: "edge",
          attrs: {
            line: {
              stroke: "#A2B1C3",
              targetMarker: { name: "block", width: 12, height: 8 },
            },
          },
          id: "54e93bcd-35a7-4843-a2b2-7f4a88b98272",
          zIndex: 0,
          source: {
            cell: "0f31ddf6-5e36-4252-a613-16e6adbf408d",
            port: "665feac9-899b-4ba4-9816-d04ba9430d8d",
          },
          target: {
            cell: "65b7edf1-7749-4922-af0b-853e8df02d07",
            port: "839325f4-34e1-4812-8291-563405778377",
          },
        },
        {
          shape: "edge",
          attrs: {
            line: {
              stroke: "#A2B1C3",
              targetMarker: { name: "block", width: 12, height: 8 },
            },
          },
          id: "89df7596-1156-4e82-b0b3-696a795c469a",
          zIndex: 0,
          source: {
            cell: "65b7edf1-7749-4922-af0b-853e8df02d07",
            port: "8c0a4778-28e1-4dea-9c77-7468ced8d212",
          },
          target: {
            cell: "5de02e50-f0c4-4f66-a036-d9afdf439bdc",
            port: "dddf9360-5624-4f77-af79-c4cf01353e4d",
          },
        },
        {
          position: { x: 120, y: 70 },
          size: { width: 100, height: 50 },
          attrs: {
            text: { text: "椭圆形" },
            body: { rx: 20, ry: 26, fill: "#fff", stroke: "#333" },
            label: { text: "开始", fontSize: 16, fill: "#333" },
          },
          visible: true,
          shape: "rect",
          id: "0f31ddf6-5e36-4252-a613-16e6adbf408d",
          data: { type: "defaultOval" },
          ports: {
            items: [
              { group: "top", id: "715caa5e-4f34-4f2a-ab13-0b6f6df68054" },
              { group: "right", id: "8fd14a00-e82a-4522-b52c-b37680a9fb6d" },
              { group: "bottom", id: "665feac9-899b-4ba4-9816-d04ba9430d8d" },
              { group: "left", id: "e962caf9-5db7-4da8-8c87-e788fa5bacb4" },
            ],
          },
          zIndex: 1,
        },
        {
          position: { x: 20, y: 400 },
          size: { width: 100, height: 50 },
          attrs: {
            text: { text: "方形" },
            body: { fill: "#fff", stroke: "#333" },
            label: {
              text: "结果1",
              textarea: { title: "app名称" },
              fontSize: 16,
              fill: "#333",
            },
          },
          visible: true,
          shape: "rect",
          id: "5de02e50-f0c4-4f66-a036-d9afdf439bdc",
          data: { type: "defaultSquare" },
          ports: {
            items: [
              { group: "top", id: "dddf9360-5624-4f77-af79-c4cf01353e4d" },
              { group: "right", id: "6e6c553b-06ec-44eb-9f88-663ee69453db" },
              { group: "bottom", id: "3fa6a2ec-b413-4df9-9a96-e4a3c6a12904" },
              { group: "left", id: "fecd2de8-8e4a-4a45-ab81-d9ce1484f6b6" },
            ],
          },
          zIndex: 2,
        },
        {
          position: { x: 129, y: 225 },
          size: { width: 120, height: 50 },
          attrs: {
            text: { text: "菱形" },
            body: {
              refPoints: "0,10 10,0 20,10 10,20",
              fill: "#fff",
              stroke: "#333",
            },
            label: { text: "判断", fontSize: 16, fill: "#333" },
          },
          visible: true,
          shape: "polygon",
          id: "65b7edf1-7749-4922-af0b-853e8df02d07",
          data: { type: "defaultRhombus" },
          ports: {
            items: [
              { group: "top", id: "839325f4-34e1-4812-8291-563405778377" },
              { group: "right", id: "e92d7d86-eeb6-4996-a3da-a201b849323e" },
              { group: "bottom", id: "5f094420-46d3-4264-88bc-cf0bb1ef71bf" },
              { group: "left", id: "8c0a4778-28e1-4dea-9c77-7468ced8d212" },
            ],
          },
          zIndex: 3,
        },
        {
          position: { x: 310, y: 410 },
          size: { width: 100, height: 50 },
          attrs: {
            text: { text: "圆角矩形" },
            body: { rx: 6, ry: 6, fill: "#fff", stroke: "#333" },
            label: { text: "结果2", fontSize: 16, fill: "#333" },
          },
          visible: true,
          shape: "rect",
          id: "452cefc5-84c7-412e-9dce-97aafb7149c4",
          data: { type: "defaultYSquare" },
          ports: {
            items: [
              { group: "top", id: "51fa1dec-1d01-41fc-bb0c-c80693b0dba8" },
              { group: "right", id: "8055397d-9790-47ea-bb63-d2b6b36f0c9a" },
              { group: "bottom", id: "62f258d0-393b-4650-98e6-08390d2cfc11" },
              { group: "left", id: "f7659f47-670d-4cf0-860d-147b4fbc1df9" },
            ],
          },
          zIndex: 4,
        },
        {
          position: { x: 711, y: 145 },
          size: { width: 80, height: 80 },
          attrs: {
            text: { text: "圆形" },
            body: { fill: "#fff", stroke: "#333" },
            label: { text: "圆形", fontSize: 16, fill: "#333" },
          },
          visible: true,
          shape: "circle",
          id: "afe961c3-5311-4317-9035-9080bf489961",
          data: { type: "defaultCircle" },
          ports: {
            items: [
              { group: "top", id: "65f67c74-d9c6-44ce-909e-51ca1482c920" },
              { group: "right", id: "658fcbaa-7c51-4d9a-bf5e-47bf33b87b32" },
              { group: "bottom", id: "8122c489-c0d6-404c-a522-953c0d94adf2" },
              { group: "left", id: "39d401ae-491c-4dce-b57a-f53a5512be76" },
            ],
          },
          zIndex: 5,
        },
      ],
      selectCell: "",
      //流程选项展示状态，false首页面(没有配置)，true详情页面(已有配置)
      prostate: null,
      whetherData: true, // 是否有历史的antv图表数据
      editDrawer: false, //是否显示修改表单
      labelForm: "", //编辑线用的数据
      timer: null, // 存储定时器
    };
  },
  mounted() {
    this.initGraph();
  },

  methods: {
    // 运行
    actionStore() {
      // 获取工作流数据
      const { cells: jsonArr } = this.graph.toJSON();
      const tempGroupJson = jsonArr.map((item) => {
        if (item.ports && item.ports.groups) delete item.ports.groups;
        if (item.tools) delete item.tools;
        return item;
      });
      console.log("tempGroupJson", tempGroupJson);
      // 运行部分
      this.showNodeStatus(tempGroupJson);
      // 返现数据
      // const portsGroups = configNodePorts().groups;
      // if (tempGroupJson.length) {
      //   const jsonTemp = tempGroupJson.map((item) => {
      //     if (item.ports) item.ports.groups = portsGroups;
      //     return item;
      //   });
      //   this.graph.fromJSON(jsonTemp);
      // }
      this.graph.centerContent();
    },
    // 节点状态修改
    showNodeStatus(statusList) {
      // const status = statusList.shift();
      statusList?.forEach((item) => {
        const { id, shape } = item;
        if (shape !== "edge") {
          const node = this.graph.getCellById(id);
          const data = node.getData();
          node.setData({
            ...data,
            status: "running",
          });
        }
      });
      // this.timer = setTimeout(() => {
      //   this.showNodeStatus(statusList);
      // }, 300);
    },
    // 已有历史数据加载
    antvX6Check(type) {
      this.prostate = type;
      // setTimeout(() => {
      //   // console.log(this.graphJson);
      //   this.graph.fromJSON(this.graphJson);
      // }, 500);
    },
    // 搜索
    search() {
      console.log("iptvalue", this.input3);
    },
    toggleSelection(rows) {
      if (rows) {
        rows.forEach((row) => {
          this.$refs.multipleTable.toggleRowSelection(row);
        });
      } else {
        this.$refs.multipleTable.clearSelection();
      }
    },
    handleSelectionChange(val) {
      this.multipleSelection = val;
    },
    // 添加表单
    addTable() {
      this.showAddTable = !this.showAddTable;
    },
    // 折叠面板事件
    handleChange(val) {
      console.log(val);
    },
    // 折叠面板-标签框
    handleClick(tab, event) {
      console.log(tab, event);
    },
    // //显示应用中枢配置添加流程
    // showAddProcess() {
    //   this.showProcess = !this.showProcess;
    // },
    // 点击开始配置
    changepro() {
      this.prostate = !this.prostate;
    },
    // antv x6方法
    // 初始化渲染画布
    initGraph() {
      // 流程选项展示状态，false首页面(没有配置)，true详情页面(已有配置)
      // 这里需要添加请求的逻辑
      this.prostate = true;
      this.whetherData = true; // 是否有历史的antv图表数据
      // 注册节点
      // 自定义节点 数据
      Graph.registerNode(
        "dag-node",
        {
          inherit: "vue-shape",
          width: 180,
          height: 36,
          component: {
            template: `<database />`,
            components: {
              database,
            },
          },
          ports: {
            groups: {
              top: {
                position: "top",
                attrs: {
                  circle: {
                    r: 4,
                    magnet: true,
                    stroke: "#C2C8D5",
                    strokeWidth: 1,
                    fill: "#fff",
                  },
                },
              },
              bottom: {
                position: "bottom",
                attrs: {
                  circle: {
                    r: 4,
                    magnet: true,
                    stroke: "#C2C8D5",
                    strokeWidth: 1,
                    fill: "#fff",
                  },
                },
              },
            },
          },
        },
        true
      );
      // 自定义节点  判断
      Graph.registerNode(
        "dag-condition",
        {
          inherit: "vue-shape",
          width: 180,
          height: 36,
          component: {
            template: `<condition />`,
            components: {
              condition,
            },
          },
          ports: {
            groups: {
              top: {
                position: "top",
                attrs: {
                  circle: {
                    r: 4,
                    magnet: true,
                    stroke: "#C2C8D5",
                    strokeWidth: 1,
                    fill: "#fff",
                  },
                },
              },
              bottom: {
                position: "bottom",
                attrs: {
                  circle: {
                    r: 4,
                    magnet: true,
                    stroke: "#C2C8D5",
                    strokeWidth: 1,
                    fill: "#fff",
                  },
                },
              },
            },
          },
        },
        true
      );
      // 自定义节点  输出
      Graph.registerNode(
        "dag-output",
        {
          inherit: "vue-shape",
          width: 180,
          height: 36,
          component: {
            template: `<onlyout />`,
            components: {
              onlyout,
            },
          },
          ports: {
            groups: {
              top: {
                position: "top",
                attrs: {
                  circle: {
                    r: 4,
                    magnet: true,
                    stroke: "#C2C8D5",
                    strokeWidth: 1,
                    fill: "#fff",
                  },
                },
              },
              bottom: {
                position: "bottom",
                attrs: {
                  circle: {
                    r: 4,
                    magnet: true,
                    stroke: "#C2C8D5",
                    strokeWidth: 1,
                    fill: "#fff",
                  },
                },
              },
            },
          },
        },
        true
      );
      // 自定义节点  输入
      Graph.registerNode(
        "dag-onlyIn",
        {
          inherit: "vue-shape",
          width: 180,
          height: 36,
          component: {
            template: `<onlyin />`,
            components: {
              onlyin,
            },
          },
          ports: {
            groups: {
              top: {
                position: "top",
                attrs: {
                  circle: {
                    r: 4,
                    magnet: true,
                    stroke: "#C2C8D5",
                    strokeWidth: 1,
                    fill: "#fff",
                  },
                },
              },
              bottom: {
                position: "bottom",
                attrs: {
                  circle: {
                    r: 4,
                    magnet: true,
                    stroke: "#C2C8D5",
                    strokeWidth: 1,
                    fill: "#fff",
                  },
                },
              },
            },
          },
        },
        true
      );
      // 自定义 边
      Graph.registerEdge(
        "dag-edge",
        {
          inherit: "edge",
          attrs: {
            line: {
              stroke: "#C2C8D5",
              strokeWidth: 2,
              targetMarker: {
                name: "block",
                width: 12,
                height: 8,
              },
            },
          },
        },
        true
      );
      // 自定义 线
      Graph.registerConnector(
        "algo-connector",
        (s, e) => {
          const offset = 4;
          const deltaY = Math.abs(e.y - s.y);
          const control = Math.floor((deltaY / 3) * 2);

          const v1 = { x: s.x, y: s.y + offset + control };
          const v2 = { x: e.x, y: e.y - offset - control };

          return Path.normalize(
            `M ${s.x} ${s.y}
           L ${s.x} ${s.y + offset}
           C ${v1.x} ${v1.y} ${v2.x} ${v2.y} ${e.x} ${e.y - offset}
           L ${e.x} ${e.y}
          `
          );
        },
        true
      );

      // 初始化画布
      // const graph = new Graph({
      //   container: document.getElementById("wrapper"),
      //   ...configSetting(Shape),
      // });
      const graph = new Graph({
        grid: {
          size: 10,
          visible: true,
          type: "dot", // 'dot' | 'fixedDot' | 'mesh'
          args: {
            color: "#a05410", // 网格线/点颜色
            thickness: 1, // 网格线宽度/网格点大小
          },
        },
        background: {
          color: "#fffbe6", // 设置画布背景颜色
        },
        container: document.getElementById("wrapper"),
        panning: {
          enabled: true,
          eventTypes: ["leftMouseDown", "mouseWheel"],
        },
        mousewheel: {
          enabled: true,
          modifiers: "ctrl",
          factor: 1.1,
          maxScale: 1.5,
          minScale: 0.5,
        },
        highlighting: {
          magnetAdsorbed: {
            name: "stroke",
            args: {
              attrs: {
                fill: "#fff",
                stroke: "#31d0c6",
                strokeWidth: 4,
              },
            },
          },
        },
        connecting: {
          snap: true,
          allowBlank: false,
          allowLoop: false,
          highlight: true,
          connector: "algo-connector",
          connectionPoint: "anchor",
          anchor: "center",
          validateMagnet({ magnet }) {
            // return magnet.getAttribute('port-group') !== 'top'

            // 限制连线配置
            return true;
          },
          createEdge() {
            return graph.createEdge({
              shape: "dag-edge",
              attrs: {
                line: {
                  strokeDasharray: "5 5",
                  targetMarker: {
                    name: "block",
                    width: 12,
                    height: 8,
                  },
                },
              },
              zIndex: -1,
            });
          },
        },
        selecting: {
          enabled: true,
          multiple: true,
          rubberEdge: true,
          rubberNode: true,
          modifiers: "shift",
          rubberband: true,
        },
        keyboard: true,
        clipboard: true,
        history: true,
      });
      console.log("初始画布graph :", graph);

      // 画布事件
      graph.on("node:mouseenter", () => {
        this.changePortsShow(true);
      });
      graph.on("node:mouseleave", () => {
        if (this.isPortsShow) return;
        this.changePortsShow(false);
      });
      // 点击编辑
      graph.on("cell:click", ({ cell }) => {
        // console.log('点击编辑',cell);
        this.editForm(cell);
      });
      // 画布键盘事件
      graphBindKey(graph);
      // 删除
      graph.bindKey(["delete", "backspace"], () => {
        this.handlerDel();
      });
      // 赋值
      this.graph = graph;
      // 返现方法
      // if (this.value && JSON.parse(this.value).length) {
      // const resArr = JSON.parse(this.value);
      // 导出的时候删除了链接桩设置加回来
      const portsGroups = configNodePorts().groups;
      if (this.graphJson.length) {
        const jsonTemp = this.graphJson.map((item) => {
          if (item.ports) item.ports.groups = portsGroups;
          return item;
        });
        graph.fromJSON(jsonTemp);
      }
      // }
      // 画布有变化
      graph.on("cell:changed", () => {
        this.isChangeValue();
      });
      //console.log("111", this.initGraph.fromJSON(this.ruleForm));
    },
    // 预设节点
    menuDrag(type) {
      console.log("预设节点", type, "模拟请求的数据", this.ruleForm);
      // 节点预设类型选择 configNodeShape
      this.menuItem = configNodeShape(type);
    },
    // 画布是否有变动
    isChangeValue() {
      if (!this.isChange) {
        this.isChange = true;
        this.$emit("cellChanged", true);
      }
    },
    // 点击图标内容,打开详情设置
    editForm(cell) {
      console.log("cell", cell);

      if (this.selectCell) this.selectCell.removeTools(); // 删除修改线的工具
      this.selectCell = cell;
      // 编辑node节点
      if (
        cell.isNode() &&
        cell.data.type &&
        cell.data.type.includes("default")
      ) {
        // 判断有没有数据，打开详情或首页
        if (cell.attrs.label.text == "") {
          this.prostate = false;
        } else {
          this.prostate = true;
        }
        // 输入节点 _model.incomings:[输入连接线id]
        // 输出节点  _model.outgoings:[输出连接线id]
        let incomings = cell._model.incomings;
        let incomingsLineId = []; // 输入连接线id
        let isIncomings = false; // 是否是输入点
        for (const key in incomings) {
          if (Object.hasOwnProperty.call(incomings, key)) {
            console.log("是否是输入点", cell.id === key);
            if (cell.id === key) {
              incomingsLineId = incomings[key];
              isIncomings = true;
              break;
            }
          }
        }
        //  获取输入列表
        let inputList = [];
        if (isIncomings) {
          incomingsLineId.forEach((item) => {
            let datajson = localStorage.getItem(item);
            // console.log('localStorage formData', datajson)
            if (datajson) {
              inputList.push(JSON.parse(datajson));
              console.log("inputList", inputList);
            }
          });
        }
        // console.log(
        //   "编辑node节点,数据：",
        //   cell.attrs.label.textarea,
        //   "ID",
        //   cell.id
        // );

        console.log("编辑node节点,数据：", cell);
        // 数据赋值

        this.editTitle = "编辑节点";
        const body =
          cell.attrs.body ||
          cell.attrs.rect ||
          cell.attrs.polygon ||
          cell.attrs.circle;
        // this.form = {
        //   id: cell.id, //id
        //   labelText: cell.attrs.label.text || "", //文本
        //   fontSize: cell.attrs.label.fontSize || 14, //字体大小
        //   fontFill: cell.attrs.label.fill || "", //文字颜色
        //   fill: body.fill || "", //背景颜色
        //   stroke: body.stroke || "", //字体颜色
        // };
        this.ruleForm = {
          appname: cell.attrs.label.text || "", //标题
          id: cell.id, //id
          // 这里只保留标题的内容，antv配置项不建议过多配置数据
        };
        return (this.editDrawer = true);
      }
      // 编辑线
      if (!cell.isNode() && cell.shape === "edge") {
        this.prostate = "line";
        // 判断是否有缓存 暂时这样写
        let formData = localStorage.getItem(cell.id);
        if (formData) {
          formData = JSON.parse(formData);
        } else {
          formData = {
            output: "",
          };
          // localStorage.setItem(cell.id, JSON.stringify(formData))
        }
        // console.log('编辑连线', cell, cell.id, cell.source, formData)
        // cell.id 连接点
        // cell.source.cell连接线的起始点的id   cell.source.port接线的起始点的连接点
        // cell.target.cell连接线的目标点的id   cell.target.port接线的目标点的连接点
        this.editTitle = "编辑连线";

        this.ruleForm = {
          // appname: cell.attrs.label.text || "", //标题
          id: cell.id, //id
          ...formData,
          id: cell.id,
          label:
            cell.labels && cell.labels[0]
              ? cell.labels[0].attrs.labelText.text
              : "",
          stroke: cell.attrs.line.stroke || "",
          connector: "rounded",
          strokeWidth: cell.attrs.line.strokeWidth || "",
          isArrows: cell.attrs.line.sourceMarker ? true : false,
          isAnit: cell.attrs.line.strokeDasharray ? true : false,
          isTools: false,
          // 这里只保留标题的内容，antv配置项不建议过多配置数据
        };

        console.log("编辑连线", this.ruleForm);
        this.ruleForm.appidline = this.ruleForm.id;

        // 看是否有label
        const edgeCellLabel =
          (cell.labels && cell.labels[0] && cell.labels[0].attrs) || false;
        if (this.form.label && edgeCellLabel) {
          this.labelForm = {
            fontColor: edgeCellLabel.labelText.fill || "#333",
            fill: edgeCellLabel.labelBody.fill || "#fff",
            stroke: edgeCellLabel.labelBody.stroke || "#555",
          };
        } else {
          this.labelForm = { fontColor: "#333", fill: "#FFF", stroke: "#555" };
        }
        return (this.editDrawer = true);
      }
    },
    // 拖拽事件
    drop(event) {
      // 拖拽生成图形时 生成额外数据
      const nodeItem = {
        ...this.menuItem,
        x: event.offsetX - this.menuItem.width / 2,
        y: event.offsetY - this.menuItem.height / 2,
        // ports: configNodePorts(),
      };
      console.log("graph", this.graph, "nodeItem 123", nodeItem, event);
      // 创建节点
      let cesItem = this.nodeConfig(nodeItem);
      this.graph.addNode(cesItem);
      this.isChangeValue();
    },
    nodeConfig(item) {
      let config = "";
      const time = new Date().getTime();
      console.log("判断item", item);

      // 链接桩3种状态 1、in | 只允许被连  2、out | 只允许输出  3、any | 不限制
      switch (item.data.type) {
        case "output":
          config = {
            x: item.x,
            y: item.y,
            width: 180,
            height: 40,
            shape: "dag-output",
            data: item.data,
            ports: {
              groups: {
                bottom: {
                  position: "bottom",
                  attrs: {
                    circle: {
                      r: 4,
                      magnet: true,
                      stroke: "#C2C8D5",
                      strokeWidth: 1,
                      fill: "#fff",
                    },
                  },
                },
              },
              items: [
                {
                  id: `out-${time}`,
                  group: "bottom", // 指定分组名称
                },
              ],
            },
          };
          break;
        case "onlyIn":
          config = {
            // x: x,
            // y: y,
            width: 180,
            height: 40,
            shape: "dag-onlyIn",
            data: item,
            ports: {
              groups: {
                top: {
                  position: "top",
                  attrs: {
                    circle: {
                      r: 4,
                      magnet: true,
                      stroke: "#C2C8D5",
                      strokeWidth: 1,
                      fill: "#fff",
                    },
                  },
                },
              },
              items: [
                {
                  id: `in-${time}`,
                  group: "top", // 指定分组名称
                },
              ],
            },
          };
          break;
        case "database":
          config = {
            // x: x,
            // y: y,
            width: 180,
            height: 40,
            shape: "dag-node",
            data: item,
            ports: {
              groups: {
                top: {
                  position: "top",
                  attrs: {
                    circle: {
                      r: 4,
                      magnet: true,
                      stroke: "#C2C8D5",
                      strokeWidth: 1,
                      fill: "#fff",
                    },
                  },
                },
                bottom: {
                  position: "bottom",
                  attrs: {
                    circle: {
                      r: 4,
                      magnet: true,
                      stroke: "#C2C8D5",
                      strokeWidth: 1,
                      fill: "#fff",
                    },
                  },
                },
              },
              items: [
                {
                  id: `in-${time}`,
                  group: "top", // 指定分组名称
                },
                {
                  id: `out-${time}`,
                  group: "bottom", // 指定分组名称
                },
              ],
            },
          };
          break;
        case "condition":
          config = {
            width: 180,
            height: 40,
            shape: "dag-condition",
            data: item,
            ports: {
              groups: {
                top: {
                  position: "top",
                  attrs: {
                    circle: {
                      r: 4,
                      magnet: true,
                      stroke: "#C2C8D5",
                      strokeWidth: 1,
                      fill: "#fff",
                    },
                  },
                },
                bottom: {
                  position: "bottom",
                  attrs: {
                    circle: {
                      r: 4,
                      magnet: true,
                      stroke: "#C2C8D5",
                      strokeWidth: 1,
                      fill: "#fff",
                    },
                  },
                },
              },
              items: [
                {
                  id: `in-${time}`,
                  group: "top", // 指定分组名称
                },
                {
                  id: `out-${time}`,
                  group: "bottom", // 指定分组名称
                },
                {
                  id: `out-${time}-2`,
                  group: "bottom", // 指定分组名称
                },
              ],
            },
          };
          break;
        default:
          config = item;
          break;
      }
      console.log("判断item config", config);
      return config;
    },
    // 链接桩的显示与隐藏，主要是照顾菱形
    changePortsShow(val) {
      const container = document.getElementById("wrapper");
      const ports = container.querySelectorAll(".x6-port-body");
      for (let i = 0, len = ports.length; i < len; i = i + 1) {
        ports[i].style.visibility = val ? "visible" : "hidden";
      }
    },
    // 删除节点
    handlerDel() {
      this.$confirm(
        `此操作将永久删除此${
          this.editTitle === "编辑节点" ? "节点" : "连线"
        }, 是否继续?`,
        "提示",
        {
          confirmButtonText: "确定",
          cancelButtonText: "取消",
          type: "warning",
        }
      )
        .then(() => {
          const cells = this.graph.getSelectedCells();
          console.log("测试删除", cells);
          if (cells.length) {
            // 删除前清除数据
            // cells.forEach((item) => {
            //   // console.log('获取id',item.id);
            //   localStorage.removeItem(item.id);
            // });
            this.graph.removeCells(cells);
            this.form = {};
            this.editDrawer = false;
            this.$message({ type: "success", message: "删除成功!" });
            this.ruleForm = null; //清空数据
          }
        })
        .catch(() => {});
    },
    // 确认/导出
    handlerSend(formName) {
      console.log(formName);
      // 我在这里删除了链接桩的设置，和工具（为了减少数据），反显的时候要把删除的链接桩加回来
      if (formName) {
        this.$refs[formName].validate((valid) => {
          if (valid) {
            const { cells: jsonArr } = this.graph.toJSON();
            const tempGroupJson = jsonArr.map((item) => {
              if (item.ports && item.ports.groups) delete item.ports.groups;
              if (item.tools) delete item.tools;
              return item;
            });
            console.log("tempGroupJson", tempGroupJson);
            if (this.selectCell) {
              this.selectCell.removeTools();
              this.selectCell = "";
            }
            this.$emit("finish", JSON.stringify(tempGroupJson));
            console.log(JSON.stringify(tempGroupJson)); //antv数据
            console.log(this.ruleForm); //总页面配置数据
          }
        });
      }
    },
    changeNode(type, value) {
      switch (type) {
        case "labelText":
          this.selectCell.attr("label/text", value);
          break;
      }
    },
  },
};
</script>
<style scoped lang="scss">
.application {
  // 穿透修改按钮颜色和间距，card间距
  ::v-deep .el-card {
    margin: 0.625rem;
  }
  ::v-deep .el-button--danger {
    background-color: #7266ba;
    border: transparent;
  }
  .nav {
    // border-bottom: .0625rem solid #cfdbe2;
    display: flex;
    align-items: center;
    justify-content: center;

    .shoptit {
      // font-size: 1.5rem;
      background-color: #ffffff;
      color: #929292;
      font-weight: normal;
      // padding: .9375rem;
    }
    .discover {
      color: #37bc9b;
    }
  }
  .main {
    .preservation {
      background-color: #f5f7fa;
      padding: 0.625rem 0.9375rem;
      display: flex;
      align-items: center;
      justify-content: flex-end;
    }
    // 添加流程区块
    .addArea {
      width: 100%;
      height: 3.125rem;
      border-radius: 0.25rem;
      margin: 0.625rem;
      display: flex;
      align-items: center;
      justify-content: flex-start;
      cursor: pointer;

      .addAreain {
        border: 0.0625rem solid rgba(0, 0, 0, 0.12);
        width: 6.25rem;
        height: 3.125rem;
        line-height: 3.125rem;
        text-align: center;
        margin: 0;
        margin-right: 0.625rem;
        border-radius: 0.3125rem;
      }
      span {
        font-size: 1rem;
        font-weight: bold;
      }
    }
    .addAreain:hover {
      background-color: #37bc9b;
      color: white;
    }
    .predetail {
      margin-top: 0.625rem;
    }
    .mainTitle {
      display: flex;
      align-items: center;
      justify-content: flex-start;
      padding: 0.625rem;
    }
    .mainCenter {
      padding: 0.625rem;
    }
    // 详情部分
    .iptArea {
      display: flex;
      flex-direction: column;
      align-items: flex-start;
      justify-content: flex-end;
      margin-bottom: 0.625rem;
      display: flex;
      flex-direction: column;

      span {
        font-weight: bold;
        font-family: "Source Sans Pro";
        color: #656565;
        display: inline-block;
        margin-bottom: 0.625rem;
      }
    }
    .repeatArea {
      margin-bottom: 0.625rem;

      span {
        font-weight: bold;
        font-family: "Source Sans Pro";
        color: #656565;
        display: inline-block;
      }
    }
    .graycolor {
      margin-top: 0.3125rem;
      margin-bottom: 0.625rem;
      color: #909293;
      font-size: 0.75rem;
    }
    .prepri {
      display: flex;
      align-items: flex-start;
      justify-content: flex-start;
      .process {
        width: 26vw;

        .editsmall {
          span {
            font-weight: bold;
          }
          .shopping {
            color: #37bc9b;
            cursor: pointer;
          }
        }
        .editbig {
          color: #656565;
          h2 {
            font-size: 1.125rem;
            margin: 0.625rem 0;
          }
          .check {
            display: flex;
            font-size: 0.875rem;
            margin: 0.625rem 0;

            .checkl {
              display: flex;
              align-items: center;
              p {
                font-weight: bold;
              }
            }
            .checkr {
              display: flex;
              align-items: center;
              span {
                color: #37bc9b;
                padding: 0 0.9375rem;
              }
              p {
                color: gray;
              }
            }
            .itemtit {
              font-weight: bold;
            }
          }
          .cessout {
            display: flex;
            flex-direction: column;
            align-items: flex-start;
            justify-content: space-between;
            .cess {
              display: flex;
              align-items: center;
              margin: 0.625rem 0;
              span {
                font-weight: bold;
                font-size: 0.875rem;
              }
              p {
                font-weight: normal;
                color: gray;
                font-size: 0.75rem;
              }
            }
          }
          .elform {
            display: flex;
            flex-direction: column;
            .itemtit {
              font-family: "Source Sans Pro", sans-serif;
              color: #656565;
              font-size: 0.875rem;
              font-weight: bold;
              margin-bottom: 0.625rem;
            }
          }
        }
        // 编辑线的位置
        .editline {
        }
      }
      // ANTV区域
      .antv-wrapper {
        width: 48vw;
        height: 100%;
        border: #37bc9b 0.0625rem solid;
        position: relative;
        .wrapper-canvas {
          position: relative;
          height: 100vh;
          min-height: 45rem;
        }
        .wrapper-tips {
          padding: 0.625rem;
          display: flex;
          align-items: center;
          position: absolute;
          top: 0;
          left: 0;
          .wrapper-tips-item {
            span {
              padding-left: 0.625rem;
              font-size: 0.75rem;
            }
          }
        }
      }
    }
  }
}
</style>
<style scoped>
.main >>> .el-input {
  margin: 0rem 0.625rem;
  width: 18.75rem;
}
.main >>> .el-textarea {
  margin: 0rem 0.625rem;
  width: 98%;
}
/* 折叠面板 */
.main >>> .el-collapse-item__header {
  /* background-color: ; */
  background-image: linear-gradient(to right, #37bc9b 0%, #58ceb1 100%);
  color: white;
  padding-left: 0.9375rem;
  font-weight: bold;
}
/* 单选框 */
.main >>> .el-radio__label {
  font-weight: bold;
  font-family: "Source Sans Pro";
  color: #656565;
  display: inline-block;
}
.main >>> .el-radio__input {
  margin-left: 0.625rem;
}
/* 按钮 */
.repeatArea >>> .el-button--success {
  border-color: #37bc9b;
  color: #37bc9b;
  margin-left: 0.625rem;
}
.main >>> .el-tabs__content {
  margin-left: 1.25rem;
}
.elform >>> .el-form-item__content {
  margin: 0 !important;
}
/* 流程部分 */
.elform >>> .el-textarea {
  width: 25.625rem;
}
</style>