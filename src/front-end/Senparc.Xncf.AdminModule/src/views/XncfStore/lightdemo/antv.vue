<template>
  <div class="all">
    <!-- 头部 -->
    <!-- <div class="antv-head">
      <el-tooltip class="item" effect="dark" content="项目" placement="bottom">
        <i class="el-icon-menu" @click="showDrawerFn()" />
      </el-tooltip>
      <el-tooltip class="item" effect="dark" content="长按shift多选" placement="bottom">
        <i class="el-icon-crop" />
      </el-tooltip>
      <el-tooltip class="item" effect="dark" content="放大" placement="bottom">
        <i class="el-icon-zoom-in" @click="zoomFn(0.2)" />
      </el-tooltip>
      <el-tooltip class="item" effect="dark" content="缩小" placement="bottom">
        <i class="el-icon-zoom-out" @click="zoomFn(-0.2)" />
      </el-tooltip>
      <el-tooltip class="item" effect="dark" content="适应屏幕" placement="bottom">
        <i class="el-icon-full-screen" @click="centerFn" />
      </el-tooltip>
      <el-tooltip class="item" effect="dark" content="执行" placement="bottom">
        <i class="el-icon-video-play" @click="startFn()" />
      </el-tooltip>
      <el-tooltip class="item" effect="dark" content="保存" placement="bottom">
        <i class="el-icon-upload" @click="saveFn()" />
      </el-tooltip>
      <el-tooltip class="item" effect="dark" content="加载保存页面" placement="bottom">
        <i class="el-icon-link" @click="loadFn()" />
      </el-tooltip>
      <el-tooltip class="item" effect="dark" content="是否禁用" placement="bottom">
        <i :class="{'el-icon-lock':isLock, 'el-icon-unlock':!isLock}" @click="lockFn()" />
      </el-tooltip>
    </div> -->
    <!-- 主体内容 -->
    <div class="antv-content">
      <!-- 左侧基础图形 -->
      <div class="antv-menu">
        <h3>基础图形列表</h3>
        <ul class="menu-list">
          <li draggable="true" @drag="menuDrag('defaultOval')">
            <i class="icon-oval"></i> <strong>椭圆形</strong>
          </li>
          <li draggable="true" @drag="menuDrag('defaultSquare')">
            <i class="icon-square"></i><strong>矩形</strong>
          </li>
          <li draggable="true" @drag="menuDrag('defaultYSquare')">
            <i class="icon-ysquare"></i><strong>圆角矩形</strong>
          </li>
          <li draggable="true" @drag="menuDrag('defaultRhombus')">
            <i class="icon-rhombus"></i><strong>菱形</strong>
          </li>
          <li draggable="true" @drag="menuDrag('defaultRhomboid')">
            <i class="icon-rhomboid"></i><strong>平行四边形</strong>
          </li>
          <li draggable="true" @drag="menuDrag('defaultCircle')">
            <i class="icon-circle"></i><strong>圆形</strong>
          </li>
          <li draggable="true" @drag="menuDrag('otherImage')">
            <i class="el-icon-picture"></i><strong>图片</strong>
          </li>
        </ul>
        <div class="wrapper-btn" v-if="isChange">
          <el-button type="success" @click="handlerSend"
            >保存当前方案</el-button
          >
        </div>
      </div>
      <!-- 中间 区域 -->
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
      <!-- 右侧编辑 -->
      <div v-if="editDrawer" class="edit-main">
        <div class="edit-main-title">
          <h3>{{ editTitle }}</h3>
          <i class="el-icon-close" @click="closeEditForm"></i>
        </div>
        <div v-if="editTitle === '编辑节点'" class="form-main">
          <el-form ref="nodeForm" :model="form" label-width="80px">
            <el-form-item label="节点文本">
              <el-input
                v-model="form.labelText"
                size="small"
                @input="changeNode('labelText', form.labelText)"
              ></el-input>
            </el-form-item>
            <el-form-item label="字体大小">
              <el-input
                v-model="form.fontSize"
                size="small"
                @input="changeNode('fontSize', form.fontSize)"
              ></el-input>
            </el-form-item>
            <el-form-item label="字体颜色">
              <el-color-picker
                v-model="form.fontFill"
                @change="changeNode('fontFill', form.fontFill)"
              ></el-color-picker>
            </el-form-item>
            <el-form-item label="节点背景">
              <el-color-picker
                v-model="form.fill"
                @change="changeNode('fill', form.fill)"
              ></el-color-picker>
            </el-form-item>
            <el-form-item label="边框颜色">
              <el-color-picker
                v-model="form.stroke"
                @change="changeNode('stroke', form.stroke)"
              ></el-color-picker>
            </el-form-item>
            <div class="see-box">
              <h5>预览</h5>
              <div
                class="see-item"
                :style="{
                  background: form.fill,
                  color: form.fontFill,
                  'border-color': form.stroke,
                  'font-size': form.fontSize + 'px',
                }"
              >
                {{ form.labelText }}
              </div>
            </div>
            <!-- inputList -->
            <el-form-item
              v-for="(item, index) in form.inputList"
              :key="index"
              :label="'输入接口' + (index + 1)"
            >
              <el-input
                v-model="form.inputList[index].output"
                disabled
                size="small"
                placeholder="输入接口数据"
                @input="saveOutput"
              ></el-input>
            </el-form-item>
          </el-form>
        </div>
        <div v-if="editTitle === '编辑图片节点'" class="form-main">
          <el-form ref="imageForm" :model="form" label-width="80px">
            <el-form-item label="节点文本">
              <el-input
                v-model="form.labelText"
                size="small"
                @input="changeImageNode('labelText', form.labelText)"
              ></el-input>
            </el-form-item>
            <el-form-item label="字体颜色">
              <el-color-picker
                v-model="form.labelFill"
                @change="changeImageNode('labelFill', form.labelFill)"
              ></el-color-picker>
            </el-form-item>
            <el-form-item label="节点背景">
              <el-color-picker
                v-model="form.fill"
                @change="changeImageNode('fill', form.fill)"
              ></el-color-picker>
            </el-form-item>
            <el-form-item label="图片地址">
              <el-input
                v-model="form.xlinkHref"
                size="small"
                placeholder="图片地址"
                @input="changeImageNode('xlinkHref', form.xlinkHref)"
              ></el-input>
              <el-image
                :src="form.xlinkHref"
                style="width: 80px; height: 80px; background: #f2f2f2"
                fit="fill"
              ></el-image>
            </el-form-item>
            <el-form-item label="图片尺寸">
              <span style="font-size: 14px; padding-right: 5px; color: #888"
                >宽</span
              >
              <el-input-number
                v-model="form.width"
                :min="0"
                label="宽"
                size="mini"
                @change="changeImageNode('width', form.width)"
              ></el-input-number>
              <span style="font-size: 14px; padding-right: 5px; color: #888"
                >高</span
              >
              <el-input-number
                v-model="form.height"
                :min="0"
                label="高"
                size="mini"
                @change="changeImageNode('height', form.height)"
              ></el-input-number>
            </el-form-item>
            <!-- inputList -->
            <el-form-item
              v-for="(item, index) in form.inputList"
              :key="index"
              :label="'输入接口' + index"
            >
              <el-input
                v-model="form.inputList[index].output"
                disabled
                size="small"
                placeholder="输入接口数据"
                @input="saveOutput"
              ></el-input>
            </el-form-item>
          </el-form>
        </div>
        <div v-if="editTitle === '编辑连线'" class="form-main">
          <el-form ref="edgeForm" :model="form" label-width="80px">
            <el-form-item label="标签内容">
              <el-input
                v-model="form.label"
                size="small"
                placeholder="标签文字，空则没有"
                @input="
                  changeEdgeLabel(
                    form.label,
                    labelForm.fontColor,
                    labelForm.fill,
                    labelForm.stroke
                  )
                "
              ></el-input>
              <div v-if="form.label" class="label-style">
                <p>
                  字体颜色：<el-color-picker
                    v-model="labelForm.fontColor"
                    size="mini"
                    @change="
                      changeEdgeLabel(
                        form.label,
                        labelForm.fontColor,
                        labelForm.fill,
                        labelForm.stroke
                      )
                    "
                  ></el-color-picker>
                </p>
                <p>
                  背景颜色：<el-color-picker
                    v-model="labelForm.fill"
                    size="mini"
                    @change="
                      changeEdgeLabel(
                        form.label,
                        labelForm.fontColor,
                        labelForm.fill,
                        labelForm.stroke
                      )
                    "
                  ></el-color-picker>
                </p>
                <p>
                  描边颜色：<el-color-picker
                    v-model="labelForm.stroke"
                    size="mini"
                    @change="
                      changeEdgeLabel(
                        form.label,
                        labelForm.fontColor,
                        labelForm.fill,
                        labelForm.stroke
                      )
                    "
                  ></el-color-picker>
                </p>
              </div>
            </el-form-item>
            <el-form-item label="线条颜色">
              <el-color-picker
                v-model="form.stroke"
                size="small"
                @change="changeEdgeStroke"
              ></el-color-picker>
            </el-form-item>
            <el-form-item label="线条样式">
              <el-select
                v-model="form.connector"
                size="small"
                placeholder="请选择"
                @change="changeEdgeConnector"
              >
                <el-option label="直角" value="normal"></el-option>
                <el-option label="圆角" value="rounded"></el-option>
                <el-option label="平滑" value="smooth"></el-option>
                <el-option label="跳线(两线交叉)" value="jumpover"></el-option>
              </el-select>
            </el-form-item>
            <el-form-item label="线条宽度">
              <el-input-number
                v-model="form.strokeWidth"
                size="small"
                @change="changeEdgeStrokeWidth"
                :min="2"
                :step="2"
                :max="6"
                label="线条宽度"
              ></el-input-number>
            </el-form-item>
            <!-- <el-form-item label="双向箭头">
              <el-switch v-model="form.isArrows" @change="changeEdgeArrows"></el-switch>
            </el-form-item> -->
            <el-form-item label="流动线条">
              <el-switch
                v-model="form.isAnit"
                @change="changeEdgeAnit"
              ></el-switch>
            </el-form-item>
            <!-- <el-form-item label="调整线条">
              <el-switch v-model="form.isTools" @change="changeEdgeTools"></el-switch>
            </el-form-item> -->
            <el-form-item label="输出接口">
              <el-input
                v-model="form.output"
                size="small"
                placeholder="输出接口数据"
                @input="saveOutput"
              ></el-input>
            </el-form-item>
          </el-form>
        </div>
        <div class="edit-btn">
          <el-button type="danger" @click="handlerDel" style="width: 100%"
            >删除此{{ editTitle === "编辑节点" ? "节点" : "连线" }}</el-button
          >
        </div>
      </div>
    </div>
  </div>
