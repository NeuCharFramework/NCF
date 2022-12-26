// 基本设置
export const configSetting = (Shape) => {
  return {
    // 网格配置
    grid: {
      size: 10,// 网格大小 10px
      visible: true,// 绘制网格，默认绘制 dot 类型网格
      type: 'dot', // 网格类型 'dot' | 'fixedDot' | 'mesh'
      args: {
        color: '#a0a0a0', // 网格线/点颜色
        thickness: 1,     // 网格线宽度/网格点大小
      }
      // type: 'doubleMesh', // 网格类型 'doubleMesh' 时 以下设置
      // args: [
      //   { 
      //     color: '#eee', // 主网格线颜色
      //     thickness: 1,     // 主网格线宽度
      //   },
      //   { 
      //     color: '#ddd', // 次网格线颜色
      //     thickness: 1,     // 次网格线宽度
      //     factor: 4,        // 主次网格线间隔
      //   },
      // ]
    },
    // 是否自动扩充/缩小画布，默认为 true。开启后，移动节点/边时将自动计算需要的画布大小，当超出当前画布大小时，按照 pageWidth 和 pageHeight 自动扩充画布。反之，则自动缩小画布。
    autoResize: true,
    translating: { restrict: true },
    mousewheel: {
      enabled: true,
      zoomAtMousePosition: true,
      modifiers: 'ctrl',
      minScale: 0.5,
      maxScale: 3,
    },
    connecting: {
      router: {
        name: 'manhattan',
        args: {
          padding: 1,
        },
      },
      connector: {
        name: 'rounded',
        args: {
          radius: 8,
        },
      },
      anchor: 'center',
      connectionPoint: 'anchor',
      allowBlank: false,
      snap: {
        radius: 20,
      },
      createEdge() {
        return new Shape.Edge({
          attrs: {
            line: {
              stroke: '#A2B1C3',
              strokeWidth: 2,
              targetMarker: {
                name: 'block',
                width: 12,
                height: 8
              },
            },
          },
          zIndex: 0,
        })
      },
      validateConnection({ targetMagnet }) {
        return !!targetMagnet
      },
    },
    onToolItemCreated({ tool }) {
      const handle = tool
      const options = handle.options
      if (options && options.index % 2 === 1) {
        tool.setAttrs({ fill: 'red' })
      }
    },
    highlighting: {
      // 当链接桩可以被链接时，在链接桩外围渲染一个的#5F95FF色矩形框
      magnetAdsorbed: {
        name: 'stroke',
        args: {
          attrs: {
            fill: '#5F95FF',
            stroke: '#5F95FF',
          },
        },
      },
    },
    resizing: true,
    rotating: true,
    selecting: {
      enabled: true,
      rubberband: true,
      showNodeSelectionBox: true,
    },
    snapline: true,
    keyboard: true,
    clipboard: true
  }
}

// 节点预设类型 （0椭圆形: defaultOval, 1方形: defaultSquare, 2圆角矩形: defaultYSquare, 3菱形: defaultRhombus, 4平行四边形: defaultRhomboid, 5圆形: defaultCircle, 6图片: otherImage)
export const configNodeShape = (type) => {
  const nodeShapeList = [{
    label: '椭圆形',
    /**
     * 
     *  加入data里面的标识type是为了方便编辑的时候找到相对应的类型进行不同的编辑处理
     *  另外获取初始对应的设置
    */
    data: {
      type: 'defaultOval'
    },
    shape: 'rect',
    width: 100,
    height: 50,
    attrs: {
      body: {
        rx: 20,
        ry: 26,
        fill: '#fff',
        stroke: '#333'
      },
      label: {
        text: '椭圆形',
        fontSize: 16,
        fill: '#333'
      }
    }
  },
  {
    label: '方形',
    data: {
      type: 'defaultSquare',
    },
    shape: 'rect',
    width: 100,
    height: 50,
    attrs: {
      label: {
        text: '方形',
        fontSize: 16,
        fill: '#333'
      },
      body: {
        fill: '#fff',
        stroke: '#333'
      }
    },
  },
  {
    label: '圆角矩形',
    data: {
      type: 'defaultYSquare'
    },
    shape: 'rect',
    width: 100,
    height: 50,
    attrs: {
      body: {
        rx: 6,
        ry: 6,
        fill: '#fff',
        stroke: '#333'
      },
      label: {
        text: '圆角矩形',
        fontSize: 16,
        fill: '#333'
      }
    },
  },
  {
    label: '菱形',
    data: {
      type: 'defaultRhombus'
    },
    shape: 'polygon',
    width: 120,
    height: 50,
    attrs: {
      body: {
        refPoints: '0,10 10,0 20,10 10,20',
        fill: '#fff',
        stroke: '#333'
      },
      label: {
        text: '菱形',
        fontSize: 16,
        fill: '#333'
      }
    },
  },
  {
    label: '平行四边形',
    data: {
      type: 'defaultRhomboid'
    },
    shape: 'polygon',
    width: 120,
    height: 50,
    attrs: {
      body: {
        refPoints: '10,0 40,0 30,20 0,20',
        fill: '#fff',
        stroke: '#333'
      },
      label: {
        text: '平行四边形',
        fontSize: 16,
        fill: '#333'
      }
    }
  },
  {
    label: '圆形',
    data: {
      type: 'defaultCircle'
    },
    shape: 'circle',
    width: 80,
    height: 80,
    attrs: {
      label: {
        text: '圆形',
        fontSize: 16,
        fill: '#333'
      },
      body: {
        fill: '#fff',
        stroke: '#333'
      }
    }
  },
  {
    label: "图片",
    data: {
      type: 'otherImage'
    },
    shape: 'rect',
    width: 80,
    height: 80,
    markup: [
      {
        tagName: 'rect',
        selector: 'body',
      },
      {
        tagName: 'image',
      },
      {
        tagName: 'text',
        selector: 'label',
      },
    ],
    attrs: {
      body: {
        stroke: '#5F95FF',
        fill: '#5F95FF',
      },
      image: {
        width: 80,
        height: 80,
        refX: 0,
        refY: 0,
        xlinkHref: 'https://gw.alipayobjects.com/zos/bmw-prod/2010ac9f-40e7-49d4-8c4a-4fcf2f83033b.svg',
      },
      label: {
        fontSize: 14,
        fill: '#fff',
        text: '图片'
      },
    },
  }
  ]
  if (type) {
    const obj = nodeShapeList.find(item => { return item.data.type === type })
    return obj || nodeShapeList
  }
  return nodeShapeList
}

// 节点连接装设置
export const configNodePorts = () => {
  return {
    groups: {
      top: {
        position: 'top',
        attrs: {
          circle: {
            r: 4,
            magnet: true,
            stroke: '#5F95FF',
            strokeWidth: 1,
            fill: '#fff',
            style: {
              visibility: 'hidden',
            },
          },
        },
      },
      right: {
        position: 'right',
        attrs: {
          circle: {
            r: 4,
            magnet: true,
            stroke: '#5F95FF',
            strokeWidth: 1,
            fill: '#fff',
            style: {
              visibility: 'hidden',
            },
          },
        },
      },
      bottom: {
        position: 'bottom',
        attrs: {
          circle: {
            r: 4,
            magnet: true,
            stroke: '#5F95FF',
            strokeWidth: 1,
            fill: '#fff',
            style: {
              visibility: 'hidden',
            },
          },
        },
      },
      left: {
        position: 'left',
        attrs: {
          circle: {
            r: 4,
            magnet: true,
            stroke: '#5F95FF',
            strokeWidth: 1,
            fill: '#fff',
            style: {
              visibility: 'hidden',
            },
          },
        },
      },
    },
    items: [
      {
        group: 'top',
      },
      {
        group: 'right',
      },
      {
        group: 'bottom',
      },
      {
        group: 'left',
      },
    ]
  }
}

// 连线 label 设置
export const configEdgeLabel = (labelText, fontColor, fill, stroke) => {
  if (!labelText) return { attrs: { labelText: { text: '' }, labelBody: { fill: '', stroke: '' } } }
  return {
    markup: [
      {
        tagName: 'rect',
        selector: 'labelBody',
      },
      {
        tagName: 'text',
        selector: 'labelText',
      },
    ],
    attrs: {
      labelText: {
        text: labelText || '',
        fill: fontColor || '#333',
        textAnchor: 'middle',
        textVerticalAnchor: 'middle',
      },
      labelBody: {
        ref: 'labelText',
        refX: -8,
        refY: -5,
        refWidth: '100%',
        refHeight: '100%',
        refWidth2: 16,
        refHeight2: 10,
        stroke: stroke || '#555',
        fill: fill || '#fff',
        strokeWidth: 2,
        rx: 5,
        ry: 5,
      },
      form: '12121'
    },

  }
}

// 键盘事件
export const graphBindKey = (graph) => {
  graph.bindKey(['meta+c', 'ctrl+c'], () => {
    const cells = graph.getSelectedCells()
    if (cells.length) {
      graph.copy(cells)
    }
    return false
  })
  graph.bindKey(['meta+x', 'ctrl+x'], () => {
    const cells = graph.getSelectedCells()
    if (cells.length) {
      graph.cut(cells)
    }
    return false
  })
  graph.bindKey(['meta+v', 'ctrl+v'], () => {
    if (!graph.isClipboardEmpty()) {
      const cells = graph.paste({ offset: 32 })
      graph.cleanSelection()
      graph.select(cells)
    }
    return false
  })
  //undo redo
  graph.bindKey(['meta+z', 'ctrl+z'], () => {
    if (graph.history.canUndo()) {
      graph.history.undo()
    }
    return false
  })
  graph.bindKey(['meta+shift+z', 'ctrl+shift+z'], () => {
    if (graph.history.canRedo()) {
      graph.history.redo()
    }
    return false
  })
  // select all
  graph.bindKey(['meta+a', 'ctrl+a'], () => {
    const nodes = graph.getNodes()
    if (nodes) {
      graph.select(nodes)
    }
  })
  //delete
  /*
  graph.bindKey('delete', () => {
    const cells = graph.getSelectedCells()
    if (cells.length) {
      graph.removeCells(cells)
    }
  })
  */
  // zoom
  graph.bindKey(['ctrl+1', 'meta+1'], () => {
    const zoom = graph.zoom()
    if (zoom < 1.5) {
      graph.zoom(0.1)
    }
  })
  graph.bindKey(['ctrl+2', 'meta+2'], () => {
    const zoom = graph.zoom()
    if (zoom > 0.5) {
      graph.zoom(-0.1)
    }
  })
  return graph
}