</template>
<script>
import { Graph, Shape } from "@antv/x6";
import {
  configSetting,
  configNodeShape,
  configNodePorts,
  configEdgeLabel,
  graphBindKey,
} from "@/utils/antvSetting";
export default {
  name: "AntV6X",
  /**
   * 这个是作为子组件分别接受了两个数据一个是高度height，一个是反显图表数据tempGroupJson
   * 作为子组件例子 <AntVXSix v-model="tempGroupJson" height="720px" />
   *
   */
  props: {
    height: {
      type: String,
      default: "100vh", //'720px'
    },
    value: {
      type: String,
      default: "",
    },
  },
  data() {
    return {
      graph: null,
      isChange: false,
      isPortsShow: false,
      menuItem: "",
      selectCell: "",
      editDrawer: false,
      editTitle: "",
      form: {},
      labelForm: {
        fontColor: "#333",
        fill: "#FFF",
        stroke: "#555",
      },
      isLock: true, // 画布是否锁定(拖动) 默认不可拖动
    };
  },
  created() {},
  watch: {
    value: {
      handler: function () {
        if (this.graph) {
          this.isChange = false;
          this.isPortsShow = false;
          this.menuItem = "";
          this.selectCell = "";
          this.editDrawer = false;
          this.graph.dispose();
          this.initGraph();
        }
      },
      deep: true,
      immediate: true,
    },
  },
  mounted() {
    this.initGraph();
  },
  beforeDestroy() {
    this.graph.dispose();
  },
  methods: {
    // 缩放 画布尺寸
    zoomFn(num) {
      this.graph.zoom(num);
    },
    // 适应屏幕 画布尺寸
    centerFn() {
      console.log(this.graph.zoom());
      const num = 1 - this.graph.zoom();
      num > 1 ? this.graph.zoom(num * -1) : this.graph.zoom(num);
      this.graph.centerContent();
    },
    // 执行
    // startFn(item) {
    //   this.timer && clearTimeout(this.timer)
    //   this.init(item || DataJson)
    //   this.showNodeStatus(Object.assign([], nodeStatusList))
    //   this.graph.centerContent()
    // },
    // 初始化节点/边
    init(data = []) {
      const cells = [];
      data.forEach((item) => {
        if (item.shape === "dag-edge") {
          cells.push(this.graph.createEdge(item));
        } else {
          delete item.component;
          cells.push(this.graph.createNode(item));
        }
      });
      this.graph.resetCells(cells);
    },
    showNodeStatus(statusList) {
      const status = statusList.shift();
      if (status) {
        status.forEach((item) => {
          const { id, status } = item;
          const node = this.graph.getCellById(id);
          const data = node.getData();
          node.setData({
            ...data,
            status: status,
          });
        });
      }

      this.timer = setTimeout(() => {
        this.showNodeStatus(statusList);
      }, 300);
    },
    // 是否允许画布 可拖动
    lockFn() {
      this.isLock = !this.isLock;
      console.log(this.isLock);
      if (this.isLock) {
        this.graph.enablePanning();
        this.graph.enableKeyboard();
      } else {
        this.graph.disablePanning();
        this.graph.disableKeyboard();
      }
    },
    // 链接桩的显示与隐藏，主要是照顾菱形
    changePortsShow(val) {
      const container = document.getElementById("wrapper");
      const ports = container.querySelectorAll(".x6-port-body");
      for (let i = 0, len = ports.length; i < len; i = i + 1) {
        ports[i].style.visibility = val ? "visible" : "hidden";
      }
    },
    // 初始化渲染画布
    initGraph() {
      const graph = new Graph({
        container: document.getElementById("wrapper"),
        ...configSetting(Shape),
      });
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
      if (this.value && JSON.parse(this.value).length) {
        const resArr = JSON.parse(this.value);
        // 导出的时候删除了链接桩设置加回来
        const portsGroups = configNodePorts().groups;
        if (resArr.length) {
          const jsonTemp = resArr.map((item) => {
            if (item.ports) item.ports.groups = portsGroups;
            return item;
          });
          graph.fromJSON(jsonTemp);
        }
      }
      // 画布有变化
      graph.on("cell:changed", () => {
        this.isChangeValue();
      });
    },
    // 画布是否有变动
    isChangeValue() {
      if (!this.isChange) {
        this.isChange = true;
        this.$emit("cellChanged", true);
      }
    },
    menuDrag(type) {
      this.menuItem = configNodeShape(type);
    },
    drop(event) {
      // 拖拽生成图形时 生成额外数据
      const nodeItem = {
        ...this.menuItem,
        x: event.offsetX - this.menuItem.width / 2,
        y: event.offsetY - this.menuItem.height / 2,
        ports: configNodePorts(),
      };
      // console.log('nodeItem 123',nodeItem,event);
      // 创建节点
      this.graph.addNode(nodeItem);
      this.isChangeValue();
    },
    editForm(cell) {
      if (this.selectCell) this.selectCell.removeTools(); // 删除修改线的工具
      this.selectCell = cell;
      // 编辑node节点
      if (
        cell.isNode() &&
        cell.data.type &&
        cell.data.type.includes("default")
      ) {
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
            }
          });
        }
        console.log("编辑node节点", cell, cell.id);
        this.editTitle = "编辑节点";
        const body =
          cell.attrs.body ||
          cell.attrs.rect ||
          cell.attrs.polygon ||
          cell.attrs.circle;
        this.form = {
          id: cell.id,
          labelText: cell.attrs.label.text || "",
          fontSize: cell.attrs.label.fontSize || 14,
          fontFill: cell.attrs.label.fill || "",
          fill: body.fill || "",
          stroke: body.stroke || "",
          inputList: inputList || [],
        };
        return (this.editDrawer = true);
      }
      // 编辑图片节点
      if (cell.isNode() && cell.data.type && cell.data.type === "otherImage") {
        console.log("编辑图片节点", cell);
        this.editTitle = "编辑图片节点";
        const attrs = cell.attrs || {
          body: { fill: "" },
          label: { text: "", fill: "" },
          image: { xlinkHref: "", height: 80, width: 80 },
        };
        // 输入节点 _model.incomings:[输入连接线id]
        // 输出节点  _model.outgoings:[输出连接线id]
        let incomings = cell._model.incomings;
        let incomingsLineId = ""; // 输入连接线id
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
            }
          });
        }
        this.form = {
          id: cell.id,
          fill: attrs.body.fill,
          labelText: attrs.label.text,
          labelFill: attrs.label.fill,
          height: (attrs.image && attrs.image.height) || 80,
          width: (attrs.image && attrs.image.width) || 80,
          xlinkHref:
            (attrs.xlinkHref && attrs.image.xlinkHref) ||
            "https://gw.alipayobjects.com/zos/bmw-prod/2010ac9f-40e7-49d4-8c4a-4fcf2f83033b.svg",
          inputList: inputList || [],
        };
        return (this.editDrawer = true);
      }
      // 编辑线
      if (!cell.isNode() && cell.shape === "edge") {
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
        this.form = {
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
        };

        console.log("编辑连线", this.form);
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
    closeEditForm() {
      this.editDrawer = false;
      if (this.selectCell) this.selectCell.removeTools();
    },
    // 存储输出节点
    saveOutput() {
      localStorage.setItem(this.form.id, JSON.stringify(this.form));
    },
    // 修改一般节点
    changeNode(type, value) {
      switch (type) {
        case "labelText":
          this.selectCell.attr("label/text", value);
          break;
        case "fontSize":
          this.selectCell.attr("label/fontSize", value);
          break;
        case "fontFill":
          this.selectCell.attr("label/fill", value);
          break;
        case "fill":
          this.selectCell.attr("body/fill", value);
          break;
        case "stroke":
          this.selectCell.attr("body/stroke", value);
          break;
      }
    },
    // 修改图片节点
    changeImageNode(type, value) {
      switch (type) {
        case "labelText":
          this.selectCell.attr("label/text", value);
          break;
        case "labelFill":
          this.selectCell.attr("label/fill", value);
          break;
        case "fill":
          this.selectCell.attr("body/fill", value);
          break;
        case "xlinkHref":
          this.selectCell.attr("image/xlinkHref", value);
          break;
        case "height":
          this.selectCell.attr("image/height", value);
          break;
        case "width":
          this.selectCell.attr("image/width", value);
          break;
      }
    },
    // 修改边label属性
    changeEdgeLabel(label, fontColor, fill, stroke) {
      this.selectCell.setLabels([
        configEdgeLabel(label, fontColor, fill, stroke),
      ]);
      if (!label)
        this.labelForm = { fontColor: "#333", fill: "#FFF", stroke: "#555" };
    },
    // 修改边的颜色
    changeEdgeStroke(val) {
      this.selectCell.attr("line/stroke", val);
    },
    // 边的样式
    changeEdgeConnector(val) {
      switch (val) {
        case "normal":
          this.selectCell.setConnector(val);
          break;
        case "smooth":
          this.selectCell.setConnector(val);
          break;
        case "rounded":
          this.selectCell.setConnector(val, { radius: 20 });
          break;
        case "jumpover":
          this.selectCell.setConnector(val, { radius: 20 });
          break;
      }
    },
    // 边的宽度
    changeEdgeStrokeWidth(val) {
      if (this.form.isArrows) {
        this.selectCell.attr({
          line: {
            strokeWidth: val,
            sourceMarker: {
              width: 12 * (val / 2) || 12,
              height: 8 * (val / 2) || 8,
            },
            targetMarker: {
              width: 12 * (val / 2) || 12,
              height: 8 * (val / 2) || 8,
            },
          },
        });
      } else {
        this.selectCell.attr({
          line: {
            strokeWidth: val,
            targetMarker: {
              width: 12 * (val / 2) || 12,
              height: 8 * (val / 2) || 8,
            },
          },
        });
      }
    },
    // 边的箭头
    changeEdgeArrows(val) {
      if (val) {
        this.selectCell.attr({
          line: {
            sourceMarker: {
              name: "block",
              width: 12 * (this.form.strokeWidth / 2) || 12,
              height: 8 * (this.form.strokeWidth / 2) || 8,
            },
            targetMarker: {
              name: "block",
              width: 12 * (this.form.strokeWidth / 2) || 12,
              height: 8 * (this.form.strokeWidth / 2) || 8,
            },
          },
        });
      } else {
        this.selectCell.attr({
          line: {
            sourceMarker: "",
            targetMarker: {
              name: "block",
              size: 10 * (this.form.strokeWidth / 2) || 10,
            },
          },
        });
      }
    },
    // 边的添加蚂蚁线
    changeEdgeAnit(val) {
      if (val) {
        this.selectCell.attr({
          line: {
            strokeDasharray: 5,
            style: {
              animation: "ant-line 30s infinite linear",
            },
          },
        });
      } else {
        this.selectCell.attr({
          line: {
            strokeDasharray: 0,
            style: {
              animation: "",
            },
          },
        });
      }
    },
    // 给线添加调节工具
    changeEdgeTools(val) {
      if (val) this.selectCell.addTools(["vertices", "segments"]);
      else this.selectCell.removeTools();
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
          // console.log('测试删除',cells);
          if (cells.length) {
            // 删除前清除数据
            cells.forEach((item) => {
              // console.log('获取id',item.id);
              localStorage.removeItem(item.id);
            });
            this.graph.removeCells(cells);
            this.form = {};
            this.editDrawer = false;
            this.$message({ type: "success", message: "删除成功!" });
          }
        })
        .catch(() => {});
    },
    // 导出
    handlerSend() {
      // 我在这里删除了链接桩的设置，和工具（为了减少数据），反显的时候要把删除的链接桩加回来
      const { cells: jsonArr } = this.graph.toJSON();
      const tempGroupJson = jsonArr.map((item) => {
        if (item.ports && item.ports.groups) delete item.ports.groups;
        if (item.tools) delete item.tools;
        return item;
      });
      if (this.selectCell) {
        this.selectCell.removeTools();
        this.selectCell = "";
      }
      this.$emit("finish", JSON.stringify(tempGroupJson));
      console.log(JSON.stringify(tempGroupJson));
    },
  },
};
</script>
<style lang="scss">
@keyframes ant-line {
  to {
    stroke-dashoffset: -1000;
  }
}
</style>
<style lang="scss" scoped="scoped">
.all {
  border-radius: 8px;
  overflow: hidden;
  // 头部
  .antv-head {
  }
  // 内容
  .antv-content {
    background: #fff;
    display: flex;
    flex-direction: row;
    flex-wrap: nowrap;
    overflow: hidden;
    position: relative;
    // 左侧 侧栏
    .antv-menu {
      width: 200px;
      border-right: 1px solid #d5d5d5;
      padding: 10px;
      overflow: hidden;
      transition: all 1s;
      h3 {
        padding: 10px;
      }
      li {
        padding: 10px;
        border-radius: 8px;
        border: 1px solid #555;
        background: #fff;
        margin: 5px 10px;
        font-size: 12px;
        display: flex;
        align-items: center;
        cursor: pointer;
        transition: all 0.5s ease;
        &:hover {
          box-shadow: 0 0 5px rgba($color: #000000, $alpha: 0.3);
        }
        i {
          font-size: 18px;
          margin-right: 10px;
        }
        strong {
          flex: 1;
        }
      }
    }
    // 右侧区域
    .antv-wrapper {
      flex: 1;
      position: relative;
      .wrapper-canvas {
        position: relative;
        height: 100vh;
        min-height: 720px;
      }
      .wrapper-tips {
        padding: 10px;
        display: flex;
        align-items: center;
        position: absolute;
        top: 0;
        left: 0;
        .wrapper-tips-item {
          span {
            padding-left: 10px;
            font-size: 12px;
          }
        }
      }
    }
    // 侧栏隐藏
    .antv-hideMenu {
      width: 0;
      border: none;
    }
  }
}

i.icon-oval {
  display: inline-block;
  width: 16px;
  height: 10px;
  border-radius: 10px;
  border: 2px solid #555;
}
i.icon-square {
  display: inline-block;
  width: 16px;
  height: 10px;
  border: 2px solid #555;
}
i.icon-ysquare {
  display: inline-block;
  width: 16px;
  height: 10px;
  border-radius: 4px;
  border: 2px solid #555;
}
i.icon-rhombus {
  display: inline-block;
  width: 10px;
  height: 10px;
  border: 2px solid #555;
  transform: rotate(45deg);
}
i.icon-rhomboid {
  display: inline-block;
  width: 10px;
  height: 10px;
  border: 2px solid #555;
  transform: skew(-30deg);
}
i.icon-circle {
  display: inline-block;
  width: 16px;
  height: 16px;
  border-radius: 16px;
  border: 2px solid #555;
}
.edit-main {
  position: absolute;
  right: 0;
  top: 0;
  height: 100%;
  width: 280px;
  border-left: 1px solid #f2f2f2;
  box-shadow: 0 -10px 10px rgba($color: #000000, $alpha: 0.3);
  padding: 20px;
  background: #fff;
  box-sizing: border-box;
  .edit-main-title {
    display: flex;
    justify-content: space-between;
    align-items: center;
    h3 {
      flex: 1;
    }
    i {
      cursor: pointer;
      font-size: 20px;
      opacity: 0.7;
      &:hover {
        opacity: 1;
      }
    }
  }

  .form-main {
    padding: 20px 0;
    .label-style {
      background: #f2f2f2;
      padding: 0 10px;
      p {
        display: flex;
        align-items: center;
        font-size: 12px;
      }
    }
  }
  .edit-btn {
  }
  .see-box {
    padding: 20px;
    background: #f2f2f2;
    h5 {
      padding-bottom: 10px;
    }
    .see-item {
      padding: 10px 30px;
      border: 2px solid #333;
      text-align: center;
    }
  }
}
.wrapper-btn {
  text-align: center;
  padding: 20px;
  button {
    width: 100%;
  }
}
</style>